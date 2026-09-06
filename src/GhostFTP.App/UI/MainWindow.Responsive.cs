using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private const double DefaultConnectionPanelHeight = 184;
    private const double DefaultTransferPanelHeight = 198;

    private void ConfigureResponsiveColumns()
    {
        _localList.SizeChanged += (_, _) => ResizeFileColumns(_localList);
        _remoteList.SizeChanged += (_, _) => ResizeFileColumns(_remoteList);
        _queueList.SizeChanged += (_, _) => ResizeQueueColumns();

        Loaded += (_, _) => ResizeAllColumns();
        SizeChanged += (_, _) => ResizeAllColumns();
    }

    private void ResizeAllColumns()
    {
        ResizeFileColumns(_localList);
        ResizeFileColumns(_remoteList);
        ResizeQueueColumns();
    }

    private void ConfigureWorkspaceResizing()
    {
        if (_workspaceContent is null || _filePanesGrid is null)
            return;

        // The reference shell normalizes the desktop workspace to five active rows:
        // connection area / splitter / file panes / splitter / transfers.
        _workspaceContent.RowDefinitions[0].MinHeight = 160;
        _workspaceContent.RowDefinitions[0].MaxHeight = 360;
        _workspaceContent.RowDefinitions[1].Height = new GridLength(7);
        _workspaceContent.RowDefinitions[2].MinHeight = 250;
        _workspaceContent.RowDefinitions[3].Height = new GridLength(7);
        _workspaceContent.RowDefinitions[4].MinHeight = 128;
        _workspaceContent.RowDefinitions[4].MaxHeight = 440;

        var connectionSplitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);
        connectionSplitter.ToolTip = "Drag to resize Connection Log and Quick Connect · double-click to reset";
        connectionSplitter.MouseDoubleClick += (_, _) =>
            _workspaceContent.RowDefinitions[0].Height = new GridLength(DefaultConnectionPanelHeight);
        Grid.SetRow(connectionSplitter, 1);
        _workspaceContent.Children.Add(connectionSplitter);

        var transferSplitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);
        transferSplitter.ToolTip = "Drag to resize the Transfers queue · double-click to reset";
        transferSplitter.MouseDoubleClick += (_, _) =>
            _workspaceContent.RowDefinitions[4].Height = new GridLength(DefaultTransferPanelHeight);
        Grid.SetRow(transferSplitter, 3);
        _workspaceContent.Children.Add(transferSplitter);

        _filePanesGrid.ColumnDefinitions[0].MinWidth = 280;
        _filePanesGrid.ColumnDefinitions[1].Width = new GridLength(7);
        _filePanesGrid.ColumnDefinitions[2].MinWidth = 280;

        var paneSplitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);
        paneSplitter.ToolTip = "Drag to resize Local and Remote panes · double-click to reset";
        paneSplitter.MouseDoubleClick += (_, _) =>
        {
            _filePanesGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            _filePanesGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
        };
        Grid.SetColumn(paneSplitter, 1);
        _filePanesGrid.Children.Add(paneSplitter);
    }

    private void ApplyWorkspaceSettings()
    {
        var workArea = SystemParameters.WorkArea;
        var maxWidth = Math.Max(MinWidth, workArea.Width);
        var maxHeight = Math.Max(MinHeight, workArea.Height);
        Width = Math.Clamp(_settings.WindowWidth, MinWidth, maxWidth);
        Height = Math.Clamp(_settings.WindowHeight, MinHeight, maxHeight);

        if (_workspaceContent is not null && _filePanesGrid is not null)
        {
            _workspaceContent.RowDefinitions[0].Height = new GridLength(
                Math.Clamp(_settings.ConnectionPanelHeight, 160, 360));
            _workspaceContent.RowDefinitions[4].Height = new GridLength(
                Math.Clamp(_settings.TransferPanelHeight, 128, 440));

            var localFraction = Math.Clamp(_settings.LocalPaneFraction, 0.25, 0.75);
            _filePanesGrid.ColumnDefinitions[0].Width = new GridLength(localFraction, GridUnitType.Star);
            _filePanesGrid.ColumnDefinitions[2].Width = new GridLength(1 - localFraction, GridUnitType.Star);
        }

        if (_referenceSidebarColumn is not null)
            _referenceSidebarColumn.Width = new GridLength(Math.Clamp(_settings.SidebarWidth, 220, 380));

        if (_settings.WindowMaximized && _captureDirectory is null)
        {
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (IsVisible)
                    WindowState = WindowState.Maximized;
            }));
        }

        ResizeAllColumns();
    }

    private void CaptureWorkspaceSettings()
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;

        if (IsFinitePositive(bounds.Width))
            _settings.WindowWidth = Math.Max(MinWidth, bounds.Width);
        if (IsFinitePositive(bounds.Height))
            _settings.WindowHeight = Math.Max(MinHeight, bounds.Height);

        _settings.WindowMaximized = WindowState == WindowState.Maximized;

        if (_referenceSidebarColumn is not null && IsFinitePositive(_referenceSidebarColumn.ActualWidth))
            _settings.SidebarWidth = Math.Clamp(_referenceSidebarColumn.ActualWidth, 220, 380);

        if (_workspaceContent is null || _filePanesGrid is null)
            return;

        if (IsFinitePositive(_workspaceContent.RowDefinitions[0].ActualHeight))
        {
            _settings.ConnectionPanelHeight = Math.Clamp(
                _workspaceContent.RowDefinitions[0].ActualHeight,
                160,
                360);
        }

        if (IsFinitePositive(_workspaceContent.RowDefinitions[4].ActualHeight))
        {
            _settings.TransferPanelHeight = Math.Clamp(
                _workspaceContent.RowDefinitions[4].ActualHeight,
                128,
                440);
        }

        var local = _filePanesGrid.ColumnDefinitions[0].ActualWidth;
        var remote = _filePanesGrid.ColumnDefinitions[2].ActualWidth;
        var total = local + remote;
        if (IsFinitePositive(total))
            _settings.LocalPaneFraction = Math.Clamp(local / total, 0.25, 0.75);
    }

    private static GridSplitter CreateSplitter(GridResizeDirection direction, Cursor cursor)
    {
        return new GridSplitter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeDirection = direction,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ShowsPreview = false,
            Background = GhostTheme.R("Border"),
            Cursor = cursor,
            Focusable = false,
            Opacity = 0.72
        };
    }

    private static bool IsFinitePositive(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
}
