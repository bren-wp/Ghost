$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Require-File([string]$relative) {
    $path = Join-Path $root $relative
    if (!(Test-Path $path -PathType Leaf)) {
        throw "Required Ghost FTP hardening artifact is missing: $relative"
    }
    return $path
}

function Require-Text([string]$relative, [string[]]$tokens) {
    $path = Require-File $relative
    $text = Get-Content $path -Raw
    foreach ($token in $tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            throw "$relative is missing required hardening contract text: $token"
        }
    }
    return $text
}

function Reject-Text([string]$relative, [string[]]$tokens) {
    $path = Require-File $relative
    $text = Get-Content $path -Raw
    foreach ($token in $tokens) {
        if ($text -match [regex]::Escape($token)) {
            throw "$relative contains forbidden hardening contract text: $token"
        }
    }
}

$version = (Get-Content (Require-File 'VERSION') -Raw).Trim()
$channel = (Get-Content (Require-File 'RELEASE_CHANNEL') -Raw).Trim().ToLowerInvariant()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid VERSION: $version" }
if ($channel -notin @('beta', 'stable')) { throw "Invalid RELEASE_CHANNEL: $channel" }
if ([int]($version.Split('.')[0]) -eq 0 -and $channel -ne 'beta') { throw 'Every Ghost FTP 0.x source must remain Beta.' }

$expectedAssembly = "$version.0"
$expectedInformational = if ($channel -eq 'beta') { "$version-beta" } else { $version }
$expectedTag = if ($channel -eq 'beta') { "v$version-beta" } else { "v$version" }

# Release identity must agree across source, manifests, trigger and detailed notes.
$props = [xml](Get-Content (Require-File 'Directory.Build.props') -Raw)
$pg = $props.Project.PropertyGroup
if ([string]$pg.Version -ne $version) { throw 'Directory.Build.props Version does not match VERSION.' }
if ([string]$pg.AssemblyVersion -ne $expectedAssembly) { throw "AssemblyVersion must be $expectedAssembly." }
if ([string]$pg.FileVersion -ne $expectedAssembly) { throw "FileVersion must be $expectedAssembly." }
if ([string]$pg.InformationalVersion -ne $expectedInformational) { throw "InformationalVersion must be $expectedInformational." }
if ([string]$pg.Product -ne 'Ghost FTP' -or [string]$pg.Company -ne 'BRENDIGO LTD') {
    throw 'Release product/company metadata is inconsistent.'
}
foreach ($manifest in @('src/GhostFTP.App/app.manifest', 'src/GhostFTP.Setup/app.manifest')) {
    $text = Get-Content (Require-File $manifest) -Raw
    if ($text -notmatch [regex]::Escape("version=`"$expectedAssembly`"")) {
        throw "$manifest is not synchronized with $expectedAssembly."
    }
}
$trigger = ".github/release-trigger-$version"
Require-File $trigger | Out-Null
$notes = "docs/releases/v$version.md"
$notesText = Require-Text $notes @("Ghost FTP $version", 'Beta')
if ($notesText -notmatch 'resume') { throw "$notes must document the 0.1.6 resume-integrity contract." }

# No previous active 0.x trigger may coexist with the current line.
$activeTriggers = @(Get-ChildItem (Join-Path $root '.github') -File -Filter 'release-trigger-*')
if ($activeTriggers.Count -ne 1 -or $activeTriggers[0].Name -ne "release-trigger-$version") {
    throw "Exactly one active release trigger is required for $version. Found: $($activeTriggers.Name -join ', ')"
}

# Control and TLS state stay fail-closed and bounded.
$core = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Core.cs' @(
    'Enum.IsDefined(options.Security)',
    'MaxReplyLines = 256',
    'MaxReplyChars = 1_048_576',
    'MaxReplyLineChars = 65_536',
    'MaxGreetingReplies = 4',
    'AUTH TLS',
    'PBSZ 0',
    'PROT P',
    'DownloadFileWithResumeIntegrityCoreAsync',
    'DownloadDirectoryWithResumeIntegrityCoreAsync'
)
$control = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Control.cs' @(
    'ReadReplyAsync',
    'MaxReplyLineChars',
    'MaxReplyLines',
    'MaxReplyChars'
)
$data = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Data.cs' @(
    'TYPE I',
    'EnsureBinaryTransferModeAsync',
    'ArrayPool<byte>.Shared.Rent',
    'clearArray: true',
    'authenticated control host',
    'TryParseEpsvPort',
    'TryParsePasvPort'
)

# Server-controlled directory text stays bounded/non-backtracking.
$listing = Require-Text 'src/GhostFTP.Core/Protocol/FtpListingParser.cs' @(
    'MaxListingLineChars = 64 * 1024',
    'MaxMlsdFactsPerEntry = 64',
    'RegexOptions.NonBacktracking',
    'EnumerateLines',
    'StringReader'
)

# 0.1.6 safe-resume integrity: no length-only public resume, staged commit, required cleanup.
$resume = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Resume.cs' @(
    'DownloadResumeMetadataVersion = 1',
    'MaxDownloadResumeMetadataBytes = 16 * 1024',
    '.ghostftp.part',
    '.meta',
    'TryGetFileModifiedUtcAsync',
    'RemoteIdentityMatchesAsync',
    'ReceiveDownloadIntoPartAsync',
    'DeleteLocalRequired',
    'Unable to remove an untrusted partial download.',
    'existing local destination was preserved',
    'File.Move(partPath, localPath, true)'
)
if ($resume -match 'DownloadFileCoreAsync\(') {
    throw 'The safe-resume wrapper must not call the legacy DownloadFileCoreAsync path that commits before post-validation.'
}
if ($resume -notmatch 'RemoteIdentityMatchesAsync[\s\S]*File\.Move\(partPath, localPath, true\)') {
    throw 'A staged partial must be remote-identity validated before final destination commit.'
}

# Existing legacy helper may remain for internal compatibility, but public downloads route only through safe resume.
$coreOperations = Require-Text 'src/GhostFTP.Core/Protocol/FtpSession.Operations.cs' @(
    'DownloadFileCoreAsync',
    'UploadFileCoreAsync',
    'TryGetFileSizeAsync'
)
if ($core -notmatch 'DownloadFileAsync[\s\S]*DownloadFileWithResumeIntegrityCoreAsync') {
    throw 'Public file download is not routed through safe resume integrity.'
}
if ($core -notmatch 'DownloadDirectoryAsync[\s\S]*DownloadDirectoryWithResumeIntegrityCoreAsync') {
    throw 'Public directory download is not routed through safe resume integrity.'
}

# Queue bounds and truthful dispatch-pause semantics remain mandatory.
$queue = Require-Text 'src/GhostFTP.Core/Services/TransferQueueService.cs' @(
    'MaxQueuedTransfers = 4096',
    'MaxParallelTransfers = 8',
    'PauseQueue()',
    'ResumeQueue()',
    'WaitForDispatchAsync',
    'ClearCompleted()',
    'ClearFailed()',
    'ClearCancelled()',
    'DisposeAsync'
)

# Credential and profile protection remain local/opt-in.
$profiles = Require-Text 'src/GhostFTP.Core/Services/ProfileStore.cs' @('profile.IsSessionOnly', 'ProtectedPassword', 'EnsureDemo')
$linuxSecrets = Require-Text 'src/GhostFTP.Core/Services/AesFileSecretProtector.cs' @('AesGcm', 'RandomNumberGenerator', 'PrivateFilePermissions.TryHardenFile')
$windowsSecrets = Require-Text 'src/GhostFTP.App/Services/DpapiSecretProtector.cs' @('ProtectedData', 'DataProtectionScope.CurrentUser', 'RtlSecureZeroMemory')

# Setup remains one per-user maintenance executable with no separate uninstaller product.
$setup = Require-Text 'src/GhostFTP.Setup/SetupWindow.cs' @(
    'ResizeMode.CanResizeWithGrip',
    'Local-only Setup',
    'Transactional maintenance'
)
$installer = Require-Text 'src/GhostFTP.Setup/Services/InstallerService.cs' @('GhostFTP-Setup.exe', 'UninstallString')
Reject-Text 'src/GhostFTP.Setup/Services/InstallerService.cs' @('uninstall.exe')

# Dedicated resume regression must cover normal resume plus the two review-critical fail-closed cases.
Require-File 'tests/GhostFTP.ResumeSelfTest/GhostFTP.ResumeSelfTest.csproj' | Out-Null
$resumeTest = Require-Text 'tests/GhostFTP.ResumeSelfTest/Program.cs' @(
    'Validated partial resumes at the exact REST offset',
    'Changed remote identity restarts from zero',
    'Remote mutation during transfer discards the completed file'
)
$destinationRegression = Require-Text 'tests/GhostFTP.ResumeSelfTest/DestinationSafetyRegression.cs' @(
    'Existing destination bytes were replaced before remote post-validation completed.',
    'Failure to remove an untrusted partial did not abort the download.',
    'An untrusted partial reached REST despite failed cleanup.',
    'An untrusted partial reached RETR despite failed cleanup.'
)

# CI and release publication must independently execute the resume suite on Windows and Linux.
$ci = Require-Text '.github/workflows/ci.yml' @(
    'tests/GhostFTP.ResumeSelfTest/GhostFTP.ResumeSelfTest.csproj',
    'Safe download resume integrity self-test',
    'Safe download resume integrity self-test on Linux',
    'Capture authentic production UI',
    'Verify canonical and architecture-specific executables'
)
$release = Require-Text '.github/workflows/release.yml' @(
    'tests/GhostFTP.ResumeSelfTest/GhostFTP.ResumeSelfTest.csproj',
    'Safe download resume integrity self-test',
    'Safe download resume integrity self-test on Linux',
    'Create or synchronize GitHub release with Windows assets',
    'Attach verified Linux assets to GitHub release',
    'SHA256SUMS.txt',
    'SHA256SUMS-linux.txt'
)

# Current public documentation must state the same version/scope and explicit safe-resume guarantee.
$readme = Require-Text 'README.md' @(
    "Current source version: **$version**",
    "Informational version: **$expectedInformational**",
    'safe download resume-integrity self-test on Windows and Linux',
    'Windows and Linux'
)
$security = Require-Text 'SECURITY.md' @("Ghost FTP $version Beta", 'resume')
$privacy = Require-Text 'PRIVACY.md' @("$version Beta", 'resume')
$notice = Require-Text 'NOTICE.md' @("Ghost FTP $version Beta", 'Windows and Linux')
$architecture = Require-Text 'docs/ARCHITECTURE.md' @("$version Beta", 'Safe download resume architecture')
$platform = Require-Text 'docs/PLATFORM-SUPPORT.md' @("$version Beta", 'Windows', 'Linux')
$uiUx = Require-Text 'docs/UI-UX.md' @("$version Beta", 'Safe resume UX contract')
$uiParity = Require-Text 'docs/UI-PARITY.md' @("$version Beta", 'Safe download resume parity', 'existing local destination remains untouched')
$installation = Require-Text 'docs/INSTALLATION.md' @("$version Beta", 'Upgrade from 0.1.5', '.ghostftp.part.meta')
$localization = Require-Text 'docs/LOCALIZATION.md' @("$version Beta", 'Resume-integrity messages', '29 selectable languages')
$versioning = Require-Text 'docs/VERSIONING.md' @("$version Beta", $expectedTag, $trigger)
$releasePolicy = Require-Text 'docs/RELEASE-POLICY.md' @("VERSION=$version", $expectedTag, 'resume integrity')

# Platform and privacy scope must not drift during release hardening.
$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) { throw 'Third-party NuGet PackageReference found in the package-free Ghost FTP release contract.' }
$mobileTfms = Get-ChildItem (Join-Path $root 'src') -Recurse -Filter *.csproj | Select-String -Pattern 'net[0-9.]+-(android|ios|maccatalyst)'
if ($mobileTfms) { throw 'Unsupported mobile target framework found in shipping source.' }

Write-Host "Ghost FTP $version $channel hardening audit passed: strict FTP/FTPS boundaries, bounded parser/queue resources, staged identity-checked downloads that preserve existing destinations, fail-closed stale-partial cleanup, local credential/privacy boundaries, Windows/Linux resume regression gates, Setup integrity and release publication checks."
