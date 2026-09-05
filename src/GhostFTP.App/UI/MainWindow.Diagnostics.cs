using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Design;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private async Task ShowConnectionDiagnosticsAsync()
    {
        if (!IsConnected || _session is null)
        {
            GhostMessageDialog.Information(
                this,
                "Connection diagnostics",
                "Connect to an FTP or FTPS server first. Diagnostics run locally and never send results to Ghost FTP or any third party.");
            return;
        }

        try
        {
            SetStatus("Checking connection…", "Warning");
            var info = await _session.GetServerInfoAsync(_connectionCts?.Token ?? CancellationToken.None);
            ShowDiagnostics(info);
            SetStatus(info.IsEncrypted ? "Connected · TLS" : _session is DemoFtpSession ? "Demo · local" : "Connected · FTP",
                info.IsEncrypted ? "Success" : "AccentSoft");
        }
        catch (OperationCanceledException)
        {
            // Disconnect/shutdown already owns the visible connection state.
        }
        catch (Exception ex)
        {
            SetStatus("Diagnostics failed", "Danger");
            GhostMessageDialog.Error(
                this,
                "Ghost FTP could not complete the local connection diagnostic check.",
                ex.Message,
                "Connection diagnostics");
        }
    }

    private void ShowDiagnostics(FtpServerInfo info)
    {
        var features = info.Features.Count == 0
            ? "None reported"
            : string.Join(", ", info.Features.Take(80));
        if (info.Features.Count > 80)
            features += $" … (+{info.Features.Count - 80} more)";

        var transport = info.IsEncrypted ? "FTPS · TLS protected" : _session is DemoFtpSession ? "Local demo · no network" : "FTP · unencrypted";
        var summary =
            $"Host: {info.Host}\n" +
            $"Transport: {transport}\n" +
            $"Server system: {info.ServerSystem}\n" +
            $"Working directory: {info.WorkingDirectory}\n" +
            $"Capabilities: {features}\n" +
            $"Checked: {info.CheckedUtc.LocalDateTime:yyyy-MM-dd HH:mm:ss}\n\n" +
            "This diagnostic is performed directly against the connected server. Ghost FTP does not upload, log or transmit the result elsewhere.";

        GhostMessageDialog.Information(this, "Connection diagnostics", summary);
    }
}
