namespace GhostFTP.Core.Protocol;

public sealed partial class FtpSession
{
    private async Task EnsureDirectoryCreatedOrExistingAsync(
        string remotePath,
        FtpReply mkdirReply,
        CancellationToken cancellationToken)
    {
        if (mkdirReply.IsPositiveCompletion)
            return;

        if (mkdirReply.Code != 550)
            throw CreateReplyException(mkdirReply, "Unable to create the remote directory.");

        // 550 is ambiguous: it can mean "already exists", but also permission denied,
        // unavailable path, quota or another server-side failure. Verify the path is
        // actually an accessible directory before treating MKD 550 as success.
        var originalDirectory = await GetWorkingDirectoryCoreAsync(cancellationToken).ConfigureAwait(false);
        var verify = await SendCommandAsync("CWD " + remotePath, cancellationToken).ConfigureAwait(false);
        if (!verify.IsPositiveCompletion)
            throw CreateReplyException(mkdirReply, "Unable to create the remote directory and the path is not an accessible existing directory.");

        var restore = await SendCommandAsync("CWD " + originalDirectory, cancellationToken).ConfigureAwait(false);
        if (!restore.IsPositiveCompletion)
        {
            try
            {
                WorkingDirectory = await GetWorkingDirectoryCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                WorkingDirectory = remotePath;
            }
            throw CreateReplyException(restore, "The directory exists, but Ghost FTP could not restore the previous working directory.");
        }

        WorkingDirectory = originalDirectory;
    }
}
