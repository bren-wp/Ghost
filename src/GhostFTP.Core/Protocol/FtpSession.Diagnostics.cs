using GhostFTP.Core.Models;

namespace GhostFTP.Core.Protocol;

public sealed partial class FtpSession
{
    public Task<FtpServerInfo> GetServerInfoAsync(CancellationToken cancellationToken = default) =>
        LockedAsync(async ct =>
        {
            var noop = await SendCommandAsync("NOOP", ct).ConfigureAwait(false);
            Ensure(noop, 200, 299, "FTP health check failed.");

            var systemReply = await TryCommandAsync("SYST", ct).ConfigureAwait(false);
            var workingDirectory = await GetWorkingDirectoryCoreAsync(ct).ConfigureAwait(false);
            var serverSystem = systemReply is not null && systemReply.IsPositiveCompletion
                ? systemReply.Message.Trim()
                : "Unavailable";

            return new FtpServerInfo(
                Host,
                IsEncrypted,
                workingDirectory,
                serverSystem,
                _features.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
                DateTimeOffset.UtcNow);
        }, cancellationToken);
}
