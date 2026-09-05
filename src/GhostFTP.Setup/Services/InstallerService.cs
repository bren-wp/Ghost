using GhostFTP.Design;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GhostFTP.Setup.Services;

internal sealed class InstallerService
{
    private const string PayloadResourceName = "GhostFTP.PortablePayload.exe";
    private const long MinimumPayloadBytes = 64 * 1024;
    private const long MaxSettingsFileBytes = 1024 * 1024;

    public string InstallDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        GhostBrand.ProductName);

    public string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        GhostBrand.ProductName);

    public string AppPath => Path.Combine(InstallDirectory, "GhostFTP.exe");
    public string InstalledSetupPath => Path.Combine(InstallDirectory, "GhostFTP-Setup.exe");
    public string SettingsPath => Path.Combine(DataDirectory, "settings.json");
    public bool IsInstalled => File.Exists(AppPath);

    public string LoadPreferredLanguage()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return GhostLocalization.DefaultLanguageCode;
            var info = new FileInfo(SettingsPath);
            if (info.Length <= 0 || info.Length > MaxSettingsFileBytes)
                return GhostLocalization.DefaultLanguageCode;

            using var stream = File.OpenRead(SettingsPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("languageCode", out var language) && language.ValueKind == JsonValueKind.String)
                return GhostLocalization.NormalizeLanguageCode(language.GetString());
        }
        catch
        {
            // A damaged local settings file must never prevent setup from starting.
        }

        return GhostLocalization.DefaultLanguageCode;
    }

    public async Task InstallAsync(bool desktopShortcut, string languageCode, CancellationToken cancellationToken)
    {
        languageCode = GhostLocalization.NormalizeLanguageCode(languageCode);
        Directory.CreateDirectory(InstallDirectory);
        var tempApp = Path.Combine(InstallDirectory, $"GhostFTP.exe.new-{Guid.NewGuid():N}");
        var backupApp = Path.Combine(InstallDirectory, $"GhostFTP.exe.backup-{Guid.NewGuid():N}");

        try
        {
            await ExtractPayloadAsync(tempApp, cancellationToken).ConfigureAwait(false);
            await ValidatePayloadAsync(tempApp, cancellationToken).ConfigureAwait(false);

            if (File.Exists(AppPath))
            {
                try
                {
                    File.Replace(tempApp, AppPath, backupApp, ignoreMetadataErrors: true);
                    TryDelete(backupApp);
                }
                catch (IOException ex)
                {
                    throw new IOException($"{GhostBrand.DisplayName} appears to be running or the existing installation is locked. Close the app and run setup again.", ex);
                }
            }
            else
            {
                File.Move(tempApp, AppPath);
            }

            var currentSetup = Environment.ProcessPath
                ?? throw new InvalidOperationException("Setup executable path is unavailable.");
            if (!string.Equals(currentSetup, InstalledSetupPath, StringComparison.OrdinalIgnoreCase))
                File.Copy(currentSetup, InstalledSetupPath, overwrite: true);

            CreateShortcuts(desktopShortcut);
            await WritePreferredLanguageAsync(languageCode, cancellationToken).ConfigureAwait(false);
            WriteUninstallRegistry();
        }
        finally
        {
            TryDelete(tempApp);
            TryDelete(backupApp);
        }
    }

    public Task UninstallAsync(bool removeUserData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "GhostFTP.lnk"));
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "GhostFTP.lnk"));

        DeleteRequiredFile(AppPath, "The installed application could not be removed. Close Ghost FTP and try again.");
        foreach (var stale in Directory.Exists(InstallDirectory)
                     ? Directory.EnumerateFiles(InstallDirectory, "GhostFTP.exe.*", SearchOption.TopDirectoryOnly).ToArray()
                     : [])
        {
            TryDelete(stale);
        }

        using (var root = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", writable: true))
            root?.DeleteSubKeyTree(GhostBrand.ProductName, throwOnMissingSubKey: false);

        if (removeUserData)
            DeleteDirectoryRequired(DataDirectory, "Local Ghost FTP data could not be removed completely.");

        var current = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(current) && File.Exists(current))
            _ = MoveFileEx(current, null, MoveFileDelayUntilReboot);

        TryRemoveInstallDirectoryWhenEmpty();
        return Task.CompletedTask;
    }

    public void LaunchApp()
    {
        if (!File.Exists(AppPath))
            throw new FileNotFoundException("The installed Ghost FTP executable was not found.", AppPath);

        Process.Start(new ProcessStartInfo(AppPath)
        {
            UseShellExecute = true,
            WorkingDirectory = InstallDirectory
        });
    }

    private async Task WritePreferredLanguageAsync(string languageCode, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DataDirectory);
        JsonObject root;
        try
        {
            if (File.Exists(SettingsPath) && new FileInfo(SettingsPath).Length <= MaxSettingsFileBytes)
            {
                await using var input = File.OpenRead(SettingsPath);
                root = await JsonNode.ParseAsync(input, cancellationToken: cancellationToken).ConfigureAwait(false) as JsonObject
                    ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }
        }
        catch (JsonException)
        {
            root = new JsonObject();
        }

        root["languageCode"] = languageCode;
        root["languageConfiguredBySetup"] = true;

        var temp = SettingsPath + ".setup-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var output = new FileStream(
                temp,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(output, root, cancellationToken: cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(SettingsPath))
                File.Replace(temp, SettingsPath, SettingsPath + ".bak", ignoreMetadataErrors: true);
            else
                File.Move(temp, SettingsPath);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private async Task ExtractPayloadAsync(string targetPath, CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var input = assembly.GetManifestResourceStream(PayloadResourceName)
            ?? throw new InvalidOperationException("Ghost FTP application payload is missing from this setup build.");

        await using var output = new FileStream(
            targetPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough);

        await input.CopyToAsync(output, 128 * 1024, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ValidatePayloadAsync(string payloadPath, CancellationToken cancellationToken)
    {
        var info = new FileInfo(payloadPath);
        if (!info.Exists || info.Length < MinimumPayloadBytes)
            throw new InvalidDataException("The embedded Ghost FTP payload is missing, empty or unexpectedly small.");

        var signature = new byte[2];
        await using var stream = new FileStream(
            payloadPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var read = await stream.ReadAsync(signature, cancellationToken).ConfigureAwait(false);
        if (read != 2 || signature[0] != (byte)'M' || signature[1] != (byte)'Z')
            throw new InvalidDataException("The embedded Ghost FTP payload is not a valid Windows executable.");
    }

    private void CreateShortcuts(bool desktopShortcut)
    {
        var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "GhostFTP.lnk");
        ShellLink.Create(startMenu, AppPath, null, InstallDirectory, "Ghost FTP client", AppPath);

        var desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "GhostFTP.lnk");
        if (desktopShortcut)
            ShellLink.Create(desktop, AppPath, null, InstallDirectory, "Ghost FTP client", AppPath);
        else
            TryDelete(desktop);
    }

    private void WriteUninstallRegistry()
    {
        using var root = Registry.CurrentUser.CreateSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{GhostBrand.ProductName}",
            writable: true);

        root.SetValue("DisplayName", GhostBrand.DisplayName);
        root.SetValue("DisplayVersion", typeof(InstallerService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");
        root.SetValue("Publisher", GhostBrand.Publisher);
        root.SetValue("URLInfoAbout", GhostBrand.Website);
        root.SetValue("HelpLink", GhostBrand.Website);
        root.SetValue("InstallLocation", InstallDirectory);
        root.SetValue("DisplayIcon", AppPath);
        root.SetValue("UninstallString", $"\"{InstalledSetupPath}\" --uninstall");
        root.SetValue("QuietUninstallString", $"\"{InstalledSetupPath}\" --uninstall");
        root.SetValue("NoModify", 1, RegistryValueKind.DWord);
        root.SetValue("NoRepair", 1, RegistryValueKind.DWord);

        if (File.Exists(AppPath))
        {
            var sizeKb = Math.Max(1L, new FileInfo(AppPath).Length / 1024L);
            root.SetValue("EstimatedSize", Math.Min(sizeKb, int.MaxValue), RegistryValueKind.DWord);
        }
    }

    private void TryRemoveInstallDirectoryWhenEmpty()
    {
        try
        {
            if (!Directory.Exists(InstallDirectory))
                return;
            var current = Environment.ProcessPath;
            var remaining = Directory.EnumerateFileSystemEntries(InstallDirectory)
                .Where(path => !string.Equals(path, current, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (remaining.Length == 0 && (string.IsNullOrWhiteSpace(current) || !current.StartsWith(InstallDirectory, StringComparison.OrdinalIgnoreCase)))
                Directory.Delete(InstallDirectory);
        }
        catch
        {
            // The running installed setup may keep the directory non-empty until reboot/self-delete.
        }
    }

    private static void DeleteRequiredFile(string path, string failureMessage)
    {
        if (!File.Exists(path))
            return;
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException(failureMessage, ex);
        }

        if (File.Exists(path))
            throw new IOException(failureMessage);
    }

    private static void DeleteDirectoryRequired(string path, string failureMessage)
    {
        if (!Directory.Exists(path))
            return;
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException(failureMessage, ex);
        }

        if (Directory.Exists(path))
            throw new IOException(failureMessage);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cleanup of temporary/optional files is best effort only.
        }
    }

    private const int MoveFileDelayUntilReboot = 0x4;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags);
}
