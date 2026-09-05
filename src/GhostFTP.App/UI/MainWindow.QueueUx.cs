using System.Windows;
using System.Windows.Controls;

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
    }
}
