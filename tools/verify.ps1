param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$program = Get-WinFormsSourcePath
$csc = Join-Path $env:windir 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$tempDir = Join-Path ([IO.Path]::GetTempPath()) ('wt-protector-verify-' + [Guid]::NewGuid().ToString('N'))
$out = Join-Path $tempDir 'WT Launcher.verify.exe'
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check([string]$Name, [object]$Detail) {
    $checks.Add([pscustomobject]@{ name = $Name; detail = $Detail }) | Out-Null
}

try {
    foreach ($forbiddenDir in @('WT Launcher', 'WT_Shortcuts_Backup')) {
        $path = Join-Path $root $forbiddenDir
        if (Test-Path -LiteralPath $path) {
            throw "Retired directory must not remain: $path"
        }
    }
    Add-Check 'retired-directories-removed' @{ directories = @('WT Launcher', 'WT_Shortcuts_Backup') }

    if (-not (Test-Path -LiteralPath $program)) { throw "Missing Program.cs: $program" }
    if (-not (Test-Path -LiteralPath $csc)) { throw "Missing csc.exe: $csc" }

    $source = Get-Content -Raw -Encoding UTF8 -LiteralPath $program
    foreach ($bad in @('Resize -= null', 'CheckedChanged -= null', 'ProcessStartInfo("wt")', 'string.Format("-d \"{0}\" pwsh -NoExit -Command \"{1}\"')) {
        if ($source.Contains($bad)) { throw "Forbidden stale pattern remains: $bad" }
    }
    foreach ($required in @('LayoutShell()', 'SelectAll_CheckedChanged', 'TryBuildWtArguments', 'QuoteArgument', 'ResolveConfigPath')) {
        if (-not $source.Contains($required)) { throw "Required safety helper missing: $required" }
    }
    Add-Check 'source-patterns' @{ path = 'src\winforms-launcher\Program.cs'; forbidden = 4; required = 5 }

    foreach ($required in @(
        'private bool _sidebarCollapsed = true',
        '_statsRow.Controls.AddRange(new Control[] { stat1, stat2 })',
        'TextRenderer.DrawText(g, item.directory',
        'TextRenderer.DrawText(g, item.command',
        'ClientSize = new Size(620, 460)'
    )) {
        if (-not $source.Contains($required)) { throw "HTA-derived UX guard missing: $required" }
    }
    if ($source.Contains('card.Location = new Point(card.Location.X')) {
        throw 'HTA-derived UX guard failed: hover must not shift card layout'
    }
    foreach ($bad in @('DrawString(item.directory', 'DrawString(item.command')) {
        if ($source.Contains($bad)) { throw "HTA-derived UX guard failed: card text must use bounded ellipsis rendering, found $bad" }
    }
    $ellipsisNoPrefixCount = [regex]::Matches($source, 'TextFormatFlags\.EndEllipsis \| TextFormatFlags\.SingleLine \| TextFormatFlags\.NoPrefix').Count
    if ($ellipsisNoPrefixCount -lt 2) {
        throw "HTA-derived UX guard failed: expected at least 2 bounded ellipsis path/command renderers, found $ellipsisNoPrefixCount"
    }
    Add-Check 'hta-derived-ux' @{
        defaultSidebarCollapsed = $true
        statCards = 2
        ellipsisCardTextRenderers = $ellipsisNoPrefixCount
        hoverLayoutShift = $false
        editDialog = '620x460'
    }

    $readmePath = Join-Path $root 'README.md'
    if (-not (Test-Path -LiteralPath $readmePath)) { throw 'Missing README.md' }
    $readme = Get-Content -Raw -Encoding UTF8 -LiteralPath $readmePath
    foreach ($section in @('Configuration schema', 'Build and verification', 'Config hardening helper', 'Shortcut backup retirement', 'Safety notes', 'Current implementation status')) {
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
    $shippedExe = Get-WinFormsExePath
    if (-not (Test-Path -LiteralPath $shippedExe)) { throw "Missing shipped exe: $shippedExe" }
    $programItem = Get-Item -LiteralPath $program
    $exeItem = Get-Item -LiteralPath $shippedExe
    if ($exeItem.LastWriteTimeUtc -lt $programItem.LastWriteTimeUtc) {
        throw "Shipped exe is older than Program.cs; run tools\build-release.ps1"
    }
    Add-Check 'shipped-exe' @{
        path = 'WT Launcher.exe'
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

    foreach ($forbiddenConfig in @(
        (Join-Path $root 'config.json'),
        (Join-Path $root 'retired\config.json'),
        (Join-Path $root 'WT Launcher\config.json'),
        (Join-Path $root 'WT Launcher\config\config.json')
    )) {
        if (Test-Path -LiteralPath $forbiddenConfig) {
            throw "Config must live under an active config directory, but legacy file exists: $forbiddenConfig"
        }
    }

    $configs = @{}
    $rootConfigPath = Get-LauncherConfigPath
    $canonicalKey = 'config\config.json'
    $configs[$canonicalKey] = @(Read-ConfigItems $rootConfigPath)
    Add-Check 'json' @{ path = $canonicalKey; items = $configs[$canonicalKey].Count }

    $seenIds = @{}
    $idx = 0
    foreach ($item in $configs[$canonicalKey]) {
        foreach ($field in @('id', 'name', 'directory', 'command')) {
            if ([string]::IsNullOrWhiteSpace([string]$item.$field)) {
                throw "Config item $idx is missing field: $field"
            }
        }
        if ($seenIds.ContainsKey([string]$item.id)) {
            throw "Duplicate config id: $($item.id)"
        }
        $seenIds[[string]$item.id] = $true
        $idx++
    }
    Add-Check 'config-schema' @{ uniqueIds = $seenIds.Count; selectedCapable = $true }

    $settingsPath = Get-LauncherSettingsPath
    if (-not (Test-Path -LiteralPath $settingsPath)) { throw "Missing portable settings: $settingsPath" }
    $settings = Get-Content -Raw -Encoding UTF8 -LiteralPath $settingsPath | ConvertFrom-Json
    if ($null -eq $settings) { throw "Settings parsed to null: $settingsPath" }
    Add-Check 'settings-json' @{
        path = 'config\settings.json'
        theme = $settings.theme
        hasWindow = $null -ne $settings.window
    }

    $rootBackupPath = $rootConfigPath + '.bak'
    if (Test-Path -LiteralPath $rootBackupPath) {
        $configs['config\config.json.bak'] = @(Read-ConfigItems $rootBackupPath)
        Add-Check 'json' @{ path = 'config\config.json.bak'; items = $configs['config\config.json.bak'].Count }
    } else {
        Add-Check 'json' @{ path = 'config\config.json.bak'; optional = $true; present = $false }
    }

    Add-Check 'config-layout' @{
        canonical = $canonicalKey
        canonicalItems = $configs[$canonicalKey].Count
        canonicalHash = Get-ShortHash $rootConfigPath
        settings = 'config\settings.json'
        settingsHash = Get-ShortHash $settingsPath
        source = 'src\winforms-launcher\Program.cs'
        executable = 'WT Launcher.exe'
        removedExportDirectory = $true
    }

    $dangerous = New-Object System.Collections.Generic.List[object]
    $idx = 0
    foreach ($item in $configs[$canonicalKey]) {
        $command = [string]$item.command
        if ($command -match 'dangerously' -and $item.confirm -eq $false) {
            $dangerous.Add([pscustomobject]@{
                path = $canonicalKey
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
    $rootHashBeforeHarden = Get-FileSha256 $rootConfigPath
    $hardenJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $hardenScript
    $harden = $hardenJson | ConvertFrom-Json
    $rootHashAfterHarden = Get-FileSha256 $rootConfigPath
    if ($harden.status -ne 'PASS') { throw 'harden-config dry-run did not pass' }
    if ($harden.mode -ne 'dry-run') { throw 'harden-config verifier call was not dry-run' }
    if ($harden.applied) { throw 'harden-config dry-run unexpectedly applied changes' }
    if ($rootHashBeforeHarden -ne $rootHashAfterHarden) {
        throw 'harden-config dry-run changed canonical config hash'
    }
    Add-Check 'config-harden-dry-run' @{
        wouldChange = [bool]$harden.wouldChange
        count = $harden.count
        rootHash = $harden.rootHash
    }
    if ($harden.wouldChange -or ($harden.count -gt 0)) {
        throw "harden-config dry-run reports outstanding changes: wouldChange=$($harden.wouldChange) count=$($harden.count)"
    }

    $manifestPath = Get-ShortcutBackupManifestPath
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Missing shortcut backup manifest: $manifestPath" }
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $shortcutCount = @($manifest.shortcuts).Count
    $convertedCount = @($manifest.convertedConfigIds).Count
    if ($shortcutCount -lt 1) { throw 'shortcut backup manifest has no shortcuts' }
    if ($convertedCount -lt 1) { throw 'shortcut backup manifest did not record any converted config ids' }
    foreach ($id in @($manifest.convertedConfigIds)) {
        if (-not $seenIds.ContainsKey([string]$id)) { throw "Converted shortcut id missing from active config: $id" }
    }
    Add-Check 'shortcut-backup-retirement' @{
        manifest = 'docs\reference\shortcut-backup-manifest.json'
        shortcuts = $shortcutCount
        convertedConfigIds = $convertedCount
        sourceDirectoryRemoved = -not (Test-Path -LiteralPath (Join-Path $root 'WT_Shortcuts_Backup'))
    }

    $activeHta = Join-Path $root 'WT Launcher.hta'
    if (Test-Path -LiteralPath $activeHta) {
        throw "Retired HTA must not remain on the active root path: $activeHta"
    }
    $hta = Join-Path $root 'retired\WT Launcher.hta'
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

    $retiredConfigPath = Get-RetiredConfigPath
    if (-not (Test-Path -LiteralPath $retiredConfigPath)) {
        throw "Missing retired HTA-local config: $retiredConfigPath"
    }
    $retiredItems = @(Read-ConfigItems $retiredConfigPath)
    $retiredDangerous = New-Object System.Collections.Generic.List[object]
    $retiredIdx = 0
    foreach ($item in $retiredItems) {
        $command = [string]$item.command
        if ($command -match 'dangerously' -and $item.confirm -eq $false) {
            $retiredDangerous.Add([pscustomobject]@{
                path = 'retired\config\config.json'
                index = $retiredIdx
                name = $item.name
                command = $command
            }) | Out-Null
        }
        $retiredIdx++
    }
    Add-Check 'hta-retired-config' @{
        path = 'retired\config\config.json'
        items = $retiredItems.Count
        hash = Get-ShortHash $retiredConfigPath
        activeSource = $false
        dangerousUnconfirmed = $retiredDangerous.Count
    }
    if ($retiredDangerous.Count -gt 0) {
        throw "retired HTA config has unconfirmed dangerous commands: count=$($retiredDangerous.Count)"
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
