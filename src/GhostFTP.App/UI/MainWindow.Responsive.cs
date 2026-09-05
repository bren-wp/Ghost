using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
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
        if (_workspaceBody is null || _workspaceContent is null || _filePanesGrid is null)
            return;

        var sidebar = _workspaceBody.ColumnDefinitions[0];
        sidebar.MinWidth = 210;
        sidebar.MaxWidth = 420;
        _workspaceBody.ColumnDefinitions[1].Width = new GridLength(8);

        var sidebarSplitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);
        sidebarSplitter.MouseDoubleClick += (_, _) => sidebar.Width = new GridLength(252);
        Grid.SetColumn(sidebarSplitter, 1);
        _workspaceBody.Children.Add(sidebarSplitter);

        _workspaceContent.RowDefinitions[2].MinHeight = 250;
        _workspaceContent.RowDefinitions[3].Height = new GridLength(8);
        _workspaceContent.RowDefinitions[4].MinHeight = 130;
        _workspaceContent.RowDefinitions[4].MaxHeight = 440;

        var transferSplitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);
        transferSplitter.MouseDoubleClick += (_, _) => _workspaceContent.RowDefinitions[4].Height = new GridLength(210);
        Grid.SetRow(transferSplitter, 3);
        _workspaceContent.Children.Add(transferSplitter);

        _filePanesGrid.ColumnDefinitions[0].MinWidth = 320;
        _filePanesGrid.ColumnDefinitions[1].Width = new GridLength(8);
        _filePanesGrid.ColumnDefinitions[2].MinWidth = 320;

        var paneSplitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);
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

        if (_workspaceBody is not null && _workspaceContent is not null && _filePanesGrid is not null)
        {
            _workspaceBody.ColumnDefinitions[0].Width = new GridLength(Math.Clamp(_settings.SidebarWidth, 210, 420));
            _workspaceContent.RowDefinitions[4].Height = new GridLength(Math.Clamp(_settings.TransferPanelHeight, 130, 440));

            var localFraction = Math.Clamp(_settings.LocalPaneFraction, 0.25, 0.75);
            _filePanesGrid.ColumnDefinitions[0].Width = new GridLength(localFraction, GridUnitType.Star);
            _filePanesGrid.ColumnDefinitions[2].Width = new GridLength(1 - localFraction, GridUnitType.Star);
        }

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

        if (_workspaceBody is null || _workspaceContent is null || _filePanesGrid is null)
            return;

        if (IsFinitePositive(_workspaceBody.ColumnDefinitions[0].ActualWidth))
            _settings.SidebarWidth = Math.Clamp(_workspaceBody.ColumnDefinitions[0].ActualWidth, 210, 420);
        if (IsFinitePositive(_workspaceContent.RowDefinitions[4].ActualHeight))
            _settings.TransferPanelHeight = Math.Clamp(_workspaceContent.RowDefinitions[4].ActualHeight, 130, 440);

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
            ToolTip = direction == GridResizeDirection.Columns
                ? "Drag to resize panes · double-click to reset"
                : "Drag to resize the transfer queue · double-click to reset"
        };
    }

    private static bool IsFinitePositive(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
}
