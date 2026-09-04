using System.Windows;
using System.Windows.Controls;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private void ConfigureQueueUx()
    {
        _queueList.SelectionMode = SelectionMode.Extended;
        _queueList.ContextMenu = CreateContextMenu(
            ("Retry selected", (_, _) => RetrySelectedTransfers()),
            ("Cancel selected", (_, _) => CancelSelectedTransfer()),
            ("Cancel all active", (_, _) => CancelAllTransfers()),
            ("Copy source path", (_, _) => CopySelectedTransferSource()),
            ("Copy destination path", (_, _) => CopySelectedTransferDestination()),
            ("Clear finished", (_, _) =>
            {
                _queue?.ClearFinished();
                UpdateQueueSummary();
            }));

        _queueList.MouseDoubleClick += (_, _) =>
        {
            if (_queueList.SelectedItems.Count > 0)
                RetrySelectedTransfers();
        };
    }
}
