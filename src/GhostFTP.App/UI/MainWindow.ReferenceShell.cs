using GhostFTP.Core.Models;
using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private ListBox? _referenceSitesList;
    private Button? _referenceNewFolderButton;
    private Button? _referenceRenameButton;
    private Button? _referenceDeleteButton;

    private static string R(string key) => GhostReferenceText.T(key);

    private UIElement BuildReferenceShell(UIElement workstation)
    {
        NormalizeReferenceWorkstation(workstation);

        var root = new Grid
        {
            Background = GhostTheme.R("Bg")
        };
        root.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(GhostReferencePalette.SidebarWidth),
            MinWidth = 250,
            MaxWidth = 360
        });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = BuildReferenceSidebar();
        Grid.SetColumn(sidebar, 0);
        root.Children.Add(sidebar);

        var mainHost = new Grid { Background = GhostTheme.R("Bg") };
        mainHost.Children.Add(workstation);
        mainHost.Children.Add(BuildReferenceHeaderControls());
        Grid.SetColumn(mainHost, 1);
        root.Children.Add(mainHost);

        return root;
    }

    private void NormalizeReferenceWorkstation(UIElement workstation)
    {
        if (workstation is not Grid root || root.RowDefinitions.Count < 4)
            return;

        root.RowDefinitions[0].Height = new GridLength(GhostReferencePalette.MenuHeight);
        root.RowDefinitions[1].Height = new GridLength(GhostReferencePalette.ToolbarHeight);
        root.RowDefinitions[3].Height = new GridLength(28);

        var menu = root.Children
            .OfType<Menu>()
            .FirstOrDefault(x => Grid.GetRow(x) == 0);
        if (menu is not null)
        {
            menu.Background = ReferenceBrush(GhostReferencePalette.Menu);
            menu.Padding = new Thickness(7, 0, 430, 0);
            LocalizeAndOrderReferenceMenu(menu);
        }

        var toolbar = root.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetRow(x) == 1);
        if (toolbar is not null)
        {
            toolbar.Background = ReferenceBrush(GhostReferencePalette.Toolbar);
            toolbar.BorderBrush = GhostTheme.R("Border");
            toolbar.BorderThickness = new Thickness(0, 0, 0, 1);
            toolbar.Padding = new Thickness(0);

            if (toolbar.Child is DockPanel dock)
            {
                if (dock.Children.Count > 1 && dock.Children[0] is StackPanel identity)
                    dock.Children.Remove(identity);

                StyleReferenceToolbar(dock);
            }
        }

        NormalizeReferenceWorkspace();
    }

    private static void LocalizeAndOrderReferenceMenu(Menu menu)
    {
        var items = menu.Items.OfType<MenuItem>().Take(6).ToArray();
        if (items.Length != 6)
            return;

        items[0].Header = R("FileMenu");
        items[1].Header = R("ViewMenu");
        items[2].Header = R("SitesMenu");
        items[3].Header = R("TransfersMenu");
        items[4].Header = R("ToolsMenu");
        items[5].Header = R("HelpMenu");

        menu.Items.Clear();
        menu.Items.Add(items[0]);
        menu.Items.Add(items[1]);
        menu.Items.Add(items[3]);
        menu.Items.Add(items[2]);
        menu.Items.Add(items[4]);
        menu.Items.Add(items[5]);
    }

    private void StyleReferenceToolbar(DockPanel dock)
    {
        var actions = dock.Children.OfType<WrapPanel>().FirstOrDefault();
        if (actions is null)
            return;

        EnsureReferenceToolbarActions(actions);
        actions.Margin = new Thickness(0);
        actions.VerticalAlignment = VerticalAlignment.Stretch;

        foreach (var button in actions.Children.OfType<Button>())
        {
            button.MinWidth = 82;
            button.Width = double.NaN;
            button.MinHeight = GhostReferencePalette.ToolbarHeight - 1;
            button.Margin = new Thickness(0);
            button.Padding = new Thickness(12, 6, 12, 5);
            button.Background = Brushes.Transparent;
            button.BorderBrush = GhostTheme.R("Border");
            button.BorderThickness = new Thickness(0, 0, 1, 0);
            button.HorizontalContentAlignment = HorizontalAlignment.Center;
            button.VerticalContentAlignment = VerticalAlignment.Center;

            if (button.Content is string text)
                button.Content = ReferenceToolbarContent(text);
        }

        _localList.SelectionChanged += (_, _) => UpdateReferenceToolbarState();
        _remoteList.SelectionChanged += (_, _) => UpdateReferenceToolbarState();
        _localList.GotKeyboardFocus += (_, _) => UpdateReferenceToolbarState();
        _remoteList.GotKeyboardFocus += (_, _) => UpdateReferenceToolbarState();
        UpdateReferenceToolbarState();
    }

    private void EnsureReferenceToolbarActions(WrapPanel actions)
    {
        if (_referenceNewFolderButton is not null)
            return;

        _referenceNewFolderButton = ToolButton($"▣ {L("NewFolder")}", ReferenceNewFolderAsync);
        _referenceRenameButton = ToolButton($"✎ {L("Rename")}", ReferenceRenameAsync);
        _referenceDeleteButton = ToolButton($"⌫ {L("Delete")}", ReferenceDeleteAsync, danger: true);

        var insertionIndex = Math.Min(5, actions.Children.Count);
        actions.Children.Insert(insertionIndex++, _referenceNewFolderButton);
        actions.Children.Insert(insertionIndex++, _referenceRenameButton);
        actions.Children.Insert(insertionIndex, _referenceDeleteButton);
    }

    private bool ReferenceTargetRemote() =>
        _remoteList.IsKeyboardFocusWithin
        || (_remoteList.SelectedItems.Count > 0 && _localList.SelectedItems.Count == 0);

    private async Task ReferenceNewFolderAsync()
    {
        if (ReferenceTargetRemote())
        {
            if (IsConnected)
                await NewRemoteFolderAsync();
            return;
        }

        NewLocalFolder();
    }

    private async Task ReferenceRenameAsync()
    {
        if (ReferenceTargetRemote())
        {
            if (IsConnected && _remoteList.SelectedItems.Count > 0)
                await RenameRemoteSelectedAsync();
            return;
        }

        if (_localList.SelectedItems.Count > 0)
            RenameLocalSelected();
    }

    private async Task ReferenceDeleteAsync()
    {
        if (ReferenceTargetRemote())
        {
            if (IsConnected && _remoteList.SelectedItems.Count > 0)
                await DeleteRemoteSelectedAsync();
            return;
        }

        if (_localList.SelectedItems.Count > 0)
            DeleteLocalSelected();
    }

    private void UpdateReferenceToolbarState()
    {
        if (_referenceNewFolderButton is null || _referenceRenameButton is null || _referenceDeleteButton is null)
            return;

        var remote = ReferenceTargetRemote();
        var hasSelection = remote ? _remoteList.SelectedItems.Count > 0 : _localList.SelectedItems.Count > 0;
        _referenceNewFolderButton.IsEnabled = !remote || IsConnected;
        _referenceRenameButton.IsEnabled = hasSelection && (!remote || IsConnected);
        _referenceDeleteButton.IsEnabled = hasSelection && (!remote || IsConnected);
    }

    private static UIElement ReferenceToolbarContent(string text)
    {
        var split = text.IndexOf(' ');
        var icon = split > 0 ? text[..split] : "·";
        var label = split > 0 ? text[(split + 1)..].Trim() : text;
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var iconText = GhostTheme.Text(icon, 18, weight: FontWeights.SemiBold);
        iconText.Foreground = GhostTheme.R("Accent");
        iconText.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(iconText);
        var labelText = GhostTheme.Text(label, 9.75, weight: FontWeights.Medium);
        labelText.HorizontalAlignment = HorizontalAlignment.Center;
        labelText.Margin = new Thickness(0, 4, 0, 0);
        stack.Children.Add(labelText);
        return stack;
    }

    private void NormalizeReferenceWorkspace()
    {
        if (_workspaceContent is null || _workspaceContent.RowDefinitions.Count < 7)
            return;

        var log = _workspaceContent.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetRow(x) == 0);
        var quickConnect = _workspaceContent.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetRow(x) == 2);

        if (log is null || quickConnect is null)
            return;

        NormalizeQuickConnect(quickConnect);

        _workspaceContent.Children.Remove(log);
        _workspaceContent.Children.Remove(quickConnect);

        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 360 });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.08, GridUnitType.Star), MinWidth = 390 });

        log.Margin = new Thickness(0);
        quickConnect.Margin = new Thickness(0);
        Grid.SetRow(log, 0);
        Grid.SetColumn(log, 0);
        Grid.SetRow(quickConnect, 0);
        Grid.SetColumn(quickConnect, 2);
        top.Children.Add(log);
        top.Children.Add(quickConnect);

        _workspaceContent.RowDefinitions[0].Height = new GridLength(205);
        _workspaceContent.RowDefinitions[0].MinHeight = 170;
        _workspaceContent.RowDefinitions[0].MaxHeight = 245;
        _workspaceContent.RowDefinitions[1].Height = new GridLength(8);
        _workspaceContent.RowDefinitions[2].MinHeight = 0;
        _workspaceContent.RowDefinitions[2].MaxHeight = 0;
        _workspaceContent.RowDefinitions[2].Height = new GridLength(0);
        _workspaceContent.RowDefinitions[3].Height = new GridLength(0);

        Grid.SetRow(top, 0);
        _workspaceContent.Children.Add(top);
    }

    private void NormalizeQuickConnect(Border quickConnect)
    {
        if (quickConnect.Child is not StackPanel root || root.Children.Count < 3)
            return;
        if (root.Children[0] is not Grid header
            || root.Children[1] is not Grid rowOne
            || root.Children[2] is not Grid rowTwo)
            return;

        var title = header.Children.OfType<StackPanel>().FirstOrDefault();
        if (title is not null)
        {
            header.Children.Remove(title);
            header.Children.Clear();
            header.ColumnDefinitions.Clear();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(title, 0);
            header.Children.Add(title);
        }
        _profilesList.Visibility = Visibility.Collapsed;

        var username = rowTwo.Children.Cast<UIElement>().FirstOrDefault(x => Grid.GetColumn(x) == 0);
        var password = rowTwo.Children.Cast<UIElement>().FirstOrDefault(x => Grid.GetColumn(x) == 2);
        if (username is not null && password is not null)
        {
            rowTwo.Children.Remove(username);
            rowTwo.Children.Remove(password);
            rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 92 });
            rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            rowOne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 92 });
            Grid.SetColumn(username, 6);
            Grid.SetColumn(password, 8);
            rowOne.Children.Add(username);
            rowOne.Children.Add(password);
        }

        var note = GhostTheme.Text(R("CredentialsLocal"), 9, muted: true);
        note.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(note, 0);
        Grid.SetColumnSpan(note, 3);
        rowTwo.Children.Add(note);
    }

    private Border BuildReferenceSidebar()
    {
        var root = new Grid
        {
            Background = ReferenceBrush(GhostReferencePalette.Sidebar),
            Margin = new Thickness(0)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

        var brand = new StackPanel
        {
            Margin = new Thickness(20, 18, 16, 12),
            VerticalAlignment = VerticalAlignment.Top
        };
        var brandRow = new StackPanel { Orientation = Orientation.Horizontal };
        brandRow.Children.Add(GhostBrand.IconControl(38));
        var name = new StackPanel { Margin = new Thickness(9, 1, 0, 0) };
        name.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 15, weight: FontWeights.Bold));
        name.Children.Add(GhostTheme.Text(R("PrivateFileClient"), 7.5, muted: true, weight: FontWeights.SemiBold));
        brandRow.Children.Add(name);
        brand.Children.Add(brandRow);
        var tagline = GhostTheme.Text(R("Tagline"), 10.5, muted: true);
        tagline.Margin = new Thickness(0, 10, 0, 0);
        brand.Children.Add(tagline);
        Grid.SetRow(brand, 0);
        root.Children.Add(brand);

        var nav = new StackPanel { Margin = new Thickness(12, 8, 12, 8) };
        var savedHeader = new Grid { Margin = new Thickness(8, 0, 2, 8) };
        savedHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        savedHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        savedHeader.Children.Add(GhostTheme.Text(L("SavedServers"), 12.5, weight: FontWeights.Bold));
        var add = ReferenceSquareButton("＋", async () => await AddProfileAsync());
        Grid.SetColumn(add, 1);
        savedHeader.Children.Add(add);
        nav.Children.Add(savedHeader);

        nav.Children.Add(ReferenceNavButton("⌂", R("Home"), () =>
        {
            RefreshLocal();
            if (IsConnected) _ = RefreshRemoteAsync();
        }));

        var sitesLabel = new Grid { Margin = new Thickness(8, 8, 8, 3) };
        sitesLabel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sitesLabel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sitesLabel.Children.Add(GhostTheme.Text("▣  " + R("ThisTab"), 11.5, weight: FontWeights.SemiBold));
        var count = GhostTheme.Text("0", 10.5, muted: true);
        count.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Count") { Source = _profiles });
        Grid.SetColumn(count, 1);
        sitesLabel.Children.Add(count);
        nav.Children.Add(sitesLabel);

        _referenceSitesList = new ListBox
        {
            ItemsSource = _profiles,
            DisplayMemberPath = nameof(ServerProfile.Name),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = GhostTheme.R("Text"),
            MaxHeight = 180,
            Margin = new Thickness(7, 0, 7, 2)
        };
        _referenceSitesList.SelectionChanged += (_, _) =>
        {
            if (_referenceSitesList.SelectedItem is not ServerProfile profile)
                return;
            _profilesList.SelectedItem = profile;
            ProfileSelected();
        };
        nav.Children.Add(_referenceSitesList);

        var empty = GhostTheme.Text(R("NoSavedConnection"), 10, muted: true);
        empty.Margin = new Thickness(24, 4, 8, 12);
        empty.Visibility = _profiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _profiles.CollectionChanged += (_, _) =>
            empty.Visibility = _profiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        nav.Children.Add(empty);
        nav.Children.Add(ReferenceNavButton("☆", R("FavoritesInTab"), () => _ = OpenSiteManagerAsync()));
        nav.Children.Add(ReferenceNavButton("◷", R("RecentInTab"), () => _ = ShowConnectionDiagnosticsAsync()));
        Grid.SetRow(nav, 1);
        root.Children.Add(nav);

        var privacy = new Border
        {
            Margin = new Thickness(12, 8, 12, 10),
            Padding = new Thickness(14, 12, 14, 12),
            Background = GhostTheme.R("Surface2"),
            BorderBrush = GhostTheme.R("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(GhostReferencePalette.CardRadius)
        };
        var privacyStack = new StackPanel();
        privacyStack.Children.Add(GhostTheme.Text("◇  " + R("AccountNotRequired"), 10.5, weight: FontWeights.Bold));
        var privacyText = GhostTheme.Text(R("PrivacyDescription"), 9.25, muted: true);
        privacyText.Margin = new Thickness(0, 5, 0, 0);
        privacyStack.Children.Add(privacyText);
        privacy.Child = privacyStack;
        Grid.SetRow(privacy, 2);
        root.Children.Add(privacy);

        var footer = new Grid
        {
            Background = ReferenceBrush(GhostReferencePalette.Sidebar),
            Margin = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var settings = ReferenceFooterButton("⚙  " + L("Settings"), async () => await OpenSettingsAsync());
        var about = ReferenceFooterButton("ⓘ  " + L("About"), () => new AboutDialog(this).ShowDialog());
        Grid.SetColumn(settings, 0);
        Grid.SetColumn(about, 1);
        footer.Children.Add(settings);
        footer.Children.Add(about);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        return new Border
        {
            Background = ReferenceBrush(GhostReferencePalette.Sidebar),
            BorderBrush = GhostTheme.R("Border"),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = root
        };
    }

    private UIElement BuildReferenceHeaderControls()
    {
        var overlay = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 430,
            Height = GhostReferencePalette.MenuHeight + GhostReferencePalette.ToolbarHeight,
            Margin = new Thickness(0, 0, 14, 0),
            Background = Brushes.Transparent,
            IsHitTestVisible = true
        };
        overlay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GhostReferencePalette.MenuHeight) });
        overlay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GhostReferencePalette.ToolbarHeight) });

        var language = new Button
        {
            Content = "☆  " + CurrentLanguageName() + " ⌄",
            Background = Brushes.Transparent,
            Foreground = GhostTheme.R("Muted"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 5, 10, 5),
            FontFamily = GhostTheme.UiFont,
            FontSize = 11,
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        language.Click += async (_, _) => await OpenSettingsAsync();
        Grid.SetRow(language, 0);
        overlay.Children.Add(language);

        var searchHost = new Border
        {
            Width = 342,
            Height = 42,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Background = GhostTheme.R("Surface2"),
            BorderBrush = GhostTheme.R("Border"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(GhostReferencePalette.FieldRadius),
            Padding = new Thickness(11, 0, 11, 0),
            Margin = new Thickness(0, 0, 0, 2)
        };
        var searchGrid = new Grid();
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var icon = GhostTheme.Text("⌕", 19, muted: true);
        Grid.SetColumn(icon, 0);
        searchGrid.Children.Add(icon);

        var watermark = GhostTheme.Text(R("SearchRemote"), 10.5, muted: true);
        watermark.VerticalAlignment = VerticalAlignment.Center;
        watermark.IsHitTestVisible = false;
        Grid.SetColumn(watermark, 1);
        searchGrid.Children.Add(watermark);

        var search = GhostTheme.TextBox();
        search.Background = Brushes.Transparent;
        search.BorderThickness = new Thickness(0);
        search.Padding = new Thickness(0);
        search.MinHeight = 34;
        search.ToolTip = R("SearchRemote");
        search.TextChanged += (_, _) =>
        {
            watermark.Visibility = string.IsNullOrEmpty(search.Text) ? Visibility.Visible : Visibility.Collapsed;
            _remoteFilter.Text = search.Text;
        };
        Grid.SetColumn(search, 1);
        searchGrid.Children.Add(search);
        searchHost.Child = searchGrid;
        Grid.SetRow(searchHost, 1);
        overlay.Children.Add(searchHost);

        return overlay;
    }

    private Button ReferenceNavButton(string icon, string text, Action action)
    {
        var button = GhostTheme.Button($"{icon}  {text}", subtle: true);
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.Padding = new Thickness(8, 6, 8, 6);
        button.Margin = new Thickness(0, 1, 0, 1);
        button.MinHeight = 34;
        button.Click += (_, _) => action();
        return button;
    }

    private Button ReferenceSquareButton(string text, Func<Task> action)
    {
        var button = GhostTheme.Button(text, subtle: true);
        button.Width = 34;
        button.Height = 34;
        button.Padding = new Thickness(0);
        button.Click += async (_, _) => await action();
        return button;
    }

    private Button ReferenceFooterButton(string text, Action action)
    {
        var button = GhostTheme.Button(text, subtle: true);
        button.Margin = new Thickness(4, 6, 4, 6);
        button.Padding = new Thickness(8, 4, 8, 4);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.Click += (_, _) => action();
        return button;
    }

    private Button ReferenceFooterButton(string text, Func<Task> action)
    {
        var button = GhostTheme.Button(text, subtle: true);
        button.Margin = new Thickness(4, 6, 4, 6);
        button.Padding = new Thickness(8, 4, 8, 4);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.Click += async (_, _) => await action();
        return button;
    }

    private static string CurrentLanguageName() =>
        GhostLocalization.SupportedLanguages.FirstOrDefault(x =>
            string.Equals(x.Code, GhostLocalization.CurrentLanguageCode, StringComparison.OrdinalIgnoreCase))?.NativeName
        ?? "English";

    private static SolidColorBrush ReferenceBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
