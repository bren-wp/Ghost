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
        var root = new Grid { Background = Brushes.Transparent };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

        var menu = BuildTopMenu();
        Grid.SetRow(menu, 0);
        root.Children.Add(menu);

        var toolbar = BuildMainToolbar();
        Grid.SetRow(toolbar, 1);
        root.Children.Add(toolbar);

        _workspaceBody = new Grid { Margin = new Thickness(10, 8, 10, 8) };
        _workspaceBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(252) });
        _workspaceBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        _workspaceBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = BuildSidebar();
        Grid.SetColumn(sidebar, 0);
        _workspaceBody.Children.Add(sidebar);

        _workspaceContent = new Grid();
        _workspaceContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(154) });
        _workspaceContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        _workspaceContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _workspaceContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        _workspaceContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });

        var connectionStrip = BuildConnectionStrip();
        Grid.SetRow(connectionStrip, 0);
        _workspaceContent.Children.Add(connectionStrip);

        var panes = BuildFilePanes();
        Grid.SetRow(panes, 2);
        _workspaceContent.Children.Add(panes);

        var transfers = BuildTransfers();
        Grid.SetRow(transfers, 4);
        _workspaceContent.Children.Add(transfers);

        Grid.SetColumn(_workspaceContent, 2);
        _workspaceBody.Children.Add(_workspaceContent);
        Grid.SetRow(_workspaceBody, 2);
        root.Children.Add(_workspaceBody);

        var status = BuildStatusBar();
        Grid.SetRow(status, 3);
        root.Children.Add(status);
        return root;
    }

    private Menu BuildTopMenu()
    {
        var menu = new Menu
        {
            Background = GhostTheme.R("Surface"),
            Foreground = GhostTheme.R("Text"),
            BorderBrush = GhostTheme.R("Border"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 0, 8, 0),
            FontFamily = GhostTheme.UiFont,
            FontSize = 12.5
        };

        var file = TopMenuItem("File");
        file.Items.Add(ActionMenuItem(L("Connect"), async (_, _) => await ConnectAsync()));
        file.Items.Add(ActionMenuItem(L("Disconnect"), async (_, _) => await DisconnectAsync()));
        file.Items.Add(new Separator());
        file.Items.Add(ActionMenuItem("Exit", (_, _) => Close()));

        var view = TopMenuItem("View");
        view.Items.Add(ActionMenuItem($"{L("Refresh")} local", (_, _) => RefreshLocal()));
        view.Items.Add(ActionMenuItem($"{L("Refresh")} remote", async (_, _) => await RefreshRemoteAsync()));
        view.Items.Add(new Separator());
        view.Items.Add(ActionMenuItem(L("Settings"), async (_, _) => await OpenSettingsAsync()));

        var sites = TopMenuItem("Sites");
        sites.Items.Add(ActionMenuItem("Site Manager", async (_, _) => await OpenSiteManagerAsync()));
        sites.Items.Add(ActionMenuItem(L("Add"), async (_, _) => await AddProfileAsync()));
        sites.Items.Add(ActionMenuItem(L("Edit"), async (_, _) => await EditSelectedProfileAsync()));
        sites.Items.Add(ActionMenuItem(L("Remove"), async (_, _) => await RemoveSelectedProfileAsync()));

        var transfers = TopMenuItem("Transfers");
        transfers.Items.Add(ActionMenuItem(L("Upload"), (_, _) => QueueUploadSelected()));
        transfers.Items.Add(ActionMenuItem(L("Download"), (_, _) => QueueDownloadSelected()));
        transfers.Items.Add(new Separator());
        transfers.Items.Add(ActionMenuItem(L("CancelAll"), (_, _) => CancelAllTransfers()));
        transfers.Items.Add(ActionMenuItem(L("ClearFinished"), (_, _) =>
        {
            _queue?.ClearFinished();
            UpdateQueueSummary();
        }));

        var tools = TopMenuItem("Tools");
        tools.Items.Add(ActionMenuItem("Connection diagnostics", async (_, _) => await ShowConnectionDiagnosticsAsync()));
        tools.Items.Add(ActionMenuItem(L("Settings"), async (_, _) => await OpenSettingsAsync()));

        var help = TopMenuItem("Help");
        help.Items.Add(ActionMenuItem($"{L("About")} {GhostBrand.DisplayName}", (_, _) => new AboutDialog(this).ShowDialog()));

        menu.Items.Add(file);
        menu.Items.Add(view);
        menu.Items.Add(sites);
        menu.Items.Add(transfers);
        menu.Items.Add(tools);
        menu.Items.Add(help);
        return menu;
    }

    private static MenuItem TopMenuItem(string header) => new()
    {
        Header = header,
        Foreground = GhostTheme.R("Text"),
        Padding = new Thickness(10, 5, 10, 5)
    };

    private static MenuItem ActionMenuItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private Border BuildMainToolbar()
    {
        var toolbar = new WrapPanel { Margin = new Thickness(10, 7, 10, 2) };
        toolbar.Children.Add(ToolButton($"⚡ {L("Connect")}", ConnectAsync, primary: true));
        toolbar.Children.Add(ToolButton($"⏻ {L("Disconnect")}", DisconnectAsync));
        toolbar.Children.Add(ToolButton($"↑ {L("Upload")}", QueueUploadSelected));
        toolbar.Children.Add(ToolButton($"↓ {L("Download")}", QueueDownloadSelected));
        toolbar.Children.Add(ToolButton($"↻ {L("Refresh")}", async () =>
        {
            RefreshLocal();
            if (IsConnected) await RefreshRemoteAsync();
        }));
        toolbar.Children.Add(ToolButton($"＋ {L("NewFolder")}", NewLocalFolder));
        toolbar.Children.Add(ToolButton($"✎ {L("Rename")}", RenameLocalSelected));
        toolbar.Children.Add(ToolButton($"⌫ {L("Delete")}", DeleteLocalSelected, danger: true));
        toolbar.Children.Add(ToolButton("▣ Site Manager", OpenSiteManagerAsync));
        toolbar.Children.Add(ToolButton($"⚙ {L("Settings")}", OpenSettingsAsync));
        toolbar.Children.Add(ToolButton("◉ Diagnostics", ShowConnectionDiagnosticsAsync));
        return GhostTheme.Surface(toolbar, new Thickness(6), 0);
    }

    private Grid BuildConnectionStrip()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var log = BuildConnectionLog();
        Grid.SetColumn(log, 0);
        grid.Children.Add(log);

        var quick = BuildQuickConnect();
        Grid.SetColumn(quick, 2);
        grid.Children.Add(quick);
        return grid;
    }

    private Border BuildConnectionLog()
    {
        var dock = new DockPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heading = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        heading.Children.Add(GhostTheme.Text("Connection Log", 14, weight: FontWeights.SemiBold));
        var privacy = GhostTheme.Text("  local session activity", 10, muted: true);
        privacy.VerticalAlignment = VerticalAlignment.Center;
        heading.Children.Add(privacy);
        header.Children.Add(heading);

        var clear = GhostTheme.Button("Clear", subtle: true);
        clear.Padding = new Thickness(10, 4, 10, 4);
        clear.Click += (_, _) => _connectionLog.Clear();
        Grid.SetColumn(clear, 1);
        header.Children.Add(clear);
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);

        _connectionLogList.ItemsSource = _connectionLog;
        _connectionLogList.Background = GhostTheme.R("Surface2");
        _connectionLogList.Foreground = GhostTheme.R("Text");
        _connectionLogList.BorderBrush = GhostTheme.R("Border");
        _connectionLogList.BorderThickness = new Thickness(1);
        _connectionLogList.FontFamily = new FontFamily("Cascadia Mono");
        _connectionLogList.FontSize = 10.5;
        _connectionLogList.Padding = new Thickness(8, 4, 8, 4);
        _connectionLogList.SelectionMode = SelectionMode.Single;
        ScrollViewer.SetHorizontalScrollBarVisibility(_connectionLogList, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(_connectionLogList, ScrollBarVisibility.Auto);
        dock.Children.Add(_connectionLogList);

        return GhostTheme.Card(dock, new Thickness(12), 12);
    }

    private Border BuildSidebar()
    {
        var root = new DockPanel();

        var brand = new StackPanel { Margin = new Thickness(2, 2, 2, 16) };
        var logoRow = new StackPanel { Orientation = Orientation.Horizontal };
        logoRow.Children.Add(GhostBrand.IconControl(42));
        var brandText = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        brandText.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 18, weight: FontWeights.SemiBold));
        brandText.Children.Add(GhostTheme.Text(L("PrivateFileTransfer"), 10, muted: true));
        logoRow.Children.Add(brandText);
        brand.Children.Add(logoRow);
        DockPanel.SetDock(brand, Dock.Top);
        root.Children.Add(brand);

        var bottom = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        var settings = GhostTheme.Button($"⚙  {L("Settings")}", subtle: true);
        settings.HorizontalContentAlignment = HorizontalAlignment.Left;
        settings.Click += async (_, _) => await OpenSettingsAsync();
        var about = GhostTheme.Button($"ⓘ  {L("About")} {GhostBrand.DisplayName}", subtle: true);
        about.HorizontalContentAlignment = HorizontalAlignment.Left;
        about.Margin = new Thickness(0, 3, 0, 0);
        about.Click += (_, _) => new AboutDialog(this).ShowDialog();
        bottom.Children.Add(settings);
        bottom.Children.Add(about);
        var privacyNote = GhostTheme.Text(L("NoTelemetryTracking"), 9.5, muted: true);
        privacyNote.Margin = new Thickness(10, 8, 0, 0);
        bottom.Children.Add(privacyNote);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var servers = new DockPanel();
        var heading = new Grid { Margin = new Thickness(2, 0, 2, 8) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(GhostTheme.Text(L("SavedServers"), 12.5, weight: FontWeights.SemiBold));
        title.Children.Add(GhostTheme.Text("Double-click to connect", 9.5, muted: true));
        heading.Children.Add(title);
        var add = GhostTheme.Button("＋", subtle: true);
        add.ToolTip = L("Add");
        add.MinWidth = 32;
        add.Click += async (_, _) => await AddProfileAsync();
        Grid.SetColumn(add, 1);
        heading.Children.Add(add);
        DockPanel.SetDock(heading, Dock.Top);
        servers.Children.Add(heading);

        var actions = new StackPanel { Margin = new Thickness(2, 8, 2, 0) };
        var manager = GhostTheme.Button("Site Manager", primary: true);
        manager.Click += async (_, _) => await OpenSiteManagerAsync();
        actions.Children.Add(manager);

        var editRow = new Grid { Margin = new Thickness(0, 7, 0, 0) };
        editRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        editRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
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
        _profilesList.FontSize = 12.5;
        _profilesList.DisplayMemberPath = nameof(ServerProfile.Name);
        _profilesList.ItemsSource = _profiles;
        _profilesList.Padding = new Thickness(0);
        servers.Children.Add(_profilesList);

        root.Children.Add(servers);
        return GhostTheme.Card(root, new Thickness(14), 14);
    }

    private Border BuildQuickConnect()
    {
        var root = new StackPanel();
        var heading = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(GhostTheme.Text(L("QuickConnect"), 14, weight: FontWeights.SemiBold));
        title.Children.Add(GhostTheme.Text("Explicit FTPS recommended", 9.5, muted: true));
        heading.Children.Add(title);
        var securityBadge = GhostTheme.Badge("TLS first", "SuccessSoft", "Text");
        securityBadge.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(securityBadge, 1);
        heading.Children.Add(securityBadge);
        root.Children.Add(heading);

        var rowOne = new Grid();
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
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

        var rowTwo = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        rowTwo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var username = GhostTheme.Field(L("Username"), _username);
        var password = GhostTheme.Field(L("Password"), _password);
        Grid.SetColumn(username, 0);
        Grid.SetColumn(password, 2);
        rowTwo.Children.Add(username);
        rowTwo.Children.Add(password);

        var connectActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
        _connectButton.MinWidth = 92;
        _disconnectButton.MinWidth = 92;
        _disconnectButton.Margin = new Thickness(6, 0, 0, 0);
        _disconnectButton.Visibility = Visibility.Collapsed;
        connectActions.Children.Add(_connectButton);
        connectActions.Children.Add(_disconnectButton);
        Grid.SetColumn(connectActions, 4);
        rowTwo.Children.Add(connectActions);
        root.Children.Add(rowTwo);

        return GhostTheme.Card(root, new Thickness(12), 12);
    }

    private Grid BuildFilePanes()
    {
        _filePanesGrid = new Grid();
        _filePanesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _filePanesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        _filePanesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var local = BuildPane(L("Local"), L("ThisPc"), _localPathBox, _localFilter, _localList, _localSummary, false);
        var remote = BuildPane(L("Remote"), L("ConnectedServer"), _remotePathBox, _remoteFilter, _remoteList, _remoteSummary, true);
        Grid.SetColumn(local, 0);
        Grid.SetColumn(remote, 2);
        _filePanesGrid.Children.Add(local);
        _filePanesGrid.Children.Add(remote);
        return _filePanesGrid;
    }

    private Border BuildPane(string title, string subtitle, TextBox pathBox, TextBox filter, ListView list, TextBlock summary, bool isRemote)
    {
        var dock = new DockPanel();

        var footer = new Grid { Margin = new Thickness(0, 7, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.Children.Add(summary);
        var hint = GhostTheme.Text(isRemote ? "Double-click folder · file downloads" : "Double-click folder to open", 9.5, muted: true);
        Grid.SetColumn(hint, 1);
        footer.Children.Add(hint);
        DockPanel.SetDock(footer, Dock.Bottom);
        dock.Children.Add(footer);

        var header = new StackPanel();
        var titleRow = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titleBlock = new StackPanel { Orientation = Orientation.Horizontal };
        titleBlock.Children.Add(GhostTheme.Text(title, 15, weight: FontWeights.SemiBold));
        var sub = GhostTheme.Text($"  {subtitle}", 9.5, muted: true);
        sub.VerticalAlignment = VerticalAlignment.Center;
        titleBlock.Children.Add(sub);
        titleRow.Children.Add(titleBlock);
        header.Children.Add(titleRow);

        var pathRow = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
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
            var quick = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            quick.Children.Add(SmallNavButton(L("Desktop"), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
            quick.Children.Add(SmallNavButton(L("Documents"), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
            quick.Children.Add(SmallNavButton(L("Downloads"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")));
            header.Children.Add(quick);
        }

        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
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

        var filterRow = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
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
        list.FontSize = 12;
        list.Padding = new Thickness(0);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        dock.Children.Add(list);

        return GhostTheme.Card(dock, new Thickness(11), 12);
    }

    private Border BuildTransfers()
    {
        var dock = new DockPanel();
        var header = new Grid { Margin = new Thickness(0, 0, 0, 7) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(GhostTheme.Text(L("Transfers"), 14, weight: FontWeights.SemiBold));
        _queueSummary.Margin = new Thickness(10, 0, 0, 0);
        left.Children.Add(_queueSummary);
        header.Children.Add(left);

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        var retry = GhostTheme.Button(L("RetrySelected"));
        retry.Click += (_, _) => RetrySelectedTransfers();
        var cancel = GhostTheme.Button(L("CancelSelected"));
        cancel.Margin = new Thickness(6, 0, 0, 0);
        cancel.Click += (_, _) => CancelSelectedTransfer();
        var cancelAll = GhostTheme.Button(L("CancelAll"));
        cancelAll.Margin = new Thickness(6, 0, 0, 0);
        cancelAll.Click += (_, _) => CancelAllTransfers();
        var clear = GhostTheme.Button(L("ClearFinished"));
        clear.Margin = new Thickness(6, 0, 0, 0);
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
        _queueList.FontSize = 11.5;
        ScrollViewer.SetHorizontalScrollBarVisibility(_queueList, ScrollBarVisibility.Disabled);
        dock.Children.Add(_queueList);
        return GhostTheme.Card(dock, new Thickness(11), 12);
    }

    private Border BuildStatusBar()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = GhostTheme.Text("No telemetry · No tracking · local profiles stay on this device", 9.5, muted: true);
        left.VerticalAlignment = VerticalAlignment.Center;
        left.Margin = new Thickness(12, 0, 0, 0);
        grid.Children.Add(left);

        _statusBadge.CornerRadius = new CornerRadius(999);
        _statusBadge.Padding = new Thickness(10, 3, 10, 3);
        _statusBadge.Background = GhostTheme.R("Surface2");
        _statusBadge.Child = _statusText;
        _statusBadge.VerticalAlignment = VerticalAlignment.Center;
        _statusBadge.Margin = new Thickness(0, 0, 12, 0);
        Grid.SetColumn(_statusBadge, 1);
        grid.Children.Add(_statusBadge);
        return GhostTheme.Surface(grid, new Thickness(0), 0);
    }

    private Button SmallNavButton(string text, string path)
    {
        var button = GhostTheme.Button(text, subtle: true);
        button.Margin = new Thickness(0, 0, 5, 0);
        button.Padding = new Thickness(8, 3, 8, 3);
        button.MinHeight = 26;
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
