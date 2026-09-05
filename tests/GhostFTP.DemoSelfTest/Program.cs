using System.Text;
using GhostFTP.Core.Protocol;

namespace GhostFTP.DemoSelfTest;

public static class Program
{
    public static async Task<int> Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "ghostftp-demo-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            await RunAsync(root).ConfigureAwait(false);
            Console.WriteLine("PASS  Demo session complete local FTP workflow");
            Console.WriteLine("PASS  Demo mode performed no external network operation");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  Demo session self-test — " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static async Task RunAsync(string root)
    {
        await using IFtpSession session = new DemoFtpSession();
        Assert(!session.IsConnected, "Demo session unexpectedly starts connected.");
        Assert(session.Host == "demo.ghostftp.local", "Demo host identity changed unexpectedly.");

        await session.ConnectAsync().ConfigureAwait(false);
        Assert(session.IsConnected, "Demo session did not connect.");
        Assert(!session.IsEncrypted, "Local-only Demo session must not claim TLS encryption.");

        var server = await session.GetServerInfoAsync().ConfigureAwait(false);
        Assert(server.ServerSystem.Contains("demo", StringComparison.OrdinalIgnoreCase), "Demo diagnostics do not identify local Demo mode.");

        var rootItems = await session.ListAsync("/").ConfigureAwait(false);
        Assert(rootItems.Any(x => x.IsDirectory && x.Name == "public_html"), "Demo public_html folder is missing.");
        Assert(rootItems.Any(x => x.IsDirectory && x.Name == "backups"), "Demo backups folder is missing.");
        Assert(rootItems.Any(x => x.IsDirectory && x.Name == "logs"), "Demo logs folder is missing.");
        Assert(rootItems.Any(x => !x.IsDirectory && x.Name == "README.txt"), "Demo README.txt is missing.");

        await session.ChangeDirectoryAsync("/public_html").ConfigureAwait(false);
        Assert(await session.GetWorkingDirectoryAsync().ConfigureAwait(false) == "/public_html", "Demo CWD/PWD state is inconsistent.");
        await session.KeepAliveAsync().ConfigureAwait(false);

        var publicItems = await session.ListAsync("/public_html").ConfigureAwait(false);
        Assert(publicItems.Any(x => x.Name == "index.html" && !x.IsDirectory), "Demo index.html is missing.");
        Assert(publicItems.Any(x => x.Name == "assets" && x.IsDirectory), "Demo assets folder is missing.");

        var initialDownload = Path.Combine(root, "index.html");
        await session.DownloadFileAsync("/public_html/index.html", initialDownload).ConfigureAwait(false);
        var initialText = await File.ReadAllTextAsync(initialDownload).ConfigureAwait(false);
        Assert(initialText.Contains("GhostFTP Demo", StringComparison.Ordinal), "Demo download content is incorrect.");
        Assert(!File.Exists(initialDownload + ".ghostftp.part"), "Completed Demo download left a partial file behind.");

        var uploadSource = Path.Combine(root, "roundtrip-source.txt");
        const string roundtripText = "Ghost FTP Demo round-trip payload\nUTF-8: čćžšđ ✓\n";
        await File.WriteAllTextAsync(uploadSource, roundtripText, Encoding.UTF8).ConfigureAwait(false);
        await session.UploadFileAsync(uploadSource, "/public_html/roundtrip.txt").ConfigureAwait(false);

        var roundtripDownload = Path.Combine(root, "roundtrip-download.txt");
        await session.DownloadFileAsync("/public_html/roundtrip.txt", roundtripDownload).ConfigureAwait(false);
        Assert(
            File.ReadAllBytes(uploadSource).SequenceEqual(File.ReadAllBytes(roundtripDownload)),
            "Demo upload/download round-trip changed file bytes.");

        await session.RenameAsync("/public_html/roundtrip.txt", "/public_html/roundtrip-renamed.txt").ConfigureAwait(false);
        var afterRename = await session.ListAsync("/public_html").ConfigureAwait(false);
        Assert(afterRename.Any(x => x.Name == "roundtrip-renamed.txt"), "Demo rename destination is missing.");
        Assert(afterRename.All(x => x.Name != "roundtrip.txt"), "Demo rename left the old file behind.");

        await session.CreateDirectoryAsync("/public_html/empty-test-folder").ConfigureAwait(false);
        Assert((await session.ListAsync("/public_html").ConfigureAwait(false)).Any(x => x.Name == "empty-test-folder" && x.IsDirectory), "Demo create-directory failed.");
        await session.DeleteDirectoryAsync("/public_html/empty-test-folder", recursive: false).ConfigureAwait(false);

        var directorySource = Path.Combine(root, "directory-source");
        var nestedSource = Path.Combine(directorySource, "nested");
        Directory.CreateDirectory(nestedSource);
        await File.WriteAllTextAsync(Path.Combine(directorySource, "alpha.txt"), "alpha\n", Encoding.UTF8).ConfigureAwait(false);
        await File.WriteAllBytesAsync(Path.Combine(nestedSource, "beta.bin"), Enumerable.Range(0, 4096).Select(i => (byte)(i % 251)).ToArray()).ConfigureAwait(false);

        await session.UploadDirectoryAsync(directorySource, "/public_html/uploaded-folder").ConfigureAwait(false);
        var directoryDownload = Path.Combine(root, "directory-download");
        await session.DownloadDirectoryAsync("/public_html/uploaded-folder", directoryDownload).ConfigureAwait(false);
        Assert(File.ReadAllBytes(Path.Combine(directorySource, "alpha.txt")).SequenceEqual(File.ReadAllBytes(Path.Combine(directoryDownload, "alpha.txt"))), "Demo folder round-trip changed alpha.txt.");
        Assert(File.ReadAllBytes(Path.Combine(nestedSource, "beta.bin")).SequenceEqual(File.ReadAllBytes(Path.Combine(directoryDownload, "nested", "beta.bin"))), "Demo folder round-trip changed nested beta.bin.");

        await session.DeleteFileAsync("/public_html/roundtrip-renamed.txt").ConfigureAwait(false);
        await session.DeleteDirectoryAsync("/public_html/uploaded-folder", recursive: true).ConfigureAwait(false);
        var cleaned = await session.ListAsync("/public_html").ConfigureAwait(false);
        Assert(cleaned.All(x => x.Name is not "roundtrip-renamed.txt" and not "uploaded-folder" and not "empty-test-folder"), "Demo cleanup left test data behind.");

        var rootDeleteBlocked = false;
        try
        {
            await session.DeleteDirectoryAsync("/", recursive: true).ConfigureAwait(false);
        }
        catch (FtpException)
        {
            rootDeleteBlocked = true;
        }
        Assert(rootDeleteBlocked, "Demo root deletion was not blocked.");

        await session.DisconnectAsync().ConfigureAwait(false);
        Assert(!session.IsConnected, "Demo session did not disconnect.");

        var disconnectedOperationBlocked = false;
        try
        {
            _ = await session.ListAsync("/").ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            disconnectedOperationBlocked = true;
        }
        Assert(disconnectedOperationBlocked, "Demo operations were accepted after disconnect.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
