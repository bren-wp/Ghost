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
    protected GhostDialog(Window? owner, string title, double width = 540, double height = 440)
    {
        if (owner is not null)
            Owner = owner;
        Title = title;
        Icon = GhostBrand.IconSource;
        Width = width;
        Height = height;
        MinWidth = Math.Min(width, 460);
        MinHeight = Math.Min(height, 300);
        WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        ShowInTaskbar = owner is null;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);
    }

    protected static string L(string key) => GhostLocalization.T(key);

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

    protected static Border Shell(UIElement content) => GhostTheme.Card(content, new Thickness(24), 16);
    protected static Border Spacer(double height) => new() { Height = height };
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
        : base(owner, isNew ? L("AddServer") : L("EditServer"), 590, 720)
    {
        ResizeMode = ResizeMode.CanResizeWithGrip;
        _profile = profile.Clone();
        _name = GhostTheme.TextBox(_profile.Name);
        _host = GhostTheme.TextBox(_profile.Host);
        _port = GhostTheme.TextBox(_profile.Port.ToString());
        _username = GhostTheme.TextBox(_profile.Username);
        _password = GhostTheme.PasswordBox();
        _password.Password = existingPassword;
        _security = new GhostComboBox();
        _security.ItemsSource = new[] { "FTP (plain)", "FTPS explicit TLS", "FTPS implicit TLS" };
        _security.SelectedIndex = (int)_profile.Security;
        _initialPath = GhostTheme.TextBox(_profile.InitialPath);
        _remember = Check(L("RememberPassword"), _profile.RememberPassword);

        _name.MaxLength = 128;
        _host.MaxLength = 253;
        _port.MaxLength = 5;
        _username.MaxLength = 512;
        _initialPath.MaxLength = 4096;

        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text(L("ServerProfile"), 24, weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text(
            "Connection details are stored locally. Password storage is optional and protected by Windows DPAPI.",
            11.5,
            muted: true));
        body.Children.Add(Spacer(18));

        body.Children.Add(GhostTheme.Field(L("ProfileName"), _name));
        body.Children.Add(Spacer(12));
        body.Children.Add(GhostTheme.Field(L("Host"), _host, "Hostname or IP address only; do not include ftp://."));
        body.Children.Add(Spacer(12));

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var portField = GhostTheme.Field(L("Port"), _port);
        var securityField = GhostTheme.Field(L("Security"), _security);
        Grid.SetColumn(portField, 0);
        Grid.SetColumn(securityField, 2);
        row.Children.Add(portField);
        row.Children.Add(securityField);
        body.Children.Add(row);
        body.Children.Add(Spacer(12));

        body.Children.Add(GhostTheme.Field(L("Username"), _username));
        body.Children.Add(Spacer(12));
        body.Children.Add(GhostTheme.Field(L("Password"), _password));
        body.Children.Add(_remember);
        body.Children.Add(Spacer(8));
        body.Children.Add(GhostTheme.Field(L("InitialRemotePath"), _initialPath, "Use / for the server root."));

        var securityNote = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(L("Security"), 12, weight: FontWeights.SemiBold),
                GhostTheme.Text("FTPS Explicit is recommended. Ghost FTP never provides a bypass for invalid TLS certificates.", 11, muted: true)
            }
        }, new Thickness(12), 10);
        securityNote.Margin = new Thickness(0, 14, 0, 0);
        body.Children.Add(securityNote);

        var save = GhostTheme.Button(L("SaveServer"), primary: true);
        save.Click += (_, _) => Save();
        var cancel = GhostTheme.Button(L("Cancel"));
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
            if (!int.TryParse(_port.Text.Trim(), out var port) || port is < 1 or > 65535)
                throw new InvalidOperationException("Port must be between 1 and 65535.");
            if (_security.SelectedIndex is < 0 or > 2)
                throw new InvalidOperationException("Select a valid FTP security mode.");

            _profile.Name = _name.Text.Trim();
            _profile.Host = _host.Text.Trim();
            _profile.Port = port;
            _profile.Username = _username.Text.Trim();
            _profile.Security = (FtpSecurityMode)_security.SelectedIndex;
            _profile.InitialPath = string.IsNullOrWhiteSpace(_initialPath.Text) ? "/" : _initialPath.Text.Trim();
            _profile.RememberPassword = _remember.IsChecked == true;
            _profile.IsDemo = false;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            GhostMessageDialog.Error(this, L("OperationFailed"), ex.Message, GhostBrand.DisplayName);
        }
    }
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
        _input.MaxLength = 4096;
        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text(title, 22, weight: FontWeights.SemiBold));
        body.Children.Add(Spacer(16));
        body.Children.Add(GhostTheme.Field(label, _input));
        var ok = GhostTheme.Button(L("Continue"), primary: true);
        ok.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_input.Text)) DialogResult = true;
        };
        var cancel = GhostTheme.Button(L("Cancel"));
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
    private readonly ComboBox _language;
    private readonly CheckBox _confirmDeletes;
    private readonly CheckBox _showHidden;
    private readonly TextBox _retries;
    private readonly TextBox _parallelTransfers;
    private readonly TextBox _connectTimeout;
    private readonly TextBox _commandTimeout;
    private readonly TextBox _transferTimeout;
    private readonly TextBox _keepAlive;

    public AppTheme SelectedTheme => (AppTheme)Math.Max(0, _theme.SelectedIndex);
    public string SelectedLanguageCode => (_language.SelectedItem as GhostLanguage)?.Code ?? GhostLocalization.DefaultLanguageCode;
    public bool ConfirmDeletes => _confirmDeletes.IsChecked == true;
    public bool ShowHiddenFiles => _showHidden.IsChecked == true;
    public int AutomaticTransferRetries { get; private set; }
    public int ConcurrentTransfers { get; private set; }
    public int ConnectTimeoutSeconds { get; private set; }
    public int CommandTimeoutSeconds { get; private set; }
    public int TransferIdleTimeoutSeconds { get; private set; }
    public int KeepAliveSeconds { get; private set; }

    public SettingsDialog(Window owner, AppSettings settings) : base(owner, L("Settings"), 660, 840)
    {
        ResizeMode = ResizeMode.CanResizeWithGrip;
        _theme = new GhostComboBox();
        _theme.ItemsSource = new[] { L("UseWindowsSetting"), L("Dark"), L("Light") };
        _theme.SelectedIndex = (int)settings.Theme;

        _language = new GhostComboBox { ItemsSource = GhostLocalization.SupportedLanguages };
        _language.SelectedItem = GhostLocalization.SupportedLanguages.First(x => x.Code == GhostLocalization.NormalizeLanguageCode(settings.LanguageCode));

        _confirmDeletes = Check(L("ConfirmDeletes"), settings.ConfirmDeletes);
        _showHidden = Check(L("ShowHidden"), settings.ShowHiddenFiles);
        _retries = NumberBox(settings.AutomaticTransferRetries, 1);
        _parallelTransfers = NumberBox(settings.ConcurrentTransfers, 1);
        _connectTimeout = NumberBox(settings.ConnectTimeoutSeconds, 3);
        _commandTimeout = NumberBox(settings.CommandTimeoutSeconds, 3);
        _transferTimeout = NumberBox(settings.TransferIdleTimeoutSeconds, 4);
        _keepAlive = NumberBox(settings.KeepAliveSeconds, 3);

        var body = new StackPanel();
        body.Children.Add(GhostTheme.Text(L("Settings"), 24, weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text("Workspace preferences are stored locally and never synchronized.", 11.5, muted: true));
        body.Children.Add(Spacer(18));
        body.Children.Add(GhostTheme.Field(L("Appearance"), _theme));
        body.Children.Add(Spacer(12));
        body.Children.Add(GhostTheme.Field(L("Language"), _language, GhostLocalization.T("EnglishFallback")));
        body.Children.Add(Spacer(16));

        body.Children.Add(GhostTheme.Text(L("FileWorkspace"), 12, weight: FontWeights.SemiBold));
        body.Children.Add(_confirmDeletes);
        body.Children.Add(_showHidden);
        body.Children.Add(Spacer(14));

        body.Children.Add(GhostTheme.Text("Transfer and connection reliability", 12, weight: FontWeights.SemiBold));
        var reliabilityNote = GhostTheme.Text(
            "Retries apply only to transient network/FTP 4xx failures. Parallel transfers use isolated FTP/FTPS sessions. Keepalive sends NOOP only to the FTP/FTPS server you explicitly connected to; use 0 to disable it.",
            10.5,
            muted: true);
        reliabilityNote.TextWrapping = TextWrapping.Wrap;
        body.Children.Add(reliabilityNote);
        body.Children.Add(Spacer(10));
        body.Children.Add(TwoFields("Automatic retries (0–5)", _retries, "Concurrent transfers (1–8)", _parallelTransfers));
        body.Children.Add(Spacer(10));
        body.Children.Add(TwoFields("Connect timeout, seconds (3–120)", _connectTimeout, "Command timeout, seconds (5–300)", _commandTimeout));
        body.Children.Add(Spacer(10));
        body.Children.Add(TwoFields("Transfer idle timeout, seconds (15–3600)", _transferTimeout, "Keepalive seconds (0 or 15–600)", _keepAlive));

        var shortcuts = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(L("KeyboardShortcuts"), 12, weight: FontWeights.SemiBold),
                GhostTheme.Text("F5 Refresh · F2 Rename · Delete Remove · Ctrl+F Filter · Ctrl+L Path", 11, muted: true)
            }
        }, new Thickness(12), 10);
        shortcuts.Margin = new Thickness(0, 14, 0, 0);
        body.Children.Add(shortcuts);

        var save = GhostTheme.Button(L("SaveSettings"), primary: true);
        save.Click += (_, _) => Save();
        var cancel = GhostTheme.Button(L("Cancel"));
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
            AutomaticTransferRetries = ParseRange(_retries, 0, 5, "Automatic retries");
            ConcurrentTransfers = ParseRange(_parallelTransfers, 1, 8, "Concurrent transfers");
            ConnectTimeoutSeconds = ParseRange(_connectTimeout, 3, 120, "Connect timeout");
            CommandTimeoutSeconds = ParseRange(_commandTimeout, 5, 300, "Command timeout");
            TransferIdleTimeoutSeconds = ParseRange(_transferTimeout, 15, 3600, "Transfer idle timeout");
            KeepAliveSeconds = ParseKeepAlive(_keepAlive);
            if (_language.SelectedItem is not GhostLanguage)
                throw new InvalidOperationException("Select a valid language.");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            GhostMessageDialog.Error(this, L("OperationFailed"), ex.Message, GhostBrand.DisplayName);
        }
    }

    private static TextBox NumberBox(int value, int maxLength)
    {
        var box = GhostTheme.TextBox(value.ToString());
        box.MaxLength = maxLength;
        return box;
    }

    private static int ParseRange(TextBox box, int minimum, int maximum, string name)
    {
        if (!int.TryParse(box.Text.Trim(), out var value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"{name} must be between {minimum} and {maximum}.");
        return value;
    }

    private static int ParseKeepAlive(TextBox box)
    {
        if (!int.TryParse(box.Text.Trim(), out var value) || value < 0 || (value > 0 && value < 15) || value > 600)
            throw new InvalidOperationException("Keepalive must be 0 (disabled) or between 15 and 600 seconds.");
        return value;
    }

    private static Grid TwoFields(string leftLabel, UIElement left, string rightLabel, UIElement right)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var leftField = GhostTheme.Field(leftLabel, left);
        var rightField = GhostTheme.Field(rightLabel, right);
        Grid.SetColumn(leftField, 0);
        Grid.SetColumn(rightField, 2);
        grid.Children.Add(leftField);
        grid.Children.Add(rightField);
        return grid;
    }
}

internal sealed class AboutDialog : GhostDialog
{
    public AboutDialog(Window owner) : base(owner, $"{L("About")} {GhostBrand.DisplayName}", 610, 620)
    {
        ResizeMode = ResizeMode.NoResize;
        var body = new StackPanel();
        body.Children.Add(GhostBrand.IconControl(64));
        body.Children.Add(Spacer(14));
        body.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 28, weight: FontWeights.SemiBold));
        var version = typeof(AboutDialog).Assembly.GetName().Version?.ToString(3) ?? "Unknown";
        body.Children.Add(GhostTheme.Text($"Version {version} · Windows FTP / FTPS client", 11.5, muted: true));
        body.Children.Add(Spacer(18));

        body.Children.Add(GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(L("PrivacyByDesign"), 12.5, weight: FontWeights.SemiBold),
                GhostTheme.Text("No telemetry, analytics, tracking SDK, ads or automatic update checker. External product/publisher websites open only when you click their buttons.", 11, muted: true)
            }
        }, new Thickness(12), 10));

        body.Children.Add(Spacer(14));
        body.Children.Add(GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text("Developer and publisher", 11, muted: true),
                GhostTheme.Text(GhostBrand.Publisher, 12.5, weight: FontWeights.SemiBold),
                GhostTheme.Text($"Company number: {GhostBrand.CompanyNumber}", 10.5, muted: true),
                GhostTheme.Text(GhostBrand.RegisteredOffice, 10.5, muted: true),
                GhostTheme.Text("brendigo.com", 10.5, muted: true)
            }
        }, new Thickness(12), 10));

        body.Children.Add(Spacer(14));
        body.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 12.5, weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text("ghostftp.com", 11.5, muted: true));

        var buttons = new WrapPanel { Margin = new Thickness(0, 20, 0, 0) };
        var productWeb = GhostTheme.Button("Open ghostftp.com", primary: true);
        productWeb.Click += (_, _) => OpenUrl(GhostBrand.Website);
        buttons.Children.Add(productWeb);

        var publisherWeb = GhostTheme.Button("Open brendigo.com");
        publisherWeb.Margin = new Thickness(8, 0, 0, 0);
        publisherWeb.Click += (_, _) => OpenUrl(GhostBrand.PublisherWebsite);
        buttons.Children.Add(publisherWeb);
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
            // Opening an external product/publisher website is optional and must not affect the app session.
        }
    }
}
