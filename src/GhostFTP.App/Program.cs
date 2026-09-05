using GhostFTP.Design;
using GhostFTP.Services;
using GhostFTP.UI;
using System.Windows;
using System.Windows.Threading;

namespace GhostFTP;

public static class Program
{
    [STAThread]
    public static int Main()
    {
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

        GhostLocalization.SetLanguage(configuredLanguage);
        var dark = configuredTheme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => GhostTheme.IsSystemDark()
        };
        GhostTheme.Apply(dark);

        app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        var window = new MainWindow();
        app.MainWindow = window;
        return app.Run(window);
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
