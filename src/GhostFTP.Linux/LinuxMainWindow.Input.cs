using System.Diagnostics;
using System.Text;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;
using GhostFTP.Design;

namespace GhostFTP.Linux;

internal sealed partial class LinuxMainWindow
{
    private FtpSecurityMode _securityMode = FtpSecurityMode.ExplicitTls;
    private int _transferSelected = -1;

    private void HandleEvent(X11Native.XEvent xevent)
    {
        switch (xevent.type)
        {
            case X11Native.Expose:
                RequestRedraw();
                break;
            case X11Native.ConfigureNotify:
                _width = Math.Max(980, xevent.xconfigure.width);
                _height = Math.Max(680, xevent.xconfigure.height);
                RequestRedraw();
                break;
            case X11Native.ClientMessage:
                if ((nuint)xevent.xclient.data0 == _wmDelete)
                    _closing = true;
                break;
            case X11Native.ButtonPress:
                HandleButtonPress(xevent.xbutton);
                break;
            case X11Native.KeyPress:
                HandleKeyPress(xevent.xkey);
                break;
        }
    }

    private void HandleButtonPress(X11Native.XButtonEvent button)
    {
        if (button.button == 4 || button.button == 5)
        {
            HandleWheel(button.x, button.y, button.button == 4 ? -3 : 3);
            return;
        }
        if (button.button != 1) return;

        for (var i = _hitRegions.Count - 1; i >= 0; i--)
        {
            var hit = _hitRegions[i];
            if (!hit.Bounds.Contains(button.x, button.y)) continue;
            hit.Action();
            return;
        }

        if (_modalKind == ModalKind.None)
        {
            _focusedFieldId = null;
            RequestRedraw();
        }
    }

    private void HandleWheel(int x, int y, int delta)
    {
        if (_modalKind != ModalKind.None || x < SidebarWidth)
            return;

        var mainX = SidebarWidth + OuterGap;
        var contentWidth = Math.Max(1, _width - mainX - OuterGap);
        var contentTop = MenuHeight + ToolbarHeight + OuterGap;
        var topHeight = Math.Clamp((int)(_height * 0.23), 180, 215);
        var panesTop = contentTop + topHeight + 8;
        var transferHeight = Math.Clamp((int)_settings.TransferPanelHeight, 128, 220);
        var panesHeight = Math.Max(190, _height - StatusHeight - panesTop - OuterGap - transferHeight - 7);
        var transferTop = panesTop + panesHeight + 7;

        if (y >= transferTop)
        {
            _transferScroll = Math.Max(0, _transferScroll + delta);
        }
        else if (y >= panesTop)
        {
            var split = mainX + (int)((contentWidth - 7) * Math.Clamp(_settings.LocalPaneFraction, 0.25, 0.75));
            if (x < split) _localScroll = Math.Max(0, _localScroll + delta);
            else _remoteScroll = Math.Max(0, _remoteScroll + delta);
        }
        RequestRedraw();
    }

    private void HandleKeyPress(X11Native.XKeyEvent keyEvent)
    {
        var keysym = X11Native.XLookupKeysym(ref keyEvent, 0);

        if (_modalKind == ModalKind.Input)
        {
            if (keysym == X11Native.XkEscape) { CloseModal(); return; }
            if (keysym == X11Native.XkReturn) { AcceptModal(); return; }
            if (keysym == X11Native.XkBackSpace)
            {
                if (_modalValue.Length > 0) _modalValue = _modalValue[..^1];
                RequestRedraw();
                return;
            }

            var input = LookupText(ref keyEvent);
            if (!string.IsNullOrEmpty(input) && !input.Any(char.IsControl) && _modalValue.Length + input.Length <= 4096)
            {
                _modalValue += input;
                RequestRedraw();
            }
            return;
        }

        if (_modalKind != ModalKind.None)
        {
            if (keysym == X11Native.XkEscape) CloseModal();
            return;
        }

        if (keysym == X11Native.XkF5)
        {
            RefreshAll();
            return;
        }
        if (keysym == X11Native.XkEscape)
        {
            _focusedFieldId = null;
            RequestRedraw();
            return;
        }
        if (keysym == X11Native.XkTab)
        {
            FocusNextField();
            return;
        }
        if (keysym == X11Native.XkDelete)
        {
            if (_transferSelected >= 0)
                CancelSelectedTransfer();
            else if (_remoteSelected >= 0 && _connected)
                DeleteRemote();
            else if (_localSelected >= 0)
                DeleteLocal();
            return;
        }
        if (keysym == X11Native.XkF2)
        {
            if (_transferSelected >= 0)
                return;
            if (_remoteSelected >= 0 && _connected)
                RenameRemote();
            else if (_localSelected >= 0)
                RenameLocal();
            return;
        }

        if (_focusedFieldId is null || !_fields.TryGetValue(_focusedFieldId, out var field))
            return;

        if (keysym == X11Native.XkReturn)
        {
            switch (field.Id)
            {
                case "localPath": NavigateLocal(field.Value); break;
                case "remotePath": if (_connected) _ = RunBackground(() => NavigateRemoteAsync(field.Value)); break;
                case "password": _ = RunBackground(ConnectCoreAsync); break;
            }
            return;
        }

        if (keysym == X11Native.XkBackSpace)
        {
            if (field.Value.Length > 0) field.Value = field.Value[..^1];
            OnFieldChanged(field.Id);
            return;
        }

        var text = LookupText(ref keyEvent);
        if (string.IsNullOrEmpty(text) || text.Any(char.IsControl) || field.Value.Length + text.Length > field.MaxLength)
            return;
        field.Value += text;
        OnFieldChanged(field.Id);
    }

    private static string LookupText(ref X11Native.XKeyEvent keyEvent)
    {
        var buffer = new byte[32];
        var count = X11Native.XLookupString(ref keyEvent, buffer, buffer.Length, out _, IntPtr.Zero);
        return count <= 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, count);
    }

    private void OnFieldChanged(string id)
    {
        if (id is "localFilter" or "localPath") _localScroll = 0;
        if (id is "remoteFilter" or "remotePath") _remoteScroll = 0;
        RequestRedraw();
    }

    private void FocusNextField()
    {
        var order = new[] { "host", "port", "username", "password", "localPath", "localFilter", "remotePath", "remoteFilter" };
        var index = _focusedFieldId is null ? -1 : Array.IndexOf(order, _focusedFieldId);
        _focusedFieldId = order[(index + 1 + order.Length) % order.Length];
        RequestRedraw();
    }

    private string SecurityLabel() => _securityMode switch
    {
        FtpSecurityMode.Plain => "FTP",
        FtpSecurityMode.ImplicitTls => "FTPS Implicit",
        _ => "FTPS Explicit"
    };

    private void CycleSecurity()
    {
        _securityMode = _securityMode switch
        {
            FtpSecurityMode.ExplicitTls => FtpSecurityMode.ImplicitTls,
            FtpSecurityMode.ImplicitTls => FtpSecurityMode.Plain,
            _ => FtpSecurityMode.ExplicitTls
        };
        _fields["port"].Value = _securityMode == FtpSecurityMode.ImplicitTls ? "990" : "21";
        RequestRedraw();
    }

    private void CycleSavedSite()
    {
        if (_profiles.Count == 0) return;
        _siteSelected = (_siteSelected + 1) % _profiles.Count;
        var profile = _profiles[_siteSelected];
        LoadProfileIntoFields(profile);
        _securityMode = profile.Security;
        RequestRedraw();
    }

    private void ActivateLocalRow(int index, LocalEntry item)
    {
        _localSelected = index;
        _remoteSelected = -1;
        _transferSelected = -1;
        var target = "local:" + item.FullPath;
        if (IsDoubleClick(target))
        {
            if (item.IsDirectory)
                NavigateLocal(item.FullPath);
            else
                OpenLocalFile(item.FullPath);
        }
        RequestRedraw();
    }

    private void ActivateRemoteRow(int index, FtpEntry item)
    {
        _remoteSelected = index;
        _localSelected = -1;
        _transferSelected = -1;
        var target = "remote:" + item.FullPath;
        if (IsDoubleClick(target))
        {
            if (item.IsDirectory)
                _ = RunBackground(() => NavigateRemoteAsync(item.FullPath));
            else
                QueueDownloadSelected();
        }
        RequestRedraw();
    }

    private bool IsDoubleClick(string target)
    {
        var now = DateTimeOffset.UtcNow;
        var result = string.Equals(_lastClickTarget, target, StringComparison.Ordinal)
            && now - _lastClickUtc <= TimeSpan.FromMilliseconds(450);
        _lastClickTarget = target;
        _lastClickUtc = now;
        return result;
    }

    private static void OpenLocalFile(string path)
    {
        try
        {
            var info = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
            info.ArgumentList.Add(path);
            _ = Process.Start(info);
        }
        catch
        {
        }
    }

    private void LocalUp()
    {
        var parent = Directory.GetParent(_localPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent)) NavigateLocal(parent);
    }

    private void LocalHome() => NavigateLocal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private void RemoteUp()
    {
        if (_connected) _ = RunBackground(() => NavigateRemoteAsync(FtpListingParser.ParentRemote(_remotePath)));
    }

    private void RemoteHome()
    {
        if (_connected) _ = RunBackground(() => NavigateRemoteAsync("/"));
    }

    private void RefreshAll()
    {
        ReloadLocal();
        if (_connected) _ = RunBackground(RefreshRemoteCoreAsync);
    }

    private void NewLocalFolder()
    {
        ShowInput(L("NewFolder"), "Folder name", string.Empty, value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                var path = LocalPathSafety.CombineUnderRoot(_localPath, value);
                Directory.CreateDirectory(path);
                ReloadLocal();
            }
            catch (Exception ex) { Log("New folder failed: " + ex.Message); }
        });
    }

    private void RenameLocal()
    {
        var item = FilteredLocal().ElementAtOrDefault(_localSelected);
        if (item is null) return;
        ShowInput(L("Rename"), item.Name, item.Name, value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                var destination = LocalPathSafety.CombineUnderRoot(_localPath, value);
                if (File.Exists(destination) || Directory.Exists(destination))
                    throw new IOException("A local item with that name already exists.");
                if (item.IsDirectory) Directory.Move(item.FullPath, destination);
                else File.Move(item.FullPath, destination);
                ReloadLocal();
            }
            catch (Exception ex) { Log("Rename failed: " + ex.Message); }
        });
    }

    private void DeleteLocal()
    {
        var item = FilteredLocal().ElementAtOrDefault(_localSelected);
        if (item is null) return;
        ShowConfirm(L("Delete"), $"Delete '{item.Name}' from this device?", () =>
        {
            try
            {
                if (item.IsDirectory)
                {
                    var attributes = File.GetAttributes(item.FullPath);
                    Directory.Delete(item.FullPath, recursive: (attributes & FileAttributes.ReparsePoint) == 0);
                }
                else File.Delete(item.FullPath);
                _localSelected = -1;
                ReloadLocal();
            }
            catch (Exception ex) { Log("Delete failed: " + ex.Message); }
        });
    }

    private void NewRemoteFolder()
    {
        if (!_connected || _session is null) return;
        ShowInput(L("NewFolder"), "Remote folder name", string.Empty, value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            _ = RunBackground(async () =>
            {
                var name = InputGuard.RemoteName(value);
                await _session.CreateDirectoryAsync(FtpListingParser.CombineRemote(_remotePath, name), _connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                await RefreshRemoteCoreAsync().ConfigureAwait(false);
            });
        });
    }

    private void RenameRemote()
    {
        var item = FilteredRemote().ElementAtOrDefault(_remoteSelected);
        if (item is null || _session is null) return;
        ShowInput(L("Rename"), item.Name, item.Name, value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            _ = RunBackground(async () =>
            {
                var name = InputGuard.RemoteName(value);
                await _session.RenameAsync(item.FullPath, FtpListingParser.CombineRemote(_remotePath, name), _connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                await RefreshRemoteCoreAsync().ConfigureAwait(false);
            });
        });
    }

    private void DeleteRemote()
    {
        var item = FilteredRemote().ElementAtOrDefault(_remoteSelected);
        if (item is null || _session is null) return;
        ShowConfirm(L("Delete"), $"Delete remote '{item.Name}'?", () =>
        {
            _ = RunBackground(async () =>
            {
                if (item.IsDirectory)
                    await _session.DeleteDirectoryAsync(item.FullPath, recursive: true, _connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                else
                    await _session.DeleteFileAsync(item.FullPath, _connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                await RefreshRemoteCoreAsync().ConfigureAwait(false);
            });
        });
    }

    private void CancelSelectedTransfer()
    {
        if (_queue is null) return;
        var jobs = _queue.Jobs.ToArray();
        if (jobs.Length == 0) return;
        var index = Math.Clamp(_transferSelected < 0 ? jobs.Length - 1 : _transferSelected, 0, jobs.Length - 1);
        _queue.Cancel(jobs[index].Id);
        RequestRedraw();
    }

    private void CancelAllTransfers()
    {
        if (_queue is null) return;
        foreach (var job in _queue.Jobs.Where(x => x.State is TransferState.Queued or TransferState.Running or TransferState.Retrying).ToArray())
            _queue.Cancel(job.Id);
        RequestRedraw();
    }

    private void ClearFinishedTransfers()
    {
        _queue?.ClearFinished();
        _transferSelected = -1;
        RequestRedraw();
    }

    private async Task ShowDiagnosticsAsync()
    {
        if (_session is null || !_connected) return;
        var info = await _session.GetServerInfoAsync(_connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
        Post(() =>
        {
            Log($"Diagnostics: {info.Host} · {(info.IsEncrypted ? "TLS" : "plain FTP")} · PWD {info.WorkingDirectory}");
            if (!string.IsNullOrWhiteSpace(info.ServerSystem)) Log("SYST: " + info.ServerSystem);
            if (info.Features.Count > 0) Log("FEAT: " + string.Join(", ", info.Features.Take(12)));
        });
    }

    private void OpenSiteManager()
    {
        _modalKind = ModalKind.SiteManager;
        _focusedFieldId = null;
        RequestRedraw();
    }

    private void OpenSettings()
    {
        _languageIndex = Math.Max(0, GhostLocalization.SupportedLanguages.ToList().FindIndex(x => x.Code == GhostLocalization.CurrentLanguageCode));
        _modalKind = ModalKind.Settings;
        _focusedFieldId = null;
        RequestRedraw();
    }

    private void SaveCurrentSite()
    {
        ShowInput("Save site", "Profile name", string.Empty, value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (!int.TryParse(_fields["port"].Value, out var port)) port = _securityMode == FtpSecurityMode.ImplicitTls ? 990 : 21;
            var profile = new ServerProfile
            {
                Id = Guid.NewGuid(),
                Name = value.Trim(),
                Host = _fields["host"].Value,
                Port = port,
                Username = _fields["username"].Value,
                Security = _securityMode,
                InitialPath = string.IsNullOrWhiteSpace(_fields["remotePath"].Value) ? "/" : _fields["remotePath"].Value,
                RememberPassword = false
            };
            _profiles.Add(profile);
            _siteSelected = _profiles.Count - 1;
            try { _profileStore.SaveAsync(_profiles).GetAwaiter().GetResult(); }
            catch (Exception ex) { Log("Could not save site: " + ex.Message); }
            OpenSiteManager();
        });
    }

    private void DeleteSelectedSite()
    {
        var profile = _profiles.ElementAtOrDefault(_siteSelected);
        if (profile is null || profile.IsDemo) return;
        ShowConfirm(L("Delete"), $"Remove saved site '{profile.Name}'?", () =>
        {
            _profiles.Remove(profile);
            _siteSelected = Math.Clamp(_siteSelected, 0, Math.Max(0, _profiles.Count - 1));
            try { _profileStore.SaveAsync(_profiles).GetAwaiter().GetResult(); }
            catch (Exception ex) { Log("Could not save site changes: " + ex.Message); }
            OpenSiteManager();
        });
    }

    private void PreviousLanguage()
    {
        var count = GhostLocalization.SupportedLanguages.Count;
        _languageIndex = (_languageIndex - 1 + count) % count;
        RequestRedraw();
    }

    private void NextLanguage()
    {
        var count = GhostLocalization.SupportedLanguages.Count;
        _languageIndex = (_languageIndex + 1) % count;
        RequestRedraw();
    }

    private void CycleTheme()
    {
        _settings.Theme = _settings.Theme switch
        {
            AppTheme.System => AppTheme.Dark,
            AppTheme.Dark => AppTheme.Light,
            _ => AppTheme.System
        };
        RequestRedraw();
    }

    private void SaveSettings()
    {
        var language = GhostLocalization.SupportedLanguages[Math.Clamp(_languageIndex, 0, GhostLocalization.SupportedLanguages.Count - 1)];
        _settings.LanguageCode = language.Code;
        GhostLocalization.SetLanguage(language.Code);
        ApplyPalette();
        _referencePaletteApplied = false;
        try { _settingsStore.SaveAsync(_settings).GetAwaiter().GetResult(); }
        catch (Exception ex) { Log("Could not save settings: " + ex.Message); }
        Log("Settings saved. Transfer concurrency/retry changes apply to the next connection.");
        CloseModal();
    }

    private void ShowInput(string title, string text, string initial, Action<string?> callback)
    {
        _modalKind = ModalKind.Input;
        _modalTitle = title;
        _modalText = text;
        _modalValue = initial;
        _modalCallback = callback;
        _focusedFieldId = null;
        RequestRedraw();
    }

    private void ShowConfirm(string title, string text, Action confirmed)
    {
        _modalKind = ModalKind.Confirm;
        _modalTitle = title;
        _modalText = text;
        _modalValue = string.Empty;
        _modalCallback = _ => confirmed();
        _focusedFieldId = null;
        RequestRedraw();
    }

    private void AcceptModal()
    {
        var callback = _modalCallback;
        var value = _modalKind == ModalKind.Input ? _modalValue : "yes";
        CloseModal();
        callback?.Invoke(value);
    }

    private void CloseModal()
    {
        _modalKind = ModalKind.None;
        _modalTitle = string.Empty;
        _modalText = string.Empty;
        _modalValue = string.Empty;
        _modalCallback = null;
        RequestRedraw();
    }
}
