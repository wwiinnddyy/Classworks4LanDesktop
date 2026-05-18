[CmdletBinding()]
param(
    [string]$FeedPath,
    [string]$CoreContractsProjectPath,
    [string]$PluginSdkProjectPath,
    [string]$PluginIsolationContractsProjectPath,
    [string]$SharedIpcProjectPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($FeedPath)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $FeedPath = Join-Path (Resolve-Path (Join-Path $scriptRoot "..")).Path "packages"
}

if ([string]::IsNullOrWhiteSpace($PluginSdkProjectPath)) {
    $PluginSdkProjectPath = (Resolve-Path "..\LanMountainDesktop\LanMountainDesktop.PluginSdk\LanMountainDesktop.PluginSdk.csproj").Path
}

if ([string]::IsNullOrWhiteSpace($CoreContractsProjectPath)) {
    $CoreContractsProjectPath = (Resolve-Path "..\LanMountainDesktop\LanMountainDesktop.Shared.Contracts\LanMountainDesktop.Shared.Contracts.csproj").Path
}

if ([string]::IsNullOrWhiteSpace($PluginIsolationContractsProjectPath)) {
    $PluginIsolationContractsProjectPath = (Resolve-Path "..\LanMountainDesktop\LanMountainDesktop.PluginIsolation.Contracts\LanMountainDesktop.PluginIsolation.Contracts.csproj").Path
}

if ([string]::IsNullOrWhiteSpace($SharedIpcProjectPath)) {
    $SharedIpcProjectPath = (Resolve-Path "..\LanMountainDesktop\LanMountainDesktop.Shared.IPC\LanMountainDesktop.Shared.IPC.csproj").Path
}

function Pack-Project([string]$ProjectPath, [string]$OutputDirectory) {
    if (-not (Test-Path $ProjectPath)) {
        throw "Project '$ProjectPath' was not found."
    }

    dotnet pack $ProjectPath -c Release -o $OutputDirectory -p:ContinuousIntegrationBuild=true | Out-Host
}

New-Item -ItemType Directory -Force -Path $FeedPath | Out-Null
Pack-Project -ProjectPath $CoreContractsProjectPath -OutputDirectory $FeedPath
Pack-Project -ProjectPath $PluginIsolationContractsProjectPath -OutputDirectory $FeedPath
Pack-Project -ProjectPath $SharedIpcProjectPath -OutputDirectory $FeedPath
Pack-Project -ProjectPath $PluginSdkProjectPath -OutputDirectory $FeedPath

Write-Host "Local package feed initialized at '$FeedPath'."
