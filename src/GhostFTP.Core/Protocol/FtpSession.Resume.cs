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

        Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? Directory.GetCurrentDirectory());
        var partPath = localPath + ".ghostftp.part";
        var metadataPath = partPath + ".meta";
        var remoteSize = await TryGetFileSizeAsync(remotePath, cancellationToken).ConfigureAwait(false);
        var remoteModified = await TryGetFileModifiedUtcAsync(remotePath, cancellationToken).ConfigureAwait(false);
        var canIdentifyRemote = remoteSize is not null && remoteModified is not null;
        long resumeOffset = 0;

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
                // Never fall back to the legacy length-only resume path. If an untrusted partial
                // cannot be removed, abort before issuing REST/RETR instead of risking stale bytes.
                DeleteLocalRequired(partPath, "Unable to remove an untrusted partial download.");
                DeleteLocalRequired(metadataPath, "Unable to remove stale download resume metadata.");
            }
            else if (partLength == remoteSize!.Value)
            {
                // A full-length partial is still staged data. Revalidate the server revision before
                // it is allowed to replace an existing destination.
                if (!await RemoteIdentityMatchesAsync(
                        remotePath,
                        remoteSize.Value,
                        remoteModified!.Value,
                        cancellationToken).ConfigureAwait(false))
                {
                    DeleteLocalRequired(partPath, "Unable to discard a stale completed partial download.");
                    DeleteLocalRequired(metadataPath, "Unable to discard stale download resume metadata.");
                    throw new IOException("Remote file changed before the staged download could be committed. The existing local destination was preserved.");
                }

                File.Move(partPath, localPath, true);
                TryDeleteLocal(metadataPath);
                progress?.Report((remoteSize.Value, remoteSize.Value));
                return;
            }
            else
            {
                resumeOffset = partLength;
            }
        }
        else if (File.Exists(metadataPath))
        {
            // A sidecar without its partial cannot authorize anything and must not survive into a
            // later attempt where it could accidentally describe unrelated staged bytes.
            DeleteLocalRequired(metadataPath, "Unable to remove orphaned download resume metadata.");
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
            if (resumeOffset > 0)
            {
                var rest = await SendCommandAsync(
                    "REST " + resumeOffset.ToString(CultureInfo.InvariantCulture),
                    cancellationToken).ConfigureAwait(false);
                if (!rest.IsPositiveIntermediate)
                {
                    // The remote revision is still trusted, but this server/session refuses REST.
                    // Restart from byte zero after proving the old staged bytes are gone.
                    DeleteLocalRequired(partPath, "Unable to restart a partial download after the server refused REST.");
                    resumeOffset = 0;
                }
            }

            await ReceiveDownloadIntoPartAsync(
                remotePath,
                partPath,
                resumeOffset,
                remoteSize,
                progress,
                cancellationToken).ConfigureAwait(false);

            if (canIdentifyRemote
                && !await RemoteIdentityMatchesAsync(
                    remotePath,
                    remoteSize!.Value,
                    remoteModified!.Value,
                    cancellationToken).ConfigureAwait(false))
            {
                // Keep any pre-existing destination untouched. Only staged bytes are discarded.
                DeleteLocalRequired(partPath, "Unable to discard a download whose remote revision changed in flight.");
                DeleteLocalRequired(metadataPath, "Unable to discard resume metadata for a changed remote revision.");
                throw new IOException("Remote file changed while it was being downloaded. The staged result was discarded and the existing local destination was preserved; retry the transfer.");
            }

            // Commit is the final step. Until this point localPath has not been replaced, so any
            // validation error or metadata probe failure cannot destroy the user's previous file.
            File.Move(partPath, localPath, true);
            TryDeleteLocal(metadataPath);
        }
        catch
        {
            // A validated partial may remain only when a trustworthy sidecar still exists. A fresh
            // download with no trustworthy identity must not leave bytes that a later run could resume.
            if (File.Exists(partPath) && (!canIdentifyRemote || !File.Exists(metadataPath)))
            {
                DeleteLocalRequired(partPath, "Unable to remove an unverified partial download after failure.");
                DeleteLocalRequired(metadataPath, "Unable to remove unverified download resume metadata after failure.");
            }
            else if (!File.Exists(partPath))
            {
                TryDeleteLocal(metadataPath);
            }
            throw;
        }
    }

    private async Task ReceiveDownloadIntoPartAsync(
        string remotePath,
        string partPath,
        long offset,
        long? expectedSize,
        IProgress<(long transferred, long? total)>? progress,
        CancellationToken cancellationToken)
    {
        await using (var output = new FileStream(
            partPath,
            offset > 0 ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await ReceiveDataToStreamAsync(
                "RETR " + remotePath,
                output,
                offset,
                expectedSize,
                progress,
                cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        if (expectedSize is not null)
        {
            var actual = new FileInfo(partPath).Length;
            if (actual != expectedSize.Value)
            {
                throw new IOException(
                    $"Download integrity check failed. Expected {expectedSize.Value:N0} bytes but received {actual:N0} bytes. The validated partial was kept for a safe resume.");
            }
        }
    }

    private async Task<bool> RemoteIdentityMatchesAsync(
        string remotePath,
        long expectedSize,
        DateTimeOffset expectedModified,
        CancellationToken cancellationToken)
    {
        var currentSize = await TryGetFileSizeAsync(remotePath, cancellationToken).ConfigureAwait(false);
        var currentModified = await TryGetFileModifiedUtcAsync(remotePath, cancellationToken).ConfigureAwait(false);
        return currentSize == expectedSize
            && currentModified is not null
            && currentModified.Value.UtcTicks == expectedModified.UtcTicks;
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
            // Resume metadata is an integrity aid. A write failure does not block a fresh transfer,
            // but any resulting partial will be removed on failure because no trusted sidecar exists.
        }
        finally
        {
            TryDeleteLocal(tempPath);
        }
    }

    private static void DeleteLocalRequired(string path, string message)
    {
        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new IOException(message + " Ghost FTP aborted before using untrusted staged data.", ex);
        }

        if (File.Exists(path))
            throw new IOException(message + " Ghost FTP aborted before using untrusted staged data.");
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
