using System.Text.Json;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;

namespace GhostFTP.Core.Services;

public sealed class ProfileStore
{
    private const long MaxProfileFileBytes = 8L * 1024 * 1024;
    private const int MaxProfiles = 2048;
    private const int MaxProfileNameChars = 128;
    private const int MaxUsernameChars = 512;
    private const int MaxProtectedPasswordChars = 65_536;

    private readonly string _filePath;
    private readonly ISecretProtector _secretProtector;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProfileStore(string filePath, ISecretProtector secretProtector)
    {
        _filePath = Path.GetFullPath(filePath);
        _secretProtector = secretProtector ?? throw new ArgumentNullException(nameof(secretProtector));
    }

    public async Task<IReadOnlyList<ServerProfile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return [CreateDemoProfile()];

            try
            {
                return await ReadProfilesAsync(_filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException)
            {
                var backup = _filePath + ".bak";
                if (!File.Exists(backup))
                    throw;
                return await ReadProfilesAsync(backup, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IEnumerable<ServerProfile> profiles, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var list = new List<ServerProfile>();
            foreach (var profile in profiles)
            {
                if (profile is null)
                    throw new InvalidDataException("Saved profile collection contains an invalid null entry.");

                // Session-only Quick Connect entries are deliberately memory-only. Filtering
                // happens here in addition to JsonIgnore on the flag so future callers cannot
                // accidentally persist the connection definition when saving the collection.
                if (profile.IsSessionOnly)
                    continue;

                list.Add(profile.Clone());
                if (list.Count > MaxProfiles)
                    throw new InvalidOperationException($"Too many saved server profiles. Maximum: {MaxProfiles:N0}.");
            }

            Normalize(list);
            EnsureDemo(list);
            var directory = Path.GetDirectoryName(_filePath) ?? throw new InvalidOperationException("Profile path has no parent directory.");
            Directory.CreateDirectory(directory);
            PrivateFilePermissions.TryHardenDirectory(directory);

            var temp = _filePath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                await using (var stream = new FileStream(
                    temp,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    32 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(stream, list, JsonOptions, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                if (new FileInfo(temp).Length > MaxProfileFileBytes)
                    throw new InvalidDataException("Saved profile data exceeds the supported size limit.");

                PrivateFilePermissions.TryHardenFile(temp);
                AtomicFile.Replace(temp, _filePath, _filePath + ".bak");
                PrivateFilePermissions.TryHardenFile(_filePath);
                if (File.Exists(_filePath + ".bak"))
                    PrivateFilePermissions.TryHardenFile(_filePath + ".bak");
            }
            finally
            {
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch
                {
                    // Best-effort temporary-file cleanup.
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void SetPassword(ServerProfile profile, string? password)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.RememberPassword || string.IsNullOrEmpty(password))
        {
            profile.ProtectedPassword = null;
            return;
        }

        password = InputGuard.CommandArgument(password, nameof(password));
        profile.ProtectedPassword = _secretProtector.Protect(password);
        if (profile.ProtectedPassword.Length > MaxProtectedPasswordChars)
        {
            profile.ProtectedPassword = null;
            throw new InvalidDataException("Protected password data exceeds the supported size limit.");
        }
    }

    public string GetPassword(ServerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.RememberPassword || string.IsNullOrWhiteSpace(profile.ProtectedPassword))
            return string.Empty;
        if (profile.ProtectedPassword.Length > MaxProtectedPasswordChars)
            return string.Empty;

        try
        {
            var plaintext = _secretProtector.Unprotect(profile.ProtectedPassword);
            return InputGuard.CommandArgument(plaintext, "saved password");
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<IReadOnlyList<ServerProfile>> ReadProfilesAsync(string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("Profile data file was not found.", path);
        if (info.Length > MaxProfileFileBytes)
            throw new InvalidDataException("Profile data exceeds the supported size limit.");

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            32 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var rawProfiles = await JsonSerializer.DeserializeAsync<List<ServerProfile?>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? [];
        if (rawProfiles.Count > MaxProfiles)
            throw new InvalidDataException($"Profile data contains more than {MaxProfiles:N0} entries.");

        var profiles = rawProfiles.Where(static profile => profile is not null).Select(static profile => profile!).ToList();
        Normalize(profiles);
        EnsureDemo(profiles);
        return profiles;
    }

    private static void Normalize(List<ServerProfile> profiles)
    {
        var seenIds = new HashSet<Guid>();
        var seenDemo = false;

        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            if (profile.Id == Guid.Empty || !seenIds.Add(profile.Id))
            {
                profile.Id = Guid.NewGuid();
                seenIds.Add(profile.Id);
            }

            if (!Enum.IsDefined(profile.Security))
                profile.Security = FtpSecurityMode.ExplicitTls;

            if (profile.IsDemo)
            {
                if (seenDemo)
                {
                    profiles.RemoveAt(i--);
                    continue;
                }

                seenDemo = true;
                ApplyCanonicalDemo(profile);
                continue;
            }

            profile.Name = NormalizeDisplayName(profile.Name);
            profile.Host = NormalizeHost(profile.Host);
            profile.Port = profile.Port is >= 1 and <= 65535
                ? profile.Port
                : profile.Security == FtpSecurityMode.ImplicitTls ? 990 : 21;
            profile.Username = NormalizeUsername(profile.Username);
            profile.InitialPath = NormalizeRemotePath(profile.InitialPath);

            if (!profile.RememberPassword || string.IsNullOrWhiteSpace(profile.ProtectedPassword) || profile.ProtectedPassword.Length > MaxProtectedPasswordChars)
                profile.ProtectedPassword = null;
        }
    }

    private static string NormalizeDisplayName(string? value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Unnamed server" : value.Trim();
        try { InputGuard.RejectControl(value, nameof(value)); }
        catch (ArgumentException) { return "Unnamed server"; }
        return value.Length <= MaxProfileNameChars ? value : value[..MaxProfileNameChars];
    }

    private static string NormalizeHost(string? value)
    {
        try { return InputGuard.Host(value ?? string.Empty); }
        catch (ArgumentException) { return string.Empty; }
    }

    private static string NormalizeUsername(string? value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length > MaxUsernameChars)
            value = value[..MaxUsernameChars];
        try { return InputGuard.CommandArgument(value, nameof(value)); }
        catch (ArgumentException) { return string.Empty; }
    }

    private static string NormalizeRemotePath(string? value)
    {
        try { return InputGuard.RemotePath(value ?? "/"); }
        catch (ArgumentException) { return "/"; }
    }

    private static void ApplyCanonicalDemo(ServerProfile profile)
    {
        profile.Name = "GhostFTP Demo";
        profile.Host = "demo.ghostftp.local";
        profile.Port = 21;
        profile.Username = "demo";
        profile.Security = FtpSecurityMode.Plain;
        profile.InitialPath = "/";
        profile.IsDemo = true;
        profile.IsSessionOnly = false;
        profile.RememberPassword = false;
        profile.ProtectedPassword = null;
    }

    private static void EnsureDemo(List<ServerProfile> profiles)
    {
        if (!profiles.Any(x => x.IsDemo))
            profiles.Insert(0, CreateDemoProfile());
    }

    private static ServerProfile CreateDemoProfile() => new()
    {
        Name = "GhostFTP Demo",
        Host = "demo.ghostftp.local",
        Port = 21,
        Username = "demo",
        Security = FtpSecurityMode.Plain,
        InitialPath = "/",
        IsDemo = true,
        IsSessionOnly = false,
        RememberPassword = false
    };
}
