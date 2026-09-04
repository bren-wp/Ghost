using System.Text;
using GhostFTP.Core.Models;

namespace GhostFTP.Core.Protocol;

public sealed class DemoFtpSession : IFtpSession
{
    private const long MaxStoredBytes = 64L * 1024 * 1024;
    private const int MaxItems = 100_000;

    private sealed class Node
    {
        public required string Name { get; set; }
        public bool IsDirectory { get; init; }
        public byte[] Data { get; set; } = [];
        public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<string, Node> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly Node _root = DirectoryNode(string.Empty);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public bool IsConnected { get; private set; }
    public bool IsEncrypted => false;
    public string Host => "demo.ghostftp.local";
    public string WorkingDirectory { get; private set; } = "/";

    public DemoFtpSession()
    {
        var publicHtml = DirectoryNode("public_html");
        publicHtml.Children["index.html"] = TextFile("index.html", "<!doctype html>\n<html><head><title>GhostFTP Demo</title></head><body><h1>Hello from GhostFTP</h1></body></html>\n");
        publicHtml.Children["robots.txt"] = TextFile("robots.txt", "User-agent: *\nDisallow:\n");
        var assets = DirectoryNode("assets");
        assets.Children["app.css"] = TextFile("app.css", "body { font-family: system-ui; margin: 3rem; }\n");
        assets.Children["app.js"] = TextFile("app.js", "console.log('GhostFTP demo');\n");
        publicHtml.Children["assets"] = assets;
        _root.Children["public_html"] = publicHtml;

        var backups = DirectoryNode("backups");
        backups.Children["site-2026-09-01.zip"] = BinaryFile("site-2026-09-01.zip", 512 * 1024, 0x47);
        backups.Children["site-2026-08-31.zip"] = BinaryFile("site-2026-08-31.zip", 384 * 1024, 0x42);
        _root.Children["backups"] = backups;

        var logs = DirectoryNode("logs");
        logs.Children["access.log"] = TextFile("access.log", "127.0.0.1 - - [04/Sep/2026:20:00:00 +0000] \"GET / HTTP/1.1\" 200 1024\n");
        logs.Children["error.log"] = TextFile("error.log", "# Demo log file - GhostFTP sends no telemetry.\n");
        _root.Children["logs"] = logs;
        _root.Children["README.txt"] = TextFile("README.txt", "GhostFTP demo server\n\nAll demo data is generated locally. No network connection is opened.\n");
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Task.Delay(120, cancellationToken).ConfigureAwait(false);
        IsConnected = true;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FtpEntry>> ListAsync(string remotePath, CancellationToken cancellationToken = default) => LockedAsync(ct =>
    {
        ct.ThrowIfCancellationRequested();
        var path = InputGuard.RemotePath(remotePath);
        var node = Resolve(path);
        if (!node.IsDirectory) throw new FtpException("Demo path is not a directory.", 550);
        IReadOnlyList<FtpEntry> result = node.Children.Values
            .Select(child => ToEntry(path, child))
            .OrderByDescending(x => x.IsDirectory)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Task.FromResult(result);
    }, cancellationToken);

    public Task<string> GetWorkingDirectoryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        return Task.FromResult(WorkingDirectory);
    }

    public Task ChangeDirectoryAsync(string remotePath, CancellationToken cancellationToken = default) => LockedAsync(ct =>
    {
        ct.ThrowIfCancellationRequested();
        var path = InputGuard.RemotePath(remotePath);
        if (!Resolve(path).IsDirectory) throw new FtpException("Demo path is not a directory.", 550);
        WorkingDirectory = path;
        return Task.CompletedTask;
    }, cancellationToken);

    public Task CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default) => LockedAsync(ct =>
    {
        ct.ThrowIfCancellationRequested();
        var (parent, name) = ResolveParent(InputGuard.RemotePath(remotePath));
        if (parent.Children.ContainsKey(name)) throw new FtpException("An item with this name already exists.", 550);
        parent.Children[name] = DirectoryNode(name);
        return Task.CompletedTask;
    }, cancellationToken);

    public Task RenameAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default) => LockedAsync(ct =>
    {
        ct.ThrowIfCancellationRequested();
        var source = InputGuard.RemotePath(sourcePath);
        var destination = InputGuard.RemotePath(destinationPath);
        if (source == "/" || destination == "/") throw new FtpException("Demo root cannot be renamed.", 550);
        var (sourceParent, sourceName) = ResolveParent(source);
        if (!sourceParent.Children.Remove(sourceName, out var node)) throw new FtpException("Source item was not found.", 550);
        var (destinationParent, destinationName) = ResolveParent(destination);
        if (destinationParent.Children.ContainsKey(destinationName))
        {
            sourceParent.Children[sourceName] = node;
            throw new FtpException("Destination item already exists.", 550);
        }
        node.Name = destinationName;
        node.ModifiedUtc = DateTimeOffset.UtcNow;
        destinationParent.Children[destinationName] = node;
        return Task.CompletedTask;
    }, cancellationToken);

    public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default) => LockedAsync(ct =>
    {
        ct.ThrowIfCancellationRequested();
        var (parent, name) = ResolveParent(InputGuard.RemotePath(remotePath));
        if (!parent.Children.TryGetValue(name, out var node) || node.IsDirectory) throw new FtpException("Demo file was not found.", 550);
        parent.Children.Remove(name);
        return Task.CompletedTask;
    }, cancellationToken);

    public Task DeleteDirectoryAsync(string remotePath, bool recursive, CancellationToken cancellationToken = default) => LockedAsync(ct =>
    {
        ct.ThrowIfCancellationRequested();
        var path = InputGuard.RemotePath(remotePath);
        if (path == "/") throw new FtpException("Demo root cannot be deleted.", 550);
        var (parent, name) = ResolveParent(path);
        if (!parent.Children.TryGetValue(name, out var node) || !node.IsDirectory) throw new FtpException("Demo directory was not found.", 550);
        if (!recursive && node.Children.Count > 0) throw new FtpException("Directory is not empty.", 550);
        parent.Children.Remove(name);
        return Task.CompletedTask;
    }, cancellationToken);

    public Task DownloadFileAsync(string remotePath, string localPath, IProgress<(long transferred, long? total)>? progress = null, CancellationToken cancellationToken = default) => LockedAsync(async ct =>
    {
        var node = Resolve(InputGuard.RemotePath(remotePath));
        if (node.IsDirectory) throw new FtpException("Demo item is a directory.", 550);
        var destination = Path.GetFullPath(localPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? Directory.GetCurrentDirectory());
        var part = destination + ".ghostftp.part";
        await WriteFileAsync(node.Data, part, progress, ct).ConfigureAwait(false);
        System.IO.File.Move(part, destination, true);
    }, cancellationToken);

    public Task UploadFileAsync(string localPath, string remotePath, IProgress<(long transferred, long? total)>? progress = null, CancellationToken cancellationToken = default) => LockedAsync(async ct =>
    {
        var source = Path.GetFullPath(localPath);
        if (!System.IO.File.Exists(source)) throw new FileNotFoundException("Local file does not exist.", source);
        var length = new FileInfo(source).Length;
        if (length > MaxStoredBytes) throw new IOException("Demo mode accepts files up to 64 MB.");
        var data = await ReadFileAsync(source, progress, ct).ConfigureAwait(false);
        var (parent, name) = ResolveParent(InputGuard.RemotePath(remotePath));
        parent.Children[name] = new Node { Name = name, IsDirectory = false, Data = data, ModifiedUtc = DateTimeOffset.UtcNow };
    }, cancellationToken);

    public Task DownloadDirectoryAsync(string remotePath, string localDirectory, IProgress<(long transferred, long? total)>? progress = null, CancellationToken cancellationToken = default) => LockedAsync(async ct =>
    {
        var node = Resolve(InputGuard.RemotePath(remotePath));
        if (!node.IsDirectory) throw new FtpException("Demo item is not a directory.", 550);
        var total = SumBytes(node);
        long transferred = 0;
        var budget = 0;
        await DownloadNodeAsync(node, Path.GetFullPath(localDirectory), total, () => transferred, value => transferred = value, progress, () => ++budget, ct).ConfigureAwait(false);
    }, cancellationToken);

    public Task UploadDirectoryAsync(string localDirectory, string remotePath, IProgress<(long transferred, long? total)>? progress = null, CancellationToken cancellationToken = default) => LockedAsync(async ct =>
    {
        var root = Path.GetFullPath(localDirectory);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = false,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var files = Directory.EnumerateFiles(root, "*", enumeration).Take(MaxItems + 1).ToArray();
        var directories = Directory.EnumerateDirectories(root, "*", enumeration).Take(MaxItems + 1).ToArray();
        if (files.Length > MaxItems || directories.Length > MaxItems || (long)files.Length + directories.Length > MaxItems)
            throw new IOException($"Demo upload exceeds the safety limit of {MaxItems:N0} items.");
        var total = files.Sum(path => new FileInfo(path).Length);
        if (total > MaxStoredBytes) throw new IOException("Demo mode accepts folders up to 64 MB in total.");

        var remoteRoot = InputGuard.RemotePath(remotePath);
        EnsureDirectory(remoteRoot);
        foreach (var directory in directories)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, directory).Replace('\\', '/');
            EnsureDirectory(FtpListingParser.CombineRemote(remoteRoot, relative));
        }

        long transferred = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var destination = FtpListingParser.CombineRemote(remoteRoot, relative);
            var (parent, name) = ResolveParent(destination);
            var size = new FileInfo(file).Length;
            var baseTransferred = transferred;
            var fileProgress = new Progress<(long transferred, long? total)>(p => progress?.Report((baseTransferred + p.transferred, total)));
            var data = await ReadFileAsync(file, fileProgress, ct).ConfigureAwait(false);
            transferred += size;
            parent.Children[name] = new Node { Name = name, IsDirectory = false, Data = data, ModifiedUtc = DateTimeOffset.UtcNow };
            progress?.Report((transferred, total));
        }
    }, cancellationToken);

    private Node Resolve(string path)
    {
        path = InputGuard.RemotePath(path);
        var current = _root;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!current.IsDirectory || !current.Children.TryGetValue(segment, out var next)) throw new FtpException("Demo path was not found.", 550);
            current = next;
        }
        return current;
    }

    private (Node Parent, string Name) ResolveParent(string path)
    {
        path = InputGuard.RemotePath(path);
        if (path == "/") throw new FtpException("Operation is not allowed on demo root.", 550);
        var name = path.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return (Resolve(FtpListingParser.ParentRemote(path)), name);
    }

    private Node EnsureDirectory(string path)
    {
        path = InputGuard.RemotePath(path);
        var current = _root;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!current.Children.TryGetValue(segment, out var next))
            {
                next = DirectoryNode(segment);
                current.Children[segment] = next;
            }
            if (!next.IsDirectory) throw new FtpException("A file blocks creation of the requested demo folder.", 550);
            current = next;
        }
        return current;
    }

    private static async Task DownloadNodeAsync(Node node, string localDirectory, long total, Func<long> getTransferred, Action<long> setTransferred, IProgress<(long transferred, long? total)>? progress, Func<int> incrementBudget, CancellationToken ct)
    {
        Directory.CreateDirectory(localDirectory);
        foreach (var child in node.Children.Values)
        {
            ct.ThrowIfCancellationRequested();
            if (incrementBudget() > MaxItems) throw new IOException($"Demo download exceeds the safety limit of {MaxItems:N0} items.");
            var destination = LocalPathSafety.CombineUnderRoot(localDirectory, child.Name);
            if (child.IsDirectory)
            {
                await DownloadNodeAsync(child, destination, total, getTransferred, setTransferred, progress, incrementBudget, ct).ConfigureAwait(false);
                continue;
            }
            var part = destination + ".ghostftp.part";
            await using var output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
            var offset = 0;
            while (offset < child.Data.Length)
            {
                var count = Math.Min(64 * 1024, child.Data.Length - offset);
                await output.WriteAsync(child.Data.AsMemory(offset, count), ct).ConfigureAwait(false);
                offset += count;
                var transferred = getTransferred() + count;
                setTransferred(transferred);
                progress?.Report((transferred, total));
            }
            await output.FlushAsync(ct).ConfigureAwait(false);
            output.Close();
            System.IO.File.Move(part, destination, true);
        }
    }

    private static async Task WriteFileAsync(byte[] data, string path, IProgress<(long transferred, long? total)>? progress, CancellationToken ct)
    {
        await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
        var offset = 0;
        while (offset < data.Length)
        {
            var count = Math.Min(64 * 1024, data.Length - offset);
            await output.WriteAsync(data.AsMemory(offset, count), ct).ConfigureAwait(false);
            offset += count;
            progress?.Report((offset, data.LongLength));
        }
        await output.FlushAsync(ct).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadFileAsync(string path, IProgress<(long transferred, long? total)>? progress, CancellationToken ct)
    {
        var total = new FileInfo(path).Length;
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var memory = new MemoryStream(total <= int.MaxValue ? (int)total : 0);
        var buffer = new byte[64 * 1024];
        long transferred = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0) break;
            await memory.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            transferred += read;
            progress?.Report((transferred, total));
        }
        return memory.ToArray();
    }

    private static long SumBytes(Node node) => node.IsDirectory ? node.Children.Values.Sum(SumBytes) : node.Data.LongLength;
    private static Node DirectoryNode(string name) => new() { Name = name, IsDirectory = true };
    private static Node TextFile(string name, string text) => new() { Name = name, IsDirectory = false, Data = Encoding.UTF8.GetBytes(text) };
    private static Node BinaryFile(string name, int size, byte seed)
    {
        var data = new byte[size];
        for (var i = 0; i < data.Length; i++) data[i] = (byte)(seed + i % 17);
        return new Node { Name = name, IsDirectory = false, Data = data };
    }
    private static FtpEntry ToEntry(string parent, Node node) => new(node.Name, FtpListingParser.CombineRemote(parent, node.Name), node.IsDirectory, node.IsDirectory ? 0 : node.Data.LongLength, node.ModifiedUtc, node.IsDirectory ? "rwxr-xr-x" : "rw-r--r--");

    private async Task LockedAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { ThrowIfDisposed(); EnsureConnected(); await action(cancellationToken).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    private async Task<T> LockedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { ThrowIfDisposed(); EnsureConnected(); return await action(cancellationToken).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    private void EnsureConnected()
    {
        if (!IsConnected) throw new InvalidOperationException("Demo session is not connected.");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        IsConnected = false;
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }
}
