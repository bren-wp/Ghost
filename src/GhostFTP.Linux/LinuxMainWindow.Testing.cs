namespace GhostFTP.Linux;

internal sealed partial class LinuxMainWindow
{
    internal void RequestSmokeTestShutdown()
    {
        Post(() => _closing = true);
    }
}
