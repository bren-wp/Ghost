using GhostFTP.Design;
using System.Text.Json;

namespace GhostFTP.Services;

public enum AppTheme
{
    System,
    Dark,
    Light
}

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string LanguageCode { get; set; } = GhostLocalization.DefaultLanguageCode;
    public string LastLocalDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public bool ConfirmDeletes { get; set; } = true;
    public bool ShowHiddenFiles { get; set; }
    public int AutomaticTransferRetries { get; set; } = 2;
    public int ConnectTimeoutSeconds { get; set; } = 15;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int TransferIdleTimeoutSeconds { get; set; } = 120;
}

public sealed class AppSettingsStore
{
    private const long MaxSettingsFileBytes = 1024 * 1024;

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettingsStore(string path) => _path = Path.GetFullPath(path);

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path)) return new AppSettings();
            try
            {
                return await ReadAsync(_path, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                var backup = _path + ".bak";
                return File.Exists(backup)
                    ? await ReadAsync(backup, cancellationToken).ConfigureAwait(false)
                    : new AppSettings();
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
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
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

                if (File.Exists(_path))
                    File.Replace(temp, _path, _path + ".bak", ignoreMetadataErrors: true);
                else
                    File.Move(temp, _path);
            }
            finally
            {
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
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

        settings.LanguageCode = GhostLocalization.NormalizeLanguageCode(settings.LanguageCode);

        if (!Directory.Exists(settings.LastLocalDirectory))
            settings.LastLocalDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        settings.AutomaticTransferRetries = Math.Clamp(settings.AutomaticTransferRetries, 0, 5);
        settings.ConnectTimeoutSeconds = Math.Clamp(settings.ConnectTimeoutSeconds, 3, 120);
        settings.CommandTimeoutSeconds = Math.Clamp(settings.CommandTimeoutSeconds, 5, 300);
        settings.TransferIdleTimeoutSeconds = Math.Clamp(settings.TransferIdleTimeoutSeconds, 15, 3600);
    }
}
