param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$configPath = Get-RepoPath 'config.json'
$changed = New-Object System.Collections.Generic.List[object]
$backups = New-Object System.Collections.Generic.List[object]

$rel = Resolve-Path -LiteralPath $configPath -Relative
$items = @(Read-ConfigItems $configPath)
$pathChanged = $false
for ($i = 0; $i -lt $items.Count; $i++) {
    $item = $items[$i]
    $command = [string]$item.command
    if ($command -match 'dangerously' -and $item.confirm -eq $false) {
        $changed.Add([pscustomobject]@{
            path = $rel
            index = $i
            name = $item.name
            command = $command
        }) | Out-Null
        if ($Apply) {
            $item.confirm = $true
            $pathChanged = $true
        }
    }
}

if ($Apply -and $pathChanged) {
    $backup = Backup-File $configPath
    Save-ConfigItems $items $configPath
    $backups.Add([pscustomobject]@{
        path = $rel
        backup = $backup
    }) | Out-Null
}

[pscustomobject]@{
    status = 'PASS'
    mode = $(if ($Apply) { 'apply' } else { 'dry-run' })
    wouldChange = (-not $Apply) -and ($changed.Count -gt 0)
    applied = $Apply -and ($changed.Count -gt 0)
    count = $changed.Count
    entries = $changed
    backups = $backups
    rootHash = Get-ShortHash (Get-RepoPath 'config.json')
} | ConvertTo-Json -Depth 6
