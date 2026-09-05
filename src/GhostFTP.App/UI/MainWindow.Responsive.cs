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
        if (!TryGetWorkspaceGrids(out var root, out var content, out var panes))
            return;

        // Sidebar / workspace splitter.
        var sidebar = root.ColumnDefinitions[0];
        sidebar.MinWidth = 220;
        sidebar.MaxWidth = 460;
        root.ColumnDefinitions[1].Width = new GridLength(8);

        var sidebarSplitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);
        sidebarSplitter.MouseDoubleClick += (_, _) => sidebar.Width = new GridLength(300);
        Grid.SetColumn(sidebarSplitter, 1);
        root.Children.Add(sidebarSplitter);

        // Browser / transfer queue splitter.
        content.RowDefinitions[4].MinHeight = 220;
        content.RowDefinitions[5].Height = new GridLength(8);
        content.RowDefinitions[6].MinHeight = 130;
        content.RowDefinitions[6].MaxHeight = 460;

        var transferSplitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);
        transferSplitter.MouseDoubleClick += (_, _) => content.RowDefinitions[6].Height = new GridLength(210);
        Grid.SetRow(transferSplitter, 5);
        content.Children.Add(transferSplitter);

        // Local / remote pane splitter.
        panes.ColumnDefinitions[0].MinWidth = 300;
        panes.ColumnDefinitions[1].Width = new GridLength(8);
        panes.ColumnDefinitions[2].MinWidth = 300;

        var paneSplitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);
        paneSplitter.MouseDoubleClick += (_, _) =>
        {
            panes.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            panes.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
        };
        Grid.SetColumn(paneSplitter, 1);
        panes.Children.Add(paneSplitter);
    }

    private void ApplyWorkspaceSettings()
    {
        var workArea = SystemParameters.WorkArea;
        var maxWidth = Math.Max(MinWidth, workArea.Width);
        var maxHeight = Math.Max(MinHeight, workArea.Height);
        Width = Math.Clamp(_settings.WindowWidth, MinWidth, maxWidth);
        Height = Math.Clamp(_settings.WindowHeight, MinHeight, maxHeight);

        if (TryGetWorkspaceGrids(out var root, out var content, out var panes))
        {
            root.ColumnDefinitions[0].Width = new GridLength(Math.Clamp(_settings.SidebarWidth, 220, 460));
            content.RowDefinitions[6].Height = new GridLength(Math.Clamp(_settings.TransferPanelHeight, 130, 460));

            var localFraction = Math.Clamp(_settings.LocalPaneFraction, 0.25, 0.75);
            panes.ColumnDefinitions[0].Width = new GridLength(localFraction, GridUnitType.Star);
            panes.ColumnDefinitions[2].Width = new GridLength(1 - localFraction, GridUnitType.Star);
        }

        if (_settings.WindowMaximized)
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

        if (!TryGetWorkspaceGrids(out var root, out var content, out var panes))
            return;

        if (IsFinitePositive(root.ColumnDefinitions[0].ActualWidth))
            _settings.SidebarWidth = Math.Clamp(root.ColumnDefinitions[0].ActualWidth, 220, 460);
        if (IsFinitePositive(content.RowDefinitions[6].ActualHeight))
            _settings.TransferPanelHeight = Math.Clamp(content.RowDefinitions[6].ActualHeight, 130, 460);

        var local = panes.ColumnDefinitions[0].ActualWidth;
        var remote = panes.ColumnDefinitions[2].ActualWidth;
        var total = local + remote;
        if (IsFinitePositive(total))
            _settings.LocalPaneFraction = Math.Clamp(local / total, 0.25, 0.75);
    }

    private bool TryGetWorkspaceGrids(out Grid root, out Grid content, out Grid panes)
    {
        root = null!;
        content = null!;
        panes = null!;

        if (Content is not Grid rootGrid || rootGrid.ColumnDefinitions.Count < 3)
            return false;

        var contentGrid = rootGrid.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetColumn(child) == 2 && child.RowDefinitions.Count >= 7);
        if (contentGrid is null)
            return false;

        var panesGrid = contentGrid.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetRow(child) == 4 && child.ColumnDefinitions.Count == 3);
        if (panesGrid is null)
            return false;

        root = rootGrid;
        content = contentGrid;
        panes = panesGrid;
        return true;
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
