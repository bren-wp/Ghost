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
        var dark = GhostTheme.IsSystemDark();
        GhostTheme.Apply(dark);
        GhostReferenceTheme.Apply(dark);

        var window = new SetupWindow(uninstall);
        app.MainWindow = window;
        return app.Run(window);
    }
}
