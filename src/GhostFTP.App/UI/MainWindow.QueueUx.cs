using GhostFTP.Core.Models;
using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private void ConfigureQueueUx()
    {
        _queueList.SelectionMode = SelectionMode.Extended;
        _queueList.ContextMenu = CreateContextMenu(
            (L("Details"), (_, _) => ShowSelectedTransferDetails()),
            (L("RetrySelected"), (_, _) => RetrySelectedTransfers()),
            (L("CancelSelected"), (_, _) => CancelSelectedTransfer()),
            (L("CancelAll"), (_, _) => CancelAllTransfers()),
            ("Copy source path", (_, _) => CopySelectedTransferSource()),
            ("Copy destination path", (_, _) => CopySelectedTransferDestination()),
            (L("ClearFinished"), (_, _) =>
            {
                _queue?.ClearFinished();
                UpdateQueueSummary();
            }));

        _queueList.MouseDoubleClick += (_, _) => ShowSelectedTransferDetails();

        _statusBadge.Cursor = Cursors.Hand;
        _statusBadge.ToolTip = "Connection status · click for local diagnostics";
        _statusBadge.MouseLeftButtonUp += async (_, _) => await ShowConnectionDiagnosticsAsync();
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
