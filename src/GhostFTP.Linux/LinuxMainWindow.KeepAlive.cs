using GhostFTP.Core.Protocol;

namespace GhostFTP.Linux;

internal sealed partial class LinuxMainWindow
{
    private int _keepAliveLoopStarted;

    private void EnsureKeepAliveLoopStarted()
    {
        if (Interlocked.Exchange(ref _keepAliveLoopStarted, 1) != 0)
            return;

        _ = Task.Run(KeepAliveLoopAsync);
    }

    private async Task KeepAliveLoopAsync()
    {
        while (!_closing)
        {
            var seconds = _settings.KeepAliveSeconds;
            var delay = seconds <= 0 ? TimeSpan.FromSeconds(3) : TimeSpan.FromSeconds(seconds);
            try
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            if (_closing)
                return;

            seconds = _settings.KeepAliveSeconds;
            if (seconds <= 0 || _busy || !_connected)
                continue;

            var session = _session;
            if (session is null || !session.IsConnected || session is DemoFtpSession)
                continue;

            try
            {
                await session.KeepAliveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Post(() =>
                {
                    if (!ReferenceEquals(_session, session))
                        return;

                    _connected = false;
                    _activeOptions = null;
                    _remoteItems.Clear();
                    _remoteSelected = -1;
                    _status = "Connection lost";
                    Log("Server keepalive failed; the browser connection was marked offline: " + ex.Message);
                });
            }
        }
    }
}
