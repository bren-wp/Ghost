using GhostFTP.Design;
using System.Windows;

namespace GhostFTP.Setup;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var uninstall = args.Any(x => string.Equals(x, "--uninstall", StringComparison.OrdinalIgnoreCase));

        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        // Setup is part of the same product surface as the installed and portable clients.
        // Keep the approved local dark reference appearance deterministic across host themes.
        GhostTheme.Apply(dark: true);
        GhostReferenceTheme.Apply(dark: true);

        var window = new SetupWindow(uninstall);
        app.MainWindow = window;
        return app.Run(window);
    }
}
