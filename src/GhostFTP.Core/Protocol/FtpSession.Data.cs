using System.Buffers;
using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace GhostFTP.Core.Protocol;

public sealed partial class FtpSession
{
    private const int MaxListingPayloadBytes = 16 * 1024 * 1024;
    private const int DataBufferSize = 128 * 1024;

    private async Task EnsureBinaryTransferModeAsync(CancellationToken cancellationToken)
    {
        var reply = await SendCommandAsync("TYPE I", cancellationToken).ConfigureAwait(false);
        Ensure(reply, 200, 299, "FTP server refused binary transfer mode.");
    }

    private async Task<string> ReceiveTextDataAsync(string command, CancellationToken cancellationToken)
    {
        await using var memory = new MemoryStream(capacity: 64 * 1024);
        await ReceiveDataToStreamAsync(
            command,
            memory,
            0,
            null,
            null,
            cancellationToken,
            maxBytes: MaxListingPayloadBytes).ConfigureAwait(false);

        if (!memory.TryGetBuffer(out var buffer) || buffer.Array is null)
            return ControlEncoding.GetString(memory.ToArray());
        return ControlEncoding.GetString(buffer.Array, buffer.Offset, checked((int)memory.Length));
    }

    private async Task ReceiveDataToStreamAsync(
        string command,
        Stream destination,
        long initialBytes,
        long? total,
        IProgress<(long transferred, long? total)>? progress,
        CancellationToken cancellationToken,
        long? maxBytes = null)
    {
        EnsureConnected();
        await EnsureBinaryTransferModeAsync(cancellationToken).ConfigureAwait(false);
        using var data = await OpenPassiveTcpAsync(cancellationToken).ConfigureAwait(false);
        var preliminary = await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        if (!preliminary.IsPositivePreliminary && !preliminary.IsPositiveCompletion)
            throw CreateReplyException(preliminary, "FTP server refused the data transfer.");

        if (preliminary.IsPositiveCompletion)
            return;

        await using (var dataStream = await CreateDataStreamAsync(data, cancellationToken).ConfigureAwait(false))
        {
            var buffer = ArrayPool<byte>.Shared.Rent(DataBufferSize);
            try
            {
                long transferred = initialBytes;
                while (true)
                {
                    var read = await dataStream.ReadAsync(buffer.AsMemory(0, DataBufferSize), cancellationToken)
                        .AsTask()
                        .WaitAsync(_options.TransferTimeout, cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                        break;

                    if (maxBytes is > 0 && transferred > maxBytes.Value - read)
                        throw new FtpException($"FTP directory listing exceeded the safety limit of {maxBytes.Value / (1024 * 1024)} MiB.");

                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    transferred = checked(transferred + read);
                    progress?.Report((transferred, total));
                }
            }
            finally
            {
                // Transfer buffers may contain private file contents. Reuse them to reduce GC pressure,
                // but clear the rented array before it becomes available to another pool consumer.
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        var final = await ReadReplyAsync(cancellationToken).ConfigureAwait(false);
        Ensure(final, 200, 299, "FTP transfer did not complete successfully.");
    }

    private async Task SendStreamAsDataAsync(
        string command,
        Stream source,
        long total,
        IProgress<(long transferred, long? total)>? progress,
        CancellationToken cancellationToken)
    {
        EnsureConnected();
        await EnsureBinaryTransferModeAsync(cancellationToken).ConfigureAwait(false);
        using var data = await OpenPassiveTcpAsync(cancellationToken).ConfigureAwait(false);
        var preliminary = await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        if (!preliminary.IsPositivePreliminary && !preliminary.IsPositiveCompletion)
            throw CreateReplyException(preliminary, "FTP server refused the upload.");
        if (preliminary.IsPositiveCompletion)
            return;

        await using (var dataStream = await CreateDataStreamAsync(data, cancellationToken).ConfigureAwait(false))
        {
            var buffer = ArrayPool<byte>.Shared.Rent(DataBufferSize);
            try
            {
                long transferred = 0;
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, DataBufferSize), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;
                    await dataStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                        .AsTask()
                        .WaitAsync(_options.TransferTimeout, cancellationToken)
                        .ConfigureAwait(false);
                    transferred = checked(transferred + read);
                    progress?.Report((transferred, total));
                }
                await dataStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        var final = await ReadReplyAsync(cancellationToken).ConfigureAwait(false);
        Ensure(final, 200, 299, "FTP upload did not complete successfully.");
    }

    private async Task<TcpClient> OpenPassiveTcpAsync(CancellationToken cancellationToken)
    {
        var epsv = await TryCommandAsync("EPSV", cancellationToken).ConfigureAwait(false);
        int port;
        if (epsv is not null && epsv.IsPositiveCompletion)
        {
            if (!TryParseEpsvPort(epsv.Message, out port))
                throw new FtpException("Server returned an invalid EPSV response.", epsv.Code);
        }
        else
        {
            var pasv = await SendCommandAsync("PASV", cancellationToken).ConfigureAwait(false);
            Ensure(pasv, 200, 299, "Server does not support passive FTP data connections.");
            if (!TryParsePasvPort(pasv.Message, out port))
                throw new FtpException("Server returned an invalid PASV response.", pasv.Code);
        }

        InputGuard.Port(port);
        var client = new TcpClient(AddressFamily.InterNetworkV6) { NoDelay = true };
        client.Client.DualMode = true;
        try
        {
            // Data channels intentionally use the authenticated control host, never host data supplied by PASV.
            // This prevents an FTP server response from redirecting the client to an arbitrary third-party host.
            await client.ConnectAsync(_options.Host, port, cancellationToken)
                .AsTask().WaitAsync(_options.ConnectTimeout, cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static bool TryParseEpsvPort(string message, out int port)
    {
        port = 0;
        if (string.IsNullOrEmpty(message))
            return false;

        var open = message.IndexOf('(');
        if (open < 0)
            return false;
        var close = message.IndexOf(')', open + 1);
        if (close < 0)
            return false;

        var body = message.AsSpan(open + 1, close - open - 1);
        if (body.Length < 5)
            return false;

        var delimiter = body[0];
        if (char.IsDigit(delimiter) || char.IsWhiteSpace(delimiter) || delimiter is '\r' or '\n' or '(' or ')')
            return false;
        if (body[1] != delimiter || body[2] != delimiter || body[^1] != delimiter)
            return false;

        var portSpan = body[3..^1];
        return portSpan.Length is >= 1 and <= 5
            && int.TryParse(portSpan, NumberStyles.None, CultureInfo.InvariantCulture, out port)
            && port is >= 1 and <= 65535;
    }

    private static bool TryParsePasvPort(string message, out int port)
    {
        port = 0;
        if (string.IsNullOrEmpty(message))
            return false;

        var open = message.IndexOf('(');
        if (open < 0)
            return false;
        var close = message.IndexOf(')', open + 1);
        if (close < 0)
            return false;

        var parts = message[(open + 1)..close].Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 6)
            return false;

        Span<int> values = stackalloc int[6];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out values[index])
                || values[index] is < 0 or > 255)
            {
                return false;
            }
        }

        port = values[4] * 256 + values[5];
        return port is >= 1 and <= 65535;
    }

    private async Task<Stream> CreateDataStreamAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        if (!_dataProtection)
            return stream;

        var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
        try
        {
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = _options.Host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.Offline
            }, cancellationToken).WaitAsync(_options.ConnectTimeout, cancellationToken).ConfigureAwait(false);
            return ssl;
        }
        catch
        {
            await ssl.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task UpgradeControlToTlsAsync(CancellationToken cancellationToken)
    {
        if (_controlStream is null)
            throw new InvalidOperationException("Control stream is unavailable.");

        _reader?.Dispose();
        _writer?.Dispose();
        var ssl = new SslStream(_controlStream, leaveInnerStreamOpen: false);
        try
        {
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = _options.Host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.Offline
            }, cancellationToken).WaitAsync(_options.ConnectTimeout, cancellationToken).ConfigureAwait(false);
            _controlStream = ssl;
            IsEncrypted = true;
        }
        catch
        {
            await ssl.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private void BuildControlTextStreams()
    {
        if (_controlStream is null)
            throw new InvalidOperationException("Control stream is unavailable.");
        _reader = new StreamReader(_controlStream, ControlEncoding, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true);
        _writer = new StreamWriter(_controlStream, ControlEncoding, bufferSize: 8192, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };
    }
}
