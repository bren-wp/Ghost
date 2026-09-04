using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;
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
            _profileStore = new ProfileStore(_paths.ProfilesFile, _secrets);
            _settingsStore = new AppSettingsStore(_paths.SettingsFile);
            _settings = await _settingsStore.LoadAsync();
            _localPath = _settings.LastLocalDirectory;
            _localPathBox.Text = _localPath;

            _queue = new TransferQueueService(CreateTransferSessionAsync, SynchronizationContext.Current);
            _queueList.ItemsSource = _queue.Jobs;
            _queue.JobUpdated += QueueJobUpdated;

            var profiles = await _profileStore.LoadAsync();
            foreach (var profile in profiles) _profiles.Add(profile);
            if (_profiles.Count > 0) _profilesList.SelectedIndex = 0;

            RefreshLocal();
            UpdatePaneSummaries();
            UpdateConnectionUi();
        }
        catch (Exception ex)
        {
            GhostMessageDialog.Error(this, "Ghost FTP could not finish startup.", ex.Message, "Startup error");
        }
    }

    private async void OnClosingAsync(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        IsEnabled = false;
        try
        {
            _connectionCts?.Cancel();
            CancelAllTransfers();
            if (_queue is not null) await _queue.DisposeAsync();
            if (_session is not null) await _session.DisposeAsync();

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
        if (_profilesList.SelectedItem is not ServerProfile profile || _profileStore is null) return;
        _host.Text = profile.Host;
        _port.Text = profile.Port.ToString();
        _username.Text = profile.Username;
        _security.SelectedIndex = (int)profile.Security;
        _password.Password = profile.IsDemo ? string.Empty : _profileStore.GetPassword(profile);
        _remotePathBox.Text = profile.InitialPath;
    }

    private async Task ConnectAsync()
    {
        if (_busy) return;
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
                _session = new DemoFtpSession();
            }
            else
            {
                var host = _host.Text.Trim();
                if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("Host is required.");
                if (!int.TryParse(_port.Text.Trim(), out var port) || port is < 1 or > 65535)
                    throw new InvalidOperationException("Port must be between 1 and 65535.");

                var securityMode = (FtpSecurityMode)Math.Max(0, _security.SelectedIndex);
                if (securityMode == FtpSecurityMode.Plain && !GhostMessageDialog.Confirm(
                        this,
                        "Plain FTP is not encrypted",
                        "Plain FTP sends usernames, passwords and file data without TLS encryption. Continue only when this is an intentionally trusted server or isolated network.",
                        "Use plain FTP",
                        danger: true,
                        warning: true))
                    throw new OperationCanceledException(ct);

                newOptions = new FtpConnectionOptions
                {
                    Host = host,
                    Port = port,
                    Username = _username.Text.Trim(),
                    Password = _password.Password,
                    Security = securityMode
                };
                _session = new FtpSession(newOptions);
            }

            await _session.ConnectAsync(ct);
            _activeOptions = newOptions;

            var initial = selected?.InitialPath;
            if (!string.IsNullOrWhiteSpace(initial) && initial != "/")
            {
                try
                {
                    await _session.ChangeDirectoryAsync(initial, ct);
                }
                catch
                {
                    // A stale initial path must not make the whole connection fail.
                }
            }

            _remotePath = await _session.GetWorkingDirectoryAsync(ct);
            _remotePathBox.Text = _remotePath;
            await RefreshRemoteAsync();
            SetStatus(
                _session.IsEncrypted ? "Connected · TLS" : selected?.IsDemo == true ? "Demo · local" : "Connected · FTP",
                _session.IsEncrypted ? "Success" : "AccentSoft");
        }
        catch (OperationCanceledException)
        {
            await DisconnectCoreAsync();
            SetStatus("Offline", "Surface2");
        }
        catch (Exception ex)
        {
            await DisconnectCoreAsync();
            SetStatus("Connection failed", "Danger");
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
        if (_profilesList.SelectedItem is not ServerProfile selected) return null;
        return string.Equals(_host.Text.Trim(), selected.Host, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_port.Text.Trim(), selected.Port.ToString(), StringComparison.Ordinal)
            && string.Equals(_username.Text.Trim(), selected.Username, StringComparison.Ordinal)
            && _security.SelectedIndex == (int)selected.Security
            ? selected
            : null;
    }

    private async Task DisconnectAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            _connectionCts?.Cancel();
            CancelAllTransfers();
            await DisconnectCoreAsync();
            _remoteAll.Clear();
            _remoteItems.Clear();
            _remotePath = "/";
            _remotePathBox.Text = "/";
            SetStatus("Offline", "Surface2");
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
        if (_session is null) return;
        try
        {
            await _session.DisconnectAsync();
        }
        catch
        {
            // Best effort. Disposing the session is the important cleanup step.
        }
        await _session.DisposeAsync();
        _session = null;
        _activeOptions = null;
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
        if (!IsConnected) return;
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
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not refresh the remote folder.", ex);
        }
    }
}
