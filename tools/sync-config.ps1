param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$source = Get-RepoPath 'config.json'
$target = Get-RepoPath 'WT Launcher\config.json'

$sourceItems = @(Read-ConfigItems $source)
$targetExistsBefore = Test-Path -LiteralPath $target
$targetItems = if ($targetExistsBefore) { @(Read-ConfigItems $target) } else { @() }
$sourceHash = Get-ShortHash $source
$targetHashBefore = Get-ShortHash $target
$inSyncBefore = $targetExistsBefore -and ($sourceHash -eq $targetHashBefore)
$backupPath = $null

if ($Apply -and -not $inSyncBefore) {
    $targetDir = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDir)) {
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    }
    if ($targetExistsBefore) {
        $backupPath = Backup-File $target
    }
    Copy-Item -LiteralPath $source -Destination $target -Force
}

$targetHashAfter = Get-ShortHash $target

[pscustomobject]@{
    status = 'PASS'
    mode = $(if ($Apply) { 'apply' } else { 'dry-run' })
    source = Resolve-Path -LiteralPath $source | ForEach-Object { $_.Path }
    target = $(if (Test-Path -LiteralPath $target) { Resolve-Path -LiteralPath $target | ForEach-Object { $_.Path } } else { $target })
    sourceItems = $sourceItems.Count
    targetExistsBefore = $targetExistsBefore
    targetItemsBefore = $targetItems.Count
    inSyncBefore = $inSyncBefore
    wouldCopy = (-not $Apply) -and (-not $inSyncBefore)
    applied = $Apply -and (-not $inSyncBefore)
    backupPath = $backupPath
    sourceHash = $sourceHash
    targetHashBefore = $targetHashBefore
    targetHashAfter = $targetHashAfter
} | ConvertTo-Json -Depth 4
