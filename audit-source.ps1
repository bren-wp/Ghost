$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'

$packageRefs = Get-ChildItem $root -Recurse -Filter *.csproj | Select-String -Pattern '<PackageReference'
if ($packageRefs) {
    $packageRefs | ForEach-Object { Write-Error "Third-party/package dependency found: $($_.Path):$($_.LineNumber)" }
    exit 1
}

$forbidden = @(
    'ApplicationInsights', 'Sentry', 'TelemetryClient', 'GoogleAnalytics',
    'Segment.Analytics', 'Mixpanel', 'PostHog', 'AppCenter', 'Crashlytics'
)
foreach ($token in $forbidden) {
    $matches = Get-ChildItem $src -Recurse -Filter *.cs | Select-String -SimpleMatch $token
    if ($matches) {
        $matches | ForEach-Object { Write-Error "Forbidden telemetry/tracking reference '$token': $($_.Path):$($_.LineNumber)" }
        exit 1
    }
}

$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$props = [xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)
$propsVersion = [string]$props.Project.PropertyGroup.Version
$assemblyVersion = [string]$props.Project.PropertyGroup.AssemblyVersion
$fileVersion = [string]$props.Project.PropertyGroup.FileVersion
$informationalVersion = [string]$props.Project.PropertyGroup.InformationalVersion
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

foreach ($manifest in @('src/GhostFTP.App/app.manifest','src/GhostFTP.Setup/app.manifest')) {
    $text = Get-Content (Join-Path $root $manifest) -Raw
    if ($text -notmatch [regex]::Escape("version=`"$expectedAssembly`"")) {
        throw "$manifest does not use assembly identity $expectedAssembly."
    }
}

# GhostFTP Windows sources are intentionally C#-only. XAML and alternate-language UI/source
# files would create a second presentation/code path and must be introduced explicitly rather
# than slipping into the repository unnoticed.
$forbiddenSourceExtensions = @('*.xaml','*.axaml','*.go','*.rs','*.cpp','*.c','*.cc','*.java','*.kt','*.swift')
foreach ($pattern in $forbiddenSourceExtensions) {
    $matches = Get-ChildItem $src -Recurse -File -Filter $pattern
    if ($matches) {
        throw "Forbidden/non-C# source exists under src/: $($matches.FullName -join ', ')"
    }
}

$requiredDesignFiles = @(
    'src/GhostFTP.Design/GhostFTP.Design.csproj',
    'src/GhostFTP.Design/GhostTheme.cs',
    'src/GhostFTP.Design/GhostWindowChrome.cs'
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

Write-Host "Source audit passed for GhostFTP ${version}: C#-only source, zero PackageReference entries, shared design system enforced, no known telemetry/tracking SDK references, version metadata synchronized."
