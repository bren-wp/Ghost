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
    'Segment.Analytics', 'Mixpanel', 'PostHog', 'AppCenter', 'Crashlytics'
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

if ($version -ne $propsVersion) {
    throw "VERSION ($version) does not match Directory.Build.props Version ($propsVersion)."
}
$expectedAssembly = "$version.0"
if ($assemblyVersion -ne $expectedAssembly -or $fileVersion -ne $expectedAssembly) {
    throw "AssemblyVersion/FileVersion must both be $expectedAssembly."
}
if ($informationalVersion -ne $version) {
    throw "InformationalVersion must be $version."
}
foreach ($metadata in @($authors, $company, $product)) {
    if ($metadata -ne 'Ghost FTP') {
        throw 'Authors, Company and Product metadata must all use the Ghost FTP brand.'
    }
}

foreach ($manifest in @('src/GhostFTP.App/app.manifest','src/GhostFTP.Setup/app.manifest')) {
    $text = Get-Content (Join-Path $root $manifest) -Raw
    if ($text -notmatch [regex]::Escape("version=`"$expectedAssembly`"")) {
        throw "$manifest does not use assembly identity $expectedAssembly."
    }
}

# Shipping source is intentionally C# only. XAML or alternate-language application paths are
# explicit architecture changes and must never arrive as accidental dependencies.
$forbiddenSourceExtensions = @('*.xaml','*.axaml','*.go','*.rs','*.cpp','*.c','*.cc','*.java','*.kt','*.swift')
foreach ($pattern in $forbiddenSourceExtensions) {
    $matches = Get-ChildItem $src -Recurse -File -Filter $pattern
    if ($matches) {
        throw "Forbidden/non-C# source exists under src/: $($matches.FullName -join ', ')"
    }
}

$requiredBrandFiles = @(
    'src/GhostFTP.Design/GhostBrand.cs',
    'assets/brand/ghostftp-icon.svg',
    'assets/readme/ghostftp-hero.svg'
)
foreach ($required in $requiredBrandFiles) {
    if (!(Test-Path (Join-Path $root $required) -PathType Leaf)) {
        throw "Ghost FTP brand asset/source is missing: $required"
    }
}

$readme = Get-Content (Join-Path $root 'README.md') -Raw
if ($readme -notmatch [regex]::Escape('assets/readme/ghostftp-hero.svg')) {
    throw 'README.md must reference the official Ghost FTP hero asset.'
}

$requiredDesignFiles = @(
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostTheme.cs',
    'src/GhostFTP.Design/GhostWindowChrome.cs',
    'src/GhostFTP.Design/GhostBrand.cs'
)
foreach ($required in $requiredDesignFiles) {
    if (!(Test-Path (Join-Path $root $required) -PathType Leaf)) {
        throw "Shared design-system file is missing: $required"
    }
}

$legacyUiDuplicates = @(
    'src/GhostFTP.App/UI/Theme.cs',
    'src/GhostFTP.App/UI/Win11.cs',
    'src/GhostFTP.Setup/Services/Win11Backdrop.cs'
)
foreach ($legacy in $legacyUiDuplicates) {
    if (Test-Path (Join-Path $root $legacy) -PathType Leaf) {
        throw "Legacy duplicated UI helper must not return: $legacy"
    }
}

$appProject = Get-Content (Join-Path $root 'src/GhostFTP.App/GhostFTP.App.csproj') -Raw
$setupProject = Get-Content (Join-Path $root 'src/GhostFTP.Setup/GhostFTP.Setup.csproj') -Raw
if ($appProject -notmatch [regex]::Escape('GhostFTP.Design\GhostFTP.Design.csproj')) {
    throw 'GhostFTP.App must reference the shared GhostFTP.Design project.'
}
if ($setupProject -notmatch [regex]::Escape('GhostFTP.Design\GhostFTP.Design.csproj')) {
    throw 'GhostFTP.Setup must reference the shared GhostFTP.Design project.'
}

# Never allow the previously used alternate product/author identity back into current source,
# documentation, workflows or assets. Build the token in parts so this audit script does not
# flag itself while still testing the exact legacy string.
$legacyBrand = 'Bren' + 'digo'
$legacyDomain = 'bren' + 'digo.com'
$textExtensions = @('.cs','.csproj','.props','.targets','.md','.txt','.yml','.yaml','.json','.xml','.ps1','.bat','.svg')
$scanFiles = Get-ChildItem $root -Recurse -File | Where-Object {
    $relative = $_.FullName.Substring($root.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    $textExtensions -contains $_.Extension.ToLowerInvariant() -and
    $relative -notmatch '^(\.git|bin|obj|artifacts|release)([\\/]|$)'
}
foreach ($file in $scanFiles) {
    if ($file.FullName -eq $MyInvocation.MyCommand.Path) { continue }
    $matches = Select-String -Path $file.FullName -SimpleMatch -Pattern $legacyBrand,$legacyDomain
    if ($matches) {
        $matches | ForEach-Object { Write-Error "Non-Ghost FTP brand reference found: $($_.Path):$($_.LineNumber)" }
        exit 1
    }
}

Write-Host "Source audit passed for Ghost FTP ${version}: Ghost FTP-only branding, C#-only source, zero PackageReference entries, shared design system and required brand assets enforced, no known telemetry/tracking SDK references, version metadata synchronized."
