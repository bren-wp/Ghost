using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;
using GhostFTP.Design;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostFTP.UI;

public sealed partial class MainWindow : Window
{
    private sealed class LocalItem
    {
        public required string Name { get; init; }
        public required string FullPath { get; init; }
        public required bool IsDirectory { get; init; }
        public required long Size { get; init; }
        public required DateTimeOffset Modified { get; init; }
        public string Type => IsDirectory ? "Folder" : "File";
        public string SizeText => IsDirectory ? string.Empty : FormatBytes(Size);
        public string ModifiedText => Modified.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }

    private sealed class RemoteItem
    {
        public required FtpEntry Entry { get; init; }
        public string Name => Entry.Name;
        public string FullPath => Entry.FullPath;
        public bool IsDirectory => Entry.IsDirectory;
        public string Type => Entry.Type;
        public string SizeText => Entry.IsDirectory ? string.Empty : FormatBytes(Entry.Size);
        public string ModifiedText => Entry.ModifiedUtc?.LocalDateTime.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
        public string Permissions => Entry.Permissions ?? string.Empty;
    }

    private readonly AppPaths _paths = new();
    private readonly GhostFTP.Services.DpapiSecretProtector _secrets = new();
    private readonly ObservableCollection<ServerProfile> _profiles = [];
    private readonly ObservableCollection<LocalItem> _localItems = [];
    private readonly ObservableCollection<RemoteItem> _remoteItems = [];
    private readonly ObservableCollection<string> _connectionLog = [];
    private readonly HashSet<Guid> _completedHandled = [];

    private ProfileStore? _profileStore;
    private AppSettingsStore? _settingsStore;
    private AppSettings _settings = new();
    private TransferQueueService? _queue;
    private IFtpSession? _session;
    private FtpConnectionOptions? _activeOptions;
    private CancellationTokenSource? _connectionCts;
    private string _localPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private string _remotePath = "/";
    private bool _busy;
    private bool _allowClose;
    private readonly string? _captureDirectory;

    private readonly ComboBox _profilesList = new GhostComboBox();
    private readonly ListBox _connectionLogList = new();
    private readonly TextBox _host = GhostTheme.TextBox();
    private readonly TextBox _port = GhostTheme.TextBox("21");
    private readonly TextBox _username = GhostTheme.TextBox();
    private readonly PasswordBox _password = GhostTheme.PasswordBox();
    private readonly ComboBox _security = new GhostComboBox();
    private readonly Button _connectButton = GhostTheme.Button(GhostLocalization.T("Connect"), primary: true);
    private readonly Button _disconnectButton = GhostTheme.Button(GhostLocalization.T("Disconnect"), danger: true);
    private readonly Button _queuePauseButton = GhostTheme.Button(GhostTransferText.T("PauseQueue"));
    private readonly TextBox _localPathBox = GhostTheme.TextBox();
    private readonly TextBox _remotePathBox = GhostTheme.TextBox("/");
    private readonly TextBox _localFilter = GhostTheme.TextBox();
    private readonly TextBox _remoteFilter = GhostTheme.TextBox();
    private readonly ListView _localList = new();
    private readonly ListView _remoteList = new();
    private readonly ListView _queueList = new();
    private readonly Border _statusBadge = new();
    private readonly TextBlock _statusText = GhostTheme.Text(GhostLocalization.T("Offline"), 11, muted: true, weight: FontWeights.SemiBold);
    private readonly TextBlock _queueSummary = GhostTheme.Text(GhostLocalization.T("NoTransfers"), 11.5, muted: true);
    private readonly TextBlock _localSummary = GhostTheme.Text("0 items", 11, muted: true);
    private readonly TextBlock _remoteSummary = GhostTheme.Text("0 items", 11, muted: true);

    private Grid? _workspaceContent;
    private Grid? _filePanesGrid;

    private List<LocalItem> _localAll = [];
    private List<RemoteItem> _remoteAll = [];

    private static string L(string key) => GhostLocalization.T(key);

    public MainWindow(string? captureDirectory = null)
    {
        _captureDirectory = string.IsNullOrWhiteSpace(captureDirectory) ? null : Path.GetFullPath(captureDirectory);

        Title = $"{GhostBrand.DisplayName} · {GhostBrand.ReleaseChannelDisplay}";
        Icon = GhostBrand.IconSource;
        Width = 1914;
        Height = 907;
        MinWidth = 1050;
        MinHeight = 680;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _security.ItemsSource = new[] { "FTP", "FTPS Explicit", "FTPS Implicit" };
        _security.SelectedIndex = 1;
        _profilesList.DisplayMemberPath = nameof(ServerProfile.Name);
        _profilesList.ItemsSource = _profiles;
        _profilesList.MinWidth = 210;
        _profilesList.MaxWidth = 340;
        _port.MaxLength = 5;
        _host.MaxLength = 253;
        _username.MaxLength = 512;
        _localPathBox.MaxLength = 32767;
        _remotePathBox.MaxLength = 4096;
        _localFilter.MaxLength = 512;
        _remoteFilter.MaxLength = 512;

        Content = BuildReferenceShell(BuildLayout());
        ConfigureWorkspaceResizing();
        ConfigureLists();
        ConfigureEvents();
        ConfigureQueueUx();
        ConfigureResponsiveColumns();
        ConfigureKeepAliveLoop();

        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);
        Loaded += OnLoadedAsync;
        Closing += OnClosingAsync;
    }
}
