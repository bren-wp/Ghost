namespace GhostFTP.Core.Protocol;

public static class LocalPathSafety
{
    private static readonly HashSet<string> WindowsReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string SafeFileName(string remoteName)
    {
        InputGuard.CommandArgument(remoteName, nameof(remoteName));
        var invalid = Path.GetInvalidFileNameChars();
        var chars = remoteName.Select(ch => invalid.Contains(ch) || char.IsControl(ch) ? '_' : ch).ToArray();
        var result = new string(chars).Trim();

        if (OperatingSystem.IsWindows())
            result = result.TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(result) || result is "." or "..")
            result = "unnamed";

        if (OperatingSystem.IsWindows())
        {
            var stem = Path.GetFileNameWithoutExtension(result);
            if (WindowsReservedNames.Contains(stem))
                result = "_" + result;
        }

        return result;
    }

    public static string CombineUnderRoot(string root, string childName)
    {
        var rootFull = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine(rootFull, SafeFileName(childName)));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var relative = Path.GetRelativePath(rootFull, candidate);

        if (Path.IsPathRooted(relative)
            || string.Equals(relative, "..", comparison)
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, comparison)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, comparison))
        {
            throw new IOException("Resolved path escapes the selected local directory.");
        }

        return candidate;
    }
}
