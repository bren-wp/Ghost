using GhostFTP.Design;
using GhostFTP.Setup.Services;
using System.Windows;
using System.Windows.Controls;

namespace GhostFTP.Setup;

public sealed class SetupWindow : Window
{
    private readonly InstallerService _installer = new();
    private readonly bool _uninstallMode;
    private readonly CheckBox _desktopShortcut;
    private readonly CheckBox _removeData;
    private readonly Button _primary;
    private readonly Button _secondary;
    private readonly TextBlock _status;
    private readonly ProgressBar _progress;
    private readonly ComboBox _language;
    private bool _completed;
    private bool _rebuilding;

    public SetupWindow(bool uninstallMode)
    {
        _uninstallMode = uninstallMode;
        Icon = GhostBrand.IconSource;
        Width = 820;
        Height = 620;
        MinWidth = 760;
        MinHeight = 570;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);

        GhostLocalization.SetLanguage(GhostLocalization.DefaultLanguageCode);
        _language = new GhostComboBox { ItemsSource = GhostLocalization.SupportedLanguages, MinWidth = 250 };
        _language.SelectedItem = GhostLocalization.SupportedLanguages.First(x => x.Code == GhostLocalization.DefaultLanguageCode);
        _language.SelectionChanged += (_, _) => LanguageChanged();

        _desktopShortcut = Check(string.Empty, true);
        _removeData = Check(string.Empty, false);
        _status = GhostTheme.Text(string.Empty, 11.5, muted: true);

        _progress = new ProgressBar
        {
            Height = 3,
            Minimum = 0,
            Maximum = 100,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 10, 0, 0),
            Foreground = GhostTheme.R("Accent"),
            Background = GhostTheme.R("Surface3")
        };

        _secondary = GhostTheme.Button(string.Empty);
        _secondary.Click += (_, _) => Close();
        _primary = GhostTheme.Button(string.Empty, primary: !uninstallMode, danger: uninstallMode);
        _primary.Click += async (_, _) => await ExecuteAsync();

        ApplyLocalizedControlText(resetStatus: true);
        Content = BuildLayout();
    }

    private void LanguageChanged()
    {
        if (_rebuilding || _completed || _language.SelectedItem is not GhostLanguage language)
            return;

        GhostLocalization.SetLanguage(language.Code);
        RebuildLocalizedLayout();
    }

    private void RebuildLocalizedLayout()
    {
        _rebuilding = true;
        try
        {
            Content = null;
            ApplyLocalizedControlText(resetStatus: true);
            Content = BuildLayout();
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private void ApplyLocalizedControlText(bool resetStatus)
    {
        Title = _uninstallMode
            ? $"{GhostLocalization.T("Uninstall")} {GhostBrand.DisplayName}"
            : $"{GhostBrand.DisplayName} {GhostLocalization.T("Setup")}";
        _desktopShortcut.Content = GhostLocalization.T("CreateDesktopShortcut");
        _removeData.Content = GhostLocalization.T("RemoveLocalData");
        _secondary.Content = GhostLocalization.T("Cancel");
        _primary.Content = _uninstallMode
            ? GhostLocalization.T("Uninstall")
            : _installer.IsInstalled
                ? $"{GhostLocalization.T("Update")} {GhostBrand.DisplayName}"
                : $"{GhostLocalization.T("Install")} {GhostBrand.DisplayName}";

        if (resetStatus)
        {
            _status.Text = _uninstallMode
                ? GhostLocalization.T("ReadyUninstall")
                : _installer.IsInstalled
                    ? GhostLocalization.T("ExistingInstallUpdate")
                    : GhostLocalization.T("ReadyInstall");
        }
    }

    private UIElement BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var brand = BuildBrandPanel();
        Grid.SetColumn(brand, 0);
        root.Children.Add(brand);

        var content = BuildContentPanel();
        Grid.SetColumn(content, 2);
        root.Children.Add(content);
        return root;
    }

    private Border BuildBrandPanel()
    {
        var stack = new StackPanel();
        stack.Children.Add(GhostBrand.IconControl(64));
        stack.Children.Add(new Border { Height = 14 });
        stack.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 24, weight: FontWeights.SemiBold));
        stack.Children.Add(GhostTheme.Text(GhostBrand.PrivacyTagline, 11, muted: true));
        stack.Children.Add(new Border { Height = 24 });

        stack.Children.Add(Feature(GhostLocalization.T("TlsValidation")));
        stack.Children.Add(Feature(GhostLocalization.T("NoTelemetryOrTracking")));
        stack.Children.Add(Feature(GhostLocalization.T("PerUserInstallation")));
        stack.Children.Add(Feature(GhostLocalization.T("SelfContainedRuntime")));

        var identity = GhostTheme.Text("ghostftp.com", 10.5, muted: true);
        identity.Margin = new Thickness(0, 24, 0, 0);
        stack.Children.Add(identity);
        return GhostTheme.Card(stack, new Thickness(20), 16);
    }

    private Border BuildContentPanel()
    {
        var root = new DockPanel();

        var footer = new Grid { Margin = new Thickness(0, 18, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_secondary, 1);
        Grid.SetColumn(_primary, 3);
        footer.Children.Add(_secondary);
        footer.Children.Add(_primary);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        var body = new StackPanel();
        var version = typeof(SetupWindow).Assembly.GetName().Version?.ToString(3) ?? "Unknown";
        body.Children.Add(GhostTheme.Text(
            _uninstallMode
                ? $"{GhostLocalization.T("Uninstall")} {GhostBrand.DisplayName}"
                : _installer.IsInstalled
                    ? $"{GhostLocalization.T("Update")} {GhostBrand.DisplayName}"
                    : $"{GhostLocalization.T("Install")} {GhostBrand.DisplayName}",
            26,
            weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text(
            _uninstallMode
                ? "Remove the application from this Windows account."
                : $"Version {version} · Windows x64 and ARM64 builds are published separately.",
            11.5,
            muted: true));
        body.Children.Add(new Border { Height = 16 });
        body.Children.Add(GhostTheme.Field(GhostLocalization.T("Language"), _language, GhostLocalization.T("EnglishFallback")));
        body.Children.Add(new Border { Height = 18 });

        if (!_uninstallMode)
        {
            body.Children.Add(GhostTheme.Text(GhostLocalization.T("InstallLocation"), 11.5, weight: FontWeights.SemiBold));
            var path = GhostTheme.Surface(GhostTheme.Text(_installer.InstallDirectory, 11.5), new Thickness(12, 10, 12, 10), 10);
            path.Margin = new Thickness(0, 7, 0, 14);
            body.Children.Add(path);
            body.Children.Add(_desktopShortcut);

            var privacy = GhostTheme.Surface(new StackPanel
            {
                Children =
                {
                    GhostTheme.Text(GhostLocalization.T("PrivacyByDesign"), 12, weight: FontWeights.SemiBold),
                    GhostTheme.Text("Ghost FTP contains no telemetry, analytics, ads, tracking SDK or background update checker. Network access happens only when you explicitly connect to a server or open the Ghost FTP website.", 11, muted: true)
                }
            }, new Thickness(12), 10);
            privacy.Margin = new Thickness(0, 18, 0, 0);
            body.Children.Add(privacy);
        }
        else
        {
            body.Children.Add(_removeData);
            var note = GhostTheme.Surface(new StackPanel
            {
                Children =
                {
                    GhostTheme.Text("Local data", 12, weight: FontWeights.SemiBold),
                    GhostTheme.Text("Leave this unchecked if you want to preserve saved profiles for a future reinstall. Saved passwords, when enabled, are protected by Windows DPAPI for the current user.", 11, muted: true)
                }
            }, new Thickness(12), 10);
            note.Margin = new Thickness(0, 16, 0, 0);
            body.Children.Add(note);
        }

        var statusCard = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(GhostLocalization.T("Status"), 11, weight: FontWeights.SemiBold),
                _status,
                _progress
            }
        }, new Thickness(12), 10);
        statusCard.Margin = new Thickness(0, 18, 0, 0);
        body.Children.Add(statusCard);

        root.Children.Add(body);
        return GhostTheme.Card(root, new Thickness(24), 16);
    }

    private async Task ExecuteAsync()
    {
        if (_completed)
        {
            if (!_uninstallMode) _installer.LaunchApp();
            Close();
            return;
        }

        SetBusy(true);
        try
        {
            if (_uninstallMode)
            {
                _status.Text = GhostLocalization.T("Removing");
                await _installer.UninstallAsync(_removeData.IsChecked == true, CancellationToken.None);
                _status.Text = GhostLocalization.T("RemovedSuccessfully");
                _primary.Content = GhostLocalization.T("Close");
                _secondary.Visibility = Visibility.Collapsed;
            }
            else
            {
                _status.Text = _installer.IsInstalled ? GhostLocalization.T("Updating") : GhostLocalization.T("Installing");
                await _installer.InstallAsync(_desktopShortcut.IsChecked == true, CancellationToken.None);
                _status.Text = GhostLocalization.T("InstalledReady");
                _primary.Content = $"{GhostLocalization.T("Launch")} {GhostBrand.DisplayName}";
                _secondary.Content = GhostLocalization.T("Close");
            }
            _completed = true;
            _language.IsEnabled = false;
        }
        catch (Exception ex)
        {
            _status.Text = GhostLocalization.F("OperationCouldNotComplete", ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _primary.IsEnabled = !busy;
        _secondary.IsEnabled = !busy;
        _language.IsEnabled = !busy && !_completed;
        _progress.IsIndeterminate = busy;
        _progress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Border Feature(string text)
    {
        var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        line.Children.Add(GhostTheme.Text("✓", 12, weight: FontWeights.Bold));
        var label = GhostTheme.Text(text, 11, muted: true);
        label.Margin = new Thickness(8, 0, 0, 0);
        line.Children.Add(label);
        return new Border { Child = line };
    }

    private static CheckBox Check(string text, bool selected)
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
}
