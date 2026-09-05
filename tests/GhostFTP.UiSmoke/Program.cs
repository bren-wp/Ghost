using GhostFTP.Design;
using GhostFTP.Setup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GhostFTP.UiSmoke;

public static class Program
{
    [STAThread]
    public static int Main()
    {
        var failures = new List<string>();
        try
        {
            _ = Application.Current ?? new Application();
            GhostTheme.Apply(dark: true);

            TestTextBox(failures);
            TestPasswordBox(failures);
            TestComboBox(failures);
            TestLocalization(failures);
            TestBrandIdentity(failures);
            TestSetupLanguageSwitchAndWizardRebuild(failures);
        }
        catch (Exception ex)
        {
            failures.Add("UI smoke bootstrap: " + DescribeException(ex));
        }

        foreach (var failure in failures)
            Console.Error.WriteLine("FAIL  " + failure);

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS  Ghost FTP editable input controls");
            Console.WriteLine("PASS  Ghost FTP password input control");
            Console.WriteLine("PASS  Ghost FTP security selector");
            Console.WriteLine($"PASS  Ghost FTP application localization catalog ({GhostLocalization.SupportedLanguages.Count} languages)");
            Console.WriteLine($"PASS  Ghost FTP Setup localization catalog ({GhostLocalization.SupportedLanguages.Count} languages)");
            Console.WriteLine("PASS  Ghost FTP / BRENDIGO LTD product and publisher identity");
            Console.WriteLine("PASS  Ghost FTP Setup live language switching and wizard rebuild");
            return 0;
        }

        return 1;
    }

    private static void TestTextBox(List<string> failures)
    {
        var box = GhostTheme.TextBox("host.example");
        Assert(box.Focusable, "TextBox must be focusable.", failures);
        Assert(box.IsTabStop, "TextBox must participate in keyboard tab navigation.", failures);
        Assert(!box.IsReadOnly, "TextBox must not be read-only.", failures);
        Assert(box.AcceptsReturn == false, "Single-line TextBox must not accept line breaks.", failures);
        Assert(box.ReadLocalValue(Control.TemplateProperty) == DependencyProperty.UnsetValue,
            "TextBox must use the native WPF editor template rather than a local replacement.", failures);

        box.CaretIndex = box.Text.Length;
        box.AppendText(":2121");
        Assert(box.Text == "host.example:2121", "TextBox text model is not editable.", failures);
        box.SelectAll();
        box.SelectedText = "127.0.0.1";
        Assert(box.Text == "127.0.0.1", "TextBox selection replacement failed.", failures);
    }

    private static void TestPasswordBox(List<string> failures)
    {
        var box = GhostTheme.PasswordBox();
        Assert(box.Focusable, "PasswordBox must be focusable.", failures);
        Assert(box.IsTabStop, "PasswordBox must participate in keyboard tab navigation.", failures);
        Assert(box.ReadLocalValue(Control.TemplateProperty) == DependencyProperty.UnsetValue,
            "PasswordBox must use the native WPF editor template rather than a local replacement.", failures);

        box.Password = "demo-123";
        Assert(box.Password == "demo-123", "PasswordBox value cannot be written/read.", failures);
        box.Clear();
        Assert(box.Password.Length == 0, "PasswordBox clear failed.", failures);
    }

    private static void TestComboBox(List<string> failures)
    {
        var combo = new GhostComboBox
        {
            ItemsSource = new[] { "FTP", "FTPS Explicit", "FTPS Implicit" },
            SelectedIndex = 1
        };
        Assert(combo.Focusable, "GhostComboBox must be focusable.", failures);
        Assert(combo.IsTabStop, "GhostComboBox must participate in tab navigation.", failures);
        Assert(combo.SelectedIndex == 1, "GhostComboBox selection failed.", failures);
        Assert((string?)combo.SelectedItem == "FTPS Explicit", "GhostComboBox selected value is incorrect.", failures);
    }

    private static void TestLocalization(List<string> failures)
    {
        var languages = GhostLocalization.SupportedLanguages;
        Assert(languages.Count == 29, $"Ghost FTP must ship with exactly 29 validated languages; found {languages.Count}.", failures);
        Assert(languages[0].Code == GhostLocalization.DefaultLanguageCode,
            "English must remain the primary/default language.", failures);
        Assert(languages.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == languages.Count,
            "Localization language codes must be unique.", failures);

        foreach (var language in languages)
        {
            Assert(GhostLocalization.HasCoreCoverage(language.Code),
                $"Application language '{language.Code}' is missing one or more required core translations.", failures);
            Assert(GhostSetupLocalization.HasCoverage(language.Code),
                $"Setup language '{language.Code}' is missing one or more required wizard translations.", failures);

            GhostLocalization.SetLanguage(language.Code);
            Assert(!string.IsNullOrWhiteSpace(GhostLocalization.T("Settings")),
                $"Application language '{language.Code}' returned an empty Settings label.", failures);
            Assert(!string.IsNullOrWhiteSpace(GhostSetupLocalization.T("Welcome")),
                $"Setup language '{language.Code}' returned an empty Welcome label.", failures);
            Assert(!string.IsNullOrWhiteSpace(GhostSetupLocalization.T("AcceptLicenseTerms")),
                $"Setup language '{language.Code}' returned an empty license-acceptance label.", failures);
        }

        GhostLocalization.SetLanguage("hr");
        Assert(GhostLocalization.T("Settings") != "Settings", "Croatian core translation was not applied.", failures);
        Assert(GhostSetupLocalization.T("Next") != "Next", "Croatian Setup translation was not applied.", failures);

        GhostLocalization.SetLanguage("not-a-real-language");
        Assert(GhostLocalization.CurrentLanguageCode == GhostLocalization.DefaultLanguageCode,
            "Unknown language did not fall back to English.", failures);
        Assert(GhostLocalization.T("Settings") == "Settings", "English application fallback text is incorrect.", failures);
        Assert(GhostSetupLocalization.T("Welcome") == "Welcome", "English Setup fallback text is incorrect.", failures);
        GhostLocalization.SetLanguage(GhostLocalization.DefaultLanguageCode);
    }

    private static void TestBrandIdentity(List<string> failures)
    {
        Assert(GhostBrand.DisplayName == "Ghost FTP", "Product display name drifted from Ghost FTP.", failures);
        Assert(GhostBrand.ProductName == "GhostFTP", "Compact product identifier drifted from GhostFTP.", failures);
        Assert(GhostBrand.Publisher == "BRENDIGO LTD", "Publisher drifted from BRENDIGO LTD.", failures);
        Assert(GhostBrand.Website == "https://ghostftp.com", "Product website drifted from ghostftp.com.", failures);
        Assert(GhostBrand.PublisherWebsite == "https://brendigo.com", "Publisher website drifted from brendigo.com.", failures);
        Assert(Uri.TryCreate(GhostBrand.Website, UriKind.Absolute, out var productUri) && productUri.Scheme == Uri.UriSchemeHttps,
            "Ghost FTP product website must be an absolute HTTPS URI.", failures);
        Assert(Uri.TryCreate(GhostBrand.PublisherWebsite, UriKind.Absolute, out var publisherUri) && publisherUri.Scheme == Uri.UriSchemeHttps,
            "BRENDIGO LTD publisher website must be an absolute HTTPS URI.", failures);
    }

    private static void TestSetupLanguageSwitchAndWizardRebuild(List<string> failures)
    {
        GhostLocalization.SetLanguage(GhostLocalization.DefaultLanguageCode);
        SetupWindow? window = null;
        try
        {
            window = new SetupWindow(uninstallMode: false);
            window.Show();
            PumpDispatcher();
            window.UpdateLayout();

            var language = FindVisualDescendant<GhostComboBox>(window);
            Assert(language is not null, "Setup language selector was not found in the live visual tree.", failures);
            if (language is null)
                return;

            foreach (var code in new[] { "hr", "de", "ja", "en" })
            {
                language.IsDropDownOpen = true;
                PumpDispatcher();
                language.SelectedItem = GhostLocalization.SupportedLanguages.First(x => x.Code == code);
                PumpDispatcher();
                window.UpdateLayout();

                Assert(GhostLocalization.CurrentLanguageCode == code,
                    $"Setup live language switch did not apply '{code}'.", failures);
                Assert(language.Parent is not null,
                    $"Setup language selector became detached after switching to '{code}'.", failures);
            }

            var next = FindVisualDescendants<Button>(window)
                .FirstOrDefault(x => x.IsEnabled && x.Visibility == Visibility.Visible &&
                                     string.Equals(x.Content?.ToString(), GhostSetupLocalization.T("Next"), StringComparison.Ordinal));
            Assert(next is not null, "Setup Next button was not found after language switching.", failures);
            if (next is not null)
            {
                next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                PumpDispatcher();
                window.UpdateLayout();

                var back = FindVisualDescendants<Button>(window)
                    .FirstOrDefault(x => x.IsEnabled && x.Visibility == Visibility.Visible &&
                                         string.Equals(x.Content?.ToString(), GhostSetupLocalization.T("Back"), StringComparison.Ordinal));
                Assert(back is not null, "Setup Back button was not found after moving to the License step.", failures);
                if (back is not null)
                {
                    back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    PumpDispatcher();
                    window.UpdateLayout();
                    Assert(FindVisualDescendant<GhostComboBox>(window) is not null,
                        "Setup did not rebuild the Welcome step after Back navigation.", failures);
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add("Setup language switch/rebuild crash: " + DescribeException(ex));
        }
        finally
        {
            if (window is not null)
            {
                try
                {
                    window.Close();
                    PumpDispatcher();
                }
                catch (Exception ex)
                {
                    failures.Add("Setup smoke cleanup: " + DescribeException(ex));
                }
            }
            GhostLocalization.SetLanguage(GhostLocalization.DefaultLanguageCode);
        }
    }

    private static T? FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
        => FindVisualDescendants<T>(root).FirstOrDefault();

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;
            foreach (var nested in FindVisualDescendants<T>(child))
                yield return nested;
        }
    }

    private static void PumpDispatcher()
    {
        var frame = new DispatcherFrame();
        _ = Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static string DescribeException(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
            messages.Add($"{current.GetType().Name}: {current.Message}");
        return string.Join(" -> ", messages);
    }

    private static void Assert(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add(message);
    }
}
