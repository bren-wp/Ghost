using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Design;
using System.Windows;
using System.Windows.Media;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private void QueueUploadSelected()
    {
        if (!IsConnected || _queue is null || _localList.SelectedItems.Count == 0) return;
        try
        {
            foreach (var item in _localList.SelectedItems.OfType<LocalItem>())
                _queue.EnqueueUpload(item.FullPath, FtpListingParser.CombineRemote(_remotePath, item.Name), item.IsDirectory);
            UpdateQueueSummary();
            UpdateQueueManagementUi();
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not queue all selected uploads. Items queued before the error remain in the transfer list.", ex);
        }
    }

    private void QueueDownloadSelected()
    {
        if (!IsConnected || _queue is null || _remoteList.SelectedItems.Count == 0) return;
        try
        {
            foreach (var item in _remoteList.SelectedItems.OfType<RemoteItem>())
            {
                var destination = LocalPathSafety.CombineUnderRoot(_localPath, item.Name);
                _queue.EnqueueDownload(item.FullPath, destination, item.IsDirectory, item.IsDirectory ? null : item.Entry.Size);
            }
            UpdateQueueSummary();
            UpdateQueueManagementUi();
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not queue all selected downloads. Items queued before the error remain in the transfer list.", ex);
        }
    }

    private void ToggleQueuePause()
    {
        if (_queue is null)
            return;

        if (_queue.IsQueuePaused)
        {
            _queue.ResumeQueue();
            AppendConnectionLog("Transfer queue resumed. Queued transfers may start again.", "QUEUE");
        }
        else
        {
            _queue.PauseQueue();
            AppendConnectionLog("Transfer queue paused. Running transfers continue; new transfer dispatch waits until resumed.", "QUEUE");
        }

        UpdateQueueManagementUi();
        UpdateQueueSummary();
    }

    private void CancelSelectedTransfer()
    {
        if (_queue is null) return;
        var selected = _queueList.SelectedItems.OfType<TransferJob>().ToArray();
        foreach (var job in selected.Where(x => x.State is TransferState.Queued or TransferState.Running or TransferState.Retrying))
            _queue.Cancel(job.Id);
        UpdateQueueSummary();
    }

    private void CancelAllTransfers()
    {
        if (_queue is null) return;
        foreach (var job in _queue.Jobs.Where(x => x.State is TransferState.Queued or TransferState.Running or TransferState.Retrying).ToArray())
            _queue.Cancel(job.Id);
        UpdateQueueSummary();
    }

    private void RetrySelectedTransfers()
    {
        if (_queue is null) return;
        var retryable = _queueList.SelectedItems
            .OfType<TransferJob>()
            .Where(x => x.State is TransferState.Failed or TransferState.Cancelled)
            .ToArray();
        if (retryable.Length == 0) return;

        try
        {
            foreach (var job in retryable)
                RequeueTransfer(job);
            UpdateQueueSummary();
            UpdateQueueManagementUi();
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not retry all selected transfers.", ex);
        }
    }

    private void RetryAllFailedTransfers()
    {
        if (_queue is null)
            return;

        var failed = _queue.Jobs.Where(job => job.State == TransferState.Failed).ToArray();
        if (failed.Length == 0)
            return;

        try
        {
            foreach (var job in failed)
                RequeueTransfer(job);
            AppendConnectionLog($"Queued {failed.Length} failed transfer(s) for retry.", "QUEUE");
            UpdateQueueSummary();
            UpdateQueueManagementUi();
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not retry all failed transfers.", ex);
        }
    }

    private void RequeueTransfer(TransferJob job)
    {
        if (_queue is null)
            return;

        if (job.Direction == TransferDirection.Upload)
            _queue.EnqueueUpload(job.Source, job.Destination, job.IsDirectory);
        else
            _queue.EnqueueDownload(job.Source, job.Destination, job.IsDirectory, job.TotalBytes);
    }

    private void ClearCompletedTransfers()
    {
        _queue?.ClearCompleted();
        UpdateQueueSummary();
        UpdateQueueManagementUi();
    }

    private void ClearFailedTransfers()
    {
        _queue?.ClearFailed();
        UpdateQueueSummary();
        UpdateQueueManagementUi();
    }

    private void ClearCancelledTransfers()
    {
        _queue?.ClearCancelled();
        UpdateQueueSummary();
        UpdateQueueManagementUi();
    }

    private void ClearFinishedTransfers()
    {
        _queue?.ClearFinished();
        UpdateQueueSummary();
        UpdateQueueManagementUi();
    }

    private void CopySelectedTransferSource()
    {
        if (_queueList.SelectedItem is TransferJob job) CopyText(job.Source);
    }

    private void CopySelectedTransferDestination()
    {
        if (_queueList.SelectedItem is TransferJob job) CopyText(job.Destination);
    }

    private async void QueueJobUpdated(object? sender, TransferJob job)
    {
        UpdateQueueSummary();
        UpdateQueueManagementUi();
        if (job.State == TransferState.Completed && _completedHandled.Add(job.Id))
        {
            try
            {
                RefreshLocal();
                await RefreshRemoteAsync();
            }
            catch
            {
                // Transfer completion is already recorded; a follow-up refresh is best effort.
            }
        }
    }

    private void UpdateQueueManagementUi()
    {
        var paused = _queue?.IsQueuePaused == true;
        _queuePauseButton.Content = GhostTransferText.T(paused ? "ResumeQueue" : "PauseQueue");
        _queuePauseButton.ToolTip = paused
            ? "Resume dispatch of queued and retrying transfers."
            : GhostTransferText.T("RunningContinue");
        _queuePauseButton.IsEnabled = _queue is not null;
    }

    private void UpdateQueueSummary()
    {
        if (_queue is null)
        {
            _queueSummary.Text = L("NoTransfers");
            return;
        }

        if (_queue.Jobs.Count == 0)
        {
            _queueSummary.Text = _queue.IsQueuePaused
                ? $"{GhostTransferText.T("QueuePaused")} · {L("NoTransfers").ToLowerInvariant()}"
                : L("NoTransfers");
            return;
        }

        var running = _queue.Jobs.Count(x => x.State == TransferState.Running);
        var retrying = _queue.Jobs.Count(x => x.State == TransferState.Retrying);
        var queued = _queue.Jobs.Count(x => x.State == TransferState.Queued);
        var failed = _queue.Jobs.Count(x => x.State == TransferState.Failed);
        var cancelled = _queue.Jobs.Count(x => x.State == TransferState.Cancelled);
        var completed = _queue.Jobs.Count(x => x.State == TransferState.Completed);
        var aggregateSpeed = _queue.Jobs
            .Where(x => x.State == TransferState.Running)
            .Sum(x => Math.Max(0, x.SpeedBytesPerSecond));

        var parts = new List<string>();
        if (_queue.IsQueuePaused) parts.Add(GhostTransferText.T("QueuePaused"));
        if (running > 0) parts.Add($"{running} running");
        if (retrying > 0) parts.Add($"{retrying} retrying");
        if (queued > 0) parts.Add($"{queued} queued");
        if (aggregateSpeed >= 1)
            parts.Add($"{FormatBytes((long)Math.Min(long.MaxValue, aggregateSpeed))}/s total");
        if (failed > 0) parts.Add($"{failed} failed");
        if (cancelled > 0) parts.Add($"{cancelled} cancelled");
        if (completed > 0) parts.Add($"{completed} completed");
        _queueSummary.Text = string.Join(" · ", parts);
    }

    private async Task AddProfileAsync()
    {
        if (_profileStore is null) return;
        var profile = new ServerProfile
        {
            Id = Guid.NewGuid(),
            Name = "New server",
            Port = 21,
            Security = FtpSecurityMode.ExplicitTls,
            InitialPath = "/"
        };
        var dialog = new ProfileDialog(this, profile, string.Empty, isNew: true);
        if (dialog.ShowDialog() != true) return;

        var result = dialog.Result;
        _profileStore.SetPassword(result, dialog.Password);
        _profiles.Add(result);
        await SaveProfilesSafeAsync();
        _profilesList.SelectedItem = result;
    }

    private async Task EditSelectedProfileAsync()
    {
        if (_profileStore is null || _profilesList.SelectedItem is not ServerProfile selected || selected.IsDemo) return;
        var dialog = new ProfileDialog(this, selected, _profileStore.GetPassword(selected));
        if (dialog.ShowDialog() != true) return;

        var updated = dialog.Result;
        _profileStore.SetPassword(updated, dialog.Password);
        var index = _profiles.IndexOf(selected);
        _profiles[index] = updated;
        _profilesList.SelectedItem = updated;
        await SaveProfilesSafeAsync();
    }

    private async Task RemoveSelectedProfileAsync()
    {
        if (_profilesList.SelectedItem is not ServerProfile selected || selected.IsDemo) return;
        if (!GhostMessageDialog.Confirm(
                this,
                "Remove saved server?",
                $"Remove saved server '{selected.Name}' from this device?",
                L("Remove"),
                danger: true))
            return;

        _profiles.Remove(selected);
        await SaveProfilesSafeAsync();
    }

    private async Task SaveProfilesSafeAsync()
    {
        try
        {
            if (_profileStore is not null) await _profileStore.SaveAsync(_profiles);
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not save server profiles.", ex);
        }
    }

    private async Task OpenSettingsAsync()
    {
        var dialog = new SettingsDialog(this, _settings);
        if (dialog.ShowDialog() != true) return;

        var showHiddenChanged = _settings.ShowHiddenFiles != dialog.ShowHiddenFiles;
        var languageChanged = !string.Equals(_settings.LanguageCode, dialog.SelectedLanguageCode, StringComparison.OrdinalIgnoreCase);
        var themeChanged = _settings.Theme != dialog.SelectedTheme;
        var queueRestartNeeded = _settings.AutomaticTransferRetries != dialog.AutomaticTransferRetries
            || _settings.ConcurrentTransfers != dialog.ConcurrentTransfers;
        var connectionBehaviorChanged = _settings.ConnectTimeoutSeconds != dialog.ConnectTimeoutSeconds
            || _settings.CommandTimeoutSeconds != dialog.CommandTimeoutSeconds
            || _settings.TransferIdleTimeoutSeconds != dialog.TransferIdleTimeoutSeconds;

        _settings.Theme = dialog.SelectedTheme;
        _settings.LanguageCode = dialog.SelectedLanguageCode;
        _settings.ConfirmDeletes = dialog.ConfirmDeletes;
        _settings.ShowHiddenFiles = dialog.ShowHiddenFiles;
        _settings.AutomaticTransferRetries = dialog.AutomaticTransferRetries;
        _settings.ConcurrentTransfers = dialog.ConcurrentTransfers;
        _settings.ConnectTimeoutSeconds = dialog.ConnectTimeoutSeconds;
        _settings.CommandTimeoutSeconds = dialog.CommandTimeoutSeconds;
        _settings.TransferIdleTimeoutSeconds = dialog.TransferIdleTimeoutSeconds;
        _settings.KeepAliveSeconds = dialog.KeepAliveSeconds;

        if (_settingsStore is not null) await _settingsStore.SaveAsync(_settings);
        if (showHiddenChanged) RefreshLocal();

        var restartNeeded = languageChanged || themeChanged || queueRestartNeeded;
        GhostMessageDialog.Information(
            this,
            L("Settings"),
            restartNeeded
                ? "Settings were saved. File-view and keepalive changes apply immediately. Language, appearance, automatic retry and concurrent-transfer changes apply after Ghost FTP restarts. Connection timeout changes apply to the next connection."
                : connectionBehaviorChanged
                    ? "Settings were saved. Keepalive and file-view changes apply immediately; connection timeout changes apply to the next connection."
                    : "Settings were saved. Changes apply immediately.");
    }

    private void SetStatus(string text, string brushKey)
    {
        _statusText.Text = text;
        _statusText.Foreground = brushKey is "Success" or "Danger" or "Warning"
            ? Brushes.White
            : GhostTheme.R("Text");
        _statusBadge.Background = GhostTheme.R(brushKey);
    }

    private void UpdateConnectionUi()
    {
        var connected = IsConnected;
        _connectButton.IsEnabled = !connected && !_busy;
        _disconnectButton.IsEnabled = connected && !_busy;
        _connectButton.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
        _disconnectButton.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;

        _host.IsEnabled = !connected && !_busy;
        _port.IsEnabled = !connected && !_busy;
        _username.IsEnabled = !connected && !_busy;
        _password.IsEnabled = !connected && !_busy;
        _security.IsEnabled = !connected && !_busy;
        _remotePathBox.IsEnabled = connected;
    }

    private bool IsConnected => _session?.IsConnected == true;
}
