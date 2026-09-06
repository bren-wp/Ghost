using System.Globalization;
using System.Text.RegularExpressions;
using GhostFTP.Core.Models;

namespace GhostFTP.Core.Protocol;

public static partial class FtpListingParser
{
    private const int MaxListingLineChars = 64 * 1024;
    private const int MaxMlsdFactsPerEntry = 64;

    [GeneratedRegex(@"^(?<perm>[bcdlps-][rwxstST-]{9})\s+\d+\s+\S+\s+\S+\s+(?<size>\d+)\s+(?<month>[A-Za-z]{3})\s+(?<day>\d{1,2})\s+(?<timeyear>\d{2}:\d{2}|\d{4})\s+(?<name>.+)$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex UnixRegex();

    [GeneratedRegex(@"^(?<date>\d{2}-\d{2}-\d{2})\s+(?<time>\d{2}:\d{2}(?:AM|PM))\s+(?<size><DIR>|\d+)\s+(?<name>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex WindowsRegex();

    public static IReadOnlyList<FtpEntry> ParseMlsd(string text, string parentPath)
    {
        var list = new List<FtpEntry>();
        foreach (var rawLine in EnumerateLines(text))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0 || line.Length > MaxListingLineChars)
                continue;

            var separator = line.IndexOf(' ');
            if (separator <= 0 || separator == line.Length - 1)
                continue;

            var factsText = line[..separator];
            var name = line[(separator + 1)..].Trim();
            if (!TrySafeRemoteName(name, out name))
                continue;

            var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var factCount = 0;
            var tooManyFacts = false;
            foreach (var fact in factsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (++factCount > MaxMlsdFactsPerEntry)
                {
                    tooManyFacts = true;
                    break;
                }

                var equals = fact.IndexOf('=');
                if (equals > 0)
                    facts[fact[..equals]] = fact[(equals + 1)..];
            }

            if (tooManyFacts || !facts.TryGetValue("type", out var type))
                continue;
            if (type.Equals("cdir", StringComparison.OrdinalIgnoreCase) || type.Equals("pdir", StringComparison.OrdinalIgnoreCase))
                continue;

            var isDirectory = type.Equals("dir", StringComparison.OrdinalIgnoreCase);
            long size = 0;
            if (facts.TryGetValue("size", out var sizeText))
                long.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out size);

            DateTimeOffset? modified = null;
            if (facts.TryGetValue("modify", out var modifyText) &&
                DateTime.TryParseExact(modifyText.TrimEnd('Z'), "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var modifiedDate))
            {
                modified = new DateTimeOffset(DateTime.SpecifyKind(modifiedDate, DateTimeKind.Utc));
            }

            facts.TryGetValue("unix.mode", out var permissions);
            list.Add(new FtpEntry(name, CombineRemote(parentPath, name), isDirectory, isDirectory ? 0 : size, modified, permissions, rawLine));
        }
        return list;
    }

    public static IReadOnlyList<FtpEntry> ParseList(string text, string parentPath, DateTimeOffset nowUtc)
    {
        var list = new List<FtpEntry>();
        foreach (var rawLine in EnumerateLines(text))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0 || line.Length > MaxListingLineChars || line.StartsWith("total ", StringComparison.OrdinalIgnoreCase))
                continue;

            var unix = UnixRegex().Match(line);
            if (unix.Success)
            {
                var permissions = unix.Groups["perm"].Value;
                var name = unix.Groups["name"].Value;

                // A LIST symlink target is server metadata, not part of the entry name. Strip it
                // before validating the link name so an absolute target cannot make a safe link vanish.
                if (permissions.StartsWith('l'))
                {
                    var arrow = name.IndexOf(" -> ", StringComparison.Ordinal);
                    if (arrow > 0)
                        name = name[..arrow];
                }

                if (!TrySafeRemoteName(name, out name))
                    continue;

                var isDirectory = permissions.StartsWith('d');
                _ = long.TryParse(unix.Groups["size"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var size);
                var modified = ParseUnixDate(unix.Groups["month"].Value, unix.Groups["day"].Value, unix.Groups["timeyear"].Value, nowUtc);
                list.Add(new FtpEntry(name, CombineRemote(parentPath, name), isDirectory, isDirectory ? 0 : size, modified, permissions, rawLine));
                continue;
            }

            var windows = WindowsRegex().Match(line);
            if (!windows.Success)
                continue;

            var windowsName = windows.Groups["name"].Value;
            if (!TrySafeRemoteName(windowsName, out windowsName))
                continue;
            var sizeToken = windows.Groups["size"].Value;
            var windowsDirectory = sizeToken.Equals("<DIR>", StringComparison.OrdinalIgnoreCase);
            long windowsSize = 0;
            if (!windowsDirectory)
                long.TryParse(sizeToken, NumberStyles.None, CultureInfo.InvariantCulture, out windowsSize);

            DateTimeOffset? windowsModified = null;
            if (DateTime.TryParseExact(
                windows.Groups["date"].Value + " " + windows.Groups["time"].Value,
                "MM-dd-yy hh:mmtt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsedWindowsDate))
            {
                windowsModified = new DateTimeOffset(parsedWindowsDate).ToUniversalTime();
            }

            list.Add(new FtpEntry(windowsName, CombineRemote(parentPath, windowsName), windowsDirectory, windowsDirectory ? 0 : windowsSize, windowsModified, null, rawLine));
        }
        return list;
    }

    public static string CombineRemote(string parentPath, string name)
    {
        parentPath = InputGuard.RemotePath(parentPath);
        InputGuard.CommandArgument(name, nameof(name));
        var combined = parentPath == "/" ? "/" + name.TrimStart('/') : parentPath.TrimEnd('/') + "/" + name.TrimStart('/');
        return InputGuard.RemotePath(combined);
    }

    public static string ParentRemote(string path)
    {
        path = InputGuard.RemotePath(path);
        if (path == "/")
            return "/";
        var index = path.TrimEnd('/').LastIndexOf('/');
        return index <= 0 ? "/" : path[..index];
    }

    private static DateTimeOffset? ParseUnixDate(string month, string day, string timeOrYear, DateTimeOffset nowUtc)
    {
        if (!int.TryParse(day, NumberStyles.None, CultureInfo.InvariantCulture, out var dayValue))
            return null;
        if (!DateTime.TryParseExact(month, "MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthDate))
            return null;

        if (timeOrYear.Contains(':'))
        {
            var parts = timeOrYear.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute))
                return null;
            try
            {
                var candidate = new DateTimeOffset(nowUtc.Year, monthDate.Month, dayValue, hour, minute, 0, TimeSpan.Zero);
                if (candidate > nowUtc.AddDays(2))
                    candidate = candidate.AddYears(-1);
                return candidate;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        if (!int.TryParse(timeOrYear, NumberStyles.None, CultureInfo.InvariantCulture, out var year))
            return null;
        try
        {
            return new DateTimeOffset(year, monthDate.Month, dayValue, 0, 0, 0, TimeSpan.Zero);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static bool TrySafeRemoteName(string value, out string safe)
    {
        try
        {
            safe = InputGuard.RemoteName(value);
            return true;
        }
        catch (ArgumentException)
        {
            safe = string.Empty;
            return false;
        }
    }

    private static IEnumerable<string> EnumerateLines(string text)
    {
        using var reader = new StringReader(text ?? string.Empty);
        while (reader.ReadLine() is { } line)
            yield return line;
    }
}
