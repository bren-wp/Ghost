using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;
using GhostFTP.Design;
using GhostFTP.Services;
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
    }

    private readonly AppPaths _paths = new();
    private readonly DpapiSecretProtector _secrets = new();
    private readonly ObservableCollection<ServerProfile> _profiles = [];
    private readonly ObservableCollection<LocalItem> _localItems = [];
    private readonly ObservableCollection<RemoteItem> _remoteItems = [];
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

    private readonly ListBox _profilesList = new();
    private readonly TextBox _host = GhostTheme.TextBox();
    private readonly TextBox _port = GhostTheme.TextBox("21");
    private readonly TextBox _username = GhostTheme.TextBox();
    private readonly PasswordBox _password = GhostTheme.PasswordBox();
    private readonly ComboBox _security = new GhostComboBox();
    private readonly Button _connectButton = GhostTheme.Button("Connect", primary: true);
    private readonly Button _disconnectButton = GhostTheme.Button("Disconnect", danger: true);
    private readonly TextBox _localPathBox = GhostTheme.TextBox();
    private readonly TextBox _remotePathBox = GhostTheme.TextBox("/");
    private readonly TextBox _localFilter = GhostTheme.TextBox();
    private readonly TextBox _remoteFilter = GhostTheme.TextBox();
    private readonly ListView _localList = new();
    private readonly ListView _remoteList = new();
    private readonly ListView _queueList = new();
    private readonly Border _statusBadge = new();
    private readonly TextBlock _statusText = GhostTheme.Text("Offline", 11, muted: true, weight: FontWeights.SemiBold);
    private readonly TextBlock _queueSummary = GhostTheme.Text("No transfers", 11.5, muted: true);
    private readonly TextBlock _localSummary = GhostTheme.Text("0 items", 11, muted: true);
    private readonly TextBlock _remoteSummary = GhostTheme.Text("0 items", 11, muted: true);

    private List<LocalItem> _localAll = [];
    private List<RemoteItem> _remoteAll = [];

    public MainWindow()
    {
        Title = GhostBrand.DisplayName;
        Icon = GhostBrand.IconSource;
        Width = 1520;
        Height = 920;
        MinWidth = 1180;
        MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = GhostTheme.R("Bg");
        Foreground = GhostTheme.R("Text");
        FontFamily = GhostTheme.UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _security.ItemsSource = new[] { "FTP", "FTPS Explicit", "FTPS Implicit" };
        _security.SelectedIndex = 1;

        Content = BuildLayout();
        ConfigureLists();
        ConfigureEvents();

        SourceInitialized += (_, _) => GhostWindowChrome.Apply(this, GhostTheme.IsDark);
        Loaded += OnLoadedAsync;
        Closing += OnClosingAsync;
    }
}
