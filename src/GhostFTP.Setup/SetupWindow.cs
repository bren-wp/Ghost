using GhostFTP.Design;
using GhostFTP.Setup.Services;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostFTP.Setup;

public sealed class SetupWindow : Window
{
    private enum WizardStep
    {
        Welcome,
        License,
        Options,
        Ready,
        Progress,
        Finish
    }

    private const string LicenseResourceName = "GhostFTP.License.txt";

    private readonly InstallerService _installer = new();
    private readonly bool _uninstallMode;
    private readonly ComboBox _language;
    private readonly CheckBox _acceptLicense;
    private readonly CheckBox _desktopShortcut;
    private readonly CheckBox _removeData;
    private readonly Button _back;
    private readonly Button _secondary;
    private readonly Button _primary;
    private readonly TextBlock _status;
    private readonly ProgressBar _progress;
    private readonly string _licenseText;

    private WizardStep _step = WizardStep.Welcome;
    private bool _busy;
    private bool _rebuilding;
    private bool _languageRenderPending;
    private bool _closed;
    private string? _lastError;

    public SetupWindow(bool uninstallMode)
    {
        _uninstallMode = uninstallMode;
        _licenseText = LoadLicenseText();

        Icon = GhostBrand.IconSource;
        Width = 900;
        Height = 680;
        MinWidth = 820;
        MinHeight = 620;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);
        Closing += OnClosing;
        Closed += (_, _) => _closed = true;

        var preferredLanguage = _installer.LoadPreferredLanguage();
        GhostLocalization.SetLanguage(preferredLanguage);

        _language = new GhostComboBox
        {
            ItemsSource = GhostLocalization.SupportedLanguages,
            MinWidth = 290,
            SelectedItem = GhostLocalization.SupportedLanguages.First(x => x.Code == GhostLocalization.CurrentLanguageCode)
        };
        _language.SelectionChanged += (_, _) => LanguageChanged();

        _acceptLicense = Check(string.Empty, false);
        _acceptLicense.Checked += (_, _) => RefreshFooter();
        _acceptLicense.Unchecked += (_, _) => RefreshFooter();
        _desktopShortcut = Check(GhostLocalization.T("CreateDesktopShortcut"), true);
        _removeData = Check(GhostLocalization.T("RemoveLocalData"), false);

        _status = GhostTheme.Text(string.Empty, 11.5, muted: true);
        _progress = new ProgressBar
        {
            Height = 4,
            Minimum = 0,
            Maximum = 100,
            IsIndeterminate = true,
            Foreground = GhostTheme.R("Accent"),
            Background = GhostTheme.R("Surface3"),
            Margin = new Thickness(0, 14, 0, 0)
        };

        _back = GhostTheme.Button(string.Empty);
        _back.Click += (_, _) => Back();
        _secondary = GhostTheme.Button(string.Empty);
        _secondary.Click += (_, _) => Close();
        _primary = GhostTheme.Button(string.Empty, primary: !uninstallMode, danger: uninstallMode);
        _primary.Click += async (_, _) => await NextAsync();

        Render();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_busy)
            e.Cancel = true;
    }

    private void LanguageChanged()
    {
        if (_rebuilding || _busy || _language.SelectedItem is not GhostLanguage language)
            return;

        if (string.Equals(language.Code, GhostLocalization.CurrentLanguageCode, StringComparison.OrdinalIgnoreCase))
            return;

        GhostLocalization.SetLanguage(language.Code);

        // SelectionChanged is raised while the ComboBox popup and old logical tree are still active.
        // Close the popup and defer the full wizard rebuild until that input event has unwound.
        _language.IsDropDownOpen = false;
        QueueLanguageRender();
    }

    private void QueueLanguageRender()
    {
        if (_languageRenderPending)
            return;

        _languageRenderPending = true;
        _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
        {
            _languageRenderPending = false;
            if (_closed || _busy)
                return;
            Render();
        }));
    }

    private void Render()
    {
        _rebuilding = true;
        try
        {
            // Several controls intentionally preserve state across wizard steps. A detached root still
            // owns its descendants logically, so explicitly remove those controls from the old parent
            // before adding them to the newly built tree.
            DetachReusableControls();
            Content = null;
            Title = _uninstallMode
                ? $"{GhostLocalization.T("Uninstall")} {GhostBrand.DisplayName}"
                : $"{GhostBrand.DisplayName} {GhostLocalization.T("Setup")}";
            _desktopShortcut.Content = GhostLocalization.T("CreateDesktopShortcut");
            _removeData.Content = GhostLocalization.T("RemoveLocalData");
            _acceptLicense.Content = GhostSetupLocalization.T("AcceptLicenseTerms");
            Content = BuildLayout();
            RefreshFooter();
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private void DetachReusableControls()
    {
        DetachFromParent(_language);
        DetachFromParent(_acceptLicense);
        DetachFromParent(_desktopShortcut);
        DetachFromParent(_removeData);
        DetachFromParent(_back);
        DetachFromParent(_secondary);
        DetachFromParent(_primary);
        DetachFromParent(_status);
        DetachFromParent(_progress);
    }

    private static void DetachFromParent(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case null:
                return;
            case Panel panel:
                panel.Children.Remove(element);
                return;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                return;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                return;
            default:
                throw new InvalidOperationException($"Cannot safely detach {element.GetType().Name} from {element.Parent.GetType().Name}.");
        }
    }

    private UIElement BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
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
        stack.Children.Add(GhostBrand.IconControl(68));
        stack.Children.Add(new Border { Height = 14 });
        stack.Children.Add(GhostTheme.Text(GhostBrand.DisplayName, 25, weight: FontWeights.SemiBold));
        stack.Children.Add(GhostTheme.Text(GhostBrand.PrivacyTagline, 11, muted: true));
        stack.Children.Add(new Border { Height = 26 });
        stack.Children.Add(Feature(GhostLocalization.T("TlsValidation")));
        stack.Children.Add(Feature(GhostLocalization.T("NoTelemetryOrTracking")));
        stack.Children.Add(Feature(GhostLocalization.T("PerUserInstallation")));
        stack.Children.Add(Feature(GhostLocalization.T("SelfContainedRuntime")));

        var publisher = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text("Published by", 10, muted: true),
                GhostTheme.Text(GhostBrand.Publisher, 11.5, weight: FontWeights.SemiBold),
                GhostTheme.Text($"Company no. {GhostBrand.CompanyNumber}", 10.5, muted: true),
                GhostTheme.Text("London, United Kingdom", 10.5, muted: true)
            }
        }, new Thickness(12), 10);
        publisher.Margin = new Thickness(0, 18, 0, 0);
        stack.Children.Add(publisher);

        var identity = GhostTheme.Text("ghostftp.com", 10.5, muted: true);
        identity.Margin = new Thickness(0, 18, 0, 0);
        stack.Children.Add(identity);
        return GhostTheme.Card(stack, new Thickness(20), 16);
    }

    private Border BuildContentPanel()
    {
        var root = new DockPanel();
        var footer = BuildFooter();
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        var body = new Grid();
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = BuildStepHeader();
        Grid.SetRow(header, 0);
        body.Children.Add(header);

        var stepBody = BuildStepBody();
        Grid.SetRow(stepBody, 2);
        body.Children.Add(stepBody);

        root.Children.Add(body);
        return GhostTheme.Card(root, new Thickness(24), 16);
    }

    private UIElement BuildStepHeader()
    {
        var stack = new StackPanel();
        var version = typeof(SetupWindow).Assembly.GetName().Version?.ToString(3) ?? "Unknown";
        stack.Children.Add(GhostTheme.Text(StepTitle(), 27, weight: FontWeights.SemiBold));
        stack.Children.Add(GhostTheme.Text(
            _uninstallMode
                ? $"{GhostBrand.DisplayName} {version} · {GhostBrand.Publisher}"
                : $"Version {version} · {GhostBrand.Publisher}",
            11,
            muted: true));
        return stack;
    }

    private UIElement BuildStepBody()
    {
        return _step switch
        {
            WizardStep.Welcome => BuildWelcomeStep(),
            WizardStep.License => BuildLicenseStep(),
            WizardStep.Options => _uninstallMode ? BuildUninstallOptionsStep() : BuildInstallOptionsStep(),
            WizardStep.Ready => BuildReadyStep(),
            WizardStep.Progress => BuildProgressStep(),
            WizardStep.Finish => BuildFinishStep(),
            _ => throw new InvalidOperationException("Unknown setup wizard step.")
        };
    }

    private UIElement BuildWelcomeStep()
    {
        var stack = new StackPanel();
        var intro = GhostTheme.Text(
            _uninstallMode
                ? "This wizard removes Ghost FTP from the current Windows account. You can keep or remove local profiles and settings."
                : "This wizard installs Ghost FTP for the current Windows user without administrator rights. Choose the language used by Setup and the Ghost FTP client.",
            12,
            muted: true);
        intro.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(intro);
        stack.Children.Add(new Border { Height = 20 });
        stack.Children.Add(GhostTheme.Field(
            GhostSetupLocalization.T("ClientLanguage"),
            _language,
            GhostSetupLocalization.T("ChooseLanguage")));

        var legal = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(GhostBrand.Publisher, 12.5, weight: FontWeights.SemiBold),
                GhostTheme.Text($"Company number: {GhostBrand.CompanyNumber}", 11, muted: true),
                GhostTheme.Text(GhostBrand.RegisteredOffice, 11, muted: true)
            }
        }, new Thickness(14), 10);
        legal.Margin = new Thickness(0, 22, 0, 0);
        stack.Children.Add(legal);
        return stack;
    }

    private UIElement BuildLicenseStep()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var note = GhostTheme.Text(
            "Please review the license before continuing. The English license text below is the governing license for Ghost FTP.",
            11.5,
            muted: true);
        note.TextWrapping = TextWrapping.Wrap;
        grid.Children.Add(note);

        var license = new TextBox
        {
            Text = _licenseText,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = GhostTheme.R("Surface2"),
            Foreground = GhostTheme.R("Text"),
            BorderBrush = GhostTheme.R("Border"),
            BorderThickness = new Thickness(1),
            FontFamily = GhostTheme.UiFont,
            FontSize = 11.5,
            Padding = new Thickness(14),
            IsTabStop = true
        };
        Grid.SetRow(license, 2);
        grid.Children.Add(license);

        Grid.SetRow(_acceptLicense, 4);
        grid.Children.Add(_acceptLicense);
        return grid;
    }

    private UIElement BuildInstallOptionsStep()
    {
        var stack = new StackPanel();
        stack.Children.Add(GhostTheme.Text(GhostLocalization.T("InstallLocation"), 11.5, weight: FontWeights.SemiBold));
        var path = GhostTheme.Surface(GhostTheme.Text(_installer.InstallDirectory, 11.5), new Thickness(12, 10, 12, 10), 10);
        path.Margin = new Thickness(0, 7, 0, 14);
        stack.Children.Add(path);
        stack.Children.Add(_desktopShortcut);

        var privacy = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text(GhostLocalization.T("PrivacyByDesign"), 12, weight: FontWeights.SemiBold),
                GhostTheme.Text("Ghost FTP contains no telemetry, analytics, ads, tracking SDK, crash upload or background update checker. Network access occurs only for user-initiated FTP/FTPS connections and links opened by the user.", 11, muted: true)
            }
        }, new Thickness(14), 10);
        privacy.Margin = new Thickness(0, 20, 0, 0);
        stack.Children.Add(privacy);
        return stack;
    }

    private UIElement BuildUninstallOptionsStep()
    {
        var stack = new StackPanel();
        stack.Children.Add(_removeData);
        var note = GhostTheme.Surface(new StackPanel
        {
            Children =
            {
                GhostTheme.Text("Local Ghost FTP data", 12, weight: FontWeights.SemiBold),
                GhostTheme.Text("Leave this unchecked to preserve saved profiles and settings for a future reinstall. Saved passwords, when enabled, remain protected by Windows DPAPI for the current user.", 11, muted: true)
            }
        }, new Thickness(14), 10);
        note.Margin = new Thickness(0, 16, 0, 0);
        stack.Children.Add(note);
        return stack;
    }

    private UIElement BuildReadyStep()
    {
        var stack = new StackPanel();

        if (!string.IsNullOrWhiteSpace(_lastError))
        {
            var title = GhostTheme.Text("Setup could not complete", 12.5, weight: FontWeights.SemiBold);
            title.Foreground = GhostTheme.R("Danger");
            var message = GhostTheme.Text(_lastError, 11, muted: true);
            message.TextWrapping = TextWrapping.Wrap;
            var error = GhostTheme.Surface(new StackPanel { Children = { title, message } }, new Thickness(14), 10);
            error.Margin = new Thickness(0, 0, 0, 16);
            stack.Children.Add(error);
        }

        var selectedLanguage = _language.SelectedItem as GhostLanguage;
        stack.Children.Add(SummaryRow("Action", _uninstallMode
            ? GhostLocalization.T("Uninstall")
            : _installer.IsInstalled ? GhostLocalization.T("Update") : GhostLocalization.T("Install")));
        stack.Children.Add(SummaryRow("Product", GhostBrand.DisplayName));
        stack.Children.Add(SummaryRow("Publisher", GhostBrand.Publisher));
        stack.Children.Add(SummaryRow("Language", selectedLanguage?.ToString() ?? "English"));

        if (_uninstallMode)
        {
            stack.Children.Add(SummaryRow("Local data", _removeData.IsChecked == true ? "Remove profiles and settings" : "Keep profiles and settings"));
        }
        else
        {
            stack.Children.Add(SummaryRow("Install location", _installer.InstallDirectory));
            stack.Children.Add(SummaryRow("Desktop shortcut", _desktopShortcut.IsChecked == true ? "Create" : "Do not create"));
            stack.Children.Add(SummaryRow("License", "Accepted"));
        }

        var ready = GhostTheme.Surface(GhostTheme.Text(
            _uninstallMode
                ? "Click Uninstall to remove Ghost FTP. The same GhostFTP-Setup.exe handles uninstall; no separate uninstaller executable is generated."
                : "Click Install to begin. Setup validates the embedded Ghost FTP payload, updates the application safely and registers the same Setup executable for future uninstall operations.",
            11.5,
            muted: true), new Thickness(14), 10);
        ready.Margin = new Thickness(0, 20, 0, 0);
        stack.Children.Add(ready);
        return stack;
    }

    private UIElement BuildProgressStep()
    {
        var stack = new StackPanel();
        stack.Children.Add(_status);
        stack.Children.Add(_progress);
        var note = GhostTheme.Text("Do not close Setup while files and Windows registration are being updated.", 10.5, muted: true);
        note.Margin = new Thickness(0, 12, 0, 0);
        stack.Children.Add(note);
        return stack;
    }

    private UIElement BuildFinishStep()
    {
        var stack = new StackPanel();
        stack.Children.Add(GhostTheme.Text(
            _uninstallMode ? GhostLocalization.T("RemovedSuccessfully") : GhostLocalization.T("InstalledReady"),
            15,
            weight: FontWeights.SemiBold));
        var detail = GhostTheme.Text(
            _uninstallMode
                ? "Ghost FTP has been removed from Windows. Any local data you chose to preserve remains in your user profile."
                : "Ghost FTP is ready. The selected language has been saved as the client language and can later be changed in Settings.",
            11.5,
            muted: true);
        detail.TextWrapping = TextWrapping.Wrap;
        detail.Margin = new Thickness(0, 10, 0, 0);
        stack.Children.Add(detail);
        return stack;
    }

    private Grid BuildFooter()
    {
        var footer = new Grid { Margin = new Thickness(0, 20, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_back, 0);
        Grid.SetColumn(_secondary, 2);
        Grid.SetColumn(_primary, 4);
        footer.Children.Add(_back);
        footer.Children.Add(_secondary);
        footer.Children.Add(_primary);
        return footer;
    }

    private void RefreshFooter()
    {
        _back.Content = GhostSetupLocalization.T("Back");
        _secondary.Content = _step == WizardStep.Finish ? GhostLocalization.T("Close") : GhostLocalization.T("Cancel");
        _back.Visibility = CanGoBack() ? Visibility.Visible : Visibility.Collapsed;
        _secondary.Visibility = _uninstallMode && _step == WizardStep.Finish ? Visibility.Collapsed : Visibility.Visible;

        _primary.Content = _step switch
        {
            WizardStep.Welcome or WizardStep.License or WizardStep.Options => GhostSetupLocalization.T("Next"),
            WizardStep.Ready when _uninstallMode => GhostLocalization.T("Uninstall"),
            WizardStep.Ready when _installer.IsInstalled => GhostLocalization.T("Update"),
            WizardStep.Ready => GhostLocalization.T("Install"),
            WizardStep.Progress => _uninstallMode ? GhostLocalization.T("Removing") : GhostLocalization.T("Installing"),
            WizardStep.Finish when _uninstallMode => GhostLocalization.T("Close"),
            WizardStep.Finish => $"{GhostLocalization.T("Launch")} {GhostBrand.DisplayName}",
            _ => GhostSetupLocalization.T("Next")
        };

        var licenseAccepted = _step != WizardStep.License || _acceptLicense.IsChecked == true;
        _primary.IsEnabled = !_busy && licenseAccepted;
        _back.IsEnabled = !_busy;
        _secondary.IsEnabled = !_busy;
        _language.IsEnabled = !_busy && _step == WizardStep.Welcome;
    }

    private bool CanGoBack() => !_busy && _step switch
    {
        WizardStep.License => !_uninstallMode,
        WizardStep.Options => true,
        WizardStep.Ready => true,
        _ => false
    };

    private async Task NextAsync()
    {
        if (_busy)
            return;

        if (_step == WizardStep.Finish)
        {
            if (!_uninstallMode)
                _installer.LaunchApp();
            Close();
            return;
        }

        if (_uninstallMode)
        {
            switch (_step)
            {
                case WizardStep.Welcome:
                    SetStep(WizardStep.Options);
                    return;
                case WizardStep.Options:
                    SetStep(WizardStep.Ready);
                    return;
                case WizardStep.Ready:
                    await ExecuteUninstallAsync();
                    return;
            }
        }
        else
        {
            switch (_step)
            {
                case WizardStep.Welcome:
                    SetStep(WizardStep.License);
                    return;
                case WizardStep.License:
                    if (_acceptLicense.IsChecked == true)
                        SetStep(WizardStep.Options);
                    return;
                case WizardStep.Options:
                    SetStep(WizardStep.Ready);
                    return;
                case WizardStep.Ready:
                    await ExecuteInstallAsync();
                    return;
            }
        }
    }

    private void Back()
    {
        if (!CanGoBack())
            return;

        _lastError = null;
        if (_uninstallMode)
        {
            SetStep(_step == WizardStep.Ready ? WizardStep.Options : WizardStep.Welcome);
            return;
        }

        SetStep(_step switch
        {
            WizardStep.License => WizardStep.Welcome,
            WizardStep.Options => WizardStep.License,
            WizardStep.Ready => WizardStep.Options,
            _ => WizardStep.Welcome
        });
    }

    private void SetStep(WizardStep step)
    {
        if (step != WizardStep.Ready)
            _lastError = null;
        _step = step;
        Render();
    }

    private async Task ExecuteInstallAsync()
    {
        _lastError = null;
        SetBusy(true);
        _step = WizardStep.Progress;
        _status.Text = _installer.IsInstalled ? GhostLocalization.T("Updating") : GhostLocalization.T("Installing");
        Render();
        try
        {
            var languageCode = (_language.SelectedItem as GhostLanguage)?.Code ?? GhostLocalization.DefaultLanguageCode;
            await _installer.InstallAsync(_desktopShortcut.IsChecked == true, languageCode, CancellationToken.None);
            _step = WizardStep.Finish;
        }
        catch (Exception ex)
        {
            _lastError = GhostLocalization.F("OperationCouldNotComplete", ex.Message);
            _step = WizardStep.Ready;
        }
        finally
        {
            SetBusy(false);
            Render();
        }
    }

    private async Task ExecuteUninstallAsync()
    {
        _lastError = null;
        SetBusy(true);
        _step = WizardStep.Progress;
        _status.Text = GhostLocalization.T("Removing");
        Render();
        try
        {
            await _installer.UninstallAsync(_removeData.IsChecked == true, CancellationToken.None);
            _step = WizardStep.Finish;
        }
        catch (Exception ex)
        {
            _lastError = GhostLocalization.F("OperationCouldNotComplete", ex.Message);
            _step = WizardStep.Ready;
        }
        finally
        {
            SetBusy(false);
            Render();
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        RefreshFooter();
    }

    private string StepTitle()
    {
        return _step switch
        {
            WizardStep.Welcome => GhostSetupLocalization.T("Welcome"),
            WizardStep.License => GhostSetupLocalization.T("LicenseAgreement"),
            WizardStep.Options => _uninstallMode ? GhostLocalization.T("Uninstall") : GhostSetupLocalization.T("InstallOptions"),
            WizardStep.Ready => GhostSetupLocalization.T("ReadyToInstall"),
            WizardStep.Progress => _uninstallMode ? GhostLocalization.T("Removing") : GhostLocalization.T("Installing"),
            WizardStep.Finish => GhostSetupLocalization.T("Finish"),
            _ => GhostBrand.DisplayName
        };
    }

    private static Border SummaryRow(string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(GhostTheme.Text(label, 11, muted: true));
        var text = GhostTheme.Text(value, 11.5, weight: FontWeights.Medium);
        text.TextWrapping = TextWrapping.Wrap;
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return GhostTheme.Surface(grid, new Thickness(10, 8, 10, 8), 8);
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

    private static string LoadLicenseText()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LicenseResourceName)
            ?? throw new InvalidOperationException("The Ghost FTP license is missing from this Setup build.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException("The embedded Ghost FTP license is empty.");
        return text;
    }
}
