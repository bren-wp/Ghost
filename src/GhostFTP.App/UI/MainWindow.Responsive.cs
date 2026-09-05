using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        if (Content is not Grid root || root.ColumnDefinitions.Count < 3)
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

        var content = root.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetColumn(child) == 2 && child.RowDefinitions.Count >= 7);
        if (content is null)
            return;

        // Browser / transfer queue splitter.
        content.RowDefinitions[4].MinHeight = 220;
        content.RowDefinitions[5].Height = new GridLength(8);
        content.RowDefinitions[6].MinHeight = 130;
        content.RowDefinitions[6].MaxHeight = 460;

        var transferSplitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);
        transferSplitter.MouseDoubleClick += (_, _) => content.RowDefinitions[6].Height = new GridLength(210);
        Grid.SetRow(transferSplitter, 5);
        content.Children.Add(transferSplitter);

        var panes = content.Children
            .OfType<Grid>()
            .FirstOrDefault(child => Grid.GetRow(child) == 4 && child.ColumnDefinitions.Count == 3);
        if (panes is null)
            return;

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
}
