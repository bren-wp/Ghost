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

        _queueList.MouseDoubleClick += (_, _) =>
        {
            if (_queueList.SelectedItems.Count > 0)
                RetrySelectedTransfers();
        };

        _statusBadge.Cursor = Cursors.Hand;
        _statusBadge.ToolTip = "Connection status · click for local diagnostics";
        _statusBadge.MouseLeftButtonUp += async (_, _) => await ShowConnectionDiagnosticsAsync();
    }
}
