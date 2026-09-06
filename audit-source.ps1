$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'

function Require-File([string]$relative) {
    $path = Join-Path $root $relative
    if (!(Test-Path $path -PathType Leaf)) {
        throw "Required Ghost FTP source/asset is missing: $relative"
    }
}

function Require-Text([string]$relative, [string[]]$tokens) {
    Require-File $relative
    $text = Get-Content (Join-Path $root $relative) -Raw
    foreach ($token in $tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            throw "$relative is missing required contract text: $token"
        }
    }
    return $text
}

# Shipping projects remain self-contained and dependency-minimal.
$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) {
    $packageRefs | ForEach-Object { Write-Error "Third-party/package dependency found: $($_.Path):$($_.LineNumber)" }
    exit 1
}

# Reject known telemetry, analytics, advertising and automatic crash-upload SDKs.
$forbiddenTelemetryPatterns = @(
    '\bMicrosoft\.ApplicationInsights\b',
    '\bTelemetryClient\b',
    '\bSentry(?:\.|\s*\()',
    '\bGoogleAnalytics\b',
    '\bSegment\.Analytics\b',
    '\bMixpanel(?:\.|\s*\()',
    '\bPostHog(?:\.|\s*\()',
    '\bMicrosoft\.AppCenter\b',
    '\bCrashlytics\b',
    '\bBugsnag(?:\.|\s*\()',
    '\bRollbar(?:\.|\s*\()',
    '\bAmplitude(?:\.|\s*\()',
    '\bFirebaseAnalytics\b'
)
foreach ($pattern in $forbiddenTelemetryPatterns) {
    $matches = Get-ChildItem $src -Recurse -Filter *.cs | Select-String -Pattern $pattern
    if ($matches) {
        $matches | ForEach-Object { Write-Error "Forbidden telemetry/tracking SDK reference matching '$pattern': $($_.Path):$($_.LineNumber)" }
        exit 1
    }
}

# Private signing material must never be committed.
foreach ($pattern in @('*.pfx','*.p12','*.key')) {
    $matches = Get-ChildItem $root -Recurse -File -Filter $pattern | Where-Object {
        $_.FullName -notmatch '[\\/](\.git|artifacts|release|release-linux|bin|obj)[\\/]'
    }
    if ($matches) {
        throw "Private signing material must not exist in tracked source paths: $($matches.FullName -join ', ')"
    }
}

# Windows + Linux are the complete shipping platform scope. Mobile code must stay absent.
$forbiddenPlatformDirectories = @(
    'src/GhostFTP.Android',
    'src/GhostFTP.iOS',
    'src/GhostFTP.Mobile',
    'src/GhostFTP.Maui'
)
foreach ($relative in $forbiddenPlatformDirectories) {
    if (Test-Path (Join-Path $root $relative)) {
        throw "Unsupported mobile source directory must not exist: $relative"
    }
}
$mobileTfms = Get-ChildItem $src -Recurse -Filter *.csproj | Select-String -Pattern 'net[0-9.]+-(android|ios|maccatalyst)'
if ($mobileTfms) {
    throw 'Android/iOS/MacCatalyst shipping targets are outside the Ghost FTP desktop scope.'
}

# Keep shipping source C#-centric. Native platform access is limited to audited platform P/Invoke layers.
foreach ($pattern in @('*.xaml','*.axaml','*.go','*.rs','*.cpp','*.c','*.cc','*.java','*.kt','*.swift')) {
    $matches = Get-ChildItem $src -Recurse -File -Filter $pattern
    if ($matches) {
        throw "Forbidden/non-C# source exists under src/: $($matches.FullName -join ', ')"
    }
}

# Version, channel, product and manifests must be synchronized.
$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$channel = (Get-Content (Join-Path $root 'RELEASE_CHANNEL') -Raw).Trim().ToLowerInvariant()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "VERSION must use MAJOR.MINOR.PATCH. Got: $version" }
if ($channel -notin @('beta','stable')) { throw "RELEASE_CHANNEL must be beta or stable. Got: $channel" }
if ([int]($version.Split('.')[0]) -eq 0 -and $channel -ne 'beta') { throw 'All pre-1.0 builds must use the beta channel.' }

$props = [xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)
$propertyGroup = $props.Project.PropertyGroup
$expectedAssembly = "$version.0"
$expectedInformational = if ($channel -eq 'beta') { "$version-beta" } else { $version }
if ([string]$propertyGroup.Version -ne $version) { throw 'Directory.Build.props Version is not synchronized with VERSION.' }
if ([string]$propertyGroup.AssemblyVersion -ne $expectedAssembly) { throw "AssemblyVersion must be $expectedAssembly." }
if ([string]$propertyGroup.FileVersion -ne $expectedAssembly) { throw "FileVersion must be $expectedAssembly." }
if ([string]$propertyGroup.InformationalVersion -ne $expectedInformational) { throw "InformationalVersion must be $expectedInformational." }
if ([string]$propertyGroup.Authors -ne 'BRENDIGO LTD' -or [string]$propertyGroup.Company -ne 'BRENDIGO LTD') {
    throw 'Authors and Company metadata must identify BRENDIGO LTD.'
}
if ([string]$propertyGroup.Product -ne 'Ghost FTP') { throw 'Product metadata must be Ghost FTP.' }
foreach ($manifest in @('src/GhostFTP.App/app.manifest','src/GhostFTP.Setup/app.manifest')) {
    $text = Get-Content (Join-Path $root $manifest) -Raw
    if ($text -notmatch [regex]::Escape("version=`"$expectedAssembly`"")) {
        throw "$manifest does not use assembly identity $expectedAssembly."
    }
}

$requiredFiles = @(
    'src/GhostFTP.Core/GhostFTP.Core.csproj',
    'src/GhostFTP.Core/Protocol/FtpSession.Core.cs',
    'src/GhostFTP.Core/Protocol/FtpSession.Data.cs',
    'src/GhostFTP.Core/Protocol/InputGuard.cs',
    'src/GhostFTP.Core/Protocol/LocalPathSafety.cs',
    'src/GhostFTP.Core/Services/AppData.cs',
    'src/GhostFTP.Core/Services/ProfileStore.cs',
    'src/GhostFTP.Core/Services/AesFileSecretProtector.cs',
    'src/GhostFTP.Core/Services/TransferQueueService.cs',
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostProduct.cs',
    'src/GhostFTP.Design/GhostBrand.cs',
    'src/GhostFTP.Design/GhostLocalization.cs',
    'src/GhostFTP.Design/GhostReferencePalette.cs',
    'src/GhostFTP.Design/GhostReferenceText.cs',
    'src/GhostFTP.Design/GhostTransferText.cs',
    'src/GhostFTP.App/GhostFTP.App.csproj',
    'src/GhostFTP.App/UI/MainWindow.Layout.cs',
    'src/GhostFTP.App/UI/MainWindow.ReferenceShell.cs',
    'src/GhostFTP.App/UI/MainWindow.Responsive.cs',
    'src/GhostFTP.App/UI/MainWindow.Connection.cs',
    'src/GhostFTP.App/UI/MainWindow.Transfers.cs',
    'src/GhostFTP.App/UI/MainWindow.QueueUx.cs',
    'src/GhostFTP.App/UI/MainWindow.DocumentationCapture.cs',
    'src/GhostFTP.App/UI/SiteManagerDialog.cs',
    'src/GhostFTP.Linux/GhostFTP.Linux.csproj',
    'src/GhostFTP.Linux/Program.cs',
    'src/GhostFTP.Linux/X11Native.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Core.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Draw.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Input.cs',
    'src/GhostFTP.Setup/GhostFTP.Setup.csproj',
    'src/GhostFTP.Setup/SetupWindow.cs',
    'src/GhostFTP.Setup/Services/InstallerService.cs',
    'build-release.ps1',
    'build-linux-release.sh',
    '.github/workflows/ci.yml',
    '.github/workflows/release.yml',
    '.github/workflows/capture-ui.yml',
    'README.md',
    'PRIVACY.md',
    'SECURITY.md',
    'LICENSE',
    'tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj',
    'tests/GhostFTP.DemoSelfTest/GhostFTP.DemoSelfTest.csproj',
    'tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj',
    'tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj'
)
foreach ($required in $requiredFiles) { Require-File $required }

# Platform-neutral core and native desktop renderers.
$coreProject = Get-Content (Join-Path $root 'src/GhostFTP.Core/GhostFTP.Core.csproj') -Raw
if ($coreProject -notmatch '<TargetFramework>net10\.0</TargetFramework>') { throw 'GhostFTP.Core must remain platform-neutral net10.0.' }
$designProject = Get-Content (Join-Path $root 'src/GhostFTP.Design/GhostFTP.Design.csproj') -Raw
if ($designProject -notmatch 'net10\.0;net10\.0-windows') { throw 'GhostFTP.Design must expose platform-neutral and Windows targets.' }
$appProject = Get-Content (Join-Path $root 'src/GhostFTP.App/GhostFTP.App.csproj') -Raw
if ($appProject -notmatch '<TargetFramework>net10\.0-windows' -or $appProject -notmatch '<UseWPF>true</UseWPF>') {
    throw 'GhostFTP.App must remain the Windows WPF renderer.'
}
$linuxProject = Get-Content (Join-Path $root 'src/GhostFTP.Linux/GhostFTP.Linux.csproj') -Raw
if ($linuxProject -notmatch '<TargetFramework>net10\.0</TargetFramework>' -or
    $linuxProject -notmatch 'GhostFTP\.Core\\GhostFTP\.Core\.csproj' -or
    $linuxProject -notmatch 'GhostFTP\.Design\\GhostFTP\.Design\.csproj') {
    throw 'GhostFTP.Linux must remain the native Linux renderer sharing Core and Design.'
}

# Protocol security and untrusted-input boundaries.
$inputGuard = Require-Text 'src/GhostFTP.Core/Protocol/InputGuard.cs' @('RejectControl','Uri.CheckHostName','RemotePath','RemoteName')
$core = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Core.cs' @(
    'Enum.IsDefined(options.Security)',
    'AUTH TLS',
    'PBSZ 0',
    'PROT P',
    'InputGuard.Host',
    'InputGuard.CommandArgument'
)
$data = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Data.cs' @('TYPE I','authenticated control host','EnsureBinaryTransferModeAsync')
$connection = Require-Text 'src/GhostFTP.App/UI/MainWindow.Connection.cs' @(
    'InputGuard.Host(_host.Text)',
    'InputGuard.Port(parsedPort)',
    '_session = null',
    '_activeOptions = null',
    'Plain FTP is not encrypted'
)

# Transfer queue must keep bounded parallelism and 0.1.3 dispatch-pause semantics.
$queue = Require-Text 'src/GhostFTP.Core/Services/TransferQueueService.cs' @(
    'MaxQueuedTransfers = 4096',
    'MaxParallelTransfers = 8',
    'IsQueuePaused',
    'PauseQueue()',
    'ResumeQueue()',
    'QueueStateChanged',
    'WaitForDispatchAsync',
    'ClearCompleted()',
    'ClearFailed()',
    'ClearCancelled()'
)
$queueTest = Require-Text 'tests/GhostFTP.QueueSelfTest/Program.cs' @(
    'TestPauseResumeAndSelectiveClearAsync',
    'A paused queue started a new transfer before ResumeQueue.',
    'Selective completed-transfer cleanup',
    'Selective cancelled-transfer cleanup',
    'Selective failed-transfer cleanup'
)

# Local persistence and saved-password protection remain opt-in and local only.
$appData = Require-Text 'src/GhostFTP.Core/Services/AppData.cs' @('ConcurrentTransfers','KeepAliveSeconds','PrivateFilePermissions')
$profiles = Require-Text 'src/GhostFTP.Core/Services/ProfileStore.cs' @('profile.IsSessionOnly','ProtectedPassword','EnsureDemo')
$aes = Require-Text 'src/GhostFTP.Core/Services/AesFileSecretProtector.cs' @('AesGcm','RandomNumberGenerator','PrivateFilePermissions.TryHardenFile')
$dpapi = Require-Text 'src/GhostFTP.App/Services/DpapiSecretProtector.cs' @('ProtectedData','DataProtectionScope.CurrentUser','RtlSecureZeroMemory')

# 29-language local catalog: English is authoritative and no online translation path exists.
$localization = Require-Text 'src/GhostFTP.Design/GhostLocalization.cs' @(
    'DefaultLanguageCode = "en"',
    'new("en", "English", "English")',
    'new("hr", "Hrvatski", "Croatian")',
    'new("zh-TW", "繁體中文", "Chinese (Traditional)")'
)
$languageCount = ([regex]::Matches($localization, 'new\("[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})?",')).Count
if ($languageCount -lt 20) { throw "Ghost FTP must expose at least 20 local languages; found $languageCount." }
if ($languageCount -ne 29) { throw "Ghost FTP $version release contract expects 29 selectable languages; found $languageCount." }
$transferText = Require-Text 'src/GhostFTP.Design/GhostTransferText.cs' @(
    'PauseQueue',
    'ResumeQueue',
    'RetryFailed',
    'ClearCompleted',
    'Croatian',
    'GhostLocalization.CurrentLanguageCode'
)
$setupSource = Require-Text 'src/GhostFTP.Setup/SetupWindow.cs' @(
    'GhostLocalization.SupportedLanguages',
    'GhostComboBox',
    'ResizeMode.CanResizeWithGrip',
    'StepProgressText',
    'Local-only Setup',
    'Transactional maintenance'
)

# Clean Windows workstation: contextual file operations live in Local/Remote panes, not duplicated globally.
$referenceShell = Require-Text 'src/GhostFTP.App/UI/MainWindow.ReferenceShell.cs' @(
    'BuildReferenceShell',
    'GhostReferenceText.T',
    '_referenceSidebarColumn',
    'GridSplitter',
    'NormalizeQuickConnect',
    'KeepQuickConnectionInTabIfRequested',
    'SearchRemote'
)
foreach ($obsolete in @('ReferenceNewFolderAsync','ReferenceRenameAsync','ReferenceDeleteAsync','EnsureReferenceToolbarActions')) {
    if ($referenceShell -match [regex]::Escape($obsolete)) {
        throw "Obsolete duplicate global file action remains in reference shell: $obsolete"
    }
}
$responsive = Require-Text 'src/GhostFTP.App/UI/MainWindow.Responsive.cs' @(
    'connectionSplitter',
    'transferSplitter',
    'paneSplitter',
    'ResizeBehavior = GridResizeBehavior.PreviousAndNext'
)
$layout = Require-Text 'src/GhostFTP.App/UI/MainWindow.Layout.cs' @('BuildTopMenu','BuildMainToolbar','BuildConnectionLog','BuildFilePanes','BuildTransfers')
$windowsTransfers = Require-Text 'src/GhostFTP.App/UI/MainWindow.Transfers.cs' @(
    'ToggleQueuePause',
    'RetryAllFailedTransfers',
    'ClearCompletedTransfers',
    'ClearFailedTransfers',
    'ClearCancelledTransfers',
    'aggregateSpeed'
)
$queueUx = Require-Text 'src/GhostFTP.App/UI/MainWindow.QueueUx.cs' @(
    'GhostTransferText.T("PauseQueue")',
    'RetryAllFailedTransfers',
    'CopySelectedTransferSource',
    'CopySelectedTransferDestination'
)
$capture = Require-Text 'src/GhostFTP.App/UI/MainWindow.DocumentationCapture.cs' @(
    'ReferenceCaptureWidth = 1914',
    'ReferenceCaptureHeight = 907',
    'ghostftp-client.png',
    'ghostftp-site-manager.png'
)

# Linux must remain a real native renderer using shared protocol, palette, localization and transfer queue.
$linuxCore = Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Core.cs' @(
    'ProfileStore',
    'AesFileSecretProtector',
    'TransferQueueService',
    'FtpSession',
    'DemoFtpSession',
    'GhostLocalization.SupportedLanguages'
)
$linuxInput = Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Input.cs' @(
    'ActivateTransferRow',
    'ToggleTransferQueuePause',
    'RetryFailedTransfers',
    'ClearCompletedTransfers',
    'ClearFailedTransfers',
    'ClearCancelledTransfers'
)
$linuxDraw = Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Draw.cs' @(
    'GhostReferencePalette.Background',
    'R("ConnectionLog")',
    'R("SiteManager")',
    'R("KeepInTab")',
    'DrawFilePanes',
    'DrawTransfers',
    'DrawQuickConnect',
    'GhostTransferText.T',
    'ToggleTransferQueuePause',
    'ActivateTransferRow'
)

# Authentic product images must be real captured application output.
foreach ($image in @('assets/readme/ghostftp-client.png','assets/readme/ghostftp-site-manager.png')) {
    Require-File $image
    if ((Get-Item (Join-Path $root $image)).Length -lt 10000) {
        throw "Authentic UI screenshot is unexpectedly small: $image"
    }
}

Write-Host "Ghost FTP $version $channel source audit passed: Windows/Linux-only scope, no third-party PackageReference dependencies, no telemetry SDKs, 29 local languages with English fallback, strict FTP/FTPS input and TLS boundaries, local credential protection, bounded pause/resume transfer queue, clean resizable Windows workstation, transfer-management parity in native Linux, premium Setup contract and authentic UI capture contract."
