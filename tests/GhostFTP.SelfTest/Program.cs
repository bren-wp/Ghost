using System.Text;
using System.Text.Json;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;

namespace GhostFTP.SelfTest;

public static class Program
{
    public static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("InputGuard blocks CRLF command injection", TestCommandInjection),
            ("Remote paths normalize safely", TestRemotePath),
            ("Remote path traversal canonicalizes safely", TestRemoteTraversal),
            ("Remote names block traversal", TestRemoteName),
            ("Malicious listing names are ignored", TestMaliciousListingName),
            ("MLSD listing parser", TestMlsd),
            ("Unix LIST parser", TestUnixList),
            ("Windows LIST parser", TestWindowsList),
            ("Remote parent path", TestParent),
            ("Windows local filename safety", TestLocalName),
            ("Saved profiles normalize untrusted JSON", TestProfileNormalization),
            ("Saved-password input remains command-safe", TestSavedPasswordGuard)
        };

        var failures = new List<string>();
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine("PASS  " + test.Name);
            }
            catch (Exception ex)
            {
                failures.Add(test.Name + ": " + ex.Message);
                Console.WriteLine("FAIL  " + test.Name + " — " + ex.Message);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} self-tests passed.");
        if (failures.Count == 0) return 0;
        foreach (var failure in failures) Console.Error.WriteLine(failure);
        return 1;
    }

    private static void TestCommandInjection()
    {
        var blocked = false;
        try { _ = InputGuard.CommandArgument("safe\r\nDELE /important", "value"); }
        catch (ArgumentException) { blocked = true; }
        Assert(blocked, "CRLF was accepted.");
    }

    private static void TestRemotePath()
    {
        Assert(InputGuard.RemotePath("public_html\\assets") == "/public_html/assets", "Remote path normalization failed.");
    }

    private static void TestRemoteTraversal()
    {
        Assert(InputGuard.RemotePath("/public_html/assets/../index") == "/public_html/index", "Parent traversal did not canonicalize safely.");
        Assert(InputGuard.RemotePath("../../../../") == "/", "Traversal above root was not clamped to root.");
    }

    private static void TestRemoteName()
    {
        var blocked = false;
        try { _ = InputGuard.RemoteName("../escape"); }
        catch (ArgumentException) { blocked = true; }
        Assert(blocked, "Remote traversal-style name was accepted.");
    }

    private static void TestMaliciousListingName()
    {
        var text = "type=file;size=10; ../../escape.txt\r\ntype=file;size=11; safe.txt\r\n";
        var items = FtpListingParser.ParseMlsd(text, "/public_html");
        Assert(items.Count == 1 && items[0].Name == "safe.txt", "Traversal-style listing entry was not ignored.");
    }

    private static void TestMlsd()
    {
        var text = "type=dir;modify=20260904190000; assets\r\ntype=file;size=1234;modify=20260904190100; index.html\r\n";
        var items = FtpListingParser.ParseMlsd(text, "/public_html");
        Assert(items.Count == 2, "Unexpected MLSD item count.");
        Assert(items[0].IsDirectory && items[0].FullPath == "/public_html/assets", "MLSD directory parse failed.");
        Assert(!items[1].IsDirectory && items[1].Size == 1234, "MLSD file size was not parsed.");
    }

    private static void TestUnixList()
    {
        var text = "drwxr-xr-x 2 owner group 4096 Sep  4 20:00 assets\n-rw-r--r-- 1 owner group 512 Sep  4 2026 index.html\n";
        var items = FtpListingParser.ParseList(text, "/public_html", new DateTimeOffset(2026, 9, 4, 22, 0, 0, TimeSpan.Zero));
        Assert(items.Count == 2, "Unexpected Unix LIST item count.");
        Assert(items[0].IsDirectory, "Unix directory not recognized.");
        Assert(items[1].Size == 512, "Unix file size not parsed.");
    }

    private static void TestWindowsList()
    {
        var text = "09-04-26  08:00PM       <DIR>          assets\r\n09-04-26  08:01PM                  512 index.html\r\n";
        var items = FtpListingParser.ParseList(text, "/public_html", DateTimeOffset.UtcNow);
        Assert(items.Count == 2, "Unexpected Windows LIST item count.");
        Assert(items[0].IsDirectory, "Windows directory not recognized.");
        Assert(items[1].Size == 512, "Windows file size not parsed.");
    }

    private static void TestParent()
    {
        Assert(FtpListingParser.ParentRemote("/a/b/c") == "/a/b", "Parent path is wrong.");
        Assert(FtpListingParser.ParentRemote("/a") == "/", "Root parent path is wrong.");
    }

    private static void TestLocalName()
    {
        Assert(LocalPathSafety.SafeFileName("CON.txt").StartsWith('_'), "Reserved Windows filename was not escaped.");
        Assert(!LocalPathSafety.SafeFileName("a:b.txt").Contains(':'), "Invalid Windows character was not escaped.");
    }

    private static void TestProfileNormalization()
    {
        var root = Path.Combine(Path.GetTempPath(), "ghostftp-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var profilePath = Path.Combine(root, "profiles.json");
            var forged = new List<ServerProfile>
            {
                new()
                {
                    IsDemo = true,
                    Name = "Forged demo",
                    Host = "example.invalid",
                    Port = 65000,
                    Username = "attacker",
                    InitialPath = "/unexpected",
                    Security = FtpSecurityMode.ImplicitTls,
                    RememberPassword = true,
                    ProtectedPassword = "forged"
                },
                new()
                {
                    IsDemo = true,
                    Name = "Duplicate demo"
                },
                new()
                {
                    Name = new string('N', 200),
                    Host = "bad\r\nhost",
                    Port = -1,
                    Username = "user",
                    InitialPath = "/safe/../root",
                    Security = (FtpSecurityMode)999,
                    RememberPassword = true,
                    ProtectedPassword = new string('x', 70_000)
                }
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            File.WriteAllText(profilePath, JsonSerializer.Serialize(forged, jsonOptions));
            var store = new ProfileStore(profilePath, new TestSecretProtector());
            var profiles = store.LoadAsync().GetAwaiter().GetResult();

            Assert(profiles.Count == 2, "Duplicate Demo profile was not removed.");
            var demo = profiles.Single(x => x.IsDemo);
            Assert(demo.Name == "GhostFTP Demo", "Demo name was not canonicalized.");
            Assert(demo.Host == "demo.ghostftp.local" && demo.Port == 21 && demo.Username == "demo", "Demo connection details were not canonicalized.");
            Assert(!demo.RememberPassword && demo.ProtectedPassword is null, "Demo credentials must never persist.");

            var user = profiles.Single(x => !x.IsDemo);
            Assert(user.Name.Length == 128, "Oversized profile display name was not bounded.");
            Assert(user.Host.Length == 0, "Invalid stored host was not neutralized.");
            Assert(user.Security == FtpSecurityMode.ExplicitTls, "Invalid security mode was not reset to FTPS Explicit.");
            Assert(user.Port == 21, "Invalid port was not restored to the default for the normalized security mode.");
            Assert(user.InitialPath == "/root", "Stored remote path was not canonicalized.");
            Assert(user.ProtectedPassword is null, "Oversized protected-password data was not discarded.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void TestSavedPasswordGuard()
    {
        var root = Path.Combine(Path.GetTempPath(), "ghostftp-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new ProfileStore(Path.Combine(root, "profiles.json"), new TestSecretProtector());
            var profile = new ServerProfile { RememberPassword = true };
            store.SetPassword(profile, "safe-password");
            Assert(store.GetPassword(profile) == "safe-password", "Saved password round-trip failed.");

            var blocked = false;
            try { store.SetPassword(profile, "bad\r\nPASS injected"); }
            catch (ArgumentException) { blocked = true; }
            Assert(blocked, "Unsafe saved password content was accepted.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedText) => Encoding.UTF8.GetString(Convert.FromBase64String(protectedText));
    }
}
