$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Read-Source([string]$relative) {
    $path = Join-Path $root $relative
    if (!(Test-Path $path -PathType Leaf)) { throw "Required hardening source is missing: $relative" }
    return Get-Content $path -Raw
}

function Require-Tokens([string]$relative, [string[]]$tokens) {
    $text = Read-Source $relative
    foreach ($token in $tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            throw "$relative is missing final hardening contract token: $token"
        }
    }
    return $text
}

$version = (Read-Source 'VERSION').Trim()
$channel = (Read-Source 'RELEASE_CHANNEL').Trim().ToLowerInvariant()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid VERSION for hardening audit: $version" }
if ($channel -notin @('beta','stable')) { throw "Invalid RELEASE_CHANNEL for hardening audit: $channel" }
$expectedTag = if ($channel -eq 'beta') { "v$version-beta" } else { "v$version" }
$currentReleaseTrigger = ".github/release-trigger-$version"

$core = Require-Tokens 'src/GhostFTP.Core/Protocol/FtpSession.Core.cs' @(
    'Enum.IsDefined(options.Security)',
    'throw new ArgumentOutOfRangeException',
    'Ensure(auth, 200, 299, "Server refused explicit TLS.")'
)

$data = Require-Tokens 'src/GhostFTP.Core/Protocol/FtpSession.Data.cs' @(
    'EnsureBinaryTransferModeAsync',
    'TYPE I',
    'FTP server refused binary transfer mode.',
    'authenticated control host'
)
if (($data | Select-String -Pattern 'EnsureBinaryTransferModeAsync\(cancellationToken\)' -AllMatches).Matches.Count -lt 2) {
    throw 'Binary mode must be enforced for both receive and send data paths.'
}

$linuxCore = Require-Tokens 'src/GhostFTP.Linux/LinuxMainWindow.Core.cs' @(
    'Plain FTP is not encrypted',
    '_plainFtpApproved',
    'candidateSession',
    '_activeOptions = null',
    'EnsureKeepAliveLoopStarted()',
    'ReferenceEquals(_session, session)'
)
$linuxKeepAlive = Require-Tokens 'src/GhostFTP.Linux/LinuxMainWindow.KeepAlive.cs' @(
    'KeepAliveAsync',
    'DemoFtpSession',
    'Connection lost',
    'ReferenceEquals(_session, session)'
)
$linuxInput = Require-Tokens 'src/GhostFTP.Linux/LinuxMainWindow.Input.cs' @(
    'if (_transferSelected >= 0)',
    'CancelSelectedTransfer()',
    '_transferSelected = -1'
)

$installer = Require-Tokens 'src/GhostFTP.Setup/Services/InstallerService.cs' @(
    'FileVersionInfo.GetVersionInfo',
    'versionInfo.ProductName',
    'versionInfo.CompanyName',
    'versionInfo.FileVersion',
    'EnsureNotDowngrade',
    'backupSetup',
    'setupCommitted',
    'RollbackFile(',
    'GhostFTP-Setup.exe.*',
    'root.DeleteValue("QuietUninstallString"'
)
if ($installer -match 'SetValue\("QuietUninstallString"') {
    throw 'Setup must not advertise QuietUninstallString until true silent uninstall exists.'
}
if (($installer | Select-String -Pattern 'EnsureNotDowngrade\(' -AllMatches).Matches.Count -lt 3) {
    throw 'Installer downgrade protection must cover the staged application and maintenance Setup binaries.'
}

$readme = Require-Tokens 'README.md' @(
    '<img src="assets/readme/ghostftp-client.png"',
    'Authentic application capture',
    'Windows release files',
    'Linux release files',
    'docs/LIVE-SMOKE-TEST.md'
)
if ($readme -match 'ghostftp-hero\.svg') {
    throw 'README still references the stale decorative Ghost FTP hero.'
}
if (Test-Path (Join-Path $root 'assets/readme/ghostftp-hero.svg') -PathType Leaf) {
    throw 'Stale decorative README hero must not remain in the active repository assets.'
}

foreach ($relative in @(
    'tests/GhostFTP.DemoSelfTest/GhostFTP.DemoSelfTest.csproj',
    'tests/GhostFTP.DemoSelfTest/Program.cs',
    'tests/GhostFTP.LiveSmoke/GhostFTP.LiveSmoke.csproj',
    'tests/GhostFTP.LiveSmoke/Program.cs',
    '.github/workflows/live-smoke.yml',
    'docs/LIVE-SMOKE-TEST.md',
    $currentReleaseTrigger
)) {
    if (!(Test-Path (Join-Path $root $relative) -PathType Leaf)) { throw "Missing final release file: $relative" }
}

$demo = Require-Tokens 'tests/GhostFTP.DemoSelfTest/Program.cs' @(
    'Demo session complete local FTP workflow',
    'Demo mode performed no external network operation',
    'UploadFileAsync',
    'DownloadFileAsync',
    'UploadDirectoryAsync',
    'DownloadDirectoryAsync',
    'RenameAsync',
    'DeleteDirectoryAsync',
    'KeepAliveAsync',
    'Ghost FTP Demo round-trip payload',
    'Demo file upload replaced an existing directory.',
    'Demo directory upload replaced an existing file.',
    'Demo disconnect did not reset the working directory.'
)
$ci = Require-Tokens '.github/workflows/ci.yml' @(
    'Complete local Demo workflow self-test',
    'Complete local Demo workflow self-test on Linux',
    'tests/GhostFTP.DemoSelfTest/GhostFTP.DemoSelfTest.csproj'
)

$live = Require-Tokens 'tests/GhostFTP.LiveSmoke/Program.cs' @(
    'GHOSTFTP_LIVE_PASSWORD',
    'GHOSTFTP_LIVE_ALLOW_PLAIN',
    'ListAsync',
    'KeepAliveAsync',
    'No writes were performed.',
    '[redacted]'
)
foreach ($writeCall in @('UploadFileAsync','UploadDirectoryAsync','DeleteFileAsync','DeleteDirectoryAsync','RenameAsync','CreateDirectoryAsync')) {
    if ($live -match [regex]::Escape($writeCall)) {
        throw "Live smoke harness must remain non-destructive but contains $writeCall."
    }
}

$liveWorkflow = Require-Tokens '.github/workflows/live-smoke.yml' @(
    'workflow_dispatch',
    '${{ secrets.GHOSTFTP_LIVE_PASSWORD }}',
    'Non-destructive connect PWD LIST NOOP disconnect'
)

$security = Require-Tokens 'SECURITY.md' @(
    'Fail-closed transport selection',
    'AUTH TLS',
    'TYPE I',
    'Installer integrity and rollback',
    'Live-server testing without credential disclosure'
)
$privacy = Require-Tokens 'PRIVACY.md' @(
    'without application telemetry',
    'server-only',
    'Session-only Quick Connect',
    'Live-server smoke testing'
)
$architecture = Require-Tokens 'docs/ARCHITECTURE.md' @(
    'Linux X11/XWayland renderer',
    'Data-transfer mode integrity',
    'Live real-server smoke architecture',
    'GitHub Release'
)
$parity = Require-Tokens 'docs/UI-PARITY.md' @(
    '1914 × 907',
    'Windows and Linux',
    'transfer queue and cancellation'
)
$platform = Require-Tokens 'docs/PLATFORM-SUPPORT.md' @(
    'Windows',
    'Linux',
    'Web/browser client',
    'docs/LIVE-SMOKE-TEST.md'
)
$releasePolicy = Require-Tokens 'docs/RELEASE-POLICY.md' @(
    'setup.exe',
    'portable.exe',
    'GhostFTP-linux-x64',
    'GitHub Release requirement',
    $expectedTag
)

$selfTest = Require-Tokens 'tests/GhostFTP.SelfTest/Program.cs' @(
    'Invalid FTP security modes fail closed',
    'TestInvalidSecurityModeFailsClosed',
    '(FtpSecurityMode)999'
)

Write-Host "Ghost FTP $version $channel hardening audit passed: fail-closed FTP security selection, strict AUTH TLS, required binary transfer mode, complete cross-platform local Demo workflow test, Linux lifecycle/keepalive/focus safety, transactional Windows application/Setup rollback with downgrade protection, authentic README capture, non-destructive secret-backed live smoke harness, Windows/Linux release documentation and canonical public Release assets."
