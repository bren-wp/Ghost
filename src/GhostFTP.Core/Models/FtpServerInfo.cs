namespace GhostFTP.Core.Models;

public sealed record FtpServerInfo(
    string Host,
    bool IsEncrypted,
    string WorkingDirectory,
    string ServerSystem,
    IReadOnlyList<string> Features,
    DateTimeOffset CheckedUtc);
