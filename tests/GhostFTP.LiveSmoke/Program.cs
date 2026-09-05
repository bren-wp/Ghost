using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;

namespace GhostFTP.LiveSmoke;

public static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            var host = Required("GHOSTFTP_LIVE_HOST");
            var username = Required("GHOSTFTP_LIVE_USERNAME");
            var password = Required("GHOSTFTP_LIVE_PASSWORD");
            var security = ParseSecurity(Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_SECURITY"));
            var defaultPort = security == FtpSecurityMode.ImplicitTls ? 990 : 21;
            var port = ParsePort(Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_PORT"), defaultPort);
            var path = Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_PATH");
            path = string.IsNullOrWhiteSpace(path) ? "/" : InputGuard.RemotePath(path);

            if (security == FtpSecurityMode.Plain
                && !string.Equals(Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_ALLOW_PLAIN"), "1", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Plain FTP live smoke is disabled by default. Use FTPS or set GHOSTFTP_LIVE_ALLOW_PLAIN=1 explicitly.");
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await using var session = new FtpSession(new FtpConnectionOptions
            {
                Host = host,
                Port = port,
                Username = username,
                Password = password,
                Security = security,
                ConnectTimeout = TimeSpan.FromSeconds(20),
                CommandTimeout = TimeSpan.FromSeconds(30),
                TransferTimeout = TimeSpan.FromSeconds(60)
            });

            Console.WriteLine("LIVE  Connecting with credentials supplied only through environment/secrets.");
            await session.ConnectAsync(timeout.Token).ConfigureAwait(false);
            Assert(session.IsConnected, "Session did not report connected state.");
            if (security != FtpSecurityMode.Plain)
                Assert(session.IsEncrypted, "FTPS smoke test did not establish TLS.");

            var workingDirectory = await session.GetWorkingDirectoryAsync(timeout.Token).ConfigureAwait(false);
            Assert(!string.IsNullOrWhiteSpace(workingDirectory), "PWD returned an empty path.");

            if (!string.Equals(path, workingDirectory, StringComparison.Ordinal))
            {
                await session.ChangeDirectoryAsync(path, timeout.Token).ConfigureAwait(false);
                path = await session.GetWorkingDirectoryAsync(timeout.Token).ConfigureAwait(false);
            }

            var entries = await session.ListAsync(path, timeout.Token).ConfigureAwait(false);
            await session.KeepAliveAsync(timeout.Token).ConfigureAwait(false);
            await session.DisconnectAsync(timeout.Token).ConfigureAwait(false);

            Console.WriteLine($"PASS  Live connect/PWD/LIST/NOOP/disconnect completed. Listed {entries.Count} item(s). No writes were performed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  Live FTP smoke test: " + Redact(ex.Message));
            return 1;
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Required environment variable {name} is not configured.");
        return value;
    }

    private static int ParsePort(string? text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        if (!int.TryParse(text, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("GHOSTFTP_LIVE_PORT must be between 1 and 65535.");
        return port;
    }

    private static FtpSecurityMode ParseSecurity(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        null or "" or "explicit" or "explicit-tls" or "ftps-explicit" => FtpSecurityMode.ExplicitTls,
        "implicit" or "implicit-tls" or "ftps-implicit" => FtpSecurityMode.ImplicitTls,
        "plain" or "ftp" => FtpSecurityMode.Plain,
        _ => throw new InvalidOperationException("GHOSTFTP_LIVE_SECURITY must be explicit, implicit or plain.")
    };

    private static string Redact(string message)
    {
        var password = Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_PASSWORD");
        var username = Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_USERNAME");
        var host = Environment.GetEnvironmentVariable("GHOSTFTP_LIVE_HOST");
        foreach (var secret in new[] { password, username, host }.Where(x => !string.IsNullOrEmpty(x)))
            message = message.Replace(secret!, "[redacted]", StringComparison.OrdinalIgnoreCase);
        return message;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
