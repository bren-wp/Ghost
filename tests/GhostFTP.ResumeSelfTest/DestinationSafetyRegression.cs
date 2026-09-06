using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;

namespace GhostFTP.ResumeSelfTest;

internal static class DestinationSafetyRegression
{
    internal static async Task TestExistingDestinationSurvivesRemoteMutationAsync()
    {
        var content = Encoding.UTF8.GetBytes("Ghost FTP changing remote payload");
        var previous = Encoding.UTF8.GetBytes("existing local file that must survive");
        var modified = new DateTimeOffset(2026, 9, 6, 20, 30, 0, TimeSpan.Zero);
        await using var server = new SafetyServer(content, modified, mutateAfterTransfer: true);
        await server.StartAsync().ConfigureAwait(false);

        var root = CreateTempRoot();
        try
        {
            var destination = Path.Combine(root, "preserve.bin");
            await File.WriteAllBytesAsync(destination, previous).ConfigureAwait(false);

            await using var session = CreateSession(server.Port);
            await session.ConnectAsync().ConfigureAwait(false);

            var rejected = false;
            try
            {
                await session.DownloadFileAsync("/resume.bin", destination).ConfigureAwait(false);
            }
            catch (IOException ex) when (ex.Message.Contains("changed while", StringComparison.OrdinalIgnoreCase))
            {
                rejected = true;
            }

            Assert(rejected, "Remote mutation was not rejected before destination commit.");
            Assert(File.Exists(destination), "Existing destination was removed after remote mutation.");
            Assert((await File.ReadAllBytesAsync(destination).ConfigureAwait(false)).SequenceEqual(previous),
                "Existing destination bytes were replaced before remote post-validation completed.");
            Assert(!File.Exists(destination + ".ghostftp.part"),
                "Rejected staged bytes were left behind after remote mutation.");
            Assert(!File.Exists(destination + ".ghostftp.part.meta"),
                "Rejected resume metadata was left behind after remote mutation.");
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    internal static async Task TestUntrustedPartialCleanupFailureAbortsBeforeRetrAsync()
    {
        var content = Encoding.UTF8.GetBytes("Ghost FTP trusted remote revision");
        var modified = new DateTimeOffset(2026, 9, 6, 20, 45, 0, TimeSpan.Zero);
        await using var server = new SafetyServer(content, modified, mutateAfterTransfer: false);
        await server.StartAsync().ConfigureAwait(false);

        var root = CreateTempRoot();
        var destination = Path.Combine(root, "cleanup.bin");
        var partPath = destination + ".ghostftp.part";
        var metadataPath = partPath + ".meta";
        var permissionsChanged = false;
        try
        {
            await File.WriteAllBytesAsync(partPath, Encoding.UTF8.GetBytes("stale local bytes")).ConfigureAwait(false);
            await WriteMetadataAsync(
                metadataPath,
                server.Port,
                content.LongLength,
                modified.AddMinutes(-5),
                "/resume.bin").ConfigureAwait(false);

            if (OperatingSystem.IsWindows())
            {
                File.SetAttributes(partPath, File.GetAttributes(partPath) | FileAttributes.ReadOnly);
                permissionsChanged = true;
            }
            else
            {
                File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserExecute);
                permissionsChanged = true;
            }

            await using var session = CreateSession(server.Port);
            await session.ConnectAsync().ConfigureAwait(false);

            var rejected = false;
            try
            {
                await session.DownloadFileAsync("/resume.bin", destination).ConfigureAwait(false);
            }
            catch (IOException ex) when (ex.Message.Contains("untrusted", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("partial", StringComparison.OrdinalIgnoreCase))
            {
                rejected = true;
            }

            Assert(rejected, "Failure to remove an untrusted partial did not abort the download.");
            Assert(server.RestOffsets.Count == 0, "An untrusted partial reached REST despite failed cleanup.");
            Assert(server.RetrCount == 0, "An untrusted partial reached RETR despite failed cleanup.");
            Assert(!File.Exists(destination), "A destination was committed after stale-partial cleanup failed.");
        }
        finally
        {
            if (permissionsChanged)
            {
                try
                {
                    if (OperatingSystem.IsWindows() && File.Exists(partPath))
                        File.SetAttributes(partPath, FileAttributes.Normal);
                    else if (!OperatingSystem.IsWindows() && Directory.Exists(root))
                        File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
                catch
                {
                }
            }
            TryDeleteTree(root);
        }
    }

    private static FtpSession CreateSession(int port) => new(new FtpConnectionOptions
    {
        Host = "127.0.0.1",
        Port = port,
        Username = "ghost",
        Password = "resume-test",
        Security = FtpSecurityMode.Plain,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        CommandTimeout = TimeSpan.FromSeconds(5),
        TransferTimeout = TimeSpan.FromSeconds(5)
    });

    private static async Task WriteMetadataAsync(
        string path,
        int port,
        long size,
        DateTimeOffset modified,
        string remotePath)
    {
        var json = JsonSerializer.Serialize(new
        {
            version = 1,
            host = "127.0.0.1",
            port,
            security = (int)FtpSecurityMode.Plain,
            remotePath,
            remoteSize = size,
            modifiedUtcTicks = modified.UtcTicks
        });
        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "ghostftp-resume-destination-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteTree(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class SafetyServer : IAsyncDisposable
    {
        private readonly byte[] _content;
        private readonly DateTimeOffset _initialModified;
        private readonly bool _mutateAfterTransfer;
        private readonly TcpListener _control = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _shutdown = new(TimeSpan.FromSeconds(20));
        private Task? _serverTask;
        private TcpClient? _acceptedControlClient;
        private bool _transferCompleted;

        public SafetyServer(byte[] content, DateTimeOffset initialModified, bool mutateAfterTransfer)
        {
            _content = content;
            _initialModified = initialModified;
            _mutateAfterTransfer = mutateAfterTransfer;
        }

        public int Port => ((IPEndPoint)_control.LocalEndpoint).Port;
        public List<long> RestOffsets { get; } = [];
        public int RetrCount { get; private set; }

        public Task StartAsync()
        {
            _control.Start();
            _serverTask = RunAsync(_shutdown.Token);
            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _acceptedControlClient = await _control.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            using var client = _acceptedControlClient;
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true)
            {
                NewLine = "\r\n",
                AutoFlush = true
            };

            await ReplyAsync(writer, "220 Resume safety test ready", cancellationToken).ConfigureAwait(false);
            TcpListener? dataListener = null;
            long restOffset = 0;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var command = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (command is null)
                        return;

                    if (command.StartsWith("USER ", StringComparison.Ordinal))
                        await ReplyAsync(writer, "331 Password required", cancellationToken).ConfigureAwait(false);
                    else if (command.StartsWith("PASS ", StringComparison.Ordinal))
                        await ReplyAsync(writer, "230 Logged in", cancellationToken).ConfigureAwait(false);
                    else if (command.StartsWith("OPTS UTF8", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, "200 UTF8 enabled", cancellationToken).ConfigureAwait(false);
                    else if (string.Equals(command, "FEAT", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, "500 FEAT unavailable", cancellationToken).ConfigureAwait(false);
                    else if (string.Equals(command, "PWD", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, "257 \"/\" is current directory", cancellationToken).ConfigureAwait(false);
                    else if (string.Equals(command, "TYPE I", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, "200 Binary mode", cancellationToken).ConfigureAwait(false);
                    else if (command.StartsWith("SIZE ", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, $"213 {_content.LongLength}", cancellationToken).ConfigureAwait(false);
                    else if (command.StartsWith("MDTM ", StringComparison.OrdinalIgnoreCase))
                    {
                        var modified = _mutateAfterTransfer && _transferCompleted
                            ? _initialModified.AddSeconds(1)
                            : _initialModified;
                        await ReplyAsync(
                            writer,
                            "213 " + modified.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                            cancellationToken).ConfigureAwait(false);
                    }
                    else if (string.Equals(command, "EPSV", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, "500 EPSV unavailable", cancellationToken).ConfigureAwait(false);
                    else if (string.Equals(command, "PASV", StringComparison.OrdinalIgnoreCase))
                    {
                        dataListener?.Stop();
                        dataListener = new TcpListener(IPAddress.Loopback, 0);
                        dataListener.Start();
                        var port = ((IPEndPoint)dataListener.LocalEndpoint).Port;
                        await ReplyAsync(
                            writer,
                            $"227 Entering Passive Mode (127,0,0,1,{port / 256},{port % 256})",
                            cancellationToken).ConfigureAwait(false);
                    }
                    else if (command.StartsWith("REST ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = command[5..];
                        if (!long.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out restOffset)
                            || restOffset < 0
                            || restOffset > _content.LongLength)
                        {
                            await ReplyAsync(writer, "501 Invalid restart offset", cancellationToken).ConfigureAwait(false);
                            restOffset = 0;
                        }
                        else
                        {
                            RestOffsets.Add(restOffset);
                            await ReplyAsync(writer, "350 Restart position accepted", cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else if (command.StartsWith("RETR ", StringComparison.OrdinalIgnoreCase))
                    {
                        RetrCount++;
                        if (dataListener is null)
                            throw new InvalidOperationException("RETR arrived before PASV listener creation.");

                        await ReplyAsync(writer, "150 Opening data connection", cancellationToken).ConfigureAwait(false);
                        using (var dataClient = await dataListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false))
                        await using (var dataStream = dataClient.GetStream())
                        {
                            var start = checked((int)restOffset);
                            await dataStream.WriteAsync(_content.AsMemory(start), cancellationToken).ConfigureAwait(false);
                            await dataStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }

                        dataListener.Stop();
                        dataListener = null;
                        restOffset = 0;
                        _transferCompleted = true;
                        await ReplyAsync(writer, "226 Transfer complete", cancellationToken).ConfigureAwait(false);
                    }
                    else if (string.Equals(command, "QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await ReplyAsync(writer, "221 Goodbye", cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await ReplyAsync(writer, "502 Command not implemented", cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                dataListener?.Stop();
            }
        }

        private static Task ReplyAsync(StreamWriter writer, string text, CancellationToken cancellationToken) =>
            writer.WriteLineAsync(text.AsMemory(), cancellationToken);

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _acceptedControlClient?.Dispose();
            _control.Stop();

            if (_serverTask is not null)
            {
                try
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException
                    or ObjectDisposedException
                    or IOException
                    or SocketException
                    or TimeoutException)
                {
                }
            }

            _shutdown.Dispose();
        }
    }
}
