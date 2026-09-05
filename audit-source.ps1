$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'

$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) {
    $packageRefs | ForEach-Object { Write-Error "Third-party/package dependency found: $($_.Path):$($_.LineNumber)" }
    exit 1
}

# Match telemetry SDK identifiers/namespaces, not arbitrary substrings. For example,
# WPF ScrollBarVisibility must never be mistaken for the Rollbar SDK.
$forbiddenTelemetryPatterns = @(
    '\bMicrosoft\.ApplicationInsights\b',
    '\bTelemetryClient\b',
    '\bSentry(?:\.|\s*\()',
    '\bGoogleAnalytics\b',
    '\bSegment\.Analytics\b',
    '\bMixpanel(?:\.|\s*\()',
    '\bPostHog(?:\.|\s*\()',
    '\bMicrosoft\.AppCenter\b',
    '\bAppCenter(?:\.|\s*\()',
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

$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$props = [xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)
$propertyGroup = $props.Project.PropertyGroup
$propsVersion = [string]$propertyGroup.Version
$assemblyVersion = [string]$propertyGroup.AssemblyVersion
$fileVersion = [string]$propertyGroup.FileVersion
$informationalVersion = [string]$propertyGroup.InformationalVersion
$authors = [string]$propertyGroup.Authors
$company = [string]$propertyGroup.Company
$product = [string]$propertyGroup.Product
$copyright = [string]$propertyGroup.Copyright

if ($version -ne $propsVersion) { throw "VERSION ($version) does not match Directory.Build.props Version ($propsVersion)." }
$expectedAssembly = "$version.0"
if ($assemblyVersion -ne $expectedAssembly -or $fileVersion -ne $expectedAssembly) {
    throw "AssemblyVersion/FileVersion must both be $expectedAssembly."
}
if ($informationalVersion -ne $version) { throw "InformationalVersion must be $version." }
if ($product -ne 'Ghost FTP') { throw 'Product metadata must be Ghost FTP.' }
if ($authors -ne 'BRENDIGO LTD' -or $company -ne 'BRENDIGO LTD') {
    throw 'Authors and Company metadata must identify BRENDIGO LTD as the publisher/developer.'
}
if ($copyright -notmatch [regex]::Escape('BRENDIGO LTD')) { throw 'Copyright metadata must identify BRENDIGO LTD.' }

foreach ($manifest in @('src/GhostFTP.App/app.manifest','src/GhostFTP.Setup/app.manifest')) {
    $text = Get-Content (Join-Path $root $manifest) -Raw
    if ($text -notmatch [regex]::Escape("version=`"$expectedAssembly`"")) {
        throw "$manifest does not use assembly identity $expectedAssembly."
    }
}

$forbiddenSourceExtensions = @('*.xaml','*.axaml','*.go','*.rs','*.cpp','*.c','*.cc','*.java','*.kt','*.swift')
foreach ($pattern in $forbiddenSourceExtensions) {
    $matches = Get-ChildItem $src -Recurse -File -Filter $pattern
    if ($matches) { throw "Forbidden/non-C# source exists under src/: $($matches.FullName -join ', ')" }
}

# Ghost FTP is currently a Windows + future Linux desktop product. Do not allow stale
# Android/iOS application trees or mobile TargetFrameworks to re-enter shipping source.
$mobilePaths = Get-ChildItem $src -Recurse -Force | Where-Object {
    $relative = $_.FullName.Substring($src.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    $relative -match '(^|[\\/])(android|ios)([\\/]|$)'
}
if ($mobilePaths) {
    throw "Android/iOS shipping source is outside the current Ghost FTP desktop scope: $($mobilePaths.FullName -join ', ')"
}

$mobileTfms = Get-ChildItem $src -Recurse -Filter *.csproj | Select-String -Pattern 'net[0-9.]+-(android|ios|maccatalyst)|<SupportedOSPlatformVersion>.*(android|ios)'
if ($mobileTfms) {
    $mobileTfms | ForEach-Object { Write-Error "Mobile target framework found in shipping source: $($_.Path):$($_.LineNumber)" }
    exit 1
}

$coreProject = Get-Content (Join-Path $root 'src/GhostFTP.Core/GhostFTP.Core.csproj') -Raw
if ($coreProject -notmatch '<TargetFramework>net10\.0</TargetFramework>') {
    throw 'GhostFTP.Core must remain platform-neutral net10.0 so future desktop renderers share one FTP/FTPS engine.'
}

$requiredFiles = @(
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostTheme.cs',
    'src/GhostFTP.Design/GhostWindowChrome.cs',
    'src/GhostFTP.Design/GhostBrand.cs',
    'src/GhostFTP.Design/GhostComboBox.cs',
    'src/GhostFTP.Design/GhostLocalization.cs',
    'src/GhostFTP.Design/GhostSetupLocalization.cs',
    'src/GhostFTP.App/UI/MainWindow.KeepAlive.cs',
    'assets/brand/ghostftp-icon.svg',
    'assets/readme/ghostftp-hero.svg',
    'Directory.Build.targets',
    'tools/generate-ghostftp-icon.ps1',
    'LICENSE',
    'docs/PLATFORM-SUPPORT.md',
    'tests/GhostFTP.SelfTest/GhostFTP.SelfTest.csproj',
    'tests/GhostFTP.SelfTest/Program.cs',
    'tests/GhostFTP.QueueSelfTest/GhostFTP.QueueSelfTest.csproj',
    'tests/GhostFTP.QueueSelfTest/Program.cs',
    'tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj',
    'tests/GhostFTP.UiSmoke/Program.cs'
)
foreach ($required in $requiredFiles) {
    if (!(Test-Path (Join-Path $root $required) -PathType Leaf)) { throw "Required Ghost FTP source/asset is missing: $required" }
}

$releaseNotes = Join-Path $root "docs/releases/v$version.md"
if (!(Test-Path $releaseNotes -PathType Leaf)) {
    throw "Current version $version must have detailed release notes at docs/releases/v$version.md."
}
$releaseText = Get-Content $releaseNotes -Raw
if ($releaseText -notmatch [regex]::Escape("Ghost FTP $version")) {
    throw "Current release notes must identify Ghost FTP $version."
}

$readme = Get-Content (Join-Path $root 'README.md') -Raw
if ($readme -notmatch [regex]::Escape('assets/readme/ghostftp-hero.svg')) {
    throw 'README.md must reference the official Ghost FTP hero asset.'
}
if ($readme -notmatch [regex]::Escape("Current source version: **$version**")) {
    throw "README.md current source version must be synchronized to $version."
}
if ($readme -notmatch [regex]::Escape('docs/PLATFORM-SUPPORT.md')) {
    throw 'README.md must link the authoritative platform-support contract.'
}

$privacy = Get-Content (Join-Path $root 'PRIVACY.md') -Raw
if ($privacy -notmatch 'keepalive' -or $privacy -notmatch 'NOOP' -or $privacy -notmatch '0.*disable') {
    throw 'PRIVACY.md must document configurable server-only keepalive behavior and the disable option.'
}
$security = Get-Content (Join-Path $root 'SECURITY.md') -Raw
if ($security -notmatch 'stale' -or $security -notmatch 'Keepalive' -or $security -notmatch '1.?8') {
    throw 'SECURITY.md must document keepalive stale-state handling and bounded transfer concurrency.'
}

$legacyUiDuplicates = @(
    'src/GhostFTP.App/UI/Theme.cs',
    'src/GhostFTP.App/UI/Win11.cs',
    'src/GhostFTP.Setup/Services/Win11Backdrop.cs'
)
foreach ($legacy in $legacyUiDuplicates) {
    if (Test-Path (Join-Path $root $legacy) -PathType Leaf) { throw "Legacy duplicated UI helper must not return: $legacy" }
}

$legacyHelperCalls = @('GhostTheme.Logo(', 'GhostTheme.ComboBox(')
foreach ($token in $legacyHelperCalls) {
    $matches = Get-ChildItem $src -Recurse -File -Filter *.cs | Select-String -SimpleMatch $token
    if ($matches) { throw "Obsolete shared UI helper reference found: $token" }
}

$fragileInputTemplates = @('RoundedTextBoxTemplate', 'RoundedPasswordBoxTemplate')
foreach ($token in $fragileInputTemplates) {
    $matches = Get-ChildItem $src -Recurse -File -Filter *.cs | Select-String -SimpleMatch $token
    if ($matches) { throw "Fragile editable-input template returned: $token" }
}

$appProject = Get-Content (Join-Path $root 'src/GhostFTP.App/GhostFTP.App.csproj') -Raw
$setupProject = Get-Content (Join-Path $root 'src/GhostFTP.Setup/GhostFTP.Setup.csproj') -Raw
if ($appProject -notmatch [regex]::Escape('GhostFTP.Design\GhostFTP.Design.csproj')) {
    throw 'GhostFTP.App must reference GhostFTP.Design.'
}
if ($appProject -notmatch '<TargetFramework>net10\.0-windows') {
    throw 'The current production GhostFTP.App target must remain an explicit Windows WPF target until a real Linux renderer exists.'
}
if ($setupProject -notmatch [regex]::Escape('GhostFTP.Design\GhostFTP.Design.csproj')) {
    throw 'GhostFTP.Setup must reference GhostFTP.Design.'
}
if ($setupProject -notmatch [regex]::Escape('GhostFTP.License.txt')) {
    throw 'GhostFTP.Setup must embed the repository LICENSE as GhostFTP.License.txt.'
}

$setupWindow = Get-Content (Join-Path $root 'src/GhostFTP.Setup/SetupWindow.cs') -Raw
if ($setupWindow -notmatch 'WizardStep' -or $setupWindow -notmatch 'AcceptLicenseTerms' -or $setupWindow -notmatch 'LicenseResourceName') {
    throw 'Ghost FTP Setup must retain the multi-step wizard and mandatory license acceptance flow.'
}
$installerService = Get-Content (Join-Path $root 'src/GhostFTP.Setup/Services/InstallerService.cs') -Raw
if ($installerService -match 'GhostFTP-Uninstall\.exe') {
    throw 'A separate uninstall executable must not be generated.'
}
if ($installerService -notmatch 'InstalledSetupPath' -or $installerService -notmatch '--uninstall') {
    throw 'Installed Apps uninstall must use the installed GhostFTP-Setup.exe with --uninstall.'
}
if ($installerService -notmatch 'MoveFileDelayUntilReboot' -or $installerService -notmatch 'for /l %i in \(1,1,600\)') {
    throw 'Setup self-removal must retain delete-on-reboot fallback and bounded delayed retry cleanup.'
}

$brandSource = Get-Content (Join-Path $root 'src/GhostFTP.Design/GhostBrand.cs') -Raw
foreach ($requiredBrandText in @('BRENDIGO LTD','16545639','71–75 Shelton Street','https://ghostftp.com','https://brendigo.com')) {
    if ($brandSource -notmatch [regex]::Escape($requiredBrandText)) { throw "Publisher/product identity is incomplete: $requiredBrandText" }
}
if ($brandSource -notmatch 'PublisherWebsite') {
    throw 'GhostBrand must expose the BRENDIGO LTD publisher website explicitly.'
}

$targets = Get-Content (Join-Path $root 'Directory.Build.targets') -Raw
if ($targets -notmatch 'ApplicationIcon' -or $targets -notmatch 'generate-ghostftp-icon.ps1') {
    throw 'Ghost FTP executable icon generation must remain connected to the build.'
}

# Reliability architecture introduced in 1.6.0 is intentionally explicit so later UI work
# cannot silently remove its privacy/safety properties.
$settingsSource = Get-Content (Join-Path $root 'src/GhostFTP.App/Services/AppSettings.cs') -Raw
if ($settingsSource -notmatch 'ConcurrentTransfers' -or $settingsSource -notmatch 'KeepAliveSeconds') {
    throw 'Ghost FTP settings must retain bounded concurrency and configurable keepalive values.'
}
$sessionInterface = Get-Content (Join-Path $root 'src/GhostFTP.Core/Protocol/IFtpSession.cs') -Raw
$diagnosticsSource = Get-Content (Join-Path $root 'src/GhostFTP.Core/Protocol/FtpSession.Diagnostics.cs') -Raw
if ($sessionInterface -notmatch 'KeepAliveAsync' -or $diagnosticsSource -notmatch 'NOOP' -or $diagnosticsSource -notmatch 'ResetTransportAsync') {
    throw 'FTP keepalive must remain an explicit server-only NOOP contract with stale-transport reset.'
}
$transferModel = Get-Content (Join-Path $root 'src/GhostFTP.Core/Models/TransferJob.cs') -Raw
if ($transferModel -notmatch 'TransferredText' -or $transferModel -notmatch 'EtaText') {
    throw 'Transfer observability model must retain byte-summary and ETA state.'
}
$helpers = Get-Content (Join-Path $root 'src/GhostFTP.App/UI/MainWindow.Helpers.cs') -Raw
if ($helpers -notmatch 'queueActive' -or $helpers -notmatch 'CancelSelectedTransfer') {
    throw 'Keyboard routing must retain queue-focus isolation for destructive actions.'
}

# Ban actual contiguous legacy product identifiers. Do not ban ordinary language such as
# "created by FTP/FTPS actions", which is not a product identity.
$forbiddenProductTokens = @(
    ('My' + 'FTP'),
    ('By' + 'FTP')
)
$textExtensions = @('.cs','.csproj','.props','.targets','.md','.txt','.yml','.yaml','.json','.xml','.ps1','.bat','.svg')
$scanFiles = Get-ChildItem $root -Recurse -File | Where-Object {
    $relative = $_.FullName.Substring($root.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    $textExtensions -contains $_.Extension.ToLowerInvariant() -and
    $relative -notmatch '^(\.git|bin|obj|artifacts|release)([\\/]|$)'
}
foreach ($file in $scanFiles) {
    if ($file.FullName -eq $MyInvocation.MyCommand.Path) { continue }
    $relative = $file.FullName.Substring($root.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    foreach ($token in $forbiddenProductTokens) {
        if ($relative.IndexOf($token, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Non-Ghost FTP product identity found in repository path: $relative"
        }
        $matches = Select-String -Path $file.FullName -SimpleMatch -Pattern $token
        if ($matches) {
            $matches | ForEach-Object { Write-Error "Non-Ghost FTP product identity found: $($_.Path):$($_.LineNumber)" }
            exit 1
        }
    }
}

Write-Host "Source audit passed for Ghost FTP ${version}: BRENDIGO LTD identity, Ghost FTP-only product naming, C#-only source, zero PackageReference entries, no known telemetry/tracking SDKs, no Android/iOS shipping targets, platform-neutral net10.0 core, explicit Windows WPF production target, configurable server-only keepalive with stale-state reset, bounded parallel transfer settings, focus-safe destructive shortcuts, transfer observability tests, native editable inputs, embedded-license Setup, same-Setup uninstall, detailed current release notes and synchronized version metadata."
