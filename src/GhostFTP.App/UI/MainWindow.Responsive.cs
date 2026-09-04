namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private void ConfigureResponsiveColumns()
    {
        _localList.SizeChanged += (_, _) => ResizeFileColumns(_localList);
        _remoteList.SizeChanged += (_, _) => ResizeFileColumns(_remoteList);
        _queueList.SizeChanged += (_, _) => ResizeQueueColumns();

        Loaded += (_, _) =>
        {
            ResizeFileColumns(_localList);
            ResizeFileColumns(_remoteList);
            ResizeQueueColumns();
        };
    }
}
