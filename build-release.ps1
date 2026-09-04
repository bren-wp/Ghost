$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

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

    dotnet publish src/GhostFTP.Setup/GhostFTP.Setup.csproj -c Release -r $arch.Rid --self-contained true -p:PublishReadyToRun=false -o $setupDir -p:GhostFtpPayloadPath="$payload"
    if ($LASTEXITCODE -ne 0) { throw "Setup publish failed for $($arch.Rid)." }

    $setupPayload = Join-Path $setupDir 'GhostFTP-Setup.exe'
    if (!(Test-Path $setupPayload -PathType Leaf) -or (Get-Item $setupPayload).Length -le 0) {
        throw "Setup payload is missing or empty: $setupPayload"
    }

    Copy-Item $payload (Join-Path $release ("GhostFTP-Portable-" + $arch.Suffix + '.exe'))
    Copy-Item $setupPayload (Join-Path $release ("GhostFTP-Setup-" + $arch.Suffix + '.exe'))
}

# Canonical direct-download names. These intentionally point to the standard Windows x64 build,
# while architecture-specific assets remain available alongside them.
$x64Portable = Join-Path $release 'GhostFTP-Portable-win-x64.exe'
$x64Setup = Join-Path $release 'GhostFTP-Setup-win-x64.exe'
$arm64Portable = Join-Path $release 'GhostFTP-Portable-win-arm64.exe'
$arm64Setup = Join-Path $release 'GhostFTP-Setup-win-arm64.exe'

Copy-Item $x64Portable (Join-Path $release 'portable.exe')
Copy-Item $x64Setup (Join-Path $release 'setup.exe')
Copy-Item $arm64Portable (Join-Path $release 'portable-arm64.exe')
Copy-Item $arm64Setup (Join-Path $release 'setup-arm64.exe')

foreach ($required in @('portable.exe', 'setup.exe', 'portable-arm64.exe', 'setup-arm64.exe')) {
    $path = Join-Path $release $required
    if (!(Test-Path $path -PathType Leaf) -or (Get-Item $path).Length -le 0) {
        throw "Required release asset is missing or empty: $required"
    }
}

$checksumLines = Get-ChildItem $release -Filter *.exe | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)"
}
$checksumLines | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii

Write-Host "Release ready: $release"
Get-ChildItem $release | Sort-Object Name | Format-Table Name, Length
