using GhostFTP.Design;

namespace GhostFTP.Linux;

public static class Program
{
    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsLinux())
        {
            Console.Error.WriteLine("Ghost FTP Linux renderer can only run on Linux.");
            return 2;
        }

        try
        {
            using var application = new LinuxMainWindow(args);
            if (args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(900)).ConfigureAwait(false);
                    application.RequestSmokeTestShutdown();
                });
            }

            application.Run();
            return 0;
        }
        catch (DllNotFoundException ex)
        {
            Console.Error.WriteLine("Ghost FTP requires the standard Linux X11/XWayland client library (libX11.so.6) for its native desktop renderer.");
            Console.Error.WriteLine(ex.Message);
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{GhostProduct.DisplayName} encountered a local startup error: {ex.Message}");
            Console.Error.WriteLine("No crash report, telemetry or diagnostic data was transmitted.");
            return 1;
        }
    }
}
