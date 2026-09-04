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
    private bool _completed;

    public SetupWindow(bool uninstallMode)
    {
        _uninstallMode = uninstallMode;
        Title = uninstallMode ? "Uninstall GhostFTP" : "GhostFTP Setup";
        Width = 760;
        Height = 570;
        MinWidth = 720;
        MinHeight = 540;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);

        _desktopShortcut = Check("Create a desktop shortcut", true);
        _removeData = Check("Also remove local settings and saved server profiles", false);
        _status = GhostTheme.Text(
            uninstallMode
                ? "Ready to uninstall."
                : _installer.IsInstalled
                    ? "An existing installation will be updated safely."
                    : "Ready to install.",
            11.5,
            muted: true);

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

        _secondary = GhostTheme.Button("Cancel");
        _secondary.Click += (_, _) => Close();
        _primary = GhostTheme.Button(
            uninstallMode ? "Uninstall" : _installer.IsInstalled ? "Update GhostFTP" : "Install GhostFTP",
            primary: !uninstallMode,
            danger: uninstallMode);
        _primary.Click += async (_, _) => await ExecuteAsync();

        Content = BuildLayout();
    }

    private UIElement BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
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
        stack.Children.Add(GhostTheme.Logo(56));
        stack.Children.Add(new Border { Height = 14 });
        stack.Children.Add(GhostTheme.Text("GhostFTP", 24, weight: FontWeights.SemiBold));
        stack.Children.Add(GhostTheme.Text("Private FTP / FTPS for Windows", 11, muted: true));
        stack.Children.Add(new Border { Height = 24 });

        stack.Children.Add(Feature("TLS certificate validation"));
        stack.Children.Add(Feature("No telemetry or tracking"));
        stack.Children.Add(Feature("Per-user installation"));
        stack.Children.Add(Feature("Self-contained runtime"));

        var author = GhostTheme.Text("Built by Brendigo", 10.5, muted: true);
        author.Margin = new Thickness(0, 24, 0, 0);
        stack.Children.Add(author);
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
        var version = typeof(SetupWindow).Assembly.GetName().Version?.ToString(3) ?? "1.2.0";
        body.Children.Add(GhostTheme.Text(
            _uninstallMode ? "Uninstall GhostFTP" : _installer.IsInstalled ? "Update GhostFTP" : "Install GhostFTP",
            26,
            weight: FontWeights.SemiBold));
        body.Children.Add(GhostTheme.Text(
            _uninstallMode
                ? "Remove the application from this Windows account."
                : $"Version {version} · Windows 10/11 x64 and ARM64 builds are published separately.",
            11.5,
            muted: true));
        body.Children.Add(new Border { Height = 22 });

        if (!_uninstallMode)
        {
            body.Children.Add(GhostTheme.Text("Install location", 11.5, weight: FontWeights.SemiBold));
            var path = GhostTheme.Surface(GhostTheme.Text(_installer.InstallDirectory, 11.5), new Thickness(12, 10, 12, 10), 10);
            path.Margin = new Thickness(0, 7, 0, 14);
            body.Children.Add(path);
            body.Children.Add(_desktopShortcut);

            var privacy = GhostTheme.Surface(new StackPanel
            {
                Children =
                {
                    GhostTheme.Text("Privacy by design", 12, weight: FontWeights.SemiBold),
                    GhostTheme.Text("GhostFTP contains no telemetry, analytics, ads, tracking SDK or background update checker. Network access happens only when you explicitly connect to a server or open a website link.", 11, muted: true)
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
                GhostTheme.Text("Status", 11, weight: FontWeights.SemiBold),
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
                _status.Text = "Removing GhostFTP…";
                await _installer.UninstallAsync(_removeData.IsChecked == true, CancellationToken.None);
                _status.Text = "GhostFTP has been removed successfully.";
                _primary.Content = "Close";
                _secondary.Visibility = Visibility.Collapsed;
            }
            else
            {
                _status.Text = _installer.IsInstalled ? "Updating GhostFTP…" : "Installing GhostFTP…";
                await _installer.InstallAsync(_desktopShortcut.IsChecked == true, CancellationToken.None);
                _status.Text = "GhostFTP is installed and ready to use.";
                _primary.Content = "Launch GhostFTP";
                _secondary.Content = "Close";
            }
            _completed = true;
        }
        catch (Exception ex)
        {
            _status.Text = "The operation could not be completed: " + ex.Message;
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
