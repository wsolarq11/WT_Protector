param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$program = Join-Path $root 'WT Launcher\Program.cs'
$csc = Join-Path $env:windir 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$tempDir = Join-Path ([IO.Path]::GetTempPath()) ('wt-protector-verify-' + [Guid]::NewGuid().ToString('N'))
$out = Join-Path $tempDir 'WT Launcher.verify.exe'
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check([string]$Name, [object]$Detail) {
    $checks.Add([pscustomobject]@{ name = $Name; detail = $Detail }) | Out-Null
}

try {
    if (-not (Test-Path -LiteralPath $program)) { throw "Missing Program.cs: $program" }
    if (-not (Test-Path -LiteralPath $csc)) { throw "Missing csc.exe: $csc" }

    $source = Get-Content -Raw -Encoding UTF8 -LiteralPath $program
    foreach ($bad in @('Resize -= null', 'CheckedChanged -= null', 'ProcessStartInfo("wt")', 'string.Format("-d \"{0}\" pwsh -NoExit -Command \"{1}\"')) {
        if ($source.Contains($bad)) { throw "Forbidden stale pattern remains: $bad" }
    }
    foreach ($required in @('LayoutShell()', 'SelectAll_CheckedChanged', 'TryBuildWtArguments', 'QuoteArgument', 'ResolveConfigPath')) {
        if (-not $source.Contains($required)) { throw "Required safety helper missing: $required" }
    }
    Add-Check 'source-patterns' @{ forbidden = 4; required = 5 }

    $readmePath = Join-Path $root 'README.md'
    if (-not (Test-Path -LiteralPath $readmePath)) { throw 'Missing README.md' }
    $readme = Get-Content -Raw -Encoding UTF8 -LiteralPath $readmePath
    foreach ($section in @('Configuration schema', 'Build and verification', 'Config sync helper', 'Config hardening helper', 'Safety notes', 'Current implementation status')) {
        if (-not $readme.Contains($section)) { throw "README missing section: $section" }
    }
    Add-Check 'readme' @{ sections = 6 }

    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    & $csc /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /out:"$out" "$program" | Out-String | ForEach-Object {
        if ($_.Trim()) { Write-Host $_ }
    }
    if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }
    if (-not (Test-Path -LiteralPath $out)) { throw "Compile output missing: $out" }
    Add-Check 'compile' @{ outputBytes = (Get-Item -LiteralPath $out).Length }

    $buildScript = Join-Path $PSScriptRoot 'build-release.ps1'
    if (-not (Test-Path -LiteralPath $buildScript)) { throw 'Missing tools\build-release.ps1' }
    $shippedExe = Join-Path $root 'WT Launcher\WT Launcher.exe'
    if (-not (Test-Path -LiteralPath $shippedExe)) { throw "Missing shipped exe: $shippedExe" }
    $programItem = Get-Item -LiteralPath $program
    $exeItem = Get-Item -LiteralPath $shippedExe
    if ($exeItem.LastWriteTimeUtc -lt $programItem.LastWriteTimeUtc) {
        throw "Shipped exe is older than Program.cs; run tools\build-release.ps1"
    }
    Add-Check 'shipped-exe' @{
        bytes = $exeItem.Length
        sha256 = (Get-ShortHash $shippedExe)
        builtAt = $exeItem.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
    }

    $launchArgTest = Join-Path $PSScriptRoot 'test-launch-args.ps1'
    if (-not (Test-Path -LiteralPath $launchArgTest)) { throw 'Missing tools\test-launch-args.ps1' }
    $launchArgJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $launchArgTest
    $launchArg = $launchArgJson | ConvertFrom-Json
    if ($launchArg.status -ne 'PASS') { throw 'launch argument smoke test did not pass' }
    Add-Check 'launch-arg-smoke' @{
        tests = @($launchArg.tests).Count
    }

    $configs = @{}
    $rootConfigPath = Join-Path $root 'config.json'
    $configs['config.json'] = @(Read-ConfigItems $rootConfigPath)
    Add-Check 'json' @{ path = 'config.json'; items = $configs['config.json'].Count }
    $rootBackupPath = Join-Path $root 'config.json.bak'
    if (Test-Path -LiteralPath $rootBackupPath) {
        $configs['config.json.bak'] = @(Read-ConfigItems $rootBackupPath)
        Add-Check 'json' @{ path = 'config.json.bak'; items = $configs['config.json.bak'].Count }
    } else {
        Add-Check 'json' @{ path = 'config.json.bak'; optional = $true; present = $false }
    }

    $legacyConfigPath = Join-Path $root 'WT Launcher\config.json'
    $rootConfigHash = Get-ShortHash $rootConfigPath
    $legacyConfigExists = Test-Path -LiteralPath $legacyConfigPath
    $legacyConfigHash = Get-ShortHash $legacyConfigPath
    $legacyItems = @()
    if ($legacyConfigExists) {
        $legacyItems = @(Read-ConfigItems $legacyConfigPath)
    }
    Add-Check 'config-layout' @{
        canonical = 'config.json'
        canonicalItems = $configs['config.json'].Count
        canonicalHash = $rootConfigHash
        legacyExport = 'WT Launcher\config.json'
        legacyExportExists = $legacyConfigExists
        legacyExportItems = $legacyItems.Count
        legacyExportHash = $legacyConfigHash
        legacyExportInSync = $legacyConfigExists -and ($rootConfigHash -eq $legacyConfigHash)
    }

    $dangerous = New-Object System.Collections.Generic.List[object]
    $idx = 0
    foreach ($item in $configs['config.json']) {
        $command = [string]$item.command
        if ($command -match 'dangerously' -and $item.confirm -eq $false) {
            $dangerous.Add([pscustomobject]@{
                path = 'config.json'
                index = $idx
                name = $item.name
                command = $command
            }) | Out-Null
        }
        $idx++
    }
    Add-Check 'dangerous-command-audit' @{
        count = $dangerous.Count
        entries = $dangerous
    }
    if ($dangerous.Count -gt 0) {
        throw "dangerous commands still require hardening: count=$($dangerous.Count)"
    }

    $hardenScript = Join-Path $PSScriptRoot 'harden-config.ps1'
    if (-not (Test-Path -LiteralPath $hardenScript)) { throw 'Missing tools\harden-config.ps1' }
    $rootHashBeforeHarden = Get-FileSha256 (Join-Path $root 'config.json')
    $hardenJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $hardenScript
    $harden = $hardenJson | ConvertFrom-Json
    $rootHashAfterHarden = Get-FileSha256 (Join-Path $root 'config.json')
    if ($harden.status -ne 'PASS') { throw 'harden-config dry-run did not pass' }
    if ($harden.mode -ne 'dry-run') { throw 'harden-config verifier call was not dry-run' }
    if ($harden.applied) { throw 'harden-config dry-run unexpectedly applied changes' }
    if ($rootHashBeforeHarden -ne $rootHashAfterHarden) {
        throw 'harden-config dry-run changed root config hash'
    }
    Add-Check 'config-harden-dry-run' @{
        wouldChange = [bool]$harden.wouldChange
        count = $harden.count
        rootHash = $harden.rootHash
    }
    if ($harden.wouldChange -or ($harden.count -gt 0)) {
        throw "harden-config dry-run reports outstanding changes: wouldChange=$($harden.wouldChange) count=$($harden.count)"
    }

    $syncScript = Join-Path $PSScriptRoot 'sync-config.ps1'
    if (-not (Test-Path -LiteralPath $syncScript)) { throw 'Missing tools\sync-config.ps1' }
    $syncJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $syncScript
    $sync = $syncJson | ConvertFrom-Json
    if ($sync.status -ne 'PASS') { throw 'sync-config dry-run did not pass' }
    if ($sync.mode -ne 'dry-run') { throw 'sync-config verifier call was not dry-run' }
    if ($sync.applied) { throw 'sync-config dry-run unexpectedly applied changes' }
    Add-Check 'config-sync-dry-run' @{
        wouldCopy = [bool]$sync.wouldCopy
        sourceItems = $sync.sourceItems
        targetExistsBefore = $sync.targetExistsBefore
        targetItemsBefore = $sync.targetItemsBefore
        targetHashBefore = $sync.targetHashBefore
        targetHashAfter = $sync.targetHashAfter
    }

    $activeHta = Join-Path $root 'WT Launcher.hta'
    if (Test-Path -LiteralPath $activeHta) {
        throw "Retired HTA must not remain on the active root path: $activeHta"
    }
    $hta = Join-Path $root 'retired\WT Launcher.hta'
    $htaBackup = Join-Path $root 'WT_Shortcuts_Backup\WT Launcher.hta'
    if (-not (Test-Path -LiteralPath $hta)) {
        throw "Missing retired HTA archive: $hta"
    }
    $htaSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $hta
    foreach ($required in @('function quoteArg', 'function buildWtCommand')) {
        if (-not $htaSource.Contains($required)) { throw "Retired HTA required helper missing: $required" }
    }
    $buildCalls = [regex]::Matches($htaSource, 'buildWtCommand\(b\)').Count
    if ($buildCalls -lt 2) { throw "Retired HTA launch paths do not both use buildWtCommand; count=$buildCalls" }
    Add-Check 'hta-retired-archive' @{
        path = 'retired\WT Launcher.hta'
        activeRootPresent = $false
        buildWtCommandCalls = $buildCalls
    }
    if ((Test-Path -LiteralPath $hta) -and (Test-Path -LiteralPath $htaBackup)) {
        $h1 = Get-FileSha256 $hta
        $h2 = Get-FileSha256 $htaBackup
        if ($h1 -ne $h2) { throw 'HTA backup drift detected' }
        Add-Check 'hta-backup' @{ retiredSha256 = $h1.Substring(0, 16) }
    }

    [pscustomobject]@{
        status = 'PASS'
        root = $root
        checks = $checks
    } | ConvertTo-Json -Depth 6
}
finally {
    if (Test-Path -LiteralPath $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
