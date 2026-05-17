
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check([string]$Name, [object]$Detail) {
    $checks.Add([pscustomobject]@{ name = $Name; detail = $Detail }) | Out-Null
}

foreach ($forbidden in @('spikes', 'src\winforms-launcher', 'WT Launcher.exe', 'WT Launcher')) {
    $path = Join-Path $root $forbidden
    if (Test-Path -LiteralPath $path) { throw "Retired path must not remain: $path" }
}
Add-Check 'retired-paths-removed' @{ paths = @('spikes', 'src\winforms-launcher', 'WT Launcher.exe', 'WT Launcher') }

$sourceRoot = Get-AvaloniaRoot
$solution = Get-AvaloniaSolutionPath
$app = Get-AvaloniaAppProjectPath
$tests = Get-AvaloniaTestsProjectPath
foreach ($required in @($sourceRoot, $solution, $app, $tests)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing Avalonia mainline surface: $required" }
}
Add-Check 'mainline-layout' @{
    sourceRoot = 'src\avalonia-launcher'
    solution = 'src\avalonia-launcher\WTLauncher.Avalonia.sln'
    app = 'src\avalonia-launcher\src\WTLauncher.App\WTLauncher.App.csproj'
    tests = 'src\avalonia-launcher\tests\WTLauncher.Tests\WTLauncher.Tests.csproj'
}

$readmePath = Join-Path $root 'README.md'
if (-not (Test-Path -LiteralPath $readmePath)) { throw 'Missing README.md' }
$readme = Get-Content -Raw -Encoding UTF8 -LiteralPath $readmePath
foreach ($section in @('Configuration schema', 'Build and verification', 'Run the mainline app', 'WinForms retirement', 'Safety notes', 'Current implementation status')) {
    if (-not $readme.Contains($section)) { throw "README missing section: $section" }
}
Add-Check 'readme' @{ sections = 6 }

$configs = @{}
$rootConfigPath = Get-LauncherConfigPath
$canonicalKey = 'config\config.json'
$configs[$canonicalKey] = @(Read-ConfigItems $rootConfigPath)
Add-Check 'json' @{ path = $canonicalKey; items = $configs[$canonicalKey].Count }

$seenIds = @{}
$selectedFields = 0
$idx = 0
foreach ($item in $configs[$canonicalKey]) {
    foreach ($field in @('id', 'name', 'directory', 'command')) {
        if ([string]::IsNullOrWhiteSpace([string]$item.$field)) {
            throw "Config item $idx is missing field: $field"
        }
    }
    if (-not ($item.PSObject.Properties.Name -contains 'selected')) {
        throw "Config item $idx is missing persistent selected field"
    }
    if (-not ($item.selected -is [bool])) {
        throw "Config item $idx selected field must be boolean"
    }
    $selectedFields++
    if ($seenIds.ContainsKey([string]$item.id)) { throw "Duplicate config id: $($item.id)" }
    $seenIds[[string]$item.id] = $true
    $idx++
}
Add-Check 'config-schema' @{ uniqueIds = $seenIds.Count; selectedFields = $selectedFields; selectedCapable = ($selectedFields -eq $configs[$canonicalKey].Count) }

$settingsPath = Get-LauncherSettingsPath
if (-not (Test-Path -LiteralPath $settingsPath)) { throw "Missing portable settings: $settingsPath" }
$settings = Get-Content -Raw -Encoding UTF8 -LiteralPath $settingsPath | ConvertFrom-Json
if ($null -eq $settings) { throw "Settings parsed to null: $settingsPath" }
Add-Check 'settings-json' @{
    path = 'config\settings.json'
    theme = $settings.theme
    hasWindow = $null -ne $settings.window
}

$dangerous = New-Object System.Collections.Generic.List[object]
$idx = 0
foreach ($item in $configs[$canonicalKey]) {
    $command = [string]$item.command
    if (($command -match 'dangerously') -and ($item.confirm -eq $false)) {
        $dangerous.Add([pscustomobject]@{
            path = $canonicalKey
            index = $idx
            name = $item.name
            command = $command
        }) | Out-Null
    }
    $idx++
}
Add-Check 'dangerous-command-audit' @{ count = $dangerous.Count; entries = $dangerous }
if ($dangerous.Count -gt 0) { throw "dangerous commands still require hardening: count=$($dangerous.Count)" }

$hardenScript = Join-Path $PSScriptRoot 'harden-config.ps1'
if (-not (Test-Path -LiteralPath $hardenScript)) { throw 'Missing tools\harden-config.ps1' }
$rootHashBeforeHarden = Get-FileSha256 $rootConfigPath
$hardenJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $hardenScript
$harden = $hardenJson | ConvertFrom-Json
$rootHashAfterHarden = Get-FileSha256 $rootConfigPath
if ($harden.status -ne 'PASS') { throw 'harden-config dry-run did not pass' }
if ($harden.mode -ne 'dry-run') { throw 'harden-config verifier call was not dry-run' }
if ($harden.applied) { throw 'harden-config dry-run unexpectedly applied changes' }
if ($rootHashBeforeHarden -ne $rootHashAfterHarden) { throw 'harden-config dry-run changed canonical config hash' }
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

$retiredDesign = Get-WinFormsRetiredDesignPath
if (-not (Test-Path -LiteralPath $retiredDesign)) { throw "Missing WinForms retirement design: $retiredDesign" }
$retiredDesignText = Get-Content -Raw -Encoding UTF8 -LiteralPath $retiredDesign
foreach ($required in @('Retired WinForms launcher design', 'Portable config discovery', 'Launch safety boundary', 'Successor directive')) {
    if (-not $retiredDesignText.Contains($required)) { throw "WinForms retirement design missing section: $required" }
}
Add-Check 'winforms-retired-design' @{ path = 'retired\design\winforms-launcher.md' }

$hta = Join-Path $root 'retired\WT Launcher.hta'
if (-not (Test-Path -LiteralPath $hta)) { throw "Missing retired HTA archive: $hta" }
$htaSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $hta
foreach ($required in @('function quoteArg', 'function buildWtCommand')) {
    if (-not $htaSource.Contains($required)) { throw "Retired HTA required helper missing: $required" }
}
$buildCalls = [regex]::Matches($htaSource, 'buildWtCommand\(b\)').Count
if ($buildCalls -lt 2) { throw "Retired HTA launch paths do not both use buildWtCommand; count=$buildCalls" }
Add-Check 'hta-retired-archive' @{ path = 'retired\WT Launcher.hta'; buildWtCommandCalls = $buildCalls }

$retiredConfigPath = Get-RetiredConfigPath
if (-not (Test-Path -LiteralPath $retiredConfigPath)) { throw "Missing retired HTA-local config: $retiredConfigPath" }
$retiredItems = @(Read-ConfigItems $retiredConfigPath)
$retiredDangerous = New-Object System.Collections.Generic.List[object]
$retiredIdx = 0
foreach ($item in $retiredItems) {
    $command = [string]$item.command
    if (($command -match 'dangerously') -and ($item.confirm -eq $false)) {
        $retiredDangerous.Add([pscustomobject]@{ path = 'retired\config\config.json'; index = $retiredIdx; name = $item.name; command = $command }) | Out-Null
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
if ($retiredDangerous.Count -gt 0) { throw "retired HTA config has unconfirmed dangerous commands: count=$($retiredDangerous.Count)" }

$launchArgTest = Join-Path $PSScriptRoot 'test-launch-args.ps1'
if (-not (Test-Path -LiteralPath $launchArgTest)) { throw 'Missing tools\test-launch-args.ps1' }
$launchArgJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $launchArgTest
$launchArg = ($launchArgJson | Select-Object -Last 1) | ConvertFrom-Json
if ($launchArg.status -ne 'PASS') { throw 'launch argument smoke test did not pass' }
Add-Check 'launch-arg-smoke' @{ tests = $launchArg.tests }

$mainlineVerify = Join-Path $sourceRoot 'verify.ps1'
if (-not (Test-Path -LiteralPath $mainlineVerify)) { throw "Missing mainline verify script: $mainlineVerify" }
$mainlineJson = & powershell -NoProfile -ExecutionPolicy Bypass -File $mainlineVerify
$mainline = ($mainlineJson | Select-Object -Last 1) | ConvertFrom-Json
if ($mainline.status -ne 'PASS') { throw 'Avalonia mainline verify did not pass' }
Add-Check 'avalonia-mainline-verify' @{
    status = $mainline.status
    tests = $mainline.tests
    publishedExe = $mainline.publishedExe
    sha256 = $mainline.sha256
}

[pscustomobject]@{
    status = 'PASS'
    root = $root
    checks = $checks
} | ConvertTo-Json -Depth 8
