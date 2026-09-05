using GhostFTP.Design;
using GhostFTP.Services;
using GhostFTP.UI;
using System.Windows;
using System.Windows.Threading;

namespace GhostFTP;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var captureDirectory = ParseCaptureDirectory(args);
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        AppTheme configuredTheme = AppTheme.System;
        var configuredLanguage = GhostLocalization.DefaultLanguageCode;
        try
        {
            var paths = new AppPaths();
            var settings = new AppSettingsStore(paths.SettingsFile).LoadAsync().GetAwaiter().GetResult();
            configuredTheme = settings.Theme;
            configuredLanguage = settings.LanguageCode;
        }
        catch
        {
            // Safe defaults. No crash report, telemetry or remote lookup is emitted.
        }

        if (captureDirectory is not null)
        {
            configuredTheme = AppTheme.Dark;
            configuredLanguage = GhostLocalization.DefaultLanguageCode;
        }

        GhostLocalization.SetLanguage(configuredLanguage);
        var dark = configuredTheme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => GhostTheme.IsSystemDark()
        };
        GhostTheme.Apply(dark);

        app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        var window = new MainWindow(captureDirectory);
        app.MainWindow = window;
        return app.Run(window);
    }

    private static string? ParseCaptureDirectory(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (!string.Equals(args[index], "--capture-ui", StringComparison.OrdinalIgnoreCase))
                continue;
            if (index + 1 >= args.Count || string.IsNullOrWhiteSpace(args[index + 1]))
                throw new ArgumentException("--capture-ui requires an output directory path.");
            return Path.GetFullPath(args[index + 1]);
        }

        return null;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var message = "Ghost FTP encountered an unexpected local error and will close to protect session integrity.";
        var details = e.Exception.Message + "\n\nNo crash report, telemetry or diagnostic data was transmitted.";

        try
        {
            GhostMessageDialog.Error(Application.Current.MainWindow, message, details, "Ghost FTP");
        }
        catch
        {
            // The premium dialog itself is intentionally best-effort during a fatal UI failure.
        }

        e.Handled = true;
        Application.Current.Shutdown(1);
    }
}
