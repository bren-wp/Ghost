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

# Shipping source remains dependency-minimal: no NuGet PackageReference entries.
$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) {
    $packageRefs | ForEach-Object { Write-Error "Third-party/package dependency found: $($_.Path):$($_.LineNumber)" }
    exit 1
}

# Reject known telemetry/tracking SDKs in shipping C# source.
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

# Private signing material must never be tracked.
$privateKeyExtensions = @('*.pfx','*.p12','*.key')
foreach ($pattern in $privateKeyExtensions) {
    $matches = Get-ChildItem $root -Recurse -File -Filter $pattern | Where-Object {
        $_.FullName -notmatch '[\\/](\.git|artifacts|release|release-linux|bin|obj)[\\/]'
    }
    if ($matches) {
        throw "Private signing material must not exist in tracked source paths: $($matches.FullName -join ', ')"
    }
}

# Version / channel / publisher metadata must remain synchronized.
$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$channel = (Get-Content (Join-Path $root 'RELEASE_CHANNEL') -Raw).Trim().ToLowerInvariant()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "VERSION must use MAJOR.MINOR.PATCH. Got: $version" }
if ($channel -notin @('beta','stable')) { throw "RELEASE_CHANNEL must be beta or stable. Got: $channel" }
if ([int]($version.Split('.')[0]) -eq 0 -and $channel -ne 'beta') {
    throw 'All pre-1.0 Ghost FTP builds must use the beta release channel.'
}

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

# Keep shipping source C#-centric and mobile targets out of this desktop line.
$forbiddenSourceExtensions = @('*.xaml','*.axaml','*.go','*.rs','*.cpp','*.c','*.cc','*.java','*.kt','*.swift')
foreach ($pattern in $forbiddenSourceExtensions) {
    $matches = Get-ChildItem $src -Recurse -File -Filter $pattern
    if ($matches) { throw "Forbidden/non-C# source exists under src/: $($matches.FullName -join ', ')" }
}

$mobileTfms = Get-ChildItem $src -Recurse -Filter *.csproj | Select-String -Pattern 'net[0-9.]+-(android|ios|maccatalyst)'
if ($mobileTfms) {
    throw 'Android/iOS/MacCatalyst shipping targets are outside the current desktop scope.'
}

# Required source / documentation / release-control files.
$requiredFiles = @(
    'src/GhostFTP.Core/GhostFTP.Core.csproj',
    'src/GhostFTP.Core/Services/AppData.cs',
    'src/GhostFTP.Core/Services/AesFileSecretProtector.cs',
    'src/GhostFTP.Core/Services/ProfileStore.cs',
    'src/GhostFTP.Core/Protocol/LocalPathSafety.cs',
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostProduct.cs',
    'src/GhostFTP.Design/GhostBrand.cs',
    'src/GhostFTP.Design/GhostLocalization.cs',
    'src/GhostFTP.App/GhostFTP.App.csproj',
    'src/GhostFTP.App/UI/MainWindow.Layout.cs',
    'src/GhostFTP.App/UI/MainWindow.DocumentationCapture.cs',
    'src/GhostFTP.App/UI/SiteManagerDialog.cs',
    'src/GhostFTP.Linux/GhostFTP.Linux.csproj',
    'src/GhostFTP.Linux/Program.cs',
    'src/GhostFTP.Linux/X11Native.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Core.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Draw.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Input.cs',
    'src/GhostFTP.Linux/LinuxMainWindow.Testing.cs',
    'src/GhostFTP.Setup/GhostFTP.Setup.csproj',
    'build-release.ps1',
    'build-linux-release.sh',
    'tools/Sign-WindowsRelease.ps1',
    'tools/New-DevelopmentSigningCertificate.ps1',
    '.github/workflows/ci.yml',
    '.github/workflows/release.yml',
    '.github/workflows/capture-ui.yml',
    'docs/CODE-SIGNING.md',
    'docs/PLATFORM-SUPPORT.md',
    'docs/VERSIONING.md',
    'docs/RELEASE-POLICY.md',
    'README.md',
    'PRIVACY.md',
    'SECURITY.md',
    'LICENSE',
    'tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj',
    'tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj',
    'tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj'
)
foreach ($required in $requiredFiles) { Require-File $required }

# Platform-neutral core and real Windows/Linux renderers.
$coreProject = Get-Content (Join-Path $root 'src/GhostFTP.Core/GhostFTP.Core.csproj') -Raw
if ($coreProject -notmatch '<TargetFramework>net10\.0</TargetFramework>') {
    throw 'GhostFTP.Core must remain platform-neutral net10.0.'
}

$designProject = Get-Content (Join-Path $root 'src/GhostFTP.Design/GhostFTP.Design.csproj') -Raw
if ($designProject -notmatch 'net10\.0;net10\.0-windows') {
    throw 'GhostFTP.Design must expose both platform-neutral and Windows targets.'
}

$appProject = Get-Content (Join-Path $root 'src/GhostFTP.App/GhostFTP.App.csproj') -Raw
if ($appProject -notmatch '<TargetFramework>net10\.0-windows' -or $appProject -notmatch '<UseWPF>true</UseWPF>') {
    throw 'GhostFTP.App must remain the explicit Windows WPF desktop renderer.'
}

$linuxProject = Get-Content (Join-Path $root 'src/GhostFTP.Linux/GhostFTP.Linux.csproj') -Raw
if ($linuxProject -notmatch '<TargetFramework>net10\.0</TargetFramework>' -or
    $linuxProject -notmatch 'GhostFTP\.Core\GhostFTP\.Core\.csproj' -or
    $linuxProject -notmatch 'GhostFTP\.Design\GhostFTP\.Design\.csproj') {
    throw 'GhostFTP.Linux must remain a real net10.0 renderer sharing Core and Design.'
}

$solution = Get-Content (Join-Path $root 'GhostFTP.sln') -Raw
if ($solution -notmatch 'GhostFTP\.Linux' -or $solution -notmatch 'src\GhostFTP\.Linux\GhostFTP\.Linux\.csproj') {
    throw 'GhostFTP.Linux must participate in the solution build.'
}

# Shared product identity is authoritative; Windows brand code delegates to it.
$productSource = Require-Text 'src/GhostFTP.Design/GhostProduct.cs' @(
    'Ghost FTP',
    'BRENDIGO LTD',
    '16545639',
    '71–75 Shelton Street',
    'https://ghostftp.com',
    'https://brendigo.com'
)
$brandSource = Require-Text 'src/GhostFTP.Design/GhostBrand.cs' @('GhostProduct.DisplayName','GhostProduct.Publisher','GhostProduct.PublisherWebsite')

# Shared local settings and Linux credential protection must remain explicit.
$appDataSource = Require-Text 'src/GhostFTP.Core/Services/AppData.cs' @(
    'ConcurrentTransfers',
    'KeepAliveSeconds',
    'PrivateFilePermissions',
    'SupportedOSPlatform("linux")'
)
$aesSource = Require-Text 'src/GhostFTP.Core/Services/AesFileSecretProtector.cs' @(
    'AesGcm',
    'AssociatedData',
    'RandomNumberGenerator',
    'PrivateFilePermissions.TryHardenFile'
)
$pathSafety = Require-Text 'src/GhostFTP.Core/Protocol/LocalPathSafety.cs' @('OperatingSystem.IsWindows()','Path.GetRelativePath')

# Windows professional workstation structure and authentic capture path.
$layout = Require-Text 'src/GhostFTP.App/UI/MainWindow.Layout.cs' @(
    'BuildTopMenu',
    'BuildMainToolbar',
    'BuildConnectionLog',
    'Site Manager',
    'BuildFilePanes',
    'BuildTransfers'
)
$siteManager = Require-Text 'src/GhostFTP.App/UI/SiteManagerDialog.cs' @(
    'Site name',
    'Host / IP / URL',
    'FTPS explicit TLS',
    'RememberPassword',
    'Default remote path'
)
$captureSource = Require-Text 'src/GhostFTP.App/UI/MainWindow.DocumentationCapture.cs' @('RenderTargetBitmap','ghostftp-client.png','ghostftp-site-manager.png')
$programSource = Require-Text 'src/GhostFTP.App/Program.cs' @('--capture-ui')

foreach ($image in @('assets/readme/ghostftp-client.png','assets/readme/ghostftp-site-manager.png')) {
    Require-File $image
    if ((Get-Item (Join-Path $root $image)).Length -lt 10000) {
        throw "Authentic UI screenshot is unexpectedly small: $image"
    }
}

# Linux renderer must expose the same major workstation concepts and direct X11 execution.
$linuxCore = Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Core.cs' @(
    'ProfileStore',
    'AesFileSecretProtector',
    'TransferQueueService',
    'FtpSession',
    'DemoFtpSession'
)
$linuxDraw = Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Draw.cs' @(
    'Connection Log',
    'Site Manager',
    'DrawFilePanes',
    'DrawTransfers',
    'QuickConnect'
)
$linuxInput = Require-Text 'src/GhostFTP.Linux/LinuxMainWindow.Input.cs' @(
    'NewLocalFolder',
    'RenameLocal',
    'DeleteLocal',
    'NewRemoteFolder',
    'RenameRemote',
    'DeleteRemote',
    'ServerSystem'
)
$x11 = Require-Text 'src/GhostFTP.Linux/X11Native.cs' @('libX11.so.6','XOpenDisplay','Xutf8DrawString')
$linuxProgram = Require-Text 'src/GhostFTP.Linux/Program.cs' @('--smoke-test','RequestSmokeTestShutdown')

# Signing pipeline: secure secret-based Authenticode with stable-release trust gate.
$signScript = Require-Text 'tools/Sign-WindowsRelease.ps1' @(
    'GHOSTFTP_SIGNING_PFX_BASE64',
    'GHOSTFTP_SIGNING_PFX_PASSWORD',
    'Set-AuthenticodeSignature',
    'Get-AuthenticodeSignature',
    'SHA256SUMS.txt'
)
$releaseWorkflow = Require-Text '.github/workflows/release.yml' @(
    'GHOSTFTP_SIGNING_PFX_BASE64',
    'Sign-WindowsRelease.ps1',
    'RequireTrustedSignature',
    'SIGNING.txt',
    'linux-release'
)
$signingDoc = Require-Text 'docs/CODE-SIGNING.md' @(
    'Never commit',
    'self-signed',
    'SmartScreen',
    'BRENDIGO LTD'
)

# Linux release packaging must publish both architectures and checksums.
$linuxBuild = Require-Text 'build-linux-release.sh' @(
    'linux-x64',
    'linux-arm64',
    'PublishSingleFile=true',
    'SHA256SUMS-linux.txt',
    'install.sh',
    'uninstall.sh'
)

# Release docs / README must match the current public line and current platform claims.
$releaseNotes = Join-Path $root "docs/releases/v$version.md"
if (!(Test-Path $releaseNotes -PathType Leaf)) { throw "Missing current release notes: docs/releases/v$version.md" }
$releaseText = Get-Content $releaseNotes -Raw
if ($releaseText -notmatch [regex]::Escape("Ghost FTP $version")) { throw 'Current release notes do not identify the current version.' }
if ($channel -eq 'beta' -and $releaseText -notmatch '(?i)beta') { throw 'Beta release notes must identify the build as Beta.' }

$readme = Get-Content (Join-Path $root 'README.md') -Raw
foreach ($token in @(
    "Current source version: **$version**",
    'Current release channel: **Beta**',
    'src/GhostFTP.Linux',
    'build-linux-release.sh',
    'docs/CODE-SIGNING.md',
    '--capture-ui'
)) {
    if ($readme -notmatch [regex]::Escape($token)) { throw "README.md is missing current documentation token: $token" }
}

$platformDoc = Get-Content (Join-Path $root 'docs/PLATFORM-SUPPORT.md') -Raw
if ($platformDoc -notmatch 'linux-x64' -or $platformDoc -notmatch 'linux-arm64' -or $platformDoc -notmatch 'libX11\.so\.6') {
    throw 'Platform support documentation is not synchronized with the Linux renderer.'
}

$privacy = Get-Content (Join-Path $root 'PRIVACY.md') -Raw
if ($privacy -notmatch 'keepalive' -or $privacy -notmatch 'NOOP' -or $privacy -notmatch '0.*disable') {
    throw 'PRIVACY.md must document configurable server-only keepalive and its disable option.'
}
$security = Get-Content (Join-Path $root 'SECURITY.md') -Raw
if ($security -notmatch 'stale' -or $security -notmatch 'Keepalive' -or $security -notmatch '1.?8') {
    throw 'SECURITY.md must document keepalive stale-state handling and bounded transfer concurrency.'
}

# Prevent old product identities from returning.
$forbiddenProductTokens = @( ('My' + 'FTP'), ('By' + 'FTP') )
$textExtensions = @('.cs','.csproj','.props','.targets','.md','.txt','.yml','.yaml','.json','.xml','.ps1','.sh','.bat','.svg')
$scanFiles = Get-ChildItem $root -Recurse -File | Where-Object {
    $relative = $_.FullName.Substring($root.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    $textExtensions -contains $_.Extension.ToLowerInvariant() -and
    $relative -notmatch '^(\.git|bin|obj|artifacts|release|release-linux)([\\/]|$)'
}
foreach ($file in $scanFiles) {
    if ($file.FullName -eq $MyInvocation.MyCommand.Path) { continue }
    foreach ($token in $forbiddenProductTokens) {
        $matches = Select-String -Path $file.FullName -SimpleMatch -Pattern $token
        if ($matches) {
            $matches | ForEach-Object { Write-Error "Non-Ghost FTP product identity found: $($_.Path):$($_.LineNumber)" }
            exit 1
        }
    }
}

Write-Host "Source audit passed for Ghost FTP $version ${channel}: synchronized BRENDIGO LTD identity, shared net10.0 FTP core, real Windows WPF and Linux X11/XWayland desktop renderers, zero PackageReference entries, no known telemetry/tracking SDKs, local-only settings/credentials, cross-platform path safety, Authenticode signing pipeline, architecture-explicit Linux packaging, authentic Windows screenshots and synchronized release documentation."
