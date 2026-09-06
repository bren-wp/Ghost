using System.Globalization;
using System.Text.Json;
using GhostFTP.Core.Models;

namespace GhostFTP.Core.Protocol;

public sealed partial class FtpSession
{
    private const int DownloadResumeMetadataVersion = 1;
    private const long MaxDownloadResumeMetadataBytes = 16 * 1024;
    private static readonly string[] MdtmFormats = ["yyyyMMddHHmmss", "yyyyMMddHHmmss.FFFFFFF"];
    private static readonly JsonSerializerOptions ResumeMetadataJson = new(JsonSerializerDefaults.Web);

    private async Task DownloadFileWithResumeIntegrityCoreAsync(
        string remotePath,
        string localPath,
        IProgress<(long transferred, long? total)>? progress,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var partPath = localPath + ".ghostftp.part";
        var metadataPath = partPath + ".meta";
        var remoteSize = await TryGetFileSizeAsync(remotePath, cancellationToken).ConfigureAwait(false);
        var remoteModified = await TryGetFileModifiedUtcAsync(remotePath, cancellationToken).ConfigureAwait(false);
        var canIdentifyRemote = remoteSize is not null && remoteModified is not null;

        if (File.Exists(partPath))
        {
            var partLength = new FileInfo(partPath).Length;
            var metadata = await TryLoadDownloadResumeMetadataAsync(metadataPath, cancellationToken).ConfigureAwait(false);
            var validResume = canIdentifyRemote
                && metadata is not null
                && partLength > 0
                && partLength <= remoteSize!.Value
                && metadata.Matches(
                    _options.Host,
                    _options.Port,
                    _options.Security,
                    remotePath,
                    remoteSize.Value,
                    remoteModified!.Value.UtcTicks);

            if (!validResume)
            {
                // A pre-0.1.6 partial file, a corrupt sidecar or a changed remote object must not be
                // appended blindly. Restart from zero instead of risking a mixed/corrupt local file.
                TryDeleteLocal(partPath);
                TryDeleteLocal(metadataPath);
            }
            else if (partLength == remoteSize!.Value)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? Directory.GetCurrentDirectory());
                File.Move(partPath, localPath, true);
                TryDeleteLocal(metadataPath);
                progress?.Report((remoteSize.Value, remoteSize.Value));
                return;
            }
        }
        else
        {
            TryDeleteLocal(metadataPath);
        }

        if (!File.Exists(partPath) && canIdentifyRemote)
        {
            var metadata = new DownloadResumeMetadata(
                DownloadResumeMetadataVersion,
                _options.Host,
                _options.Port,
                (int)_options.Security,
                remotePath,
                remoteSize!.Value,
                remoteModified!.Value.UtcTicks);
            await TrySaveDownloadResumeMetadataAsync(metadataPath, metadata, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await DownloadFileCoreAsync(remotePath, localPath, progress, cancellationToken).ConfigureAwait(false);

            if (canIdentifyRemote)
            {
                var postSize = await TryGetFileSizeAsync(remotePath, cancellationToken).ConfigureAwait(false);
                var postModified = await TryGetFileModifiedUtcAsync(remotePath, cancellationToken).ConfigureAwait(false);
                if (postSize != remoteSize
                    || postModified is null
                    || postModified.Value.UtcTicks != remoteModified!.Value.UtcTicks)
                {
                    // The server object changed while bytes were in flight. Do not leave a locally
                    // completed file that may combine different remote revisions.
                    TryDeleteLocal(localPath);
                    throw new IOException("Remote file changed while it was being downloaded. The local result was discarded; retry the transfer.");
                }
            }

            TryDeleteLocal(metadataPath);
        }
        catch
        {
            // Keep a validated partial and its sidecar for a future safe resume. If there is no
            // trustworthy identity sidecar, remove the partial so a later attempt cannot resume it.
            if (!File.Exists(partPath))
            {
                TryDeleteLocal(metadataPath);
            }
            else if (!canIdentifyRemote || !File.Exists(metadataPath))
            {
                TryDeleteLocal(partPath);
                TryDeleteLocal(metadataPath);
            }
            throw;
        }
    }

    private async Task DownloadDirectoryWithResumeIntegrityCoreAsync(
        string remotePath,
        string localDirectory,
        IProgress<(long transferred, long? total)>? progress,
        CancellationToken cancellationToken)
    {
        var plan = new List<(FtpEntry entry, string localPath)>();
        await BuildDownloadPlanAsync(remotePath, localDirectory, plan, 0, new TraversalBudget(), cancellationToken).ConfigureAwait(false);
        var total = SaturatingTotal(plan.Where(x => !x.entry.IsDirectory).Select(x => Math.Max(0, x.entry.Size)));
        long aggregate = 0;

        foreach (var item in plan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.entry.IsDirectory)
            {
                Directory.CreateDirectory(item.localPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(item.localPath)!);
            var baseAggregate = aggregate;
            var fileProgress = new Progress<(long transferred, long? total)>(p =>
                progress?.Report((SaturatingAdd(baseAggregate, p.transferred), total)));
            await DownloadFileWithResumeIntegrityCoreAsync(item.entry.FullPath, item.localPath, fileProgress, cancellationToken).ConfigureAwait(false);
            aggregate = SaturatingAdd(aggregate, Math.Max(0, item.entry.Size));
            progress?.Report((aggregate, total));
        }
    }

    private async Task<DateTimeOffset?> TryGetFileModifiedUtcAsync(string remotePath, CancellationToken cancellationToken)
    {
        var reply = await TryCommandAsync("MDTM " + remotePath, cancellationToken).ConfigureAwait(false);
        if (reply is null || !reply.IsPositiveCompletion)
            return null;

        var token = reply.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return DateTimeOffset.TryParseExact(
            token,
            MdtmFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var value)
            ? value
            : null;
    }

    private static async Task<DownloadResumeMetadata?> TryLoadDownloadResumeMetadataAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= 0 || info.Length > MaxDownloadResumeMetadataBytes)
                return null;

            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<DownloadResumeMetadata>(stream, ResumeMetadataJson, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static async Task TrySaveDownloadResumeMetadataAsync(
        string path,
        DownloadResumeMetadata metadata,
        CancellationToken cancellationToken)
    {
        var tempPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            var json = JsonSerializer.Serialize(metadata, ResumeMetadataJson);
            if (json.Length > MaxDownloadResumeMetadataBytes)
                return;

            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Resume metadata is an optimization/integrity aid. A local sidecar write failure must
            // not prevent a fresh download; the caller will remove any untrusted partial on failure.
        }
        finally
        {
            TryDeleteLocal(tempPath);
        }
    }

    private sealed record DownloadResumeMetadata(
        int Version,
        string Host,
        int Port,
        int Security,
        string RemotePath,
        long RemoteSize,
        long ModifiedUtcTicks)
    {
        public bool Matches(
            string host,
            int port,
            FtpSecurityMode security,
            string remotePath,
            long remoteSize,
            long modifiedUtcTicks) =>
            Version == DownloadResumeMetadataVersion
            && string.Equals(Host, host, StringComparison.OrdinalIgnoreCase)
            && Port == port
            && Security == (int)security
            && string.Equals(RemotePath, remotePath, StringComparison.Ordinal)
            && RemoteSize == remoteSize
            && ModifiedUtcTicks == modifiedUtcTicks;
    }
}
