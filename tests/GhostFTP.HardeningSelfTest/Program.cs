using System.Net;
using System.Net.Sockets;
using System.Text;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;

namespace GhostFTP.HardeningSelfTest;

public static class Program
{
    public static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("FTP session concurrent disposal is idempotent", TestConcurrentSessionDisposalAsync),
            ("Transfer queue concurrent disposal is idempotent", TestConcurrentQueueDisposalAsync),
            ("Malformed FTP reply framing is rejected", TestMalformedReplyRejectedAsync),
            ("FTP preliminary greeting and strict PASV tuple interoperate", TestProtocolCompatibilityAsync)
        };

        var failures = new List<string>();
        foreach (var test in tests)
        {
            try
            {
                await test.Run().ConfigureAwait(false);
                Console.WriteLine("PASS  " + test.Name);
            }
            catch (Exception ex)
            {
                failures.Add(test.Name + ": " + ex);
                Console.WriteLine("FAIL  " + test.Name + " — " + ex.Message);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} hardening self-tests passed.");
        if (failures.Count == 0)
            return 0;

        foreach (var failure in failures)
            Console.Error.WriteLine(failure);
        return 1;
    }

    private static async Task TestConcurrentSessionDisposalAsync()
    {
        var session = new FtpSession(new FtpConnectionOptions
        {
            Host = "127.0.0.1",
            Port = 21,
            Username = "test",
            Password = "test",
            Security = FtpSecurityMode.Plain
        });

        var disposals = Enumerable.Range(0, 32)
            .Select(_ => session.DisposeAsync().AsTask())
            .ToArray();
        await Task.WhenAll(disposals).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        var rejected = false;
        try
        {
            _ = await session.ListAsync("/").ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            rejected = true;
        }

        Assert(rejected, "A disposed FTP session accepted a new operation.");
    }

    private static async Task TestConcurrentQueueDisposalAsync()
    {
        static Task<(IFtpSession Session, bool DisposeAfter)> NoSession(CancellationToken _) =>
            Task.FromException<(IFtpSession Session, bool DisposeAfter)>(new InvalidOperationException("No transfer session should be created in this test."));

        var queue = new TransferQueueService(NoSession, concurrentTransferLimit: 4);
        var disposals = Enumerable.Range(0, 32)
            .Select(_ => queue.DisposeAsync().AsTask())
            .ToArray();
        await Task.WhenAll(disposals).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        var rejected = queue.EnqueueDownload("/never-start.bin", Path.Combine(Path.GetTempPath(), "never-start.bin"), false, 1);
        Assert(rejected.State == TransferState.Failed, "A disposed transfer queue accepted a new transfer for dispatch.");
        Assert(rejected.Error?.Contains("shutting down", StringComparison.OrdinalIgnoreCase) == true,
            "A disposed transfer queue did not report its shutdown state.");
    }

    private static async Task TestMalformedReplyRejectedAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
            await using var stream = client.GetStream();
            var malformed = Encoding.ASCII.GetBytes("220X malformed separator\r\n");
            await stream.WriteAsync(malformed, timeout.Token).ConfigureAwait(false);
            await stream.FlushAsync(timeout.Token).ConfigureAwait(false);
        }, timeout.Token);

        await using var session = new FtpSession(new FtpConnectionOptions
        {
            Host = "127.0.0.1",
            Port = port,
            Username = "ghost",
            Password = "test-password",
            Security = FtpSecurityMode.Plain,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            CommandTimeout = TimeSpan.FromSeconds(5),
            TransferTimeout = TimeSpan.FromSeconds(5)
        });

        var rejected = false;
        try
        {
            await session.ConnectAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (FtpException)
        {
            rejected = true;
        }
        finally
        {
            listener.Stop();
        }

        await serverTask.WaitAsync(TimeSpan.FromSeconds(5), timeout.Token).ConfigureAwait(false);
        Assert(rejected, "A malformed FTP reply separator was accepted as a valid 220 greeting.");
    }

    private static async Task TestProtocolCompatibilityAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var controlPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = RunFakeFtpServerAsync(listener, timeout.Token);

        await using var session = new FtpSession(new FtpConnectionOptions
        {
            Host = "127.0.0.1",
            Port = controlPort,
            Username = "ghost",
            Password = "test-password",
            Security = FtpSecurityMode.Plain,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            CommandTimeout = TimeSpan.FromSeconds(5),
            TransferTimeout = TimeSpan.FromSeconds(5)
        });

        try
        {
            await session.ConnectAsync(timeout.Token).ConfigureAwait(false);
            Assert(session.IsConnected, "Client did not accept a bounded 120 -> 220 FTP greeting sequence.");

            var entries = await session.ListAsync("/", timeout.Token).ConfigureAwait(false);
            Assert(entries.Count == 1, "Fake server listing returned an unexpected number of entries.");
            Assert(entries[0].Name == "hello.txt" && !entries[0].IsDirectory,
                "PASV data channel did not return the expected listing entry.");

            await session.DisconnectAsync(timeout.Token).ConfigureAwait(false);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5), timeout.Token).ConfigureAwait(false);
        }
        finally
        {
            listener.Stop();
            if (!serverTask.IsCompleted)
            {
                timeout.Cancel();
                try { await serverTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
            }
        }
    }

    private static async Task RunFakeFtpServerAsync(TcpListener controlListener, CancellationToken cancellationToken)
    {
        using var controlClient = await controlListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        await using var controlStream = controlClient.GetStream();
        using var reader = new StreamReader(controlStream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
        using var writer = new StreamWriter(controlStream, new UTF8Encoding(false), 4096, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };

        await writer.WriteLineAsync("120 Service ready shortly".AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.WriteLineAsync("220 GhostFTP hardening test ready".AsMemory(), cancellationToken).ConfigureAwait(false);

        TcpListener? dataListener = null;
        try
        {
            while (true)
            {
                var command = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (command is null)
                    return;

                if (command.StartsWith("USER ", StringComparison.Ordinal))
                {
                    await ReplyAsync(writer, "331 Password required", cancellationToken).ConfigureAwait(false);
                }
                else if (command.StartsWith("PASS ", StringComparison.Ordinal))
                {
                    await ReplyAsync(writer, "230 Logged in", cancellationToken).ConfigureAwait(false);
                }
                else if (command.StartsWith("OPTS UTF8", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync(writer, "200 UTF8 enabled", cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(command, "FEAT", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync(writer, "500 FEAT unavailable", cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(command, "PWD", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync(writer, "257 \"/\" is current directory", cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(command, "TYPE I", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync(writer, "200 Binary mode", cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(command, "EPSV", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyAsync(writer, "500 EPSV unavailable", cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(command, "PASV", StringComparison.OrdinalIgnoreCase))
                {
                    dataListener?.Stop();
                    dataListener = new TcpListener(IPAddress.Loopback, 0);
                    dataListener.Start();
                    var dataPort = ((IPEndPoint)dataListener.LocalEndpoint).Port;
                    var p1 = dataPort / 256;
                    var p2 = dataPort % 256;

                    // Trailing numeric diagnostics are intentional. A permissive "all digits" parser
                    // would incorrectly use 99/100 as the data port instead of the six-value tuple.
                    await ReplyAsync(
                        writer,
                        $"227 Entering Passive Mode (127,0,0,1,{p1},{p2}) diagnostics 99 100",
                        cancellationToken).ConfigureAwait(false);
                }
                else if (command.StartsWith("LIST ", StringComparison.OrdinalIgnoreCase))
                {
                    if (dataListener is null)
                        throw new InvalidOperationException("LIST arrived before a PASV listener was prepared.");

                    await ReplyAsync(writer, "150 Opening data connection", cancellationToken).ConfigureAwait(false);
                    using (var dataClient = await dataListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false))
                    await using (var dataStream = dataClient.GetStream())
                    {
                        var listing = Encoding.UTF8.GetBytes("-rw-r--r-- 1 owner group 5 Sep  6 2026 hello.txt\r\n");
                        await dataStream.WriteAsync(listing, cancellationToken).ConfigureAwait(false);
                        await dataStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    dataListener.Stop();
                    dataListener = null;
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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
