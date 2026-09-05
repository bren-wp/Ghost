using System.Text.Json;
using System.Text.RegularExpressions;

namespace GhostFTP.Core.Services;

public enum AppTheme
{
    System,
    Dark,
    Light
}

public sealed class AppPaths
{
    public bool IsPortable { get; }
    public string ExecutableDirectory { get; }
    public string DataDirectory { get; }
    public string ProfilesFile => Path.Combine(DataDirectory, "profiles.json");
    public string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public AppPaths()
    {
        var executable = Environment.ProcessPath ?? AppContext.BaseDirectory;
        ExecutableDirectory = Directory.Exists(executable)
            ? Path.GetFullPath(executable)
            : Path.GetDirectoryName(Path.GetFullPath(executable)) ?? AppContext.BaseDirectory;

        var name = Path.GetFileNameWithoutExtension(executable);
        IsPortable = name.Contains("Portable", StringComparison.OrdinalIgnoreCase)
            || File.Exists(Path.Combine(ExecutableDirectory, "portable.flag"));

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
            localData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        DataDirectory = IsPortable
            ? Path.Combine(ExecutableDirectory, "Data")
            : Path.Combine(localData, "GhostFTP");

        Directory.CreateDirectory(DataDirectory);
        PrivateFilePermissions.TryHardenDirectory(DataDirectory);
    }
}

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string LanguageCode { get; set; } = "en";
    public string LastLocalDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public bool ConfirmDeletes { get; set; } = true;
    public bool ShowHiddenFiles { get; set; }
    public int AutomaticTransferRetries { get; set; } = 2;
    public int ConcurrentTransfers { get; set; } = 3;
    public int ConnectTimeoutSeconds { get; set; } = 15;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int TransferIdleTimeoutSeconds { get; set; } = 120;
    public int KeepAliveSeconds { get; set; } = 60;
    public double WindowWidth { get; set; } = 1520;
    public double WindowHeight { get; set; } = 920;
    public bool WindowMaximized { get; set; }
    public double TransferPanelHeight { get; set; } = 198;
    public double LocalPaneFraction { get; set; } = 0.5;
}

public sealed class AppSettingsStore
{
    private const long MaxSettingsFileBytes = 1024 * 1024;
    private static readonly Regex LanguageCodePattern = new("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppSettingsStore(string path) => _path = Path.GetFullPath(path);

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return new AppSettings();

            try
            {
                return await ReadAsync(_path, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException)
            {
                var backup = _path + ".bak";
                if (!File.Exists(backup))
                    return new AppSettings();

                try
                {
                    return await ReadAsync(backup, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception backupEx) when (backupEx is JsonException or InvalidDataException or IOException)
                {
                    return new AppSettings();
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_path) ?? throw new InvalidOperationException("Settings path has no parent directory.");
            Directory.CreateDirectory(directory);
            PrivateFilePermissions.TryHardenDirectory(directory);

            var temp = _path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                await using (var stream = new FileStream(
                    temp,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    16 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                if (new FileInfo(temp).Length > MaxSettingsFileBytes)
                    throw new InvalidDataException("Settings data exceeds the supported size limit.");

                PrivateFilePermissions.TryHardenFile(temp);
                AtomicFile.Replace(temp, _path, _path + ".bak");
                PrivateFilePermissions.TryHardenFile(_path);
                if (File.Exists(_path + ".bak"))
                    PrivateFilePermissions.TryHardenFile(_path + ".bak");
            }
            finally
            {
                TryDelete(temp);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<AppSettings> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            return new AppSettings();
        if (info.Length > MaxSettingsFileBytes)
            throw new InvalidDataException("Settings data exceeds the supported size limit.");

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? new AppSettings();
        Normalize(settings);
        return settings;
    }

    private static void Normalize(AppSettings settings)
    {
        if (!Enum.IsDefined(settings.Theme))
            settings.Theme = AppTheme.System;

        var language = (settings.LanguageCode ?? string.Empty).Trim();
        settings.LanguageCode = language.Length <= 16 && LanguageCodePattern.IsMatch(language) ? language : "en";

        if (string.IsNullOrWhiteSpace(settings.LastLocalDirectory) || !Directory.Exists(settings.LastLocalDirectory))
            settings.LastLocalDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        settings.AutomaticTransferRetries = Math.Clamp(settings.AutomaticTransferRetries, 0, 5);
        settings.ConcurrentTransfers = Math.Clamp(settings.ConcurrentTransfers, 1, 8);
        settings.ConnectTimeoutSeconds = Math.Clamp(settings.ConnectTimeoutSeconds, 3, 120);
        settings.CommandTimeoutSeconds = Math.Clamp(settings.CommandTimeoutSeconds, 5, 300);
        settings.TransferIdleTimeoutSeconds = Math.Clamp(settings.TransferIdleTimeoutSeconds, 15, 3600);
        settings.KeepAliveSeconds = settings.KeepAliveSeconds == 0 ? 0 : Math.Clamp(settings.KeepAliveSeconds, 15, 600);
        settings.WindowWidth = ClampFinite(settings.WindowWidth, 980, 7680, 1520);
        settings.WindowHeight = ClampFinite(settings.WindowHeight, 640, 4320, 920);
        settings.TransferPanelHeight = ClampFinite(settings.TransferPanelHeight, 128, 440, 198);
        settings.LocalPaneFraction = ClampFinite(settings.LocalPaneFraction, 0.25, 0.75, 0.5);
    }

    private static double ClampFinite(double value, double minimum, double maximum, double fallback) =>
        double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Clamp(value, minimum, maximum);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Temporary-file cleanup is best effort; the authoritative path is already handled atomically.
        }
    }
}

public static class AtomicFile
{
    public static void Replace(string tempPath, string destinationPath, string backupPath)
    {
        tempPath = Path.GetFullPath(tempPath);
        destinationPath = Path.GetFullPath(destinationPath);
        backupPath = Path.GetFullPath(backupPath);

        if (!File.Exists(tempPath))
            throw new FileNotFoundException("Atomic replacement source does not exist.", tempPath);

        if (!File.Exists(destinationPath))
        {
            File.Move(tempPath, destinationPath);
            return;
        }

        try
        {
            File.Replace(tempPath, destinationPath, backupPath, ignoreMetadataErrors: true);
            return;
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException) when (!OperatingSystem.IsWindows())
        {
            // Some Unix filesystems/runtime combinations do not implement File.Replace.
            // Fall back to same-filesystem overwrite while preserving the previous file.
        }

        File.Copy(destinationPath, backupPath, overwrite: true);
        File.Move(tempPath, destinationPath, overwrite: true);
    }
}

public static class PrivateFilePermissions
{
    private const UnixFileMode PrivateDirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    public static void TryHardenDirectory(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        TrySetMode(path, PrivateDirectoryMode);
    }

    public static void TryHardenFile(string path)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(path)) return;
        TrySetMode(path, PrivateFileMode);
    }

    private static void TrySetMode(string path, UnixFileMode mode)
    {
        try
        {
            File.SetUnixFileMode(path, mode);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException or IOException)
        {
            // Permission hardening is best effort on filesystems that do not expose Unix modes.
        }
    }
}
