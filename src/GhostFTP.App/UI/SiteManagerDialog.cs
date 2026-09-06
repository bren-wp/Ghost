using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Design;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GhostFTP.UI;

internal sealed class SiteManagerDialog : GhostDialog
{
    private readonly ObservableCollection<ServerProfile> _profiles;
    private readonly Dictionary<Guid, string> _passwords = [];
    private readonly ListBox _sites = new();
    private readonly TextBox _name = GhostTheme.TextBox();
    private readonly TextBox _host = GhostTheme.TextBox();
    private readonly TextBox _port = GhostTheme.TextBox("21");
    private readonly ComboBox _security = new GhostComboBox();
    private readonly TextBox _username = GhostTheme.TextBox();
    private readonly PasswordBox _password = GhostTheme.PasswordBox();
    private readonly TextBox _initialPath = GhostTheme.TextBox("/");
    private readonly CheckBox _remember;
    private readonly TextBlock _selectionHint = GhostTheme.Text(
        R("SelectSavedSite"),
        10.5,
        muted: true);
    private readonly ContentControl _pageHost = new();

    private Button? _generalPageButton;
    private Button? _advancedPageButton;
    private UIElement? _generalPage;
    private UIElement? _advancedPage;

    public IReadOnlyList<ServerProfile> Profiles => _profiles.ToArray();
    public IReadOnlyDictionary<Guid, string> Passwords => _passwords;
    public Guid? ConnectProfileId { get; private set; }

    private static string R(string key) => GhostReferenceText.T(key);

    public SiteManagerDialog(
        Window owner,
        IEnumerable<ServerProfile> profiles,
        Func<ServerProfile, string> passwordGetter)
        : base(owner, R("SiteManager"), 980, 650)
    {
        ResizeMode = ResizeMode.CanResizeWithGrip;
        MinWidth = 820;
        MinHeight = 560;

        _profiles = new ObservableCollection<ServerProfile>(profiles.Select(x => x.Clone()));
        foreach (var profile in _profiles.Where(x => !x.IsDemo))
            _passwords[profile.Id] = passwordGetter(profile);

        _security.ItemsSource = new[] { "FTP (plain)", "FTPS explicit TLS", "FTPS implicit TLS" };
        _remember = Check(L("RememberPassword"), false);

        _name.MaxLength = 128;
        _host.MaxLength = 253;
        _port.MaxLength = 5;
        _username.MaxLength = 512;
        _initialPath.MaxLength = 4096;

        Content = BuildContent();
        if (_profiles.Count > 0)
            _sites.SelectedIndex = 0;
    }

    private UIElement BuildContent()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300), MinWidth = 250, MaxWidth = 340 });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 430 });

        var left = BuildSiteList();
        Grid.SetColumn(left, 0);
        root.Children.Add(left);

        var right = BuildEditor();
        Grid.SetColumn(right, 2);
        root.Children.Add(right);
        return root;
    }

    private Border BuildSiteList()
    {
        var dock = new DockPanel();
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        heading.Children.Add(GhostTheme.Text(R("SavedSites"), 18, weight: FontWeights.SemiBold));
        heading.Children.Add(GhostTheme.Text(
            R("ManageSavedSites"),
            10.5,
            muted: true));
        DockPanel.SetDock(heading, Dock.Top);
        dock.Children.Add(heading);

        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var add = GhostTheme.Button("＋ " + R("NewSite"), primary: true);
        add.Click += (_, _) => AddSite();
        var remove = GhostTheme.Button(L("Remove"), danger: true);
        remove.Click += (_, _) => RemoveSelected();
        Grid.SetColumn(add, 0);
        Grid.SetColumn(remove, 2);
        actions.Children.Add(add);
        actions.Children.Add(remove);
        DockPanel.SetDock(actions, Dock.Bottom);
        dock.Children.Add(actions);

        _sites.ItemsSource = _profiles;
        _sites.DisplayMemberPath = nameof(ServerProfile.Name);
        _sites.Background = GhostTheme.R("Surface2");
        _sites.Foreground = GhostTheme.R("Text");
        _sites.BorderBrush = GhostTheme.R("Border");
        _sites.BorderThickness = new Thickness(1);
        _sites.Padding = new Thickness(4);
        _sites.SelectionChanged += (_, _) => LoadSelected();
        dock.Children.Add(_sites);
        return GhostTheme.Card(dock, new Thickness(14), 14);
    }

    private Border BuildEditor()
    {
        var dock = new DockPanel();

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var connect = GhostTheme.Button(L("Connect"), primary: true);
        connect.MinWidth = 110;
        connect.Click += (_, _) => SaveAndClose(connect: true);
        var save = GhostTheme.Button(L("Save"));
        save.MinWidth = 90;
        save.Click += (_, _) => SaveAndClose(connect: false);
        var cancel = GhostTheme.Button(L("Cancel"));
        cancel.MinWidth = 90;
        cancel.Click += (_, _) => Close();

        Grid.SetColumn(connect, 1);
        Grid.SetColumn(save, 3);
        Grid.SetColumn(cancel, 5);
        footer.Children.Add(connect);
        footer.Children.Add(save);
        footer.Children.Add(cancel);
        DockPanel.SetDock(footer, Dock.Bottom);
        dock.Children.Add(footer);

        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text(R("SiteManager"), 22, weight: FontWeights.SemiBold));
        body.Children.Add(_selectionHint);
        body.Children.Add(Spacer(14));

        _generalPage = BuildGeneralPage();
        _advancedPage = BuildAdvancedPage();
        body.Children.Add(BuildPageSelector());
        body.Children.Add(Spacer(8));

        _pageHost.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _pageHost.Content = _generalPage;
        body.Children.Add(GhostTheme.Surface(_pageHost, new Thickness(2), 10));
        SetEditorPage(advanced: false);

        var scroll = new ScrollViewer
        {
            Content = body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        dock.Children.Add(scroll);
        return GhostTheme.Card(dock, new Thickness(14), 14);
    }

    private UIElement BuildPageSelector()
    {
        var selector = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = GhostTheme.R("Surface2")
        };
        selector.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        selector.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        selector.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

        _generalPageButton = GhostTheme.Button(R("General"), subtle: true);
        _generalPageButton.MinHeight = 34;
        _generalPageButton.Click += (_, _) => SetEditorPage(advanced: false);
        _advancedPageButton = GhostTheme.Button(R("Advanced"), subtle: true);
        _advancedPageButton.MinHeight = 34;
        _advancedPageButton.Click += (_, _) => SetEditorPage(advanced: true);

        Grid.SetColumn(_generalPageButton, 0);
        Grid.SetColumn(_advancedPageButton, 2);
        selector.Children.Add(_generalPageButton);
        selector.Children.Add(_advancedPageButton);
        return selector;
    }

    private void SetEditorPage(bool advanced)
    {
        if (_generalPageButton is null || _advancedPageButton is null || _generalPage is null || _advancedPage is null)
            return;

        _pageHost.Content = advanced ? _advancedPage : _generalPage;
        _generalPageButton.Background = GhostTheme.R(advanced ? "Surface2" : "AccentSoft");
        _generalPageButton.Foreground = GhostTheme.R(advanced ? "Muted" : "Text");
        _advancedPageButton.Background = GhostTheme.R(advanced ? "AccentSoft" : "Surface2");
        _advancedPageButton.Foreground = GhostTheme.R(advanced ? "Text" : "Muted");
        _generalPageButton.FontWeight = advanced ? FontWeights.Normal : FontWeights.SemiBold;
        _advancedPageButton.FontWeight = advanced ? FontWeights.SemiBold : FontWeights.Normal;
    }

    private UIElement BuildGeneralPage()
    {
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(GhostTheme.Field(R("SiteName"), _name));
        panel.Children.Add(Spacer(10));

        var hostRow = new Grid();
        hostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        hostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        var hostField = GhostTheme.Field(
            R("HostUrl"),
            _host,
            R("HostHint"));
        var portField = GhostTheme.Field(L("Port"), _port);
        Grid.SetColumn(hostField, 0);
        Grid.SetColumn(portField, 2);
        hostRow.Children.Add(hostField);
        hostRow.Children.Add(portField);
        panel.Children.Add(hostRow);
        panel.Children.Add(Spacer(10));

        panel.Children.Add(GhostTheme.Field(
            L("Security"),
            _security,
            R("ExplicitFtpsRecommended") + ". Invalid TLS certificates are never bypassed."));
        panel.Children.Add(Spacer(10));

        var authRow = new Grid();
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        authRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var userField = GhostTheme.Field(L("Username"), _username);
        var passwordField = GhostTheme.Field(L("Password"), _password);
        Grid.SetColumn(userField, 0);
        Grid.SetColumn(passwordField, 2);
        authRow.Children.Add(userField);
        authRow.Children.Add(passwordField);
        panel.Children.Add(authRow);
        panel.Children.Add(_remember);

        return panel;
    }

    private UIElement BuildAdvancedPage()
    {
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(GhostTheme.Field(
            R("DefaultRemotePath"),
            _initialPath,
            R("ServerRootHint")));
        panel.Children.Add(Spacer(14));

        var passive = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(R("PassiveConnections"), 12.5, weight: FontWeights.SemiBold),
                GhostTheme.Text(
                    R("PassiveDescription"),
                    10.5,
                    muted: true)
            }
        }, new Thickness(12), 10);
        panel.Children.Add(passive);
        panel.Children.Add(Spacer(10));

        var reliability = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(R("TimeoutsRetries"), 12.5, weight: FontWeights.SemiBold),
                GhostTheme.Text(
                    R("TimeoutsDescription"),
                    10.5,
                    muted: true)
            }
        }, new Thickness(12), 10);
        panel.Children.Add(reliability);

        return panel;
    }

    private void AddSite()
    {
        var profile = new ServerProfile
        {
            Id = Guid.NewGuid(),
            Name = R("NewSite"),
            Port = 21,
            Security = FtpSecurityMode.ExplicitTls,
            InitialPath = "/"
        };
        _profiles.Add(profile);
        _passwords[profile.Id] = string.Empty;
        _sites.SelectedItem = profile;
        _name.Focus();
        _name.SelectAll();
    }

    private void RemoveSelected()
    {
        if (_sites.SelectedItem is not ServerProfile selected || selected.IsDemo)
            return;

        _passwords.Remove(selected.Id);
        var index = _sites.SelectedIndex;
        _profiles.Remove(selected);
        if (_profiles.Count > 0)
            _sites.SelectedIndex = Math.Clamp(index, 0, _profiles.Count - 1);
    }

    private void LoadSelected()
    {
        if (_sites.SelectedItem is not ServerProfile selected)
        {
            SetEditorEnabled(false);
            return;
        }

        SetEditorEnabled(!selected.IsDemo);
        _selectionHint.Text = selected.IsDemo
            ? R("DemoLocked")
            : R("EditSavedSite");
        _name.Text = selected.Name;
        _host.Text = selected.Host;
        _port.Text = selected.Port.ToString();
        _security.SelectedIndex = (int)selected.Security;
        _username.Text = selected.Username;
        _initialPath.Text = selected.InitialPath;
        _remember.IsChecked = selected.RememberPassword;
        _password.Password = selected.IsDemo
            ? string.Empty
            : _passwords.GetValueOrDefault(selected.Id, string.Empty);
    }

    private void SetEditorEnabled(bool enabled)
    {
        _name.IsEnabled = enabled;
        _host.IsEnabled = enabled;
        _port.IsEnabled = enabled;
        _security.IsEnabled = enabled;
        _username.IsEnabled = enabled;
        _password.IsEnabled = enabled;
        _initialPath.IsEnabled = enabled;
        _remember.IsEnabled = enabled;
    }

    private void SaveAndClose(bool connect)
    {
        try
        {
            if (_sites.SelectedItem is not ServerProfile selected)
                throw new InvalidOperationException("Select a saved site first.");

            if (!selected.IsDemo)
            {
                if (string.IsNullOrWhiteSpace(_name.Text))
                    throw new InvalidOperationException("Site name is required.");
                if (!int.TryParse(_port.Text.Trim(), out var parsedPort))
                    throw new InvalidOperationException("Port must be a number between 1 and 65535.");
                if (_security.SelectedIndex is < 0 or > 2)
                    throw new InvalidOperationException("Select a valid FTP security mode.");

                var name = _name.Text.Trim();
                if (name.Length > 128 || name.Any(ch => ch is '\r' or '\n' or '\0'))
                    throw new InvalidOperationException("Site name contains invalid characters.");

                selected.Name = name;
                selected.Host = InputGuard.Host(_host.Text);
                selected.Port = InputGuard.Port(parsedPort);
                selected.Security = (FtpSecurityMode)_security.SelectedIndex;
                selected.Username = InputGuard.CommandArgument(_username.Text.Trim(), "username");
                selected.InitialPath = string.IsNullOrWhiteSpace(_initialPath.Text)
                    ? "/"
                    : InputGuard.RemotePath(_initialPath.Text.Trim());
                selected.RememberPassword = _remember.IsChecked == true;
                selected.IsDemo = false;
                _passwords[selected.Id] = InputGuard.CommandArgument(_password.Password, "password");
            }

            ConnectProfileId = connect ? selected.Id : null;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            GhostMessageDialog.Error(
                this,
                L("OperationFailed"),
                ex.Message,
                GhostBrand.DisplayName);
        }
    }
}
