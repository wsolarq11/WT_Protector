
Set-StrictMode -Version Latest

function Get-RepoRoot {
    return (Split-Path -Parent $PSScriptRoot)
}

function Invoke-NativeStep([string]$Name, [scriptblock]$Command) {
    & $Command
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "$Name failed with exit code $code" }
}

function Invoke-RepoGit([string[]]$GitArgs) {
    try {
        $output = & git -C (Get-RepoRoot) @GitArgs 2>$null
        if ($LASTEXITCODE -eq 0) { return @($output) }
    } catch {}
    return @()
}

function New-ToolReceipt([string]$Kind, [hashtable]$Fields = @{}) {
    $head = Invoke-RepoGit @('rev-parse', '--short', 'HEAD') | Select-Object -First 1
    $status = @(Invoke-RepoGit @('status', '--short') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $receipt = [ordered]@{
        schemaVersion = 1
        kind = $Kind
        status = 'PASS'
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        repoHead = $head
        worktreeDirty = $status.Count -gt 0
        worktreeStatus = $status
    }

    foreach ($field in $Fields.GetEnumerator()) {
        if ($receipt.Contains($field.Key)) { throw "Receipt field conflicts with built-in field: $($field.Key)" }
        $receipt[$field.Key] = $field.Value
    }

    return [pscustomobject]$receipt
}

function Get-RepoPath([string]$RelativePath) {
    return (Join-Path (Get-RepoRoot) $RelativePath)
}

function Get-AvaloniaRoot {
    return (Get-RepoPath 'src\avalonia-launcher')
}

function Get-AvaloniaSolutionPath {
    return (Join-Path (Get-AvaloniaRoot) 'WTLauncher.Avalonia.sln')
}

function Get-AvaloniaAppProjectPath {
    return (Join-Path (Get-AvaloniaRoot) 'src\WTLauncher.App\WTLauncher.App.csproj')
}

function Get-AvaloniaTestsProjectPath {
    return (Join-Path (Get-AvaloniaRoot) 'tests\WTLauncher.Tests\WTLauncher.Tests.csproj')
}

function Get-AvaloniaPublishDir {
    return (Join-Path (Get-AvaloniaRoot) 'artifacts\publish\win-x64')
}

function Get-LauncherStorePath {
    return (Get-RepoPath 'config\store.json')
}

function Get-LauncherConfigPath {
    return (Get-RepoPath 'config\config.json')
}

function Get-LauncherSettingsPath {
    return (Get-RepoPath 'config\settings.json')
}

function Get-RetiredConfigPath {
    return (Get-RepoPath 'retired\config\config.json')
}

function Get-ShortcutBackupManifestPath {
    return (Get-RepoPath 'docs\reference\shortcut-backup-manifest.json')
}

function Get-WinFormsRetiredDesignPath {
    return (Get-RepoPath 'retired\design\winforms-launcher.md')
}

function Read-ConfigItems([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "Missing config: $Path" }
    $json = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    if ($null -eq $json) { throw "Config parsed to null: $Path" }
    # Support both wrapper format {"schemaVersion":1,"entries":[...]} and legacy array format.
    $items = if ($null -ne $json.entries) { $json.entries } else { $json }
    $idx = 0
    foreach ($item in @($items)) {
        foreach ($field in @('id', 'name', 'directory', 'command')) {
            if ([string]::IsNullOrWhiteSpace([string]$item.$field)) {
                throw "Config item $idx is missing required field: $field"
            }
        }
        $idx++
    }
    return @($items)
}

function Get-FileSha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    if (Get-Command Get-FileHash -ErrorAction SilentlyContinue) { return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash }
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try { return -join ($sha256.ComputeHash([System.IO.File]::ReadAllBytes($Path)) | ForEach-Object { $_.ToString('X2') }) }
    finally { $sha256.Dispose() }
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
    $json = $Items | ConvertTo-Json -Depth 6
    # Use UTF8Encoding($false) for BOM-free UTF-8 — Set-Content -Encoding UTF8 writes
    # BOM on WinPS 5.1 but no BOM on PS7+, causing cross-edition inconsistency.
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}
