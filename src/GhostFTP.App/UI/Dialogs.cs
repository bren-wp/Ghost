using GhostFTP.Core.Models;
using GhostFTP.Design;
using GhostFTP.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostFTP.UI;

internal abstract class GhostDialog : Window
{
    protected GhostDialog(Window owner, string title, double width = 540, double height = 440)
    {
        Owner = owner;
        Title = title;
        Width = width;
        Height = height;
        MinWidth = Math.Min(width, 460);
        MinHeight = Math.Min(height, 300);
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        ShowInTaskbar = false;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);
    }

    protected static CheckBox Check(string text, bool selected)
    {
        return new CheckBox
        {
            Content = text,
            IsChecked = selected,
            Foreground = GhostTheme.R("Text"),
            FontFamily = GhostTheme.UiFont,
            FontSize = 12.5,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        };
    }

    protected static Grid Footer(Button primary, Button? secondary = null)
    {
        var grid = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        if (secondary is not null)
        {
            secondary.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(secondary, 1);
            grid.Children.Add(secondary);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(primary, 2);
        }
        else
        {
            Grid.SetColumn(primary, 1);
        }
        grid.Children.Add(primary);
        return grid;
    }

    protected static Border Shell(UIElement content)
    {
        return GhostTheme.Card(content, new Thickness(24), 16);
    }
}

internal sealed class ProfileDialog : GhostDialog
{
    private readonly TextBox _name;
    private readonly TextBox _host;
    private readonly TextBox _port;
    private readonly TextBox _username;
    private readonly PasswordBox _password;
    private readonly ComboBox _security;
    private readonly TextBox _initialPath;
    private readonly CheckBox _remember;
    private readonly ServerProfile _profile;

    public string Password => _password.Password;
    public ServerProfile Result => _profile;

    public ProfileDialog(Window owner, ServerProfile profile, string existingPassword, bool isNew = false)
        : base(owner, isNew ? "Add server" : "Edit server", 590, 720)
    {
        ResizeMode = ResizeMode.CanResizeWithGrip;
        _profile = profile.Clone();
        _name = GhostTheme.TextBox(_profile.Name);
        _host = GhostTheme.TextBox(_profile.Host);
        _port = GhostTheme.TextBox(_profile.Port.ToString());
        _username = GhostTheme.TextBox(_profile.Username);
        _password = GhostTheme.PasswordBox();
        _password.Password = existingPassword;
        _security = GhostTheme.ComboBox();
        _security.ItemsSource = new[] { "FTP (plain)", "FTPS explicit TLS", "FTPS implicit TLS" };
        _security.SelectedIndex = (int)_profile.Security;
        _initialPath = GhostTheme.TextBox(_profile.InitialPath);
        _remember = Check("Remember password for this Windows user (DPAPI encrypted)", _profile.RememberPassword);

        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text("Server profile", 24, weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text(
            "Save connection details locally. Password storage is optional and protected by Windows DPAPI.",
            11.5,
            muted: true));
        body.Children.Add(new Border { Height = 18 });

        body.Children.Add(GhostTheme.Field("Profile name", _name));
        body.Children.Add(Spacer(12));
        body.Children.Add(GhostTheme.Field("Host", _host, "Hostname or IP address only; do not include ftp://."));
        body.Children.Add(Spacer(12));

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var portField = GhostTheme.Field("Port", _port);
        var securityField = GhostTheme.Field("Security", _security);
        Grid.SetColumn(portField, 0);
        Grid.SetColumn(securityField, 2);
        row.Children.Add(portField);
        row.Children.Add(securityField);
        body.Children.Add(row);
        body.Children.Add(Spacer(12));

        body.Children.Add(GhostTheme.Field("Username", _username));
        body.Children.Add(Spacer(12));
        body.Children.Add(GhostTheme.Field("Password", _password));
        body.Children.Add(_remember);
        body.Children.Add(Spacer(8));
        body.Children.Add(GhostTheme.Field("Initial remote path", _initialPath, "Use / for the server root."));

        var securityNote = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text("Security", 12, weight: FontWeights.SemiBold),
                GhostTheme.Text("FTPS Explicit is recommended. GhostFTP does not provide an option to bypass invalid TLS certificates.", 11, muted: true)
            }
        }, new Thickness(12), 10);
        securityNote.Margin = new Thickness(0, 14, 0, 0);
        body.Children.Add(securityNote);

        var save = GhostTheme.Button("Save server", primary: true);
        save.Click += (_, _) => Save();
        var cancel = GhostTheme.Button("Cancel");
        cancel.Click += (_, _) => Close();
        body.Children.Add(Footer(save, cancel));

        Content = new ScrollViewer
        {
            Content = Shell(body),
            Margin = new Thickness(16),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private void Save()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) throw new InvalidOperationException("Profile name is required.");
            if (string.IsNullOrWhiteSpace(_host.Text)) throw new InvalidOperationException("Host is required.");
            if (!int.TryParse(_port.Text, out var port) || port is < 1 or > 65535)
                throw new InvalidOperationException("Port must be between 1 and 65535.");

            _profile.Name = _name.Text.Trim();
            _profile.Host = _host.Text.Trim();
            _profile.Port = port;
            _profile.Username = _username.Text.Trim();
            _profile.Security = (FtpSecurityMode)Math.Max(0, _security.SelectedIndex);
            _profile.InitialPath = string.IsNullOrWhiteSpace(_initialPath.Text) ? "/" : _initialPath.Text.Trim();
            _profile.RememberPassword = _remember.IsChecked == true;
            _profile.IsDemo = false;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "GhostFTP", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static Border Spacer(double height) => new() { Height = height };
}

internal sealed class TextPromptDialog : GhostDialog
{
    private readonly TextBox _input;
    public string Value => _input.Text.Trim();

    public TextPromptDialog(Window owner, string title, string label, string value = "")
        : base(owner, title, 480, 270)
    {
        ResizeMode = ResizeMode.NoResize;
        _input = GhostTheme.TextBox(value);
        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text(title, 22, weight: FontWeights.SemiBold));
        body.Children.Add(new Border { Height = 16 });
        body.Children.Add(GhostTheme.Field(label, _input));
        var ok = GhostTheme.Button("Continue", primary: true);
        ok.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_input.Text)) DialogResult = true;
        };
        var cancel = GhostTheme.Button("Cancel");
        cancel.Click += (_, _) => Close();
        body.Children.Add(Footer(ok, cancel));
        Content = Shell(body);
        Padding = new Thickness(16);
        Loaded += (_, _) =>
        {
            _input.Focus();
            _input.SelectAll();
        };
    }
}

internal sealed class SettingsDialog : GhostDialog
{
    private readonly ComboBox _theme;
    private readonly CheckBox _confirmDeletes;
    private readonly CheckBox _showHidden;

    public AppTheme SelectedTheme => (AppTheme)Math.Max(0, _theme.SelectedIndex);
    public bool ConfirmDeletes => _confirmDeletes.IsChecked == true;
    public bool ShowHiddenFiles => _showHidden.IsChecked == true;

    public SettingsDialog(Window owner, AppSettings settings) : base(owner, "Settings", 540, 470)
    {
        ResizeMode = ResizeMode.NoResize;
        _theme = GhostTheme.ComboBox();
        _theme.ItemsSource = new[] { "Use Windows setting", "Dark", "Light" };
        _theme.SelectedIndex = (int)settings.Theme;
        _confirmDeletes = Check("Ask before deleting local or remote files and folders", settings.ConfirmDeletes);
        _showHidden = Check("Show hidden and system items in the local file pane", settings.ShowHiddenFiles);

        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text("Settings", 24, weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text("Workspace preferences are stored locally and never synchronized.", 11.5, muted: true));
        body.Children.Add(new Border { Height = 18 });
        body.Children.Add(GhostTheme.Field("Appearance", _theme));
        body.Children.Add(new Border { Height = 16 });
        body.Children.Add(GhostTheme.Text("File workspace", 12, weight: FontWeights.SemiBold));
        body.Children.Add(_confirmDeletes);
        body.Children.Add(_showHidden);

        var shortcuts = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text("Keyboard shortcuts", 12, weight: FontWeights.SemiBold),
                GhostTheme.Text("F5 Refresh · F2 Rename · Delete Remove · Ctrl+F Filter · Ctrl+L Path", 11, muted: true)
            }
        }, new Thickness(12), 10);
        shortcuts.Margin = new Thickness(0, 14, 0, 0);
        body.Children.Add(shortcuts);

        var save = GhostTheme.Button("Save settings", primary: true);
        save.Click += (_, _) => DialogResult = true;
        var cancel = GhostTheme.Button("Cancel");
        cancel.Click += (_, _) => Close();
        body.Children.Add(Footer(save, cancel));
        Content = Shell(body);
        Padding = new Thickness(16);
    }
}

internal sealed class AboutDialog : GhostDialog
{
    public AboutDialog(Window owner) : base(owner, "About GhostFTP", 560, 500)
    {
        ResizeMode = ResizeMode.NoResize;
        var body = new StackPanel();
        body.Children.Add(GhostTheme.Logo(58));
        body.Children.Add(new Border { Height = 14 });
        body.Children.Add(GhostTheme.Text("GhostFTP", 28, weight: FontWeights.SemiBold));
        var version = typeof(AboutDialog).Assembly.GetName().Version?.ToString(3) ?? "1.2.0";
        body.Children.Add(GhostTheme.Text($"Version {version} · Windows FTP / FTPS client", 11.5, muted: true));
        body.Children.Add(new Border { Height = 18 });

        body.Children.Add(GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text("Privacy by design", 12.5, weight: FontWeights.SemiBold),
                GhostTheme.Text("No telemetry, analytics, tracking SDK, ads or automatic update checker. Network traffic is created only by FTP/FTPS actions you initiate or website links you open yourself.", 11, muted: true)
            }
        }, new Thickness(12), 10));

        body.Children.Add(new Border { Height = 16 });
        body.Children.Add(GhostTheme.Text("Brendigo", 12.5, weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text("ghostftp.com · brendigo.com", 11.5, muted: true));

        var buttons = new WrapPanel { Margin = new Thickness(0, 20, 0, 0) };
        var web = GhostTheme.Button("Open ghostftp.com", primary: true);
        web.Click += (_, _) => OpenUrl("https://ghostftp.com");
        var author = GhostTheme.Button("Open Brendigo");
        author.Margin = new Thickness(8, 0, 0, 0);
        author.Click += (_, _) => OpenUrl("https://brendigo.com");
        buttons.Children.Add(web);
        buttons.Children.Add(author);
        body.Children.Add(buttons);

        Content = Shell(body);
        Padding = new Thickness(16);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Opening an external website is optional and must not affect the app session.
        }
    }
}
