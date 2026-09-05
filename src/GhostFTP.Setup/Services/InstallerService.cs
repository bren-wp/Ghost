using GhostFTP.Design;
using Microsoft.Win32;
using System.Diagnostics;
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
            // Damaged local settings must never prevent Setup from starting.
        }

        return GhostLocalization.DefaultLanguageCode;
    }

    public async Task InstallAsync(bool desktopShortcut, string languageCode, CancellationToken cancellationToken)
    {
        languageCode = GhostLocalization.NormalizeLanguageCode(languageCode);
        Directory.CreateDirectory(InstallDirectory);

        var tempApp = Path.Combine(InstallDirectory, $"GhostFTP.exe.new-{Guid.NewGuid():N}");
        var backupApp = Path.Combine(InstallDirectory, $"GhostFTP.exe.backup-{Guid.NewGuid():N}");
        var tempSetup = Path.Combine(InstallDirectory, $"GhostFTP-Setup.exe.new-{Guid.NewGuid():N}");
        var backupSetup = Path.Combine(InstallDirectory, $"GhostFTP-Setup.exe.backup-{Guid.NewGuid():N}");

        var previousAppExisted = File.Exists(AppPath);
        var previousSetupExisted = File.Exists(InstalledSetupPath);
        var appCommitted = false;
        var setupCommitted = false;

        try
        {
            // Stage and validate every binary before changing the active installation. This keeps
            // a failed Setup copy or invalid payload from modifying a previously working client.
            await ExtractPayloadAsync(tempApp, cancellationToken).ConfigureAwait(false);
            await ValidateExecutableAsync(tempApp, "Ghost FTP application payload", cancellationToken).ConfigureAwait(false);
            EnsureNotDowngrade(AppPath, tempApp);

            var currentSetup = Environment.ProcessPath
                ?? throw new InvalidOperationException("Setup executable path is unavailable.");
            currentSetup = Path.GetFullPath(currentSetup);
            var replaceSetup = !string.Equals(
                currentSetup,
                Path.GetFullPath(InstalledSetupPath),
                StringComparison.OrdinalIgnoreCase);

            if (replaceSetup)
            {
                File.Copy(currentSetup, tempSetup, overwrite: false);
                await ValidateExecutableAsync(tempSetup, "Ghost FTP Setup payload", cancellationToken).ConfigureAwait(false);
                EnsureNotDowngrade(InstalledSetupPath, tempSetup);
            }

            if (previousAppExisted)
            {
                try
                {
                    File.Replace(tempApp, AppPath, backupApp, ignoreMetadataErrors: true);
                    appCommitted = true;
                }
                catch (IOException ex)
                {
                    throw new IOException($"{GhostBrand.DisplayName} appears to be running or the existing installation is locked. Close the app and run Setup again.", ex);
                }
            }
            else
            {
                File.Move(tempApp, AppPath);
                appCommitted = true;
            }

            if (replaceSetup)
            {
                try
                {
                    if (previousSetupExisted)
                        File.Replace(tempSetup, InstalledSetupPath, backupSetup, ignoreMetadataErrors: true);
                    else
                        File.Move(tempSetup, InstalledSetupPath);
                    setupCommitted = true;
                }
                catch (IOException ex)
                {
                    throw new IOException("Ghost FTP Setup could not update its installed maintenance copy. Close any running Setup window and try again.", ex);
                }
            }

            CreateShortcuts(desktopShortcut);
            await WritePreferredLanguageAsync(languageCode, cancellationToken).ConfigureAwait(false);
            WriteUninstallRegistry();

            // Both binary replacements remain rollback-capable until every install stage succeeds.
            TryDelete(backupSetup);
            TryDelete(backupApp);
        }
        catch (Exception installError)
        {
            var rollbackErrors = new List<Exception>();

            if (setupCommitted)
            {
                try
                {
                    RollbackFile(
                        InstalledSetupPath,
                        backupSetup,
                        previousSetupExisted,
                        "The incomplete Ghost FTP Setup update could not be rolled back.");
                }
                catch (Exception ex)
                {
                    rollbackErrors.Add(ex);
                }
            }

            if (appCommitted)
            {
                try
                {
                    RollbackFile(
                        AppPath,
                        backupApp,
                        previousAppExisted,
                        "The incomplete Ghost FTP application update could not be rolled back.");
                }
                catch (Exception ex)
                {
                    rollbackErrors.Add(ex);
                }
            }

            if (rollbackErrors.Count > 0)
            {
                rollbackErrors.Insert(0, installError);
                throw new AggregateException(
                    "Ghost FTP installation failed and automatic binary rollback was incomplete.",
                    rollbackErrors);
            }

            throw;
        }
        finally
        {
            TryDelete(tempApp);
            TryDelete(tempSetup);
            TryDelete(backupApp);
            TryDelete(backupSetup);
        }
    }

    public Task UninstallAsync(bool removeUserData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "GhostFTP.lnk"));
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "GhostFTP.lnk"));

        DeleteRequiredFile(AppPath, "The installed application could not be removed. Close Ghost FTP and try again.");
        if (Directory.Exists(InstallDirectory))
        {
            foreach (var pattern in new[] { "GhostFTP.exe.*", "GhostFTP-Setup.exe.*" })
            {
                foreach (var stale in Directory.EnumerateFiles(InstallDirectory, pattern, SearchOption.TopDirectoryOnly).ToArray())
                    TryDelete(stale);
            }
        }

        using (var root = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", writable: true))
            root?.DeleteSubKeyTree(GhostBrand.ProductName, throwOnMissingSubKey: false);

        if (removeUserData)
            DeleteDirectoryRequired(DataDirectory, "Local Ghost FTP data could not be removed completely.");

        var current = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(current)
            && string.Equals(Path.GetFullPath(current), Path.GetFullPath(InstalledSetupPath), StringComparison.OrdinalIgnoreCase))
        {
            ScheduleSelfDelete(current);
        }
        else
        {
            DeleteRequiredFile(InstalledSetupPath, "The installed Ghost FTP Setup executable could not be removed.");
            TryRemoveInstallDirectoryWhenEmpty();
        }

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

    private static void RollbackFile(string activePath, string backupPath, bool previousExisted, string failureMessage)
    {
        if (previousExisted)
        {
            if (!File.Exists(backupPath))
                throw new IOException(failureMessage + " The rollback copy is missing.");

            if (File.Exists(activePath))
                File.Replace(backupPath, activePath, null, ignoreMetadataErrors: true);
            else
                File.Move(backupPath, activePath);
            return;
        }

        DeleteRequiredFile(activePath, failureMessage);
    }

    private static void EnsureNotDowngrade(string installedPath, string candidatePath)
    {
        if (!File.Exists(installedPath))
            return;

        var installedText = FileVersionInfo.GetVersionInfo(installedPath).FileVersion;
        var candidateText = FileVersionInfo.GetVersionInfo(candidatePath).FileVersion;
        if (!Version.TryParse(installedText, out var installedVersion)
            || !Version.TryParse(candidateText, out var candidateVersion))
        {
            throw new InvalidDataException("Ghost FTP could not compare installed and candidate executable versions safely.");
        }

        if (candidateVersion < installedVersion)
        {
            throw new InvalidOperationException(
                $"Ghost FTP Setup refuses to downgrade from {installedVersion} to {candidateVersion}. Use a package with the same or newer version.");
        }
    }

    private async Task WritePreferredLanguageAsync(string languageCode, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DataDirectory);
        JsonObject root;
        try
        {
            if (File.Exists(SettingsPath))
            {
                var info = new FileInfo(SettingsPath);
                if (info.Length > MaxSettingsFileBytes)
                {
                    QuarantineCorruptSettings();
                    root = new JsonObject();
                }
                else if (info.Length == 0)
                {
                    root = new JsonObject();
                }
                else
                {
                    await using var input = File.OpenRead(SettingsPath);
                    root = await JsonNode.ParseAsync(input, cancellationToken: cancellationToken).ConfigureAwait(false) as JsonObject
                        ?? new JsonObject();
                }
            }
            else
            {
                root = new JsonObject();
            }
        }
        catch (JsonException)
        {
            QuarantineCorruptSettings();
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

            if (new FileInfo(temp).Length > MaxSettingsFileBytes)
                throw new InvalidDataException("Setup-generated settings exceeded the supported size limit.");

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

    private void QuarantineCorruptSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;
            var quarantine = SettingsPath + ".corrupt-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            File.Move(SettingsPath, quarantine, overwrite: false);
        }
        catch
        {
            // If quarantine fails, the later atomic replace still decides whether Setup can continue safely.
        }
    }

    private async Task ExtractPayloadAsync(string targetPath, CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var input = assembly.GetManifestResourceStream(PayloadResourceName)
            ?? throw new InvalidOperationException("Ghost FTP application payload is missing from this Setup build.");

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

    private static async Task ValidateExecutableAsync(string path, string description, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length < MinimumPayloadBytes)
            throw new InvalidDataException($"The {description} is missing, empty or unexpectedly small.");

        var signature = new byte[2];
        await using (var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            var read = await stream.ReadAsync(signature, cancellationToken).ConfigureAwait(false);
            if (read != 2 || signature[0] != (byte)'M' || signature[1] != (byte)'Z')
                throw new InvalidDataException($"The {description} is not a valid Windows executable.");
        }

        // An MZ header alone is not an adequate payload identity check. Refuse a substituted
        // executable unless its version resource identifies the expected Ghost FTP product,
        // BRENDIGO LTD publisher and this Setup build's exact file version.
        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        var expectedVersion = typeof(InstallerService).Assembly.GetName().Version?.ToString(4)
            ?? throw new InvalidOperationException("Setup assembly version is unavailable.");
        if (!string.Equals(versionInfo.ProductName, GhostBrand.DisplayName, StringComparison.Ordinal))
            throw new InvalidDataException($"The {description} does not identify the expected {GhostBrand.DisplayName} product.");
        if (!string.Equals(versionInfo.CompanyName, GhostBrand.Publisher, StringComparison.Ordinal))
            throw new InvalidDataException($"The {description} does not identify the expected {GhostBrand.Publisher} publisher.");
        if (!string.Equals(versionInfo.FileVersion, expectedVersion, StringComparison.Ordinal))
            throw new InvalidDataException($"The {description} has file version '{versionInfo.FileVersion}' but Setup requires '{expectedVersion}'.");
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
        // The current uninstaller is interactive. Do not advertise an interactive command as
        // QuietUninstallString because management software may otherwise assume silent removal.
        root.DeleteValue("QuietUninstallString", throwOnMissingValue: false);
        root.SetValue("NoModify", 1, RegistryValueKind.DWord);
        root.SetValue("NoRepair", 1, RegistryValueKind.DWord);

        if (File.Exists(AppPath))
        {
            var sizeKb = Math.Max(1L, new FileInfo(AppPath).Length / 1024L);
            root.SetValue("EstimatedSize", Math.Min(sizeKb, int.MaxValue), RegistryValueKind.DWord);
        }
    }

    private void ScheduleSelfDelete(string currentSetup)
    {
        // Register the OS-level fallback first. A running executable cannot delete itself, and
        // the user can remain on the Finish page for an arbitrary amount of time.
        _ = MoveFileEx(currentSetup, null, MoveFileDelayUntilReboot);

        try
        {
            // Retry local cleanup for up to ten minutes. Each pass first attempts deletion; once
            // Setup exits and the file unlocks, the same hidden helper removes the executable and
            // then the empty install directory. Loopback ping is only a local one-second delay and
            // never leaves the machine.
            var command = $"for /l %i in (1,1,600) do @(del /f /q \"{currentSetup}\" >nul 2>&1 & if not exist \"{currentSetup}\" (rmdir \"{InstallDirectory}\" >nul 2>&1 & exit /b 0) & ping 127.0.0.1 -n 2 >nul) & exit /b 0";
            var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            start.ArgumentList.Add("/d");
            start.ArgumentList.Add("/s");
            start.ArgumentList.Add("/c");
            start.ArgumentList.Add(command);
            _ = Process.Start(start);
        }
        catch
        {
            // MoveFileEx above remains the eventual cleanup fallback.
        }
    }

    private void TryRemoveInstallDirectoryWhenEmpty()
    {
        try
        {
            if (Directory.Exists(InstallDirectory) && !Directory.EnumerateFileSystemEntries(InstallDirectory).Any())
                Directory.Delete(InstallDirectory);
        }
        catch
        {
            // Non-critical cleanup only.
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
