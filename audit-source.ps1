$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'

$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) {
    $packageRefs | ForEach-Object { Write-Error "Third-party/package dependency found: $($_.Path):$($_.LineNumber)" }
    exit 1
}

$forbiddenTelemetry = @(
    'ApplicationInsights', 'Sentry', 'TelemetryClient', 'GoogleAnalytics',
    'Segment.Analytics', 'Mixpanel', 'PostHog', 'AppCenter', 'Crashlytics',
    'Bugsnag', 'Rollbar', 'Amplitude', 'FirebaseAnalytics'
)
foreach ($token in $forbiddenTelemetry) {
    $matches = Get-ChildItem $src -Recurse -Filter *.cs | Select-String -SimpleMatch $token
    if ($matches) {
        $matches | ForEach-Object { Write-Error "Forbidden telemetry/tracking reference '$token': $($_.Path):$($_.LineNumber)" }
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

$requiredFiles = @(
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostTheme.cs',
    'src/GhostFTP.Design/GhostWindowChrome.cs',
    'src/GhostFTP.Design/GhostBrand.cs',
    'src/GhostFTP.Design/GhostComboBox.cs',
    'src/GhostFTP.Design/GhostLocalization.cs',
    'src/GhostFTP.Design/GhostSetupLocalization.cs',
    'assets/brand/ghostftp-icon.svg',
    'assets/readme/ghostftp-hero.svg',
    'Directory.Build.targets',
    'tools/generate-ghostftp-icon.ps1',
    'LICENSE',
    'tests/GhostFTP.UiSmoke/GhostFTP.UiSmoke.csproj',
    'tests/GhostFTP.UiSmoke/Program.cs'
)
foreach ($required in $requiredFiles) {
    if (!(Test-Path (Join-Path $root $required) -PathType Leaf)) { throw "Required Ghost FTP source/asset is missing: $required" }
}

$readme = Get-Content (Join-Path $root 'README.md') -Raw
if ($readme -notmatch [regex]::Escape('assets/readme/ghostftp-hero.svg')) {
    throw 'README.md must reference the official Ghost FTP hero asset.'
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

$brandSource = Get-Content (Join-Path $root 'src/GhostFTP.Design/GhostBrand.cs') -Raw
foreach ($requiredBrandText in @('BRENDIGO LTD','16545639','71–75 Shelton Street')) {
    if ($brandSource -notmatch [regex]::Escape($requiredBrandText)) { throw "Publisher identity is incomplete: $requiredBrandText" }
}

$targets = Get-Content (Join-Path $root 'Directory.Build.targets') -Raw
if ($targets -notmatch 'ApplicationIcon' -or $targets -notmatch 'generate-ghostftp-icon.ps1') {
    throw 'Ghost FTP executable icon generation must remain connected to the build.'
}

$forbiddenProductTokens = @(
    ('My' + 'FTP'),
    ('My' + ' FTP'),
    ('By' + 'FTP'),
    ('By' + ' FTP')
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

Write-Host "Source audit passed for Ghost FTP ${version}: BRENDIGO LTD publisher metadata, Ghost FTP-only product identity, C#-only source, zero PackageReference entries, no known telemetry/tracking SDKs, native editable inputs, embedded license wizard, same-Setup uninstall, shared localization/design/icon architecture and synchronized version metadata."
