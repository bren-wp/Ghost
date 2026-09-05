using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;

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
            Console.WriteLine($"PASS  Ghost FTP localization catalog ({GhostLocalization.SupportedLanguages.Count} languages)");
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
        Assert(languages.Count >= 21, "Ghost FTP must ship with more than 20 selectable languages.", failures);
        Assert(languages[0].Code == GhostLocalization.DefaultLanguageCode,
            "English must remain the primary/default language.", failures);
        Assert(languages.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == languages.Count,
            "Localization language codes must be unique.", failures);

        foreach (var language in languages)
            Assert(GhostLocalization.HasCoreCoverage(language.Code),
                $"Language '{language.Code}' is missing one or more required core translations.", failures);

        GhostLocalization.SetLanguage("hr");
        Assert(GhostLocalization.T("Settings") != "Settings", "Croatian core translation was not applied.", failures);
        GhostLocalization.SetLanguage("not-a-real-language");
        Assert(GhostLocalization.CurrentLanguageCode == GhostLocalization.DefaultLanguageCode,
            "Unknown language did not fall back to English.", failures);
        Assert(GhostLocalization.T("Settings") == "Settings", "English fallback text is incorrect.", failures);
        GhostLocalization.SetLanguage(GhostLocalization.DefaultLanguageCode);
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
