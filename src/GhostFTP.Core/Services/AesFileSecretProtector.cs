using System.Security.Cryptography;
using System.Text;

namespace GhostFTP.Core.Services;

/// <summary>
/// Dependency-free local secret protection intended for platforms where Windows DPAPI is unavailable.
/// The random AES-256 key is stored in a user-private file (0600 on Unix where supported).
/// This protects persisted credentials from casual/plaintext disclosure, but it is not a substitute
/// for an OS keyring against an attacker who already controls the same user account.
/// </summary>
public sealed class AesFileSecretProtector : ISecretProtector
{
    private const byte FormatVersion = 1;
    private const int KeyBytes = 32;
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const int MaxProtectedPayloadBytes = 1024 * 1024;
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("GhostFTP.profile-password.v1");

    private readonly byte[] _key;

    public AesFileSecretProtector(string keyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPath);
        keyPath = Path.GetFullPath(keyPath);
        _key = LoadOrCreateKey(keyPath);
    }

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        if (plaintextBytes.Length > MaxProtectedPayloadBytes)
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            throw new InvalidDataException("Secret exceeds the supported size limit.");
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagBytes];
        try
        {
            using var aes = new AesGcm(_key, TagBytes);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, AssociatedData);

            var payload = new byte[1 + NonceBytes + TagBytes + ciphertext.Length];
            payload[0] = FormatVersion;
            nonce.CopyTo(payload, 1);
            tag.CopyTo(payload, 1 + NonceBytes);
            ciphertext.CopyTo(payload, 1 + NonceBytes + TagBytes);
            try
            {
                return Convert.ToBase64String(payload);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(payload);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    public string Unprotect(string protectedText)
    {
        ArgumentNullException.ThrowIfNull(protectedText);
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(protectedText);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Protected secret is not valid Base64 data.", ex);
        }

        try
        {
            if (payload.Length < 1 + NonceBytes + TagBytes || payload.Length > MaxProtectedPayloadBytes + 1 + NonceBytes + TagBytes)
                throw new CryptographicException("Protected secret has an invalid length.");
            if (payload[0] != FormatVersion)
                throw new CryptographicException("Protected secret uses an unsupported format version.");

            var nonce = payload.AsSpan(1, NonceBytes);
            var tag = payload.AsSpan(1 + NonceBytes, TagBytes);
            var ciphertext = payload.AsSpan(1 + NonceBytes + TagBytes);
            var plaintext = new byte[ciphertext.Length];
            try
            {
                using var aes = new AesGcm(_key, TagBytes);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);
                return Encoding.UTF8.GetString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
        }
    }

    private static byte[] LoadOrCreateKey(string keyPath)
    {
        var directory = Path.GetDirectoryName(keyPath) ?? throw new InvalidOperationException("Secret key path has no parent directory.");
        Directory.CreateDirectory(directory);
        PrivateFilePermissions.TryHardenDirectory(directory);

        if (File.Exists(keyPath))
            return ReadKey(keyPath);

        var key = RandomNumberGenerator.GetBytes(KeyBytes);
        var temp = keyPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(key);
                stream.Flush(flushToDisk: true);
            }
            PrivateFilePermissions.TryHardenFile(temp);

            try
            {
                File.Move(temp, keyPath);
                PrivateFilePermissions.TryHardenFile(keyPath);
                return key;
            }
            catch (IOException) when (File.Exists(keyPath))
            {
                CryptographicOperations.ZeroMemory(key);
                return ReadKey(keyPath);
            }
        }
        catch
        {
            CryptographicOperations.ZeroMemory(key);
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
            catch
            {
            }
        }
    }

    private static byte[] ReadKey(string keyPath)
    {
        var info = new FileInfo(keyPath);
        if (info.Length != KeyBytes)
            throw new InvalidDataException("Ghost FTP local credential key has an invalid length.");

        var key = File.ReadAllBytes(keyPath);
        if (key.Length != KeyBytes)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new InvalidDataException("Ghost FTP local credential key is invalid.");
        }
        PrivateFilePermissions.TryHardenFile(keyPath);
        return key;
    }
}
