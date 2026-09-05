using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private UIElement BuildLayout()
    {
        var root = new Grid { Background = Brushes.Transparent, Margin = new Thickness(16) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = BuildSidebar();
        Grid.SetColumn(sidebar, 0);
        root.Children.Add(sidebar);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });

        var pageHeader = BuildPageHeader();
        Grid.SetRow(pageHeader, 0);
        content.Children.Add(pageHeader);

        var quickConnect = BuildQuickConnect();
        Grid.SetRow(quickConnect, 2);
        content.Children.Add(quickConnect);

        var panes = BuildFilePanes();
        Grid.SetRow(panes, 4);
        content.Children.Add(panes);

        var transfers = BuildTransfers();
        Grid.SetRow(transfers, 6);
        content.Children.Add(transfers);

        Grid.SetColumn(content, 2);
        root.Children.Add(content);
        return root;
    }

    private Border BuildSidebar()
    {
        var root = new DockPanel();

        var brand = new StackPanel { Margin = new Thickness(2, 2, 2, 20) };
        var logoRow = new StackPanel { Orientation = Orientation.Horizontal };
        logoRow.Children.Add(GhostBrand.IconControl(50));
        var brandText = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        brandText.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 20, weight: FontWeights.SemiBold));
        brandText.Children.Add(GhostTheme.Text(L("PrivateFileTransfer"), 10.5, muted: true));
        logoRow.Children.Add(brandText);
        brand.Children.Add(logoRow);
        var privacy = GhostTheme.Text(GhostBrand.PrivacyTagline, 10.5, muted: true);
        privacy.Margin = new Thickness(0, 11, 0, 0);
        brand.Children.Add(privacy);
        DockPanel.SetDock(brand, Dock.Top);
        root.Children.Add(brand);

        var bottom = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        var settings = GhostTheme.Button($"⚙  {L("Settings")}", subtle: true);
        settings.HorizontalContentAlignment = HorizontalAlignment.Left;
        settings.Click += async (_, _) => await OpenSettingsAsync();
        var about = GhostTheme.Button($"ⓘ  {L("About")} {GhostBrand.DisplayName}", subtle: true);
        about.HorizontalContentAlignment = HorizontalAlignment.Left;
        about.Margin = new Thickness(0, 4, 0, 0);
        about.Click += (_, _) => new AboutDialog(this).ShowDialog();
        bottom.Children.Add(settings);
        bottom.Children.Add(about);
        var privacyNote = GhostTheme.Text(L("NoTelemetryTracking"), 10, muted: true);
        privacyNote.Margin = new Thickness(10, 10, 0, 0);
        bottom.Children.Add(privacyNote);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var servers = new DockPanel();
        var heading = new Grid { Margin = new Thickness(2, 0, 2, 10) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(GhostTheme.Text(L("SavedServers"), 12.5, weight: FontWeights.SemiBold));
        title.Children.Add(GhostTheme.Text("Double-click a profile to connect", 10, muted: true));
        heading.Children.Add(title);
        var add = GhostTheme.Button($"＋ {L("Add")}");
        add.Click += async (_, _) => await AddProfileAsync();
        Grid.SetColumn(add, 1);
        heading.Children.Add(add);
        DockPanel.SetDock(heading, Dock.Top);
        servers.Children.Add(heading);

        var actions = new StackPanel { Margin = new Thickness(2, 10, 2, 0) };
        var connectSaved = GhostTheme.Button(L("ConnectSelected"), primary: true);
        connectSaved.Click += async (_, _) => await ConnectAsync();
        actions.Children.Add(connectSaved);

        var editRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        editRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        editRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var edit = GhostTheme.Button(L("Edit"));
        edit.Click += async (_, _) => await EditSelectedProfileAsync();
        var remove = GhostTheme.Button(L("Remove"), danger: true);
        remove.Click += async (_, _) => await RemoveSelectedProfileAsync();
        Grid.SetColumn(edit, 0);
        Grid.SetColumn(remove, 2);
        editRow.Children.Add(edit);
        editRow.Children.Add(remove);
        actions.Children.Add(editRow);
        DockPanel.SetDock(actions, Dock.Bottom);
        servers.Children.Add(actions);

        _profilesList.Background = Brushes.Transparent;
        _profilesList.BorderThickness = new Thickness(0);
        _profilesList.Foreground = GhostTheme.R("Text");
        _profilesList.FontFamily = GhostTheme.UiFont;
        _profilesList.DisplayMemberPath = nameof(ServerProfile.Name);
        _profilesList.ItemsSource = _profiles;
        _profilesList.Padding = new Thickness(0);
        servers.Children.Add(_profilesList);

        root.Children.Add(servers);
        return GhostTheme.Card(root, new Thickness(16), 18);
    }

    private Border BuildPageHeader()
    {
        var grid = new Grid { Margin = new Thickness(2, 0, 2, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new StackPanel();
        title.Children.Add(GhostTheme.Text(L("Files"), 27, weight: FontWeights.SemiBold));
        title.Children.Add(GhostTheme.Text("Move files between this PC and your server without leaving the workspace.", 11.5, muted: true));
        grid.Children.Add(title);

        _statusBadge.CornerRadius = new CornerRadius(999);
        _statusBadge.Padding = new Thickness(10, 5, 10, 5);
        _statusBadge.Background = GhostTheme.R("Surface2");
        _statusBadge.Child = _statusText;
        _statusBadge.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_statusBadge, 1);
        grid.Children.Add(_statusBadge);
        return GhostTheme.Card(grid, new Thickness(16, 13, 16, 13), 14);
    }

    private Border BuildQuickConnect()
    {
        var root = new StackPanel();
        var heading = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(GhostTheme.Text(L("QuickConnect"), 15, weight: FontWeights.SemiBold));
        title.Children.Add(GhostTheme.Text("FTPS Explicit is recommended. Certificates are validated by Windows/.NET.", 10.5, muted: true));
        heading.Children.Add(title);
        var securityBadge = GhostTheme.Badge("TLS first", "SuccessSoft", "Text");
        securityBadge.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(securityBadge, 1);
        heading.Children.Add(securityBadge);
        root.Children.Add(heading);

        var rowOne = new Grid();
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.3, GridUnitType.Star) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        var host = GhostTheme.Field(L("Host"), _host);
        var port = GhostTheme.Field(L("Port"), _port);
        var security = GhostTheme.Field(L("Security"), _security);
        Grid.SetColumn(host, 0);
        Grid.SetColumn(port, 2);
        Grid.SetColumn(security, 4);
        rowOne.Children.Add(host);
        rowOne.Children.Add(port);
        rowOne.Children.Add(security);
        root.Children.Add(rowOne);

        var rowTwo = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var username = GhostTheme.Field(L("Username"), _username);
        var password = GhostTheme.Field(L("Password"), _password);
        Grid.SetColumn(username, 0);
        Grid.SetColumn(password, 2);
        rowTwo.Children.Add(username);
        rowTwo.Children.Add(password);

        var connectActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
        _connectButton.MinWidth = 96;
        _disconnectButton.MinWidth = 96;
        _disconnectButton.Margin = new Thickness(8, 0, 0, 0);
        _disconnectButton.Visibility = Visibility.Collapsed;
        connectActions.Children.Add(_connectButton);
        connectActions.Children.Add(_disconnectButton);
        Grid.SetColumn(connectActions, 4);
        rowTwo.Children.Add(connectActions);
        root.Children.Add(rowTwo);

        return GhostTheme.Card(root, new Thickness(16), 14);
    }

    private Grid BuildFilePanes()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var local = BuildPane(L("Local"), L("ThisPc"), _localPathBox, _localFilter, _localList, _localSummary, false);
        var remote = BuildPane(L("Remote"), L("ConnectedServer"), _remotePathBox, _remoteFilter, _remoteList, _remoteSummary, true);
        Grid.SetColumn(local, 0);
        Grid.SetColumn(remote, 2);
        grid.Children.Add(local);
        grid.Children.Add(remote);
        return grid;
    }

    private Border BuildPane(string title, string subtitle, TextBox pathBox, TextBox filter, ListView list, TextBlock summary, bool isRemote)
    {
        var dock = new DockPanel();

        var footer = new Grid { Margin = new Thickness(0, 9, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.Children.Add(summary);
        var hint = GhostTheme.Text(isRemote ? "Double-click folder · file downloads" : "Double-click folder to open", 10, muted: true);
        Grid.SetColumn(hint, 1);
        footer.Children.Add(hint);
        DockPanel.SetDock(footer, Dock.Bottom);
        dock.Children.Add(footer);

        var header = new StackPanel();
        var titleBlock = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        titleBlock.Children.Add(GhostTheme.Text(title, 17, weight: FontWeights.SemiBold));
        titleBlock.Children.Add(GhostTheme.Text(subtitle, 10.5, muted: true));
        header.Children.Add(titleBlock);

        var pathRow = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Button up;
        Button home;
        if (isRemote)
        {
            up = ToolButton($"↑ {L("Up")}", RemoteUpAsync);
            home = ToolButton("⌂ /", NavigateRemoteHomeAsync);
        }
        else
        {
            up = ToolButton($"↑ {L("Up")}", LocalUp);
            home = ToolButton($"⌂ {L("Home")}", NavigateLocalHome);
        }

        Grid.SetColumn(up, 0);
        pathRow.Children.Add(up);
        Grid.SetColumn(pathBox, 2);
        pathRow.Children.Add(pathBox);
        Grid.SetColumn(home, 4);
        pathRow.Children.Add(home);
        header.Children.Add(pathRow);

        if (!isRemote)
        {
            var quick = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            quick.Children.Add(SmallNavButton(L("Desktop"), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
            quick.Children.Add(SmallNavButton(L("Documents"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
            quick.Children.Add(SmallNavButton(L("Downloads"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")));
            header.Children.Add(quick);
        }

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 9) };
        if (isRemote)
        {
            toolbar.Children.Add(ToolButton($"↓ {L("Download")}", QueueDownloadSelected, primary: true));
            toolbar.Children.Add(ToolButton($"↻ {L("Refresh")}", RefreshRemoteAsync));
            toolbar.Children.Add(ToolButton($"＋ {L("NewFolder")}", NewRemoteFolderAsync));
            toolbar.Children.Add(ToolButton(L("Rename"), RenameRemoteSelectedAsync));
            toolbar.Children.Add(ToolButton(L("Delete"), DeleteRemoteSelectedAsync, danger: true));
        }
        else
        {
            toolbar.Children.Add(ToolButton($"↑ {L("Upload")}", QueueUploadSelected, primary: true));
            toolbar.Children.Add(ToolButton($"↻ {L("Refresh")}", RefreshLocal));
            toolbar.Children.Add(ToolButton($"＋ {L("NewFolder")}", NewLocalFolder));
            toolbar.Children.Add(ToolButton(L("Rename"), RenameLocalSelected));
            toolbar.Children.Add(ToolButton(L("Delete"), DeleteLocalSelected, danger: true));
        }
        header.Children.Add(toolbar);

        var filterRow = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        filter.ToolTip = L("FilterTooltip");
        Grid.SetColumn(filter, 0);
        filterRow.Children.Add(filter);
        var clear = ToolButton(L("ClearFilter"), () => filter.Clear());
        Grid.SetColumn(clear, 2);
        filterRow.Children.Add(clear);
        header.Children.Add(filterRow);

        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);

        list.Background = GhostTheme.R("Surface2");
        list.BorderBrush = GhostTheme.R("Border");
        list.BorderThickness = new Thickness(1);
        list.Foreground = GhostTheme.R("Text");
        list.FontFamily = GhostTheme.UiFont;
        list.FontSize = 12.5;
        list.Padding = new Thickness(0);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        dock.Children.Add(list);

        return GhostTheme.Card(dock, new Thickness(14), 14);
    }

    private Border BuildTransfers()
    {
        var dock = new DockPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(GhostTheme.Text(L("Transfers"), 16, weight: FontWeights.SemiBold));
        _queueSummary.Margin = new Thickness(10, 0, 0, 0);
        left.Children.Add(_queueSummary);
        header.Children.Add(left);

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var retry = GhostTheme.Button(L("RetrySelected"));
        retry.Click += (_, _) => RetrySelectedTransfers();
        var cancel = GhostTheme.Button(L("CancelSelected"));
        cancel.Margin = new Thickness(8, 0, 0, 0);
        cancel.Click += (_, _) => CancelSelectedTransfer();
        var cancelAll = GhostTheme.Button(L("CancelAll"));
        cancelAll.Margin = new Thickness(8, 0, 0, 0);
        cancelAll.Click += (_, _) => CancelAllTransfers();
        var clear = GhostTheme.Button(L("ClearFinished"));
        clear.Margin = new Thickness(8, 0, 0, 0);
        clear.Click += (_, _) =>
        {
            _queue?.ClearFinished();
            UpdateQueueSummary();
        };
        actions.Children.Add(retry);
        actions.Children.Add(cancel);
        actions.Children.Add(cancelAll);
        actions.Children.Add(clear);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);

        _queueList.Background = GhostTheme.R("Surface2");
        _queueList.BorderBrush = GhostTheme.R("Border");
        _queueList.BorderThickness = new Thickness(1);
        _queueList.Foreground = GhostTheme.R("Text");
        _queueList.FontFamily = GhostTheme.UiFont;
        ScrollViewer.SetHorizontalScrollBarVisibility(_queueList, ScrollBarVisibility.Disabled);
        dock.Children.Add(_queueList);
        return GhostTheme.Card(dock, new Thickness(14), 14);
    }

    private Button SmallNavButton(string text, string path)
    {
        var button = GhostTheme.Button(text, subtle: true);
        button.Margin = new Thickness(0, 0, 6, 0);
        button.Padding = new Thickness(9, 4, 9, 4);
        button.MinHeight = 28;
        button.Click += (_, _) => NavigateLocalQuick(path);
        return button;
    }

    private void ConfigureLists()
    {
        _localList.ItemsSource = _localItems;
        _remoteList.ItemsSource = _remoteItems;
        _localList.View = CreateFileGrid(local: true);
        _remoteList.View = CreateFileGrid(local: false);
        _queueList.View = CreateQueueGrid();
        _localList.SelectionMode = SelectionMode.Extended;
        _remoteList.SelectionMode = SelectionMode.Extended;

        _localList.ContextMenu = CreateContextMenu(
            (L("Open"), (_, _) => OpenLocalSelected()),
            (L("OpenExplorer"), (_, _) => RevealLocalSelected()),
            (L("CopyFullPath"), (_, _) => CopyLocalPath()),
            (L("Upload"), (_, _) => QueueUploadSelected()),
            (L("Rename"), (_, _) => RenameLocalSelected()),
            (L("Delete"), (_, _) => DeleteLocalSelected()),
            (L("Refresh"), (_, _) => RefreshLocal()));

        _remoteList.ContextMenu = CreateContextMenu(
            (L("Download"), (_, _) => QueueDownloadSelected()),
            (L("CopyRemotePath"), (_, _) => CopyRemotePath()),
            (L("Rename"), async (_, _) => await RenameRemoteSelectedAsync()),
            (L("Delete"), async (_, _) => await DeleteRemoteSelectedAsync()),
            (L("Refresh"), async (_, _) => await RefreshRemoteAsync()));
    }

    private void ConfigureEvents()
    {
        _connectButton.Click += async (_, _) => await ConnectAsync();
        _disconnectButton.Click += async (_, _) => await DisconnectAsync();
        _profilesList.SelectionChanged += (_, _) => ProfileSelected();
        _profilesList.MouseDoubleClick += async (_, _) =>
        {
            if (_profilesList.SelectedItem is ServerProfile) await ConnectAsync();
        };

        _localList.MouseDoubleClick += (_, _) => OpenLocalSelected();
        _remoteList.MouseDoubleClick += async (_, _) => await OpenRemoteSelectedAsync();
        _localPathBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) NavigateLocalPathBox();
        };
        _remotePathBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter) await NavigateRemotePathBoxAsync();
        };
        _password.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter) await ConnectAsync();
        };
        _localFilter.TextChanged += (_, _) => ApplyLocalFilter();
        _remoteFilter.TextChanged += (_, _) => ApplyRemoteFilter();
        _localList.SelectionChanged += (_, _) => UpdatePaneSummaries();
        _remoteList.SelectionChanged += (_, _) => UpdatePaneSummaries();
        PreviewKeyDown += async (_, e) => await HandleShortcutAsync(e);

        _remoteList.AllowDrop = true;
        _remoteList.DragOver += (_, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && IsConnected ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        };
        _remoteList.Drop += (_, e) =>
        {
            if (!IsConnected || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    _queue?.EnqueueUpload(path, FtpListingParser.CombineRemote(_remotePath, Path.GetFileName(path)), false);
                else if (Directory.Exists(path))
                    _queue?.EnqueueUpload(path, FtpListingParser.CombineRemote(_remotePath, Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar))), true);
            }
            UpdateQueueSummary();
        };
    }
}
