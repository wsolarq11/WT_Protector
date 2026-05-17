param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$source = Get-LauncherConfigPath
$target = Get-LegacyExportConfigPath
$settingsSource = Get-LauncherSettingsPath
$settingsTarget = Get-LegacyExportSettingsPath

$sourceItems = @(Read-ConfigItems $source)
$targetExistsBefore = Test-Path -LiteralPath $target
$targetItems = if ($targetExistsBefore) { @(Read-ConfigItems $target) } else { @() }
$sourceHash = Get-ShortHash $source
$targetHashBefore = Get-ShortHash $target
$settingsSourceHash = Get-ShortHash $settingsSource
$settingsTargetExistsBefore = Test-Path -LiteralPath $settingsTarget
$settingsTargetHashBefore = Get-ShortHash $settingsTarget
$configInSyncBefore = $targetExistsBefore -and ($sourceHash -eq $targetHashBefore)
$settingsInSyncBefore = $settingsTargetExistsBefore -and ($settingsSourceHash -eq $settingsTargetHashBefore)
$inSyncBefore = $configInSyncBefore -and $settingsInSyncBefore
$backupPath = $null
$settingsBackupPath = $null

if ($Apply -and -not $configInSyncBefore) {
    $targetDir = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDir)) {
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    }
    if ($targetExistsBefore) {
        $backupPath = Backup-File $target
    }
    Copy-Item -LiteralPath $source -Destination $target -Force
}

if ($Apply -and -not $settingsInSyncBefore) {
    $settingsTargetDir = Split-Path -Parent $settingsTarget
    if (-not (Test-Path -LiteralPath $settingsTargetDir)) {
        New-Item -ItemType Directory -Force -Path $settingsTargetDir | Out-Null
    }
    if ($settingsTargetExistsBefore) {
        $settingsBackupPath = Backup-File $settingsTarget
    }
    Copy-Item -LiteralPath $settingsSource -Destination $settingsTarget -Force
}

$targetHashAfter = Get-ShortHash $target
$settingsTargetHashAfter = Get-ShortHash $settingsTarget

[pscustomobject]@{
    status = 'PASS'
    mode = $(if ($Apply) { 'apply' } else { 'dry-run' })
    source = Resolve-Path -LiteralPath $source | ForEach-Object { $_.Path }
    target = $(if (Test-Path -LiteralPath $target) { Resolve-Path -LiteralPath $target | ForEach-Object { $_.Path } } else { $target })
    settingsSource = Resolve-Path -LiteralPath $settingsSource | ForEach-Object { $_.Path }
    settingsTarget = $(if (Test-Path -LiteralPath $settingsTarget) { Resolve-Path -LiteralPath $settingsTarget | ForEach-Object { $_.Path } } else { $settingsTarget })
    sourceItems = $sourceItems.Count
    targetExistsBefore = $targetExistsBefore
    targetItemsBefore = $targetItems.Count
    inSyncBefore = $inSyncBefore
    wouldCopy = (-not $Apply) -and (-not $inSyncBefore)
    applied = $Apply -and (-not $inSyncBefore)
    backupPath = $backupPath
    settingsBackupPath = $settingsBackupPath
    sourceHash = $sourceHash
    targetHashBefore = $targetHashBefore
    targetHashAfter = $targetHashAfter
    settingsSourceHash = $settingsSourceHash
    settingsTargetExistsBefore = $settingsTargetExistsBefore
    settingsTargetHashBefore = $settingsTargetHashBefore
    settingsTargetHashAfter = $settingsTargetHashAfter
} | ConvertTo-Json -Depth 4
