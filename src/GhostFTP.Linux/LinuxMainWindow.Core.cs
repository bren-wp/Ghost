using System.Collections.Concurrent;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;
using GhostFTP.Design;

namespace GhostFTP.Linux;

internal sealed partial class LinuxMainWindow : IDisposable
{
    private const int MinimumWindowWidth = 980;
    private const int MinimumWindowHeight = 680;

    private sealed record LocalEntry(string Name, string FullPath, bool IsDirectory, long Size, DateTimeOffset Modified);
    private sealed record HitRegion(RectI Bounds, Action Action, string? ToolTip = null);
    private sealed class TextField
    {
        internal required string Id { get; init; }
        internal string Value { get; set; } = string.Empty;
        internal bool Secret { get; init; }
        internal int MaxLength { get; init; } = 4096;
        internal RectI Bounds { get; set; }
    }

    private readonly record struct RectI(int X, int Y, int Width, int Height)
    {
        internal bool Contains(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
    }

    private enum ModalKind
    {
        None,
        Input,
        Confirm,
        SiteManager,
        Settings
    }

    private readonly AppPaths _paths = new();
    private readonly AppSettingsStore _settingsStore;
    private readonly ProfileStore _profileStore;
    private readonly AesFileSecretProtector _secretProtector;
    private readonly List<ServerProfile> _profiles = [];
    private readonly List<LocalEntry> _localItems = [];
    private readonly List<FtpEntry> _remoteItems = [];
    private readonly List<string> _connectionLog = [];
    private readonly ConcurrentQueue<Action> _posted = new();
    private readonly List<HitRegion> _hitRegions = [];
    private readonly Dictionary<string, TextField> _fields = new(StringComparer.Ordinal);

    private AppSettings _settings = new();
    private IFtpSession? _session;
    private TransferQueueService? _queue;
    private FtpConnectionOptions? _activeOptions;
    private CancellationTokenSource? _connectionCts;

    private string _localPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private string _remotePath = "/";
    private int _localSelected = -1;
    private int _remoteSelected = -1;
    private int _localScroll;
    private int _remoteScroll;
    private int _transferScroll;
    private int _siteSelected;
    private int _languageIndex;
    private bool _busy;
    private bool _connected;
    private bool _closing;
    private bool _needsRedraw = true;
    private bool _plainFtpApproved;
    private string _status = "Offline";
    private string? _focusedFieldId;
    private DateTimeOffset _lastClickUtc;
    private string? _lastClickTarget;

    private ModalKind _modalKind;
    private string _modalTitle = string.Empty;
    private string _modalText = string.Empty;
    private string _modalValue = string.Empty;
    private Action<string?>? _modalCallback;

    private IntPtr _display;
    private int _screen;
    private nuint _window;
    private IntPtr _gc;
    private IntPtr _fontSet;
    private nuint _wmDelete;
    private nuint _colormap;
    private int _width = 1500;
    private int _height = 900;

    private nuint _cBg;
    private nuint _cSurface;
    private nuint _cSurface2;
    private nuint _cBorder;
    private nuint _cText;
    private nuint _cMuted;
    private nuint _cAccent;
    private nuint _cAccentSoft;
    private nuint _cDanger;
    private nuint _cSuccess;

    internal LinuxMainWindow(IReadOnlyList<string> args)
    {
        _settingsStore = new AppSettingsStore(_paths.SettingsFile);
        _secretProtector = new AesFileSecretProtector(Path.Combine(_paths.DataDirectory, "credential.key"));
        _profileStore = new ProfileStore(_paths.ProfilesFile, _secretProtector);

        _settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _width = (int)Math.Round(Math.Clamp(_settings.WindowWidth, MinimumWindowWidth, 7680));
        _height = (int)Math.Round(Math.Clamp(_settings.WindowHeight, MinimumWindowHeight, 4320));

        var requestedLanguage = ParseArgument(args, "--lang") ?? _settings.LanguageCode;
        GhostLocalization.SetLanguage(GhostLocalization.NormalizeLanguageCode(requestedLanguage));
        _settings.LanguageCode = GhostLocalization.CurrentLanguageCode;
        _languageIndex = Math.Max(0, GhostLocalization.SupportedLanguages.ToList().FindIndex(x => x.Code == GhostLocalization.CurrentLanguageCode));

        var profiles = _profileStore.LoadAsync().GetAwaiter().GetResult();
        _profiles.AddRange(profiles);
        _siteSelected = Math.Max(0, _profiles.FindIndex(x => !x.IsDemo));

        _localPath = Directory.Exists(_settings.LastLocalDirectory)
            ? _settings.LastLocalDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        CreateFields();
        LoadProfileIntoFields(_profiles[_siteSelected]);
        ReloadLocal();
        InitializeX11();
        Log($"{GhostProduct.DisplayName} {GhostProduct.InformationalVersion} started on Linux.");
        Log("No telemetry · no tracking · profiles and settings remain local.");
    }

    internal void Run()
    {
        while (!_closing)
        {
            DrainPosted();
            while (X11Native.XPending(_display) > 0)
            {
                X11Native.XNextEvent(_display, out var xevent);
                HandleEvent(xevent);
            }

            if (_needsRedraw)
            {
                Draw();
                _needsRedraw = false;
            }

            Thread.Sleep(8);
        }
    }

    public void Dispose()
    {
        _closing = true;
        try { _connectionCts?.Cancel(); } catch { }
        try { _queue?.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
        try { _session?.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
        _connectionCts?.Dispose();

        try
        {
            _settings.LastLocalDirectory = _localPath;
            _settings.WindowWidth = Math.Max(MinimumWindowWidth, _width);
            _settings.WindowHeight = Math.Max(MinimumWindowHeight, _height);
            _settingsStore.SaveAsync(_settings).GetAwaiter().GetResult();
        }
        catch
        {
        }

        if (_display != IntPtr.Zero)
        {
            if (_fontSet != IntPtr.Zero) X11Native.XFreeFontSet(_display, _fontSet);
            if (_gc != IntPtr.Zero) X11Native.XFreeGC(_display, _gc);
            if (_window != 0) X11Native.XDestroyWindow(_display, _window);
            X11Native.XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
    }

    private static string? ParseArgument(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }

    private void CreateFields()
    {
        _fields["host"] = new TextField { Id = "host", MaxLength = 253 };
        _fields["port"] = new TextField { Id = "port", Value = "21", MaxLength = 5 };
        _fields["username"] = new TextField { Id = "username", MaxLength = 512 };
        _fields["password"] = new TextField { Id = "password", Secret = true, MaxLength = 4096 };
        _fields["localPath"] = new TextField { Id = "localPath", MaxLength = 32767 };
        _fields["remotePath"] = new TextField { Id = "remotePath", Value = "/", MaxLength = 4096 };
        _fields["localFilter"] = new TextField { Id = "localFilter", MaxLength = 512 };
        _fields["remoteFilter"] = new TextField { Id = "remoteFilter", MaxLength = 512 };
    }

    private void InitializeX11()
    {
        _ = X11Native.XInitThreads();
        _display = X11Native.XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException("No X11/XWayland display is available. Check the DISPLAY environment variable.");

        _screen = X11Native.XDefaultScreen(_display);
        var root = X11Native.XRootWindow(_display, _screen);
        _colormap = X11Native.XDefaultColormap(_display, _screen);
        _window = X11Native.XCreateSimpleWindow(
            _display,
            root,
            30,
            30,
            (uint)_width,
            (uint)_height,
            0,
            X11Native.XBlackPixel(_display, _screen),
            X11Native.XBlackPixel(_display, _screen));

        if (_window == 0)
            throw new InvalidOperationException("X11 could not create the Ghost FTP window.");

        X11Native.SetMinimumWindowSize(_display, _window, MinimumWindowWidth, MinimumWindowHeight);
        X11Native.XStoreName(_display, _window, $"{GhostProduct.DisplayName} · {GhostProduct.ReleaseChannelDisplay}");
        X11Native.XSelectInput(
            _display,
            _window,
            X11Native.KeyPressMask |
            X11Native.ButtonPressMask |
            X11Native.ButtonReleaseMask |
            X11Native.PointerMotionMask |
            X11Native.ExposureMask |
            X11Native.StructureNotifyMask);

        _wmDelete = X11Native.XInternAtom(_display, "WM_DELETE_WINDOW", 0);
        var protocol = _wmDelete;
        X11Native.XSetWMProtocols(_display, _window, ref protocol, 1);

        _gc = X11Native.XCreateGC(_display, _window, 0, IntPtr.Zero);
        if (_gc == IntPtr.Zero)
            throw new InvalidOperationException("X11 could not create the Ghost FTP graphics context.");

        _fontSet = X11Native.XCreateFontSet(
            _display,
            "-*-sans-medium-r-normal--14-*-*-*-*-*-*-*,-*-fixed-medium-r-normal--14-*-*-*-*-*-*-*",
            out var missing,
            out _,
            out _);
        if (missing != IntPtr.Zero)
            X11Native.XFreeStringList(missing);
        if (_fontSet == IntPtr.Zero)
            throw new InvalidOperationException("X11 could not create a UTF-8 font set for Ghost FTP.");

        ApplyPalette();
        X11Native.XMapWindow(_display, _window);
        X11Native.XFlush(_display);
    }

    private void ApplyPalette()
    {
        var light = _settings.Theme == AppTheme.Light;
        _cBg = Color(light ? "#F4F6FA" : "#0B1018");
        _cSurface = Color(light ? "#FFFFFF" : "#101722");
        _cSurface2 = Color(light ? "#F3F6FB" : "#151E2B");
        _cBorder = Color(light ? "#CDD6E4" : "#29364B");
        _cText = Color(light ? "#111827" : "#EEF4FF");
        _cMuted = Color(light ? "#66758C" : "#91A4C0");
        _cAccent = Color("#745CFF");
        _cAccentSoft = Color(light ? "#E9E5FF" : "#2B245C");
        _cDanger = Color("#EF5265");
        _cSuccess = Color("#2FCB8C");

        // The reference renderer normally installs its canonical dark menu/toolbar palette on
        // first draw. A user-selected Light theme must keep the light colors produced here.
        if (light)
        {
            _cMenu = _cSurface;
            _cToolbar = _cSurface2;
            _referencePaletteApplied = true;
        }
        else
        {
            _referencePaletteApplied = false;
        }
    }

    private nuint Color(string hex)
    {
        var color = new X11Native.XColor();
        if (X11Native.XParseColor(_display, _colormap, hex, ref color) == 0 || X11Native.XAllocColor(_display, _colormap, ref color) == 0)
            return X11Native.XWhitePixel(_display, _screen);
        return color.pixel;
    }

    private void LoadProfileIntoFields(ServerProfile profile)
    {
        _fields["host"].Value = profile.Host;
        _fields["port"].Value = profile.Port.ToString();
        _fields["username"].Value = profile.Username;
        _fields["password"].Value = _profileStore.GetPassword(profile);
        _securityMode = profile.Security;
        _remotePath = string.IsNullOrWhiteSpace(profile.InitialPath) ? "/" : profile.InitialPath;
        _fields["remotePath"].Value = _remotePath;
    }

    private void ReloadLocal()
    {
        _localItems.Clear();
        try
        {
            if (!Directory.Exists(_localPath))
                _localPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            foreach (var directory in new DirectoryInfo(_localPath).EnumerateDirectories())
            {
                if (!_settings.ShowHiddenFiles && directory.Name.StartsWith(".", StringComparison.Ordinal)) continue;
                _localItems.Add(new LocalEntry(directory.Name, directory.FullName, true, 0, directory.LastWriteTimeUtc));
            }
            foreach (var file in new DirectoryInfo(_localPath).EnumerateFiles())
            {
                if (!_settings.ShowHiddenFiles && file.Name.StartsWith(".", StringComparison.Ordinal)) continue;
                _localItems.Add(new LocalEntry(file.Name, file.FullName, false, file.Length, file.LastWriteTimeUtc));
            }

            _localItems.Sort(static (a, b) =>
            {
                var directoryOrder = b.IsDirectory.CompareTo(a.IsDirectory);
                return directoryOrder != 0 ? directoryOrder : StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            });
            _fields["localPath"].Value = _localPath;
            _localSelected = Math.Clamp(_localSelected, -1, _localItems.Count - 1);
        }
        catch (Exception ex)
        {
            Log("Local refresh failed: " + ex.Message);
        }
        RequestRedraw();
    }

    private IEnumerable<LocalEntry> FilteredLocal() =>
        string.IsNullOrWhiteSpace(_fields["localFilter"].Value)
            ? _localItems
            : _localItems.Where(x => x.Name.Contains(_fields["localFilter"].Value, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<FtpEntry> FilteredRemote() =>
        string.IsNullOrWhiteSpace(_fields["remoteFilter"].Value)
            ? _remoteItems
            : _remoteItems.Where(x => x.Name.Contains(_fields["remoteFilter"].Value, StringComparison.OrdinalIgnoreCase));

    private async Task ConnectCoreAsync()
    {
        if (_busy) return;

        var selected = _profiles.ElementAtOrDefault(_siteSelected);
        var isDemo = selected?.IsDemo == true
            && string.Equals(_fields["host"].Value, "demo.ghostftp.local", StringComparison.OrdinalIgnoreCase);
        if (_securityMode == FtpSecurityMode.Plain && !isDemo && !_plainFtpApproved)
        {
            Post(() => ShowConfirm(
                "Plain FTP is not encrypted",
                "Plain FTP sends usernames, passwords and file data without TLS encryption. Continue only for an intentionally trusted server or isolated network.",
                () =>
                {
                    _plainFtpApproved = true;
                    _ = RunBackground(ConnectCoreAsync);
                }));
            return;
        }

        _plainFtpApproved = false;
        _busy = true;
        Post(() =>
        {
            _status = "Connecting…";
            Log($"Connecting to {_fields["host"].Value}:{_fields["port"].Value}…");
        });

        IFtpSession? candidateSession = null;
        try
        {
            await DisconnectCoreAsync(silent: true).ConfigureAwait(false);
            FtpConnectionOptions? newOptions = null;
            if (isDemo)
            {
                candidateSession = new DemoFtpSession();
            }
            else
            {
                var defaultPort = _securityMode == FtpSecurityMode.ImplicitTls ? 990 : 21;
                if (!int.TryParse(_fields["port"].Value, out var port) || port is < 1 or > 65535)
                    port = defaultPort;

                newOptions = new FtpConnectionOptions
                {
                    Host = _fields["host"].Value,
                    Port = port,
                    Username = _fields["username"].Value,
                    Password = _fields["password"].Value,
                    Security = _securityMode,
                    ConnectTimeout = TimeSpan.FromSeconds(_settings.ConnectTimeoutSeconds),
                    CommandTimeout = TimeSpan.FromSeconds(_settings.CommandTimeoutSeconds),
                    TransferTimeout = TimeSpan.FromSeconds(_settings.TransferIdleTimeoutSeconds)
                };
                candidateSession = new FtpSession(newOptions);
            }

            _connectionCts = new CancellationTokenSource();
            await candidateSession.ConnectAsync(_connectionCts.Token).ConfigureAwait(false);
            _session = candidateSession;
            candidateSession = null;
            _activeOptions = newOptions;
            _connected = true;
            _remotePath = string.IsNullOrWhiteSpace(_fields["remotePath"].Value) ? "/" : _fields["remotePath"].Value;
            try
            {
                await _session.ChangeDirectoryAsync(_remotePath, _connectionCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_connectionCts.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                _remotePath = _session.WorkingDirectory;
            }
            await RefreshRemoteCoreAsync().ConfigureAwait(false);
            RecreateTransferQueue();
            EnsureKeepAliveLoopStarted();

            Post(() =>
            {
                KeepQuickConnectionInTabIfRequested();
                _status = _session.IsEncrypted ? "Connected · TLS" : isDemo ? "Demo · local" : "Connected · FTP";
                Log(_session.IsEncrypted
                    ? "Connected with encrypted FTP control/data channels."
                    : isDemo
                        ? "Built-in Demo session opened locally; no network connection is used."
                        : "Connected using plain FTP.");
            });
        }
        catch (OperationCanceledException)
        {
            Post(() =>
            {
                _connected = false;
                _status = "Offline";
                Log("Connection attempt cancelled.");
            });
        }
        catch (Exception ex)
        {
            Post(() =>
            {
                _connected = false;
                _status = "Offline";
                Log("Connection failed: " + ex.Message);
            });
        }
        finally
        {
            if (candidateSession is not null)
            {
                try { await candidateSession.DisposeAsync().ConfigureAwait(false); } catch { }
            }

            if (!_connected)
            {
                if (_session is not null)
                {
                    try { await _session.DisposeAsync().ConfigureAwait(false); } catch { }
                    _session = null;
                }
                _activeOptions = null;
            }

            _busy = false;
            RequestRedraw();
        }
    }

    private async Task DisconnectCoreAsync(bool silent = false)
    {
        _connectionCts?.Cancel();
        _connectionCts?.Dispose();
        _connectionCts = null;

        if (_queue is not null)
        {
            try { await _queue.DisposeAsync().ConfigureAwait(false); } catch { }
            _queue = null;
        }

        if (_session is not null)
        {
            try { await _session.DisconnectAsync().ConfigureAwait(false); } catch { }
            try { await _session.DisposeAsync().ConfigureAwait(false); } catch { }
            _session = null;
        }

        _activeOptions = null;
        _connected = false;
        _remoteItems.Clear();
        if (!silent)
        {
            Post(() =>
            {
                _status = "Offline";
                Log("Disconnected.");
            });
        }
    }

    private void RecreateTransferQueue()
    {
        if (!_connected || _session is null) return;
        _queue = new TransferQueueService(CreateTransferSessionAsync, null, _settings.AutomaticTransferRetries, _settings.ConcurrentTransfers);
        _queue.JobUpdated += (_, job) =>
        {
            Post(() =>
            {
                if (job.State == TransferState.Completed)
                {
                    ReloadLocal();
                    _ = RunBackground(RefreshRemoteCoreAsync);
                }
                RequestRedraw();
            });
        };
    }

    private async Task<(IFtpSession Session, bool DisposeAfter)> CreateTransferSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is DemoFtpSession demo && demo.IsConnected)
            return (demo, false);
        if (_activeOptions is null || !_connected)
            throw new InvalidOperationException("No active FTP connection options are available.");

        var session = new FtpSession(_activeOptions);
        try
        {
            await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
            return (session, true);
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task RefreshRemoteCoreAsync()
    {
        var session = _session;
        if (session is null || !session.IsConnected) return;
        var items = await session.ListAsync(_remotePath, _connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
        Post(() =>
        {
            if (!ReferenceEquals(_session, session))
                return;
            _remoteItems.Clear();
            _remoteItems.AddRange(items.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
            _remotePath = session.WorkingDirectory;
            _fields["remotePath"].Value = _remotePath;
            _remoteSelected = Math.Clamp(_remoteSelected, -1, _remoteItems.Count - 1);
            Log($"Remote directory loaded: {_remotePath} ({_remoteItems.Count} items).");
        });
    }

    private async Task NavigateRemoteAsync(string path)
    {
        var session = _session;
        if (session is null || !session.IsConnected) return;
        await session.ChangeDirectoryAsync(path, _connectionCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
        _remotePath = session.WorkingDirectory;
        await RefreshRemoteCoreAsync().ConfigureAwait(false);
    }

    private void NavigateLocal(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            _localPath = path;
            _localScroll = 0;
            _localSelected = -1;
            ReloadLocal();
        }
        catch (Exception ex)
        {
            Log("Local navigation failed: " + ex.Message);
        }
    }

    private void QueueUploadSelected()
    {
        var item = FilteredLocal().ElementAtOrDefault(_localSelected);
        if (item is null || _queue is null || !_connected) return;
        _queue.EnqueueUpload(item.FullPath, FtpListingParser.CombineRemote(_remotePath, item.Name), item.IsDirectory);
        Log("Queued upload: " + item.Name);
        RequestRedraw();
    }

    private void QueueDownloadSelected()
    {
        var item = FilteredRemote().ElementAtOrDefault(_remoteSelected);
        if (item is null || _queue is null || !_connected) return;
        var destination = LocalPathSafety.CombineUnderRoot(_localPath, item.Name);
        _queue.EnqueueDownload(item.FullPath, destination, item.IsDirectory, item.IsDirectory ? null : item.Size);
        Log("Queued download: " + item.Name);
        RequestRedraw();
    }

    private void Log(string message)
    {
        _connectionLog.Add($"[{DateTimeOffset.Now:HH:mm:ss}] {message}");
        while (_connectionLog.Count > 200) _connectionLog.RemoveAt(0);
        RequestRedraw();
    }

    private void Post(Action action)
    {
        _posted.Enqueue(action);
        RequestRedraw();
    }

    private void DrainPosted()
    {
        while (_posted.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Log("UI action failed: " + ex.Message); }
        }
    }

    private Task RunBackground(Func<Task> work)
    {
        return Task.Run(async () =>
        {
            try { await work().ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Post(() => Log(ex.Message)); }
        });
    }

    private void RequestRedraw() => _needsRedraw = true;

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024d).ToString("0.#") + " KB";
        if (bytes < 1024L * 1024 * 1024) return (bytes / (1024d * 1024)).ToString("0.#") + " MB";
        return (bytes / (1024d * 1024 * 1024)).ToString("0.##") + " GB";
    }
}
