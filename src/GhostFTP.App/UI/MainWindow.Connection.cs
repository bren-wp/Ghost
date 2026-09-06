using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;
using GhostFTP.Design;
using GhostFTP.Services;
using System.ComponentModel;
using System.Windows;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            AppendConnectionLog("Ghost FTP started. Telemetry and tracking are disabled.");
            _profileStore = new ProfileStore(_paths.ProfilesFile, _secrets);
            _settingsStore = new AppSettingsStore(_paths.SettingsFile);
            _settings = await _settingsStore.LoadAsync();
            ApplyWorkspaceSettings();
            _localPath = _settings.LastLocalDirectory;
            _localPathBox.Text = _localPath;

            _queue = new TransferQueueService(
                CreateTransferSessionAsync,
                SynchronizationContext.Current,
                _settings.AutomaticTransferRetries,
                _settings.ConcurrentTransfers);
            _queueList.ItemsSource = _queue.Jobs;
            _queue.JobUpdated += QueueJobUpdated;
            UpdateQueueManagementUi();
            UpdateQueueSummary();
            StartKeepAliveLoop();

            var profiles = await _profileStore.LoadAsync();
            foreach (var profile in profiles)
                _profiles.Add(profile);
            if (_profiles.Count > 0)
                _profilesList.SelectedIndex = 0;
            AppendConnectionLog($"Loaded {_profiles.Count} local saved-site profile(s).");

            RefreshLocal();
            UpdatePaneSummaries();
            UpdateConnectionUi();
            AppendConnectionLog($"Local workspace ready: {_localPath}");

            if (_captureDirectory is not null)
                await RunDocumentationCaptureAsync();
        }
        catch (Exception ex)
        {
            AppendConnectionLog($"Startup failed: {ex.Message}", "ERROR");

            if (_captureDirectory is not null)
            {
                // Documentation capture is a non-interactive CI/build path. A modal startup
                // error would leave the runner blocked until timeout and hide the real failure.
                Console.Error.WriteLine("Ghost FTP documentation capture failed:");
                Console.Error.WriteLine(ex);
                _allowClose = true;
                Application.Current.Shutdown(1);
                return;
            }

            GhostMessageDialog.Error(this, "Ghost FTP could not finish startup.", ex.Message, "Startup error");
        }
    }

    private async void OnClosingAsync(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        CaptureWorkspaceSettings();
        IsEnabled = false;
        try
        {
            _completionRefreshCts?.Cancel();
            _connectionCts?.Cancel();
            CancelAllTransfers();
            if (_queue is not null)
                await _queue.DisposeAsync();
            await DisconnectCoreAsync();

            if (_settingsStore is not null)
            {
                _settings.LastLocalDirectory = _localPath;
                await _settingsStore.SaveAsync(_settings);
            }

            if (_profileStore is not null)
                await _profileStore.SaveAsync(_profiles);
        }
        catch
        {
            // Closing must never create a crash report or background network request.
        }
        finally
        {
            _allowClose = true;
            Close();
        }
    }

    private void ProfileSelected()
    {
        if (_profilesList.SelectedItem is not ServerProfile profile || _profileStore is null)
            return;

        _host.Text = profile.Host;
        _port.Text = profile.Port.ToString();
        _username.Text = profile.Username;
        _security.SelectedIndex = (int)profile.Security;
        _password.Password = profile.IsDemo ? string.Empty : _profileStore.GetPassword(profile);
        _remotePathBox.Text = profile.InitialPath;
    }

    private async Task ConnectAsync()
    {
        if (_busy)
            return;

        _busy = true;
        _connectionCts?.Cancel();
        _connectionCts?.Dispose();
        _connectionCts = new CancellationTokenSource();
        var ct = _connectionCts.Token;

        try
        {
            CancelAllTransfers();
            await DisconnectCoreAsync();
            SetStatus("Connecting…", "Warning");
            UpdateConnectionUi();

            var selected = MatchingSelectedProfile();
            FtpConnectionOptions? newOptions = null;
            if (selected?.IsDemo == true)
            {
                AppendConnectionLog("Opening built-in Ghost FTP Demo session. No network connection is created.", "DEMO");
                _session = new DemoFtpSession();
            }
            else
            {
                // Validate untrusted UI input before it reaches logging, DNS resolution or the
                // FTP command channel. FtpSession validates again at the protocol boundary.
                var host = InputGuard.Host(_host.Text);
                if (!int.TryParse(_port.Text.Trim(), out var parsedPort))
                    throw new InvalidOperationException("Port must be a number between 1 and 65535.");
                var port = InputGuard.Port(parsedPort);
                if (_security.SelectedIndex is < 0 or > 2)
                    throw new InvalidOperationException("Select a valid FTP security mode.");

                var securityMode = (FtpSecurityMode)_security.SelectedIndex;
                var username = InputGuard.CommandArgument(_username.Text.Trim(), "username");
                var password = InputGuard.CommandArgument(_password.Password, "password");

                if (securityMode == FtpSecurityMode.Plain && !GhostMessageDialog.Confirm(
                        this,
                        "Plain FTP is not encrypted",
                        "Plain FTP sends usernames, passwords and file data without TLS encryption. Continue only when this is an intentionally trusted server or isolated network.",
                        GhostLocalization.T("Continue"),
                        danger: true,
                        warning: true))
                {
                    throw new OperationCanceledException(ct);
                }

                AppendConnectionLog($"Connecting to {host}:{port} using {securityMode}.");
                newOptions = new FtpConnectionOptions
                {
                    Host = host,
                    Port = port,
                    Username = username,
                    Password = password,
                    Security = securityMode,
                    ConnectTimeout = TimeSpan.FromSeconds(_settings.ConnectTimeoutSeconds),
                    CommandTimeout = TimeSpan.FromSeconds(_settings.CommandTimeoutSeconds),
                    TransferTimeout = TimeSpan.FromSeconds(_settings.TransferIdleTimeoutSeconds)
                };
                _session = new FtpSession(newOptions);
            }

            await _session.ConnectAsync(ct);
            _activeOptions = newOptions;
            AppendConnectionLog(
                _session.IsEncrypted
                    ? "Control connection established with TLS protection."
                    : selected?.IsDemo == true
                        ? "Demo session ready."
                        : "Control connection established without TLS.",
                _session.IsEncrypted ? "TLS" : "INFO");

            var initial = selected?.InitialPath;
            if (!string.IsNullOrWhiteSpace(initial) && initial != "/")
            {
                try
                {
                    await _session.ChangeDirectoryAsync(initial, ct);
                }
                catch
                {
                    AppendConnectionLog($"Saved initial path '{initial}' was unavailable; using the server working directory.", "WARN");
                }
            }

            _remotePath = await _session.GetWorkingDirectoryAsync(ct);
            _remotePathBox.Text = _remotePath;
            await RefreshRemoteAsync();
            KeepQuickConnectionInTabIfRequested();
            SetStatus(
                _session.IsEncrypted ? "Connected · TLS" : selected?.IsDemo == true ? "Demo · local" : "Connected · FTP",
                _session.IsEncrypted ? "Success" : "AccentSoft");
            _statusBadge.ToolTip = "Connection status · click for local diagnostics";
            AppendConnectionLog($"Ready in remote directory {_remotePath}.", "OK");
        }
        catch (OperationCanceledException)
        {
            await DisconnectCoreAsync();
            SetStatus(GhostLocalization.T("Offline"), "Surface2");
            _statusBadge.ToolTip = "Connection status · click for local diagnostics";
            AppendConnectionLog("Connection attempt cancelled.", "WARN");
        }
        catch (Exception ex)
        {
            await DisconnectCoreAsync();
            SetStatus("Connection failed", "Danger");
            _statusBadge.ToolTip = "Connection status · click for local diagnostics";
            AppendConnectionLog($"Connection failed: {ex.Message}", "ERROR");
            GhostMessageDialog.Error(this, "Ghost FTP could not connect to the server.", ex.Message, "Connection failed");
        }
        finally
        {
            _busy = false;
            UpdateConnectionUi();
            UpdatePaneSummaries();
        }
    }

    private ServerProfile? MatchingSelectedProfile()
    {
        if (_profilesList.SelectedItem is not ServerProfile selected)
            return null;

        return string.Equals(_host.Text.Trim(), selected.Host, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_port.Text.Trim(), selected.Port.ToString(), StringComparison.Ordinal)
            && string.Equals(_username.Text.Trim(), selected.Username, StringComparison.Ordinal)
            && _security.SelectedIndex == (int)selected.Security
            ? selected
            : null;
    }

    private async Task DisconnectAsync()
    {
        if (_busy)
            return;

        _busy = true;
        try
        {
            _connectionCts?.Cancel();
            CancelAllTransfers();
            var wasConnected = IsConnected;
            await DisconnectCoreAsync();
            _remoteAll.Clear();
            _remoteItems.Clear();
            _remotePath = "/";
            _remotePathBox.Text = "/";
            SetStatus(GhostLocalization.T("Offline"), "Surface2");
            _statusBadge.ToolTip = "Connection status · click for local diagnostics";
            if (wasConnected)
                AppendConnectionLog("Disconnected from the active server.");
        }
        finally
        {
            _busy = false;
            UpdateConnectionUi();
            UpdatePaneSummaries();
        }
    }

    private async Task DisconnectCoreAsync()
    {
        // Clear authoritative state first so callbacks, queue workers and keepalive logic cannot
        // observe a stale active session while QUIT/disposal is in progress.
        var session = _session;
        _session = null;
        _activeOptions = null;

        if (session is null)
            return;

        try
        {
            await session.DisconnectAsync();
        }
        catch
        {
            // QUIT is best effort. Disposal below is the authoritative transport cleanup.
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    private async Task<(IFtpSession Session, bool DisposeAfter)> CreateTransferSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is DemoFtpSession demo && demo.IsConnected)
            return (demo, false);
        if (_activeOptions is null || !IsConnected)
            throw new InvalidOperationException("No FTP/FTPS server is connected.");

        var transfer = new FtpSession(_activeOptions);
        try
        {
            await transfer.ConnectAsync(cancellationToken);
            return (transfer, true);
        }
        catch
        {
            await transfer.DisposeAsync();
            throw;
        }
    }

    private async Task RefreshRemoteAsync()
    {
        if (!IsConnected)
            return;

        try
        {
            var entries = await _session!.ListAsync(_remotePath);
            _remoteAll = entries
                .Select(x => new RemoteItem { Entry = x })
                .OrderByDescending(x => x.IsDirectory)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ApplyRemoteFilter();
            _remotePathBox.Text = _remotePath;
            AppendConnectionLog($"Directory listing completed: {_remoteAll.Count} item(s) in {_remotePath}.", "LIST");
        }
        catch (Exception ex)
        {
            AppendConnectionLog($"Remote refresh failed: {ex.Message}", "ERROR");
            ShowOperationError("Could not refresh the remote folder.", ex);
        }
    }
}
