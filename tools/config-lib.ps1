function Get-RepoRoot {
    return (Split-Path -Parent $PSScriptRoot)
}

function Get-RepoPath([string]$RelativePath) {
    return (Join-Path (Get-RepoRoot) $RelativePath)
}

function Read-ConfigItems([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "Missing config: $Path" }
    $json = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if ($null -eq $json) { throw "Config parsed to null: $Path" }
    return @($json)
}

function Get-FileSha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Get-ShortHash([string]$Path) {
    $hash = Get-FileSha256 $Path
    if ($null -eq $hash) { return $null }
    return $hash.Substring(0, 16)
}

function Backup-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backup = "$Path.bak-$stamp"
    Copy-Item -LiteralPath $Path -Destination $backup -Force
    return $backup
}

function Save-ConfigItems([object[]]$Items, [string]$Path) {
    $Items | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -LiteralPath $Path
}
