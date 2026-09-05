$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$channel = (Get-Content (Join-Path $root 'RELEASE_CHANNEL') -Raw).Trim().ToLowerInvariant()
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "VERSION must use MAJOR.MINOR.PATCH format. Got: $version"
}
if ($channel -notin @('beta', 'stable')) {
    throw "RELEASE_CHANNEL must be beta or stable. Got: $channel"
}
$major = [int]($version.Split('.')[0])
if ($major -eq 0 -and $channel -ne 'beta') {
    throw 'All Ghost FTP 0.x packages must remain Beta.'
}
$expectedFileVersion = "$version.0"

$release = Join-Path $root 'release'
$artifacts = Join-Path $root 'artifacts'
Remove-Item $release -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item $release -ItemType Directory | Out-Null
New-Item $artifacts -ItemType Directory | Out-Null

$architectures = @(
    @{ Rid = 'win-x64'; Suffix = 'win-x64' },
    @{ Rid = 'win-arm64'; Suffix = 'win-arm64' }
)

foreach ($arch in $architectures) {
    $portableDir = Join-Path $artifacts ("portable-" + $arch.Suffix)
    $setupDir = Join-Path $artifacts ("setup-" + $arch.Suffix)

    # ReadyToRun is deliberately disabled for release packaging. Cross-architecture R2R
    # generation is slower and less predictable on hosted x64 runners, while the app
    # remains fully self-contained and single-file without it.
    dotnet publish src/GhostFTP.App/GhostFTP.App.csproj -c Release -r $arch.Rid --self-contained true -p:PublishReadyToRun=false -o $portableDir
    if ($LASTEXITCODE -ne 0) { throw "Portable publish failed for $($arch.Rid)." }

    $payload = Join-Path $portableDir 'GhostFTP.exe'
    if (!(Test-Path $payload -PathType Leaf) -or (Get-Item $payload).Length -le 0) {
        throw "Portable payload is missing or empty: $payload"
    }

    $portableVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $payload)).FileVersion
    if ($portableVersion -ne $expectedFileVersion) {
        throw "Portable payload $($arch.Rid) has FileVersion '$portableVersion' but expected '$expectedFileVersion'."
    }

    dotnet publish src/GhostFTP.Setup/GhostFTP.Setup.csproj -c Release -r $arch.Rid --self-contained true -p:PublishReadyToRun=false -o $setupDir -p:GhostFtpPayloadPath="$payload"
    if ($LASTEXITCODE -ne 0) { throw "Setup publish failed for $($arch.Rid)." }

    $setupPayload = Join-Path $setupDir 'GhostFTP-Setup.exe'
    if (!(Test-Path $setupPayload -PathType Leaf) -or (Get-Item $setupPayload).Length -le 0) {
        throw "Setup payload is missing or empty: $setupPayload"
    }

    $setupVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $setupPayload)).FileVersion
    if ($setupVersion -ne $expectedFileVersion) {
        throw "Setup payload $($arch.Rid) has FileVersion '$setupVersion' but expected '$expectedFileVersion'."
    }

    Copy-Item $payload (Join-Path $release ("GhostFTP-Portable-" + $arch.Suffix + '.exe'))
    Copy-Item $setupPayload (Join-Path $release ("GhostFTP-Setup-" + $arch.Suffix + '.exe'))
}

# Canonical direct-download names. These intentionally point to the standard Windows x64 build,
# while architecture-specific assets remain available alongside them. Filenames stay stable while
# internal FileVersion metadata advances through the Beta line and eventually to stable 1.0.0.
$x64Portable = Join-Path $release 'GhostFTP-Portable-win-x64.exe'
$x64Setup = Join-Path $release 'GhostFTP-Setup-win-x64.exe'
$arm64Portable = Join-Path $release 'GhostFTP-Portable-win-arm64.exe'
$arm64Setup = Join-Path $release 'GhostFTP-Setup-win-arm64.exe'

Copy-Item $x64Portable (Join-Path $release 'portable.exe')
Copy-Item $x64Setup (Join-Path $release 'setup.exe')
Copy-Item $arm64Portable (Join-Path $release 'portable-arm64.exe')
Copy-Item $arm64Setup (Join-Path $release 'setup-arm64.exe')

$requiredExecutables = @(
    'portable.exe',
    'setup.exe',
    'portable-arm64.exe',
    'setup-arm64.exe',
    'GhostFTP-Portable-win-x64.exe',
    'GhostFTP-Setup-win-x64.exe',
    'GhostFTP-Portable-win-arm64.exe',
    'GhostFTP-Setup-win-arm64.exe'
)

foreach ($required in $requiredExecutables) {
    $path = Join-Path $release $required
    if (!(Test-Path $path -PathType Leaf) -or (Get-Item $path).Length -le 0) {
        throw "Required release asset is missing or empty: $required"
    }

    $actualFileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $path)).FileVersion
    if ($actualFileVersion -ne $expectedFileVersion) {
        throw "$required has FileVersion '$actualFileVersion' but expected '$expectedFileVersion'."
    }
}

if ($channel -eq 'stable' -and $version -eq '1.0.0') {
    Write-Host 'Stable gate: canonical portable.exe and setup.exe family verified as Ghost FTP 1.0.0.0.'
}
elseif ($channel -eq 'beta') {
    Write-Host "Beta package set verified for Ghost FTP $version (FileVersion $expectedFileVersion)."
}

$checksumLines = Get-ChildItem $release -Filter *.exe | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)"
}
$checksumLines | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii

Write-Host "Release ready: $release"
Write-Host "Ghost FTP $version $channel"
Get-ChildItem $release | Sort-Object Name | Format-Table Name, Length
