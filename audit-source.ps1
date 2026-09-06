$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'

function Require-File([string]$relative) {
    $path = Join-Path $root $relative
    if (!(Test-Path $path -PathType Leaf)) {
        throw "Required Ghost FTP source/asset is missing: $relative"
    }
    return $path
}

function Require-Text([string]$relative, [string[]]$tokens) {
    $path = Require-File $relative
    $text = Get-Content $path -Raw
    foreach ($token in $tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            throw "$relative is missing required contract text: $token"
        }
    }
    return $text
}

# Shipping and regression projects remain dependency-minimal/package-free.
$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) {
    $packageRefs | ForEach-Object { Write-Error "PackageReference found: $($_.Path):$($_.LineNumber)" }
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
        $matches | ForEach-Object { Write-Error "Forbidden telemetry/tracking reference matching '$pattern': $($_.Path):$($_.LineNumber)" }
        exit 1
    }
}

# Private signing material must never be tracked in source paths.
foreach ($pattern in @('*.pfx', '*.p12', '*.key')) {
    $matches = Get-ChildItem $root -Recurse -File -Filter $pattern | Where-Object {
        $_.FullName -notmatch '[\\/](\.git|artifacts|release|release-linux|bin|obj)[\\/]'
    }
    if ($matches) { throw "Private signing material exists in source: $($matches.FullName -join ', ')" }
}

# Windows + Linux are the complete shipping application scope.
foreach ($relative in @('src/GhostFTP.Android', 'src/GhostFTP.iOS', 'src/GhostFTP.Mobile', 'src/GhostFTP.Maui')) {
    if (Test-Path (Join-Path $root $relative)) { throw "Unsupported mobile source directory exists: $relative" }
}
$mobileTfms = Get-ChildItem $src -Recurse -Filter *.csproj | Select-String -Pattern 'net[0-9.]+-(android|ios|maccatalyst)'
if ($mobileTfms) { throw 'Android/iOS/MacCatalyst targets are outside the Ghost FTP shipping scope.' }

# Shipping source remains C#-centric; audited platform P/Invoke layers are C# files.
foreach ($pattern in @('*.axaml', '*.go', '*.rs', '*.cpp', '*.c', '*.cc', '*.java', '*.kt', '*.swift')) {
    $matches = Get-ChildItem $src -Recurse -File -Filter $pattern
    if ($matches) { throw "Unexpected non-C# source under src/: $($matches.FullName -join ', ')" }
}

# Version and product identity must be synchronized.
$version = (Get-Content (Require-File 'VERSION') -Raw).Trim()
$channel = (Get-Content (Require-File 'RELEASE_CHANNEL') -Raw).Trim().ToLowerInvariant()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "VERSION must use MAJOR.MINOR.PATCH. Got: $version" }
if ($channel -notin @('beta', 'stable')) { throw "RELEASE_CHANNEL must be beta or stable. Got: $channel" }
if ([int]($version.Split('.')[0]) -eq 0 -and $channel -ne 'beta') { throw 'All Ghost FTP pre-1.0 builds must use the beta channel.' }

$expectedAssembly = "$version.0"
$expectedInformational = if ($channel -eq 'beta') { "$version-beta" } else { $version }
$props = [xml](Get-Content (Require-File 'Directory.Build.props') -Raw)
$pg = $props.Project.PropertyGroup
if ([string]$pg.Version -ne $version) { throw 'Directory.Build.props Version is not synchronized.' }
if ([string]$pg.AssemblyVersion -ne $expectedAssembly) { throw "AssemblyVersion must be $expectedAssembly." }
if ([string]$pg.FileVersion -ne $expectedAssembly) { throw "FileVersion must be $expectedAssembly." }
if ([string]$pg.InformationalVersion -ne $expectedInformational) { throw "InformationalVersion must be $expectedInformational." }
if ([string]$pg.Authors -ne 'BRENDIGO LTD' -or [string]$pg.Company -ne 'BRENDIGO LTD' -or [string]$pg.Product -ne 'Ghost FTP') {
    throw 'Publisher/product metadata must identify Ghost FTP / BRENDIGO LTD.'
}
foreach ($manifest in @('src/GhostFTP.App/app.manifest', 'src/GhostFTP.Setup/app.manifest')) {
    $text = Get-Content (Require-File $manifest) -Raw
    if ($text -notmatch [regex]::Escape("version=`"$expectedAssembly`"")) { throw "$manifest must use $expectedAssembly." }
}

$requiredFiles = @(
    'src/GhostFTP.Core/GhostFTP.Core.csproj',
    'src/GhostFTP.Core/Protocol/FtpSession.Core.cs',
    'src/GhostFTP.Core/Protocol/FtpSession.Control.cs',
    'src/GhostFTP.Core/Protocol/FtpSession.Data.cs',
    'src/GhostFTP.Core/Protocol/FtpSession.Operations.cs',
    'src/GhostFTP.Core/Protocol/FtpSession.Resume.cs',
    'src/GhostFTP.Core/Protocol/FtpListingParser.cs',
    'src/GhostFTP.Core/Protocol/InputGuard.cs',
    'src/GhostFTP.Core/Protocol/LocalPathSafety.cs',
    'src/GhostFTP.Core/Services/AppData.cs',
    'src/GhostFTP.Core/Services/ProfileStore.cs',
    'src/GhostFTP.Core/Services/AesFileSecretProtector.cs',
    'src/GhostFTP.Core/Services/TransferQueueService.cs',
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostProduct.cs',
    'src/GhostFTP.Design/GhostLocalization.cs',
    'src/GhostFTP.Design/GhostReferencePalette.cs',
    'src/GhostFTP.Design/GhostTransferText.cs',
    'src/GhostFTP.App/GhostFTP.App.csproj',
    'src/GhostFTP.App/Services/DpapiSecretProtector.cs',
    'src/GhostFTP.App/UI/MainWindow.ReferenceShell.cs',
    'src/GhostFTP.App/UI/MainWindow.Responsive.cs',
    'src/GhostFTP.App/UI/MainWindow.Transfers.cs',
    'src/GhostFTP.App/UI/MainWindow.DocumentationCapture.cs',
    'src/GhostFTP.Linux/GhostFTP.Linux.csproj',
    'src/GhostFTP.Linux/LinuxMainWindow.Core.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Draw.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Input.cs',
    'src/GhostFTP.Setup/GhostFTP.Setup.csproj',
    'src/GhostFTP.Setup/SetupWindow.cs',
    'src/GhostFTP.Setup/Services/InstallerService.cs',
    'tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj',
    'tests/GhostFTP.DemoSelfTest/GhostFTP.DemoSelfTest.csproj',
    'tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj',
    'tests/GhostFTP.HardeningSelfTest/GhostFTP.HardeningSelfTest.csproj',
    'tests/GhostFTP.ResumeSelfTest/GhostFTP.ResumeSelfTest.csproj',
    'tests/GhostFTP.ResumeSelfTest/Program.cs',
    'tests/GhostFTP.ResumeSelfTest/DestinationSafetyRegression.cs',
    'tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj',
    'build-release.ps1',
    'build-linux-release.sh',
    '.github/workflows/ci.yml',
    '.github/workflows/release.yml',
    '.github/workflows/capture-ui.yml',
    'README.md', 'SECURITY.md', 'PRIVACY.md', 'NOTICE.md', 'LICENSE'
)
foreach ($relative in $requiredFiles) { Require-File $relative | Out-Null }

# Core and renderer project boundaries.
$coreProject = Get-Content (Require-File 'src/GhostFTP.Core/GhostFTP.Core.csproj') -Raw
if ($coreProject -notmatch '<TargetFramework>net10\.0</TargetFramework>') { throw 'GhostFTP.Core must remain platform-neutral net10.0.' }
$designProject = Get-Content (Require-File 'src/GhostFTP.Design/GhostFTP.Design.csproj') -Raw
if ($designProject -notmatch 'net10\.0;net10\.0-windows') { throw 'GhostFTP.Design must expose platform-neutral and Windows targets.' }
$appProject = Get-Content (Require-File 'src/GhostFTP.App/GhostFTP.App.csproj') -Raw
if ($appProject -notmatch '<TargetFramework>net10\.0-windows' -or $appProject -notmatch '<UseWPF>true</UseWPF>') {
    throw 'GhostFTP.App must remain the Windows WPF renderer.'
}
$linuxProject = Get-Content (Require-File 'src/GhostFTP.Linux/GhostFTP.Linux.csproj') -Raw
if ($linuxProject -notmatch '<TargetFramework>net10\.0</TargetFramework>' -or
    $linuxProject -notmatch 'GhostFTP\.Core\\GhostFTP\.Core\.csproj' -or
    $linuxProject -notmatch 'GhostFTP\.Design\\GhostFTP\.Design\.csproj') {
    throw 'GhostFTP.Linux must remain the native Linux renderer sharing Core and Design.'
}

# Protocol, input and parser boundaries.
Require-Text 'src/GhostFTP.Core/Protocol/InputGuard.cs' @('RejectControl', 'Uri.CheckHostName', 'RemotePath', 'RemoteName') | Out-Null
Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Core.cs' @(
    'Enum.IsDefined(options.Security)', 'AUTH TLS', 'PBSZ 0', 'PROT P',
    'DownloadFileWithResumeIntegrityCoreAsync', 'DownloadDirectoryWithResumeIntegrityCoreAsync'
) | Out-Null
Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Data.cs' @(
    'TYPE I', 'authenticated control host', 'EnsureBinaryTransferModeAsync',
    'ArrayPool<byte>.Shared.Rent', 'clearArray: true'
) | Out-Null
Require-Text 'src/GhostFTP.Core/Protocol/FtpListingParser.cs' @(
    'MaxListingLineChars = 64 * 1024', 'MaxMlsdFactsPerEntry = 64', 'RegexOptions.NonBacktracking', 'StringReader'
) | Out-Null

# 0.1.6 staged resume safety and regression coverage.
$resume = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Resume.cs' @(
    'MaxDownloadResumeMetadataBytes = 16 * 1024',
    'RemoteIdentityMatchesAsync',
    'ReceiveDownloadIntoPartAsync',
    'DeleteLocalRequired',
    'Unable to remove an untrusted partial download.',
    'existing local destination was preserved',
    'File.Move(partPath, localPath, true)'
)
if ($resume -match 'DownloadFileCoreAsync\(') { throw 'Safe resume must not call the legacy pre-validation commit helper.' }
$resumeRegression = Require-Text 'tests/GhostFTP.ResumeSelfTest/DestinationSafetyRegression.cs' @(
    'Existing destination bytes were replaced before remote post-validation completed.',
    'An untrusted partial reached REST despite failed cleanup.',
    'An untrusted partial reached RETR despite failed cleanup.'
)

# Transfer queue remains bounded and truthfully dispatch-paused.
Require-Text 'src/GhostFTP.Core/Services/TransferQueueService.cs' @(
    'MaxQueuedTransfers = 4096', 'MaxParallelTransfers = 8', 'IsQueuePaused',
    'PauseQueue()', 'ResumeQueue()', 'WaitForDispatchAsync',
    'ClearCompleted()', 'ClearFailed()', 'ClearCancelled()'
) | Out-Null

# Local persistence and saved-secret protection remain local/opt-in.
Require-Text 'src/GhostFTP.Core/Services/ProfileStore.cs' @('profile.IsSessionOnly', 'ProtectedPassword', 'EnsureDemo') | Out-Null
Require-Text 'src/GhostFTP.Core/Services/AesFileSecretProtector.cs' @('AesGcm', 'RandomNumberGenerator', 'PrivateFilePermissions.TryHardenFile') | Out-Null
Require-Text 'src/GhostFTP.App/Services/DpapiSecretProtector.cs' @('ProtectedData', 'DataProtectionScope.CurrentUser', 'RtlSecureZeroMemory') | Out-Null

# Local 29-language catalog with English default/fallback.
$localization = Require-Text 'src/GhostFTP.Design/GhostLocalization.cs' @(
    'DefaultLanguageCode = "en"',
    'new("en", "English", "English")',
    'new("hr", "Hrvatski", "Croatian")',
    'new("zh-TW", "繁體中文", "Chinese (Traditional)")'
)
$languageCount = ([regex]::Matches($localization, 'new\("[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})?",')).Count
if ($languageCount -ne 29) { throw "Ghost FTP $version expects 29 selectable local languages; found $languageCount." }
Require-Text 'src/GhostFTP.Setup/SetupWindow.cs' @('GhostLocalization.SupportedLanguages', 'ResizeMode.CanResizeWithGrip', 'Local-only Setup', 'Transactional maintenance') | Out-Null

# Windows and Linux workstation parity remains present.
Require-Text 'src/GhostFTP.App/UI/MainWindow.ReferenceShell.cs' @('BuildReferenceShell', 'GridSplitter', 'NormalizeQuickConnect', 'SearchRemote') | Out-Null
Require-Text 'src/GhostFTP.App/UI/MainWindow.Responsive.cs' @('connectionSplitter', 'transferSplitter', 'paneSplitter') | Out-Null
Require-Text 'src/GhostFTP.App/UI/MainWindow.Transfers.cs' @('ToggleQueuePause', 'RetryAllFailedTransfers', 'ClearCompletedTransfers', 'aggregateSpeed') | Out-Null
Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Core.cs' @('TransferQueueService', 'FtpSession', 'DemoFtpSession', 'GhostLocalization.SupportedLanguages') | Out-Null
Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Input.cs' @('ActivateTransferRow', 'ToggleTransferQueuePause', 'RetryFailedTransfers') | Out-Null
Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Draw.cs' @('GhostReferencePalette.Background', 'DrawFilePanes', 'DrawTransfers', 'DrawQuickConnect') | Out-Null

# Authentic screenshots are real application artifacts with a minimum sanity size.
foreach ($image in @('assets/readme/ghostftp-client.png', 'assets/readme/ghostftp-site-manager.png')) {
    $path = Require-File $image
    if ((Get-Item $path).Length -lt 10000) { throw "Authentic UI screenshot is unexpectedly small: $image" }
}
Require-Text 'src/GhostFTP.App/UI/MainWindow.DocumentationCapture.cs' @(
    'ReferenceCaptureWidth = 1914', 'ReferenceCaptureHeight = 907', 'ghostftp-client.png', 'ghostftp-site-manager.png'
) | Out-Null

# CI and Release independently gate the dedicated resume suite on both shipping platforms.
foreach ($workflow in @('.github/workflows/ci.yml', '.github/workflows/release.yml')) {
    Require-Text $workflow @(
        'tests/GhostFTP.ResumeSelfTest/GhostFTP.ResumeSelfTest.csproj',
        'Safe download resume integrity self-test',
        'Safe download resume integrity self-test on Linux'
    ) | Out-Null
}

# Current docs and release trigger exist without rewriting preserved historical releases.
Require-File ".github/release-trigger-$version" | Out-Null
Require-Text "docs/releases/v$version.md" @("Ghost FTP $version", 'Safe download resume model') | Out-Null
Require-Text 'README.md' @("Current source version: **$version**", 'safe download resume-integrity self-test on Windows and Linux') | Out-Null
Require-Text 'docs/UI-PARITY.md' @("$version Beta", 'Safe download resume parity') | Out-Null
Require-Text 'docs/INSTALLATION.md' @("VERSION=$version", '.ghostftp.part.meta') | Out-Null
Require-Text 'docs/LOCALIZATION.md' @("$version Beta", '29 selectable languages') | Out-Null

Write-Host "Ghost FTP $version $channel source audit passed: Windows/Linux-only C# scope, zero third-party PackageReferences, no telemetry SDKs, synchronized product/version identity, strict FTP/FTPS/parser/queue boundaries, local credential protection, 29 local languages, authentic workstation assets and fail-closed staged resume integrity gated independently in CI and Release."
