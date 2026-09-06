using GhostFTP.Core.Models;
using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private Button? _queuePauseButton;

    private void ConfigureQueueUx()
    {
        _queueList.SelectionMode = SelectionMode.Extended;
        _queueList.ContextMenu = CreateContextMenu(
            (L("Details"), (_, _) => ShowSelectedTransferDetails()),
            (GhostTransferText.T("PauseQueue"), (_, _) => ToggleQueuePause()),
            (L("RetrySelected"), (_, _) => RetrySelectedTransfers()),
            (GhostTransferText.T("RetryFailed"), (_, _) => RetryAllFailedTransfers()),
            (L("CancelSelected"), (_, _) => CancelSelectedTransfer()),
            (L("CancelAll"), (_, _) => CancelAllTransfers()),
            (GhostTransferText.T("ClearCompleted"), (_, _) => ClearCompletedTransfers()),
            (GhostTransferText.T("ClearFailed"), (_, _) => ClearFailedTransfers()),
            (GhostTransferText.T("ClearCancelled"), (_, _) => ClearCancelledTransfers()),
            (L("ClearFinished"), (_, _) => ClearFinishedTransfers()),
            ("Copy source path", (_, _) => CopySelectedTransferSource()),
            ("Copy destination path", (_, _) => CopySelectedTransferDestination()));

        _queuePauseMenuItem = _queueList.ContextMenu.Items.OfType<MenuItem>().ElementAtOrDefault(1);
        AddQueuePauseHeaderButton();
        _queueList.MouseDoubleClick += (_, _) => ShowSelectedTransferDetails();
        _queueList.SelectionChanged += (_, _) => UpdateQueueManagementUi();
        UpdateQueueManagementUi();

        _statusBadge.Cursor = Cursors.Hand;
        _statusBadge.ToolTip = "Connection status · click for local diagnostics";
        _statusBadge.MouseLeftButtonUp += async (_, _) => await ShowConnectionDiagnosticsAsync();
    }

    private void AddQueuePauseHeaderButton()
    {
        // BuildTransfers places the queue list directly inside a DockPanel and the action WrapPanel
        // in its docked header. Add pause/resume here so the primary control uses the same queue
        // semantics as the context menu without duplicating transfer logic.
        if (_queueList.Parent is not DockPanel dock)
            return;

        var header = dock.Children.OfType<Grid>().FirstOrDefault();
        var actions = header?.Children.OfType<WrapPanel>().FirstOrDefault();
        if (actions is null)
            return;

        if (actions.Children.Count > 0 && actions.Children[0] is Button firstAction)
            firstAction.Margin = new Thickness(5, 0, 0, 0);

        _queuePauseButton = GhostTheme.Button(GhostTransferText.T("PauseQueue"));
        _queuePauseButton.Click += (_, _) => ToggleQueuePause();
        actions.Children.Insert(0, _queuePauseButton);
    }

    private void ShowSelectedTransferDetails()
    {
        if (_queueList.SelectedItem is not TransferJob job)
            return;

        var error = string.IsNullOrWhiteSpace(job.Error) ? "None" : job.Error;
        var started = job.StartedUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "Not started";
        var finished = job.FinishedUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
        var details =
            $"Item: {job.DisplayName}\n" +
            $"Direction: {job.Direction}\n" +
            $"State: {job.State}\n" +
            $"Progress: {job.ProgressText}\n" +
            $"Transferred: {job.TransferredText}\n" +
            $"Speed: {job.SpeedText}\n" +
            $"ETA: {job.EtaText}\n" +
            $"Retries: {job.RetryCount}\n" +
            $"Started: {started}\n" +
            $"Finished: {finished}\n\n" +
            $"Source:\n{job.Source}\n\n" +
            $"Destination:\n{job.Destination}\n\n" +
            $"Error:\n{error}";

        GhostMessageDialog.Information(this, "Transfer details", details);
    }
}
