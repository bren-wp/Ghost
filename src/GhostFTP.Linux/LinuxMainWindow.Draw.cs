using System.Text;
using GhostFTP.Core.Models;
using GhostFTP.Design;

namespace GhostFTP.Linux;

internal sealed partial class LinuxMainWindow
{
    private const int MenuHeight = 38;
    private const int ToolbarHeight = 70;
    private const int StatusHeight = 28;
    private const int OuterGap = 10;
    private const int SidebarWidth = 292;
    private bool _referencePaletteApplied;

    private void Draw()
    {
        EnsureReferencePalette();
        _hitRegions.Clear();
        SetColor(_cBg);
        X11Native.XFillRectangle(_display, _window, _gc, 0, 0, (uint)Math.Max(1, _width), (uint)Math.Max(1, _height));

        DrawSidebar();
        DrawMenu(SidebarWidth);
        DrawToolbar(SidebarWidth);

        var mainX = SidebarWidth + OuterGap;
        var y = MenuHeight + ToolbarHeight + OuterGap;
        var contentWidth = Math.Max(700, _width - mainX - OuterGap);
        var topHeight = Math.Clamp((int)(_height * 0.23), 180, 215);
        var topGap = 8;
        var logWidth = Math.Max(330, (contentWidth - topGap) * 48 / 100);
        var quickWidth = Math.Max(370, contentWidth - topGap - logWidth);

        DrawConnectionLog(new RectI(mainX, y, logWidth, topHeight));
        DrawQuickConnect(new RectI(mainX + logWidth + topGap, y, quickWidth, topHeight));
        y += topHeight + 8;

        var transferHeight = Math.Clamp((int)_settings.TransferPanelHeight, 128, 220);
        var available = _height - StatusHeight - y - OuterGap;
        var panesHeight = Math.Max(240, available - transferHeight - 7);
        DrawFilePanes(new RectI(mainX, y, contentWidth, panesHeight));
        y += panesHeight + 7;
        DrawTransfers(new RectI(mainX, y, contentWidth, Math.Max(110, _height - StatusHeight - y)));

        DrawStatusBar();

        if (_modalKind != ModalKind.None)
            DrawModal();

        X11Native.XFlush(_display);
    }

    private void EnsureReferencePalette()
    {
        if (_referencePaletteApplied)
            return;

        _cBg = Color(GhostReferencePalette.Background);
        _cSurface = Color(GhostReferencePalette.Surface);
        _cSurface2 = Color(GhostReferencePalette.Surface2);
        _cBorder = Color(GhostReferencePalette.Border);
        _cText = Color(GhostReferencePalette.Text);
        _cMuted = Color(GhostReferencePalette.Muted);
        _cAccent = Color(GhostReferencePalette.Accent);
        _cAccentSoft = Color(GhostReferencePalette.AccentSoft);
        _cDanger = Color(GhostReferencePalette.Danger);
        _cSuccess = Color(GhostReferencePalette.Success);
        _referencePaletteApplied = true;
    }

    private void DrawSidebar()
    {
        Fill(new RectI(0, 0, SidebarWidth, _height), _cSurface);
        DrawLine(SidebarWidth - 1, 0, SidebarWidth - 1, _height, _cBorder);

        DrawText("G", 20, 43, _cAccent);
        DrawText(GhostProduct.DisplayName, 47, 37, _cText);
        DrawText("PRIVATE FILE CLIENT", 47, 51, _cMuted);
        DrawText("Private file transfers, simply.", 20, 79, _cMuted);
        DrawLine(0, 108, SidebarWidth - 1, 108, _cBorder);

        DrawText(L("SavedServers"), 20, 142, _cText);
        DrawButton(new RectI(SidebarWidth - 49, 122, 34, 34), "+", OpenSiteManager);
        DrawButton(new RectI(14, 169, SidebarWidth - 28, 34), "⌂  Home", RefreshAll);

        DrawText($"▣  This tab                                      {_profiles.Count}", 20, 235, _cText);
        var sy = 252;
        foreach (var pair in _profiles.Take(5).Select((profile, index) => (profile, index)))
        {
            var row = new RectI(20, sy, SidebarWidth - 40, 28);
            if (pair.index == _siteSelected) Fill(row, _cAccentSoft);
            DrawText(Ellipsize(pair.profile.Name, 27), row.X + 8, row.Y + 19, pair.index == _siteSelected ? _cText : _cMuted);
            var captured = pair.index;
            Register(row, () =>
            {
                _siteSelected = captured;
                LoadProfileIntoFields(_profiles[captured]);
                RequestRedraw();
            });
            sy += 30;
        }
        if (_profiles.Count == 0)
            DrawText("No saved connection in this tab.", 36, sy + 18, _cMuted);

        sy += _profiles.Count == 0 ? 42 : 12;
        DrawButton(new RectI(14, sy, SidebarWidth - 28, 34), "☆  Favorites in this tab", OpenSiteManager);
        DrawButton(new RectI(14, sy + 40, SidebarWidth - 28, 34), "◷  Recent connections in this tab", () => _ = RunBackground(ShowDiagnosticsAsync), enabled: _connected);

        var privacyY = Math.Max(sy + 88, _height - 164);
        var privacy = new RectI(12, privacyY, SidebarWidth - 24, 104);
        Fill(privacy, _cSurface2);
        Border(privacy, _cBorder);
        DrawText("◇  Account not required", privacy.X + 14, privacy.Y + 25, _cText);
        DrawText("Profiles and settings stay local.", privacy.X + 14, privacy.Y + 48, _cMuted);
        DrawText("No telemetry · no tracking.", privacy.X + 14, privacy.Y + 65, _cMuted);
        DrawText("No Ghost FTP cloud account.", privacy.X + 14, privacy.Y + 82, _cMuted);

        var footerY = _height - 50;
        Fill(new RectI(0, footerY, SidebarWidth, 50), _cSurface);
        DrawLine(0, footerY, SidebarWidth - 1, footerY, _cBorder);
        DrawButton(new RectI(12, footerY + 8, 120, 32), "⚙ " + L("Settings"), OpenSettings);
        DrawButton(new RectI(142, footerY + 8, 138, 32), "ⓘ " + L("About"), () =>
        {
            _modalKind = ModalKind.Confirm;
            _modalTitle = GhostProduct.DisplayName;
            _modalText = $"{GhostProduct.InformationalVersion} · BRENDIGO LTD · No telemetry · No tracking";
            _modalCallback = _ => { };
            RequestRedraw();
        });
    }

    private void DrawMenu(int left)
    {
        Fill(new RectI(left, 0, _width - left, MenuHeight), Color(GhostReferencePalette.Menu));
        DrawLine(left, MenuHeight - 1, _width, MenuHeight - 1, _cBorder);
        var x = left + 16;
        foreach (var label in new[] { "File", "View", "Sites", "Transfers", "Tools", "Help" })
        {
            DrawText(label, x, 25, _cText);
            x += label.Length * 9 + 22;
        }

        var language = GhostLocalization.SupportedLanguages.ElementAtOrDefault(_languageIndex)?.NativeName ?? "English";
        DrawButton(new RectI(Math.Max(left + 650, _width - 126), 4, 112, 30), "☆ " + Ellipsize(language, 10), OpenSettings);
    }

    private void DrawToolbar(int left)
    {
        var top = MenuHeight;
        Fill(new RectI(left, top, _width - left, ToolbarHeight), Color(GhostReferencePalette.Toolbar));
        DrawLine(left, top + ToolbarHeight - 1, _width, top + ToolbarHeight - 1, _cBorder);

        var x = left + 12;
        x += DrawButton(new RectI(x, top + 14, 92, 42), "⚡ " + L("Connect"), () => _ = RunBackground(ConnectCoreAsync), primary: true).Width + 5;
        x += DrawButton(new RectI(x, top + 14, 100, 42), "⏻ " + L("Disconnect"), () => _ = RunBackground(() => DisconnectCoreAsync()), enabled: _connected).Width + 5;
        x += DrawButton(new RectI(x, top + 14, 86, 42), "↑ " + L("Upload"), QueueUploadSelected, enabled: _connected).Width + 5;
        x += DrawButton(new RectI(x, top + 14, 96, 42), "↓ " + L("Download"), QueueDownloadSelected, enabled: _connected).Width + 5;
        x += DrawButton(new RectI(x, top + 14, 88, 42), "↻ " + L("Refresh"), RefreshAll).Width + 5;
        x += DrawButton(new RectI(x, top + 14, 108, 42), "▣ Site Manager", OpenSiteManager).Width + 5;
        x += DrawButton(new RectI(x, top + 14, 92, 42), "⚙ " + L("Settings"), OpenSettings).Width + 5;
        _ = DrawButton(new RectI(x, top + 14, 100, 42), "◉ Diagnostics", () => _ = RunBackground(ShowDiagnosticsAsync), enabled: _connected);

        var searchWidth = Math.Clamp((_width - left) / 4, 250, 342);
        var searchX = _width - searchWidth - 14;
        DrawText("⌕", searchX + 9, top + 39, _cMuted);
        DrawField("remoteFilter", new RectI(searchX + 30, top + 14, searchWidth - 30, 42));
    }

    private void DrawConnectionLog(RectI rect)
    {
        DrawCard(rect);
        DrawText("Connection Log", rect.X + 10, rect.Y + 20, _cText);
        DrawText("local session activity · credentials never logged", rect.X + 126, rect.Y + 20, _cMuted);
        DrawButton(new RectI(rect.X + rect.Width - 66, rect.Y + 6, 54, 25), "Clear", () =>
        {
            _connectionLog.Clear();
            RequestRedraw();
        });

        var inner = new RectI(rect.X + 9, rect.Y + 34, rect.Width - 18, rect.Height - 43);
        Fill(inner, _cSurface2);
        Border(inner, _cBorder);
        var visibleLines = Math.Max(1, (inner.Height - 8) / 16);
        var start = Math.Max(0, _connectionLog.Count - visibleLines);
        var y = inner.Y + 15;
        for (var i = start; i < _connectionLog.Count; i++)
        {
            DrawText(Ellipsize(_connectionLog[i], Math.Max(20, inner.Width / 8)), inner.X + 7, y, _cMuted);
            y += 16;
        }
    }

    private void DrawQuickConnect(RectI rect)
    {
        DrawCard(rect);
        var x = rect.X + 10;
        var y = rect.Y + 10;
        DrawText(L("QuickConnect"), x, y + 15, _cText);
        DrawText("FTPS Explicit recommended", x + 110, y + 15, _cMuted);

        var profileText = _profiles.ElementAtOrDefault(_siteSelected)?.Name ?? L("SavedServers");
        DrawButton(new RectI(rect.X + rect.Width - 290, y, 130, 26), Ellipsize(profileText, 13), CycleSavedSite);
        DrawButton(new RectI(rect.X + rect.Width - 154, y, 142, 26), "▣ Site Manager", OpenSiteManager);

        y += 42;
        var available = Math.Max(320, rect.Width - 20);
        var hostWidth = Math.Max(150, available * 33 / 100);
        var portWidth = 74;
        var securityWidth = Math.Max(125, available * 22 / 100);
        var usernameWidth = Math.Max(130, (available - hostWidth - portWidth - securityWidth - 24) / 2);
        var passwordWidth = Math.Max(130, available - hostWidth - portWidth - securityWidth - usernameWidth - 32);

        DrawLabeledField("host", L("Host"), new RectI(x, y, hostWidth, 34));
        DrawLabeledField("port", L("Port"), new RectI(x + hostWidth + 8, y, portWidth, 34));
        DrawSecurityField(new RectI(x + hostWidth + portWidth + 16, y, securityWidth, 34));
        var ux = x + hostWidth + portWidth + securityWidth + 24;
        DrawLabeledField("username", L("Username"), new RectI(ux, y, usernameWidth, 34));
        DrawLabeledField("password", L("Password"), new RectI(ux + usernameWidth + 8, y, passwordWidth, 34));

        y += 52;
        DrawText("Credentials remain local. FTPS Explicit is recommended.", x, y + 18, _cMuted);
        DrawButton(new RectI(rect.X + rect.Width - 126, y, 114, 32), _connected ? L("Disconnect") : L("Connect"),
            _connected ? () => _ = RunBackground(() => DisconnectCoreAsync()) : () => _ = RunBackground(ConnectCoreAsync),
            primary: !_connected,
            danger: _connected);
    }

    private void DrawFilePanes(RectI rect)
    {
        var gap = 7;
        var leftWidth = (int)((rect.Width - gap) * Math.Clamp(_settings.LocalPaneFraction, 0.25, 0.75));
        var rightWidth = rect.Width - gap - leftWidth;
        DrawFilePane(new RectI(rect.X, rect.Y, leftWidth, rect.Height), remote: false);
        DrawFilePane(new RectI(rect.X + leftWidth + gap, rect.Y, rightWidth, rect.Height), remote: true);
    }

    private void DrawFilePane(RectI rect, bool remote)
    {
        DrawCard(rect);
        var x = rect.X + 9;
        var y = rect.Y + 9;
        DrawText(remote ? L("Remote") : L("Local"), x, y + 15, _cText);
        DrawText(remote ? L("ConnectedServer") : "Linux", x + 62, y + 15, _cMuted);

        if (!remote)
        {
            var bx = rect.X + rect.Width - 230;
            DrawButton(new RectI(bx, y, 68, 24), "Home", () => NavigateLocal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
            DrawButton(new RectI(bx + 72, y, 74, 24), "Desktop", () => NavigateLocal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")));
            DrawButton(new RectI(bx + 150, y, 70, 24), "Downl.", () => NavigateLocal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")));
        }

        y += 32;
        DrawButton(new RectI(x, y, 54, 28), "↑ Up", remote ? RemoteUp : LocalUp);
        DrawField(remote ? "remotePath" : "localPath", new RectI(x + 60, y, rect.Width - 128, 28));
        DrawButton(new RectI(rect.X + rect.Width - 59, y, 49, 28), remote ? "⌂ /" : "⌂", remote ? RemoteHome : LocalHome);

        y += 35;
        var actionX = x;
        actionX += DrawButton(new RectI(actionX, y, 92, 28), remote ? "↓ " + L("Download") : "↑ " + L("Upload"), remote ? QueueDownloadSelected : QueueUploadSelected, enabled: !remote || _connected, primary: true).Width + 4;
        actionX += DrawButton(new RectI(actionX, y, 72, 28), "↻ " + L("Refresh"), remote ? () => _ = RunBackground(RefreshRemoteCoreAsync) : ReloadLocal, enabled: !remote || _connected).Width + 4;
        actionX += DrawButton(new RectI(actionX, y, 84, 28), "+ " + L("NewFolder"), remote ? NewRemoteFolder : NewLocalFolder, enabled: !remote || _connected).Width + 4;
        actionX += DrawButton(new RectI(actionX, y, 72, 28), L("Rename"), remote ? RenameRemote : RenameLocal, enabled: !remote || _connected).Width + 4;
        _ = DrawButton(new RectI(actionX, y, 66, 28), L("Delete"), remote ? DeleteRemote : DeleteLocal, enabled: !remote || _connected, danger: true);

        y += 35;
        DrawField(remote ? "remoteFilter" : "localFilter", new RectI(x, y, rect.Width - 84, 28));
        DrawButton(new RectI(rect.X + rect.Width - 77, y, 67, 28), "Clear", () =>
        {
            _fields[remote ? "remoteFilter" : "localFilter"].Value = string.Empty;
            if (remote) _remoteScroll = 0; else _localScroll = 0;
            RequestRedraw();
        });

        y += 35;
        var table = new RectI(x, y, rect.Width - 18, Math.Max(74, rect.Y + rect.Height - y - 27));
        Fill(table, _cSurface2);
        Border(table, _cBorder);
        Fill(new RectI(table.X, table.Y, table.Width, 25), _cSurface);
        DrawText("Name", table.X + 7, table.Y + 17, _cMuted);
        DrawText("Type", table.X + Math.Max(145, table.Width - 260), table.Y + 17, _cMuted);
        DrawText("Size", table.X + Math.Max(210, table.Width - 180), table.Y + 17, _cMuted);
        DrawText("Modified", table.X + Math.Max(275, table.Width - 100), table.Y + 17, _cMuted);
        DrawLine(table.X, table.Y + 25, table.X + table.Width, table.Y + 25, _cBorder);

        if (remote)
            DrawRemoteRows(new RectI(table.X, table.Y + 26, table.Width, table.Height - 26));
        else
            DrawLocalRows(new RectI(table.X, table.Y + 26, table.Width, table.Height - 26));

        var count = remote ? FilteredRemote().Count() : FilteredLocal().Count();
        DrawText($"{count} items", x, rect.Y + rect.Height - 8, _cMuted);
    }

    private void DrawLocalRows(RectI rect)
    {
        var items = FilteredLocal().ToArray();
        var rows = Math.Max(1, rect.Height / 24);
        _localScroll = Math.Clamp(_localScroll, 0, Math.Max(0, items.Length - rows));
        for (var row = 0; row < rows && _localScroll + row < items.Length; row++)
        {
            var index = _localScroll + row;
            var item = items[index];
            var rr = new RectI(rect.X + 1, rect.Y + row * 24, rect.Width - 2, 24);
            if (index == _localSelected) Fill(rr, _cAccentSoft);
            DrawText(Ellipsize((item.IsDirectory ? "▸ " : "  ") + item.Name, Math.Max(10, (rect.Width - 270) / 8)), rr.X + 6, rr.Y + 17, _cText);
            DrawText(item.IsDirectory ? "Folder" : "File", rect.X + Math.Max(145, rect.Width - 260), rr.Y + 17, _cMuted);
            DrawText(item.IsDirectory ? "" : FormatBytes(item.Size), rect.X + Math.Max(210, rect.Width - 180), rr.Y + 17, _cMuted);
            DrawText(item.Modified.LocalDateTime.ToString("MM-dd HH:mm"), rect.X + Math.Max(275, rect.Width - 100), rr.Y + 17, _cMuted);
            Register(rr, () => ActivateLocalRow(index, item));
        }
    }

    private void DrawRemoteRows(RectI rect)
    {
        var items = FilteredRemote().ToArray();
        var rows = Math.Max(1, rect.Height / 24);
        _remoteScroll = Math.Clamp(_remoteScroll, 0, Math.Max(0, items.Length - rows));
        for (var row = 0; row < rows && _remoteScroll + row < items.Length; row++)
        {
            var index = _remoteScroll + row;
            var item = items[index];
            var rr = new RectI(rect.X + 1, rect.Y + row * 24, rect.Width - 2, 24);
            if (index == _remoteSelected) Fill(rr, _cAccentSoft);
            DrawText(Ellipsize((item.IsDirectory ? "▸ " : "  ") + item.Name, Math.Max(10, (rect.Width - 270) / 8)), rr.X + 6, rr.Y + 17, _cText);
            DrawText(item.IsDirectory ? "Folder" : "File", rect.X + Math.Max(145, rect.Width - 260), rr.Y + 17, _cMuted);
            DrawText(item.IsDirectory ? "" : FormatBytes(item.Size), rect.X + Math.Max(210, rect.Width - 180), rr.Y + 17, _cMuted);
            DrawText(item.ModifiedUtc?.LocalDateTime.ToString("MM-dd HH:mm") ?? "", rect.X + Math.Max(275, rect.Width - 100), rr.Y + 17, _cMuted);
            Register(rr, () => ActivateRemoteRow(index, item));
        }
    }

    private void DrawTransfers(RectI rect)
    {
        DrawCard(rect);
        DrawText(L("Transfers"), rect.X + 10, rect.Y + 20, _cText);
        var jobs = _queue?.Jobs.ToArray() ?? [];
        var running = jobs.Count(x => x.State == TransferState.Running);
        var queued = jobs.Count(x => x.State == TransferState.Queued);
        var failed = jobs.Count(x => x.State == TransferState.Failed);
        DrawText($"{jobs.Length} total · {running} running · {queued} queued · {failed} failed", rect.X + 86, rect.Y + 20, _cMuted);

        DrawButton(new RectI(rect.X + rect.Width - 282, rect.Y + 6, 82, 26), "Cancel", CancelSelectedTransfer, enabled: jobs.Length > 0);
        DrawButton(new RectI(rect.X + rect.Width - 194, rect.Y + 6, 82, 26), "Cancel all", CancelAllTransfers, enabled: jobs.Length > 0);
        DrawButton(new RectI(rect.X + rect.Width - 106, rect.Y + 6, 94, 26), "Clear done", ClearFinishedTransfers, enabled: jobs.Length > 0);

        var table = new RectI(rect.X + 9, rect.Y + 36, rect.Width - 18, rect.Height - 45);
        Fill(table, _cSurface2);
        Border(table, _cBorder);
        Fill(new RectI(table.X, table.Y, table.Width, 24), _cSurface);
        DrawText("Item", table.X + 7, table.Y + 17, _cMuted);
        DrawText("Direction", table.X + Math.Max(180, table.Width / 3), table.Y + 17, _cMuted);
        DrawText("State", table.X + Math.Max(300, table.Width / 2), table.Y + 17, _cMuted);
        DrawText("Progress", table.X + Math.Max(420, table.Width * 2 / 3), table.Y + 17, _cMuted);
        DrawText("Speed", table.X + Math.Max(540, table.Width - 150), table.Y + 17, _cMuted);

        var rows = Math.Max(1, (table.Height - 25) / 23);
        _transferScroll = Math.Clamp(_transferScroll, 0, Math.Max(0, jobs.Length - rows));
        for (var row = 0; row < rows && _transferScroll + row < jobs.Length; row++)
        {
            var job = jobs[_transferScroll + row];
            var yy = table.Y + 25 + row * 23;
            DrawText(Ellipsize(Path.GetFileName(job.Source), Math.Max(10, table.Width / 28)), table.X + 7, yy + 16, _cText);
            DrawText(job.Direction.ToString(), table.X + Math.Max(180, table.Width / 3), yy + 16, _cMuted);
            DrawText(job.State.ToString(), table.X + Math.Max(300, table.Width / 2), yy + 16, job.State == TransferState.Failed ? _cDanger : _cMuted);
            DrawText(job.ProgressText, table.X + Math.Max(420, table.Width * 2 / 3), yy + 16, _cMuted);
            DrawText(job.SpeedText, table.X + Math.Max(540, table.Width - 150), yy + 16, _cMuted);
        }
    }

    private void DrawStatusBar()
    {
        var y = _height - StatusHeight;
        Fill(new RectI(SidebarWidth, y, _width - SidebarWidth, StatusHeight), _cSurface);
        DrawLine(SidebarWidth, y, _width, y, _cBorder);
        DrawText($"{GhostProduct.DisplayName} {GhostProduct.InformationalVersion} · No telemetry · No tracking", SidebarWidth + 10, y + 19, _cMuted);
        DrawText(_status, Math.Max(SidebarWidth + 10, _width - 190), y + 19, _connected ? _cSuccess : _cMuted);
    }

    private void DrawModal()
    {
        Fill(new RectI(0, 0, _width, _height), _cBg);
        var w = Math.Min(720, _width - 80);
        var h = _modalKind switch
        {
            ModalKind.SiteManager => Math.Min(560, _height - 80),
            ModalKind.Settings => 360,
            _ => 240
        };
        var rect = new RectI((_width - w) / 2, (_height - h) / 2, w, h);
        DrawCard(rect);

        if (_modalKind == ModalKind.SiteManager)
        {
            DrawSiteManagerModal(rect);
            return;
        }
        if (_modalKind == ModalKind.Settings)
        {
            DrawSettingsModal(rect);
            return;
        }

        DrawText(_modalTitle, rect.X + 20, rect.Y + 32, _cText);
        DrawText(Ellipsize(_modalText, Math.Max(30, (rect.Width - 40) / 8)), rect.X + 20, rect.Y + 58, _cMuted);

        if (_modalKind == ModalKind.Input)
        {
            var inputRect = new RectI(rect.X + 20, rect.Y + 82, rect.Width - 40, 34);
            Fill(inputRect, _cSurface2);
            Border(inputRect, _cAccent);
            DrawText(Ellipsize(_modalValue, Math.Max(10, inputRect.Width / 8)), inputRect.X + 8, inputRect.Y + 23, _cText);
        }

        DrawButton(new RectI(rect.X + rect.Width - 196, rect.Y + rect.Height - 48, 82, 32), L("Cancel"), CloseModal);
        DrawButton(new RectI(rect.X + rect.Width - 104, rect.Y + rect.Height - 48, 84, 32), _modalKind == ModalKind.Confirm ? L("Close") : L("Save"), AcceptModal, primary: _modalKind != ModalKind.Confirm);
    }

    private void DrawSiteManagerModal(RectI rect)
    {
        DrawText("Site Manager", rect.X + 20, rect.Y + 32, _cText);
        DrawText("Saved locally · passwords protected with a user-private AES key on Linux", rect.X + 120, rect.Y + 32, _cMuted);
        var list = new RectI(rect.X + 20, rect.Y + 54, rect.Width - 40, rect.Height - 130);
        Fill(list, _cSurface2);
        Border(list, _cBorder);
        var y = list.Y + 6;
        for (var i = 0; i < _profiles.Count && y + 30 < list.Y + list.Height; i++)
        {
            var row = new RectI(list.X + 4, y, list.Width - 8, 30);
            if (i == _siteSelected) Fill(row, _cAccentSoft);
            var profile = _profiles[i];
            DrawText(profile.Name, row.X + 8, row.Y + 20, _cText);
            DrawText(profile.IsDemo ? "Local demo" : $"{profile.Host}:{profile.Port} · {profile.Security}", row.X + 220, row.Y + 20, _cMuted);
            var captured = i;
            Register(row, () =>
            {
                _siteSelected = captured;
                LoadProfileIntoFields(_profiles[captured]);
                RequestRedraw();
            });
            y += 32;
        }

        var by = rect.Y + rect.Height - 56;
        DrawButton(new RectI(rect.X + 20, by, 104, 32), "+ Save current", SaveCurrentSite);
        DrawButton(new RectI(rect.X + 132, by, 82, 32), L("Delete"), DeleteSelectedSite, enabled: _profiles.ElementAtOrDefault(_siteSelected)?.IsDemo == false, danger: true);
        DrawButton(new RectI(rect.X + rect.Width - 204, by, 82, 32), L("Close"), CloseModal);
        DrawButton(new RectI(rect.X + rect.Width - 112, by, 92, 32), L("Connect"), () =>
        {
            CloseModal();
            _ = RunBackground(ConnectCoreAsync);
        }, primary: true);
    }

    private void DrawSettingsModal(RectI rect)
    {
        DrawText(L("Settings"), rect.X + 20, rect.Y + 32, _cText);
        DrawText("Linux desktop settings are stored locally only.", rect.X + 110, rect.Y + 32, _cMuted);

        var language = GhostLocalization.SupportedLanguages.ElementAtOrDefault(_languageIndex) ?? GhostLocalization.SupportedLanguages[0];
        DrawText(L("Language"), rect.X + 20, rect.Y + 82, _cMuted);
        DrawButton(new RectI(rect.X + 160, rect.Y + 60, 44, 32), "‹", PreviousLanguage);
        DrawButton(new RectI(rect.X + 210, rect.Y + 60, rect.Width - 280, 32), language.ToString(), NextLanguage);
        DrawButton(new RectI(rect.X + rect.Width - 64, rect.Y + 60, 44, 32), "›", NextLanguage);

        DrawText(L("Appearance"), rect.X + 20, rect.Y + 132, _cMuted);
        DrawButton(new RectI(rect.X + 160, rect.Y + 110, 130, 32), _settings.Theme.ToString(), CycleTheme);

        DrawText("Concurrent transfers", rect.X + 20, rect.Y + 182, _cMuted);
        DrawButton(new RectI(rect.X + 160, rect.Y + 160, 44, 32), "−", () => { _settings.ConcurrentTransfers = Math.Max(1, _settings.ConcurrentTransfers - 1); RequestRedraw(); });
        DrawText(_settings.ConcurrentTransfers.ToString(), rect.X + 222, rect.Y + 182, _cText);
        DrawButton(new RectI(rect.X + 254, rect.Y + 160, 44, 32), "+", () => { _settings.ConcurrentTransfers = Math.Min(8, _settings.ConcurrentTransfers + 1); RequestRedraw(); });

        DrawText("Automatic retries", rect.X + 20, rect.Y + 232, _cMuted);
        DrawButton(new RectI(rect.X + 160, rect.Y + 210, 44, 32), "−", () => { _settings.AutomaticTransferRetries = Math.Max(0, _settings.AutomaticTransferRetries - 1); RequestRedraw(); });
        DrawText(_settings.AutomaticTransferRetries.ToString(), rect.X + 222, rect.Y + 232, _cText);
        DrawButton(new RectI(rect.X + 254, rect.Y + 210, 44, 32), "+", () => { _settings.AutomaticTransferRetries = Math.Min(5, _settings.AutomaticTransferRetries + 1); RequestRedraw(); });

        var by = rect.Y + rect.Height - 52;
        DrawButton(new RectI(rect.X + rect.Width - 204, by, 82, 32), L("Cancel"), CloseModal);
        DrawButton(new RectI(rect.X + rect.Width - 112, by, 92, 32), L("Save"), SaveSettings, primary: true);
    }

    private RectI DrawButton(RectI rect, string text, Action action, bool enabled = true, bool primary = false, bool danger = false)
    {
        var fill = !enabled ? _cSurface2 : primary ? _cAccent : danger ? _cDanger : _cSurface2;
        Fill(rect, fill);
        Border(rect, enabled ? (primary || danger ? fill : _cBorder) : _cBorder);
        DrawText(Ellipsize(text, Math.Max(3, (rect.Width - 12) / 8)), rect.X + 7, rect.Y + rect.Height / 2 + 5, enabled ? _cText : _cMuted);
        if (enabled) Register(rect, action);
        return rect;
    }

    private void DrawLabeledField(string id, string label, RectI rect)
    {
        DrawText(label, rect.X, rect.Y - 2, _cMuted);
        var field = new RectI(rect.X, rect.Y + 4, rect.Width, rect.Height - 4);
        DrawField(id, field);
    }

    private void DrawField(string id, RectI rect)
    {
        var field = _fields[id];
        field.Bounds = rect;
        Fill(rect, _cSurface2);
        Border(rect, string.Equals(_focusedFieldId, id, StringComparison.Ordinal) ? _cAccent : _cBorder);
        var shown = field.Secret && field.Value.Length > 0 ? new string('•', Math.Min(field.Value.Length, 32)) : field.Value;
        DrawText(Ellipsize(shown, Math.Max(5, (rect.Width - 14) / 8)), rect.X + 7, rect.Y + rect.Height / 2 + 5, _cText);
        Register(rect, () =>
        {
            _focusedFieldId = id;
            RequestRedraw();
        });
    }

    private void DrawSecurityField(RectI rect)
    {
        DrawText(L("Security"), rect.X, rect.Y - 2, _cMuted);
        var box = new RectI(rect.X, rect.Y + 4, rect.Width, rect.Height - 4);
        DrawButton(box, SecurityLabel(), CycleSecurity);
    }

    private void DrawCard(RectI rect)
    {
        Fill(rect, _cSurface);
        Border(rect, _cBorder);
    }

    private void Fill(RectI rect, nuint color)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        SetColor(color);
        X11Native.XFillRectangle(_display, _window, _gc, rect.X, rect.Y, (uint)rect.Width, (uint)rect.Height);
    }

    private void Border(RectI rect, nuint color)
    {
        if (rect.Width <= 1 || rect.Height <= 1) return;
        SetColor(color);
        X11Native.XDrawRectangle(_display, _window, _gc, rect.X, rect.Y, (uint)(rect.Width - 1), (uint)(rect.Height - 1));
    }

    private void DrawLine(int x1, int y1, int x2, int y2, nuint color)
    {
        SetColor(color);
        X11Native.XDrawLine(_display, _window, _gc, x1, y1, x2, y2);
    }

    private void DrawText(string text, int x, int baselineY, nuint color)
    {
        if (string.IsNullOrEmpty(text)) return;
        SetColor(color);
        var bytes = Encoding.UTF8.GetBytes(text);
        X11Native.Xutf8DrawString(_display, _window, _fontSet, _gc, x, baselineY, bytes, bytes.Length);
    }

    private void SetColor(nuint color) => X11Native.XSetForeground(_display, _gc, color);

    private void Register(RectI rect, Action action) => _hitRegions.Add(new HitRegion(rect, action));

    private static string Ellipsize(string value, int maxChars)
    {
        if (maxChars <= 1) return string.Empty;
        if (value.Length <= maxChars) return value;
        return value[..Math.Max(1, maxChars - 1)] + "…";
    }

    private static string L(string key) => GhostLocalization.T(key);
}
