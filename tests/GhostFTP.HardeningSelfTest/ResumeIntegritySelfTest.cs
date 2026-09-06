using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;

namespace GhostFTP.HardeningSelfTest;

internal static class ResumeIntegritySelfTest
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        await TestValidResumeAsync().ConfigureAwait(false);
        await TestChangedIdentityRestartsFromZeroAsync().ConfigureAwait(false);
        await TestRemoteMutationDiscardsCompletedFileAsync().ConfigureAwait(false);
        Console.WriteLine("PASS  Download resume identity and in-flight mutation guards");
    }

    private static async Task TestValidResumeAsync()
    {
        var content = Encoding.UTF8.GetBytes("Ghost FTP deterministic resume payload");
        var modified = new DateTimeOffset(2026, 9, 6, 18, 0, 0, TimeSpan.Zero);
        using var server = new ResumeServer(content, modified, mutateAfterTransfer: false);
        await server.StartAsync().ConfigureAwait(false);

        var root = CreateTempRoot();
        try
        {
            var destination = Path.Combine(root, "resume.bin");
            var partPath = destination + ".ghostftp.part";
            var metadataPath = partPath + ".meta";
            const int offset = 9;
            await File.WriteAllBytesAsync(partPath, content[..offset]).ConfigureAwait(false);
            await WriteMetadataAsync(metadataPath, server.Port, content.LongLength, modified, "/resume.bin").ConfigureAwait(false);

            await using var session = CreateSession(server.Port);
            await session.ConnectAsync().ConfigureAwait(false);
            await session.DownloadFileAsync("/resume.bin", destination).ConfigureAwait(false);
            await session.DisconnectAsync().ConfigureAwait(false);

            Assert(File.Exists(destination), "Valid resume did not produce a final file.");
            Assert((await File.ReadAllBytesAsync(destination).ConfigureAwait(false)).SequenceEqual(content),
                "Valid resume produced incorrect bytes.");
            Assert(server.RestOffsets.Count == 1 && server.RestOffsets[0] == offset,
                "Valid resume did not use the validated REST offset exactly once.");
            Assert(!File.Exists(partPath) && !File.Exists(metadataPath),
                "Successful resume left partial/metadata files behind.");
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    private static async Task TestChangedIdentityRestartsFromZeroAsync()
    {
        var content = Encoding.UTF8.GetBytes("Ghost FTP remote revision B");
        var currentModified = new DateTimeOffset(2026, 9, 6, 19, 0, 0, TimeSpan.Zero);
        var staleModified = currentModified.AddMinutes(-10);
        using var server = new ResumeServer(content, currentModified, mutateAfterTransfer: false);
        await server.StartAsync().ConfigureAwait(false);

        var root = CreateTempRoot();
        try
        {
            var destination = Path.Combine(root, "changed.bin");
            var partPath = destination + ".ghostftp.part";
            var metadataPath = partPath + ".meta";
            await File.WriteAllBytesAsync(partPath, Encoding.UTF8.GetBytes("stale bytes")).ConfigureAwait(false);
            await WriteMetadataAsync(metadataPath, server.Port, content.LongLength, staleModified, "/resume.bin").ConfigureAwait(false);

            await using var session = CreateSession(server.Port);
            await session.ConnectAsync().ConfigureAwait(false);
            await session.DownloadFileAsync("/resume.bin", destination).ConfigureAwait(false);
            await session.DisconnectAsync().ConfigureAwait(false);

            Assert((await File.ReadAllBytesAsync(destination).ConfigureAwait(false)).SequenceEqual(content),
                "Changed remote identity was mixed with stale partial bytes.");
            Assert(server.RestOffsets.Count == 0,
                "Changed remote identity incorrectly reused a stale REST offset.");
        }
        finally
        {
            TryDeleteTree(root);
        }
    }

    private static async Task TestRemoteMutationDiscardsCompletedFileAsync()
    {
        var content = Encoding.UTF8.GetBytes("Ghost FTP in-flight mutation payload");
        var modified = new DateTimeOffset(2026, 9, 6, 20, 0, 0, TimeSpan.Zero);
        using var server = new ResumeServer(content, modified, mutateAfterTransfer: true);
        await server.StartAsync().ConfigureAwait(false);

        var root = CreateTempRoot();
        try
        {
            var destination = Path.Combine(root, "mutating.bin");
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

            Assert(rejected, "An in-flight remote revision change was not rejected.");
            Assert(!File.Exists(destination), "A download from a changing remote object was left as a completed local file.");
        }
        finally
        {
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
        var path = Path.Combine(Path.GetTempPath(), "ghostftp-resume-hardening-" + Guid.NewGuid().ToString("N"));
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

    private sealed class ResumeServer : IDisposable
    {
        private readonly byte[] _content;
        private readonly DateTimeOffset _initialModified;
        private readonly bool _mutateAfterTransfer;
        private readonly TcpListener _control = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _shutdown = new(TimeSpan.FromSeconds(30));
        private Task? _serverTask;
        private bool _transferCompleted;

        public ResumeServer(byte[] content, DateTimeOffset initialModified, bool mutateAfterTransfer)
        {
            _content = content;
            _initialModified = initialModified;
            _mutateAfterTransfer = mutateAfterTransfer;
        }

        public int Port => ((IPEndPoint)_control.LocalEndpoint).Port;
        public List<long> RestOffsets { get; } = [];

        public Task StartAsync()
        {
            _control.Start();
            _serverTask = RunAsync(_shutdown.Token);
            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            using var client = await _control.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true)
            {
                NewLine = "\r\n",
                AutoFlush = true
            };

            await ReplyAsync(writer, "220 Resume test ready", cancellationToken).ConfigureAwait(false);
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
                        await ReplyAsync(writer, "213 " + modified.UtcDateTime.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else if (string.Equals(command, "EPSV", StringComparison.OrdinalIgnoreCase))
                        await ReplyAsync(writer, "500 EPSV unavailable", cancellationToken).ConfigureAwait(false);
                    else if (string.Equals(command, "PASV", StringComparison.OrdinalIgnoreCase))
                    {
                        dataListener?.Stop();
                        dataListener = new TcpListener(IPAddress.Loopback, 0);
                        dataListener.Start();
                        var port = ((IPEndPoint)dataListener.LocalEndpoint).Port;
                        await ReplyAsync(writer, $"227 Entering Passive Mode (127,0,0,1,{port / 256},{port % 256})", cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else if (command.StartsWith("REST ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = command[5..];
                        if (!long.TryParse(token, out restOffset) || restOffset < 0 || restOffset > _content.LongLength)
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
                        await ReplyAsync(writer, "502 Command not implemented", cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                dataListener?.Stop();
            }
        }

        private static Task ReplyAsync(StreamWriter writer, string text, CancellationToken cancellationToken) =>
            writer.WriteLineAsync(text.AsMemory(), cancellationToken);

        public void Dispose()
        {
            _shutdown.Cancel();
            _control.Stop();
            if (_serverTask is not null)
            {
                try { _serverTask.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (ObjectDisposedException) { }
            }
            _shutdown.Dispose();
        }
    }
}
