[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$PackagePath,
    [string]$MarketManifestPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
}

function Get-VersionCore([string]$Value) {
    $candidate = $Value.Trim()
    if ($candidate.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
        $candidate = $candidate.Substring(1)
    }

    $core = ($candidate -split '[-+ ]', 2)[0]
    $parsed = $null
    if (-not [Version]::TryParse($core, [ref]$parsed)) {
        throw "Invalid version '$Value'."
    }

    return $candidate
}

function Get-ManifestFromPackage([string]$ArchivePath) {
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        $entry = $archive.Entries | Where-Object { $_.FullName -eq "airapp.json" } | Select-Object -First 1
        if ($null -eq $entry) {
            throw "Plugin package '$ArchivePath' does not contain airapp.json."
        }

        $stream = $entry.Open()
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.UTF8Encoding]::UTF8, $true)
        try {
            return $reader.ReadToEnd() | ConvertFrom-Json
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

$csprojPath = Join-Path $RepositoryRoot "ClassworksPlugin.csproj"
$manifestPath = Join-Path $RepositoryRoot "airapp.json"

$csprojContent = [System.IO.File]::ReadAllText($csprojPath)
$csprojMatch = [System.Text.RegularExpressions.Regex]::Match(
    $csprojContent,
    "<Version>(?<version>.*?)</Version>",
    [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $csprojMatch.Success) {
    throw "Missing <Version> in '$csprojPath'."
}

if ($csprojContent -notmatch '<PackageReference\s+Include="LanMountainDesktop\.AirAppSdk"\s+Version="1\.0\.0"') {
    throw "ClassworksPlugin.csproj must reference LanMountainDesktop.AirAppSdk 1.0.0."
}

if ($csprojContent -match 'LanMountainDesktop\.PluginSdk') {
    throw "AirApps must not reference the retired LanMountainDesktop.PluginSdk."
}

$assetsPath = Join-Path $RepositoryRoot "obj\project.assets.json"
if ($PackagePath) {
    if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) {
        throw "project.assets.json was not found. Run restore with --force --no-cache before validating a package."
    }

    $assets = Get-Content -LiteralPath $assetsPath -Encoding UTF8 -Raw | ConvertFrom-Json
    $resolvedLibraries = @($assets.libraries.PSObject.Properties.Name)
    $requiredLibraries = @(
        'LanMountainDesktop.AirAppSdk/1.0.0',
        'Avalonia/12.1.0',
        'FluentAvaloniaUI/3.0.1',
        'FluentIcons.Avalonia/2.1.331'
    )
    foreach ($requiredLibrary in $requiredLibraries) {
        if ($resolvedLibraries -notcontains $requiredLibrary) {
            throw "project.assets.json did not resolve '$requiredLibrary'. Run restore with --force --no-cache."
        }
    }
}

$csprojVersion = Get-VersionCore $csprojMatch.Groups["version"].Value
$manifest = Get-Content $manifestPath -Encoding UTF8 -Raw | ConvertFrom-Json
$manifestVersion = Get-VersionCore $manifest.version
$manifestApiVersion = Get-VersionCore $manifest.apiVersion

if ($csprojVersion -ne $manifestVersion) {
    throw "Version mismatch. csproj=$csprojVersion airapp.json=$manifestVersion"
}

if ($manifest.id -ne "Classworks4LanDesktop") {
    throw "Plugin id mismatch. Expected Classworks4LanDesktop, actual=$($manifest.id)"
}

if ($manifest.entranceAssembly -ne "ClassworksPlugin.dll") {
    throw "Entrance assembly mismatch. Expected ClassworksPlugin.dll, actual=$($manifest.entranceAssembly)"
}

if ($manifestApiVersion -ne "1.0.0") {
    throw "API version mismatch. Expected airapp.json apiVersion=1.0.0, actual=$manifestApiVersion"
}

if ($manifest.runtime.mode -ne "in-proc") {
    throw "Runtime mode mismatch. Expected in-proc, actual=$($manifest.runtime.mode)"
}

if (Test-Path (Join-Path $RepositoryRoot "plugin.json")) {
    throw "Remove the retired plugin.json manifest. AirApps are described by airapp.json only."
}

$expectedAssetName = "$($manifest.id).$csprojVersion.laapp"

if ($PackagePath) {
    $resolvedPackagePath = Resolve-Path $PackagePath -ErrorAction Stop
    if ([System.IO.Path]::GetFileName($resolvedPackagePath) -ne $expectedAssetName) {
        throw "Package name mismatch. Expected '$expectedAssetName', actual '$([System.IO.Path]::GetFileName($resolvedPackagePath))'."
    }


    $packageManifest = Get-ManifestFromPackage -ArchivePath $resolvedPackagePath
    if ($packageManifest.id -ne $manifest.id -or
        $packageManifest.version -ne $manifest.version -or
        $packageManifest.apiVersion -ne $manifest.apiVersion -or
        $packageManifest.runtime.mode -ne $manifest.runtime.mode) {
        throw "Package manifest does not match repository airapp.json."
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
    try {
        foreach ($entry in $archive.Entries) {
            $entryName = $entry.FullName.Replace('\', '/')
            $leafName = [System.IO.Path]::GetFileName($entryName)
            if ($entryName.Contains("..")) {
                throw "Package contains unsafe entry '$entryName'."
            }
            if ($leafName -match '\.pdb$' -or
                $entryName -match '(^|/)(bin|obj|node_modules)/' -or
                $leafName -eq 'LanMountainDesktop.AirAppSdk.dll' -or
                $leafName -like 'Avalonia*.dll' -or
                $leafName -like 'FluentAvalonia*.dll' -or
                $leafName -like 'FluentIcons*.dll') {
                throw "Package contains forbidden host/development asset '$entryName'."
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

if ($MarketManifestPath) {
    $market = Get-Content -LiteralPath $MarketManifestPath -Encoding UTF8 -Raw | ConvertFrom-Json
    if ($market.schemaVersion -ne '2.0.0' -or
        $market.manifest.id -ne $manifest.id -or
        $market.manifest.version -ne $manifest.version -or
        $market.manifest.apiVersion -ne '1.0.0' -or
        $market.compatibility.minHostVersion -ne '0.8.6' -or
        $market.publication.releaseTag -ne "v$csprojVersion" -or
        $market.publication.releaseAssetName -ne $expectedAssetName) {
        throw "Market manifest does not match the plugin release metadata."
    }

    $sources = @($market.publication.packageSources)
    if ($sources.Count -ne 3 -or
        $sources[0].kind -ne 'releaseAsset' -or
        $sources[1].kind -ne 'rawFallback' -or
        $sources[2].kind -ne 'workspaceLocal' -or
        -not ([string]$sources[2].url).StartsWith('workspace://', [System.StringComparison]::Ordinal)) {
        throw "Market package sources must be releaseAsset -> rawFallback -> workspaceLocal, with a workspace:// URL."
    }
}

Write-Host "Plugin version: $csprojVersion"
Write-Host "Plugin API version: $manifestApiVersion"
Write-Host "Expected asset: $expectedAssetName"
