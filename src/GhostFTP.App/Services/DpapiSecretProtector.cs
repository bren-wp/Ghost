using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using GhostFTP.Core.Services;

namespace GhostFTP.Services;

/// <summary>
/// Windows saved-secret protection implemented directly through CryptProtectData/CryptUnprotectData.
/// This is the native DPAPI equivalent of the managed ProtectedData API with
/// DataProtectionScope.CurrentUser semantics, without adding a NuGet/package dependency.
/// Plaintext buffers are explicitly zeroed before their unmanaged storage is released.
/// </summary>
public sealed class DpapiSecretProtector : ISecretProtector
{
    private const int CryptProtectUiForbidden = 0x1;

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var input = CreateBlob(bytes);
        try
        {
            if (!CryptProtectData(
                    ref input,
                    "GhostFTP saved credential",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out var output))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Windows could not protect the saved FTP password.");
            }

            try
            {
                var encrypted = new byte[output.cbData];
                Marshal.Copy(output.pbData, encrypted, 0, output.cbData);
                return Convert.ToBase64String(encrypted);
            }
            finally
            {
                SecureLocalFree(output);
            }
        }
        finally
        {
            SecureHGlobalFree(input);
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public string Unprotect(string protectedText)
    {
        ArgumentNullException.ThrowIfNull(protectedText);
        var encrypted = Convert.FromBase64String(protectedText);
        var input = CreateBlob(encrypted);
        try
        {
            if (!CryptUnprotectData(
                    ref input,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out var output))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Windows could not decrypt the saved FTP password for this user.");
            }

            try
            {
                var plaintext = new byte[output.cbData];
                Marshal.Copy(output.pbData, plaintext, 0, output.cbData);
                try
                {
                    return Encoding.UTF8.GetString(plaintext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
            finally
            {
                SecureLocalFree(output);
            }
        }
        finally
        {
            SecureHGlobalFree(input);
            CryptographicOperations.ZeroMemory(encrypted);
        }
    }

    private static DataBlob CreateBlob(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(Math.Max(1, bytes.Length));
        if (bytes.Length > 0)
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
        return new DataBlob { cbData = bytes.Length, pbData = pointer };
    }

    private static void SecureHGlobalFree(DataBlob blob)
    {
        if (blob.pbData == IntPtr.Zero)
            return;

        SecureZero(blob.pbData, blob.cbData);
        Marshal.FreeHGlobal(blob.pbData);
    }

    private static void SecureLocalFree(DataBlob blob)
    {
        if (blob.pbData == IntPtr.Zero)
            return;

        SecureZero(blob.pbData, blob.cbData);
        _ = LocalFree(blob.pbData);
    }

    private static void SecureZero(IntPtr pointer, int length)
    {
        if (pointer == IntPtr.Zero || length <= 0)
            return;

        _ = RtlSecureZeroMemory(pointer, (nuint)length);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("kernel32.dll", EntryPoint = "RtlSecureZeroMemory")]
    private static extern IntPtr RtlSecureZeroMemory(IntPtr destination, nuint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
