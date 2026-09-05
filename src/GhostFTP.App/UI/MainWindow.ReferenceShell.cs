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
        }

        var toolbar = root.Children
            .OfType<Border>()
            .FirstOrDefault(x => Grid.GetRow(x) == 1);
        if (toolbar is not null)
        {
            toolbar.Background = ReferenceBrush(GhostReferencePalette.Toolbar);
            toolbar.BorderBrush = GhostTheme.R("Border");
            toolbar.BorderThickness = new Thickness(0, 0, 0, 1);

            // The approved reference keeps identity in the permanent left rail rather than
            // repeating it in the action toolbar. Remove only that known identity child.
            if (toolbar.Child is DockPanel dock
                && dock.Children.Count > 1
                && dock.Children[0] is StackPanel identity)
            {
                dock.Children.Remove(identity);
            }
        }

        NormalizeReferenceWorkspace();
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
        name.Children.Add(GhostTheme.Text("PRIVATE FILE CLIENT", 7.5, muted: true, weight: FontWeights.SemiBold));
        brandRow.Children.Add(name);
        brand.Children.Add(brandRow);
        var tagline = GhostTheme.Text("Private file transfers, simply.", 10.5, muted: true);
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

        nav.Children.Add(ReferenceNavButton("⌂", "Home", () =>
        {
            RefreshLocal();
            if (IsConnected) _ = RefreshRemoteAsync();
        }));

        var sitesLabel = new Grid { Margin = new Thickness(8, 8, 8, 3) };
        sitesLabel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sitesLabel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sitesLabel.Children.Add(GhostTheme.Text("▣  This tab", 11.5, weight: FontWeights.SemiBold));
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

        var empty = GhostTheme.Text("No saved connection in this tab.", 10, muted: true);
        empty.Margin = new Thickness(24, 4, 8, 12);
        nav.Children.Add(empty);
        nav.Children.Add(ReferenceNavButton("☆", "Favorites in this tab", () => _ = OpenSiteManagerAsync()));
        nav.Children.Add(ReferenceNavButton("◷", "Recent connections in this tab", () => _ = ShowConnectionDiagnosticsAsync()));
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
        privacyStack.Children.Add(GhostTheme.Text("◇  Account not required", 10.5, weight: FontWeights.Bold));
        var privacyText = GhostTheme.Text("Connection data exists only in local memory and local profile storage. Nothing is sent to a Ghost FTP account.", 9.25, muted: true);
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
        var search = GhostTheme.TextBox();
        search.Background = Brushes.Transparent;
        search.BorderThickness = new Thickness(0);
        search.Padding = new Thickness(0);
        search.MinHeight = 34;
        search.ToolTip = "Search remote files";
        search.TextChanged += (_, _) => _remoteFilter.Text = search.Text;
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
