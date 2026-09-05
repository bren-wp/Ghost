using GhostFTP.Core.Protocol;
using System.Windows;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private CancellationTokenSource? _keepAliveCts;
    private Task? _keepAliveTask;

    private void ConfigureKeepAliveLoop()
    {
        Loaded += (_, _) => StartKeepAliveLoop();
        Closing += (_, _) => StopKeepAliveLoop();
    }

    private void StartKeepAliveLoop()
    {
        if (_keepAliveTask is { IsCompleted: false })
            return;

        _keepAliveCts?.Dispose();
        _keepAliveCts = new CancellationTokenSource();
        _keepAliveTask = RunKeepAliveLoopAsync(_keepAliveCts.Token);
    }

    private void StopKeepAliveLoop()
    {
        try { _keepAliveCts?.Cancel(); } catch { }
    }

    private async Task RunKeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var configuredSeconds = _settings.KeepAliveSeconds;
                var delaySeconds = configuredSeconds == 0 ? 15 : Math.Clamp(configuredSeconds, 15, 600);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);

                if (configuredSeconds == 0 || _busy)
                    continue;

                var session = _session;
                if (session is null || !session.IsConnected || session is DemoFtpSession)
                    continue;

                try
                {
                    await session.KeepAliveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() => HandleKeepAliveFailure(session, ex));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            // Keepalive is a resilience feature, never a reason to crash the application.
        }
    }

    private void HandleKeepAliveFailure(IFtpSession failedSession, Exception exception)
    {
        // Ignore a late failure from a session that has already been replaced by a reconnect.
        if (!ReferenceEquals(_session, failedSession))
            return;

        _remoteAll.Clear();
        _remoteItems.Clear();
        _remotePath = "/";
        _remotePathBox.Text = "/";
        SetStatus("Connection lost", "Danger");
        _statusBadge.ToolTip = $"FTP control connection was lost. Reconnect to continue.\n\n{exception.Message}";
        UpdateConnectionUi();
        UpdatePaneSummaries();
    }
}
