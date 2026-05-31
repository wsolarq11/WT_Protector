
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check([string]$Name, [object]$Detail) {
    $checks.Add([pscustomobject]@{ name = $Name; detail = $Detail }) | Out-Null
}

function Assert-OnlyJsonKeys([object]$Object, [string[]]$AllowedKeys, [string]$Context) {
    $allowed = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($key in $AllowedKeys) { [void]$allowed.Add($key) }

    foreach ($key in @($Object.PSObject.Properties.Name)) {
        if (-not $allowed.Contains($key)) {
            throw "$Context has unknown field: $key"
        }
    }
}

function Assert-PositiveJsonNumber([object]$Value, [string]$Context) {
    if ($null -eq $Value) { throw "$Context is required" }
    $number = [double]$Value
    if ([double]::IsNaN($number) -or [double]::IsInfinity($number) -or $number -le 0) {
        throw "$Context must be positive"
    }
}

$gitattributesPath = Join-Path $root '.gitattributes'
if (-not (Test-Path -LiteralPath $gitattributesPath)) { throw 'Missing .gitattributes line-ending policy' }
$gitattributes = Get-Content -Raw -Encoding UTF8 -LiteralPath $gitattributesPath
foreach ($required in @('* text=auto eol=lf')) {
    if (-not $gitattributes.Contains($required)) { throw ".gitattributes missing required rule: $required" }
}
Add-Check 'line-ending-policy' @{
    path = '.gitattributes'
    text = 'lf'
}

$gitignorePath = Join-Path $root '.gitignore'
if (-not (Test-Path -LiteralPath $gitignorePath)) { throw 'Missing .gitignore local-state policy' }
$gitignore = Get-Content -Raw -Encoding UTF8 -LiteralPath $gitignorePath
if (-not ($gitignore -match '(?m)^\.snow/$')) { throw '.gitignore must ignore .snow/ local state' }
$trackedSnowFiles = @(& git -C $root ls-files -- .snow)
if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while checking .snow/ local-state policy' }
if ($trackedSnowFiles.Count -gt 0) {
    throw ".snow/ must remain ignored local state, but tracked files remain: $($trackedSnowFiles -join ', ')"
}
Add-Check 'snow-ignored-local-state' @{
    path = '.snow/'
    trackedFiles = $trackedSnowFiles.Count
    ignored = $true
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

$rootConfigPath = Get-LauncherConfigPath
$canonicalKey = 'config\config.json'
$configItems = @(Read-ConfigItems $rootConfigPath)
Add-Check 'json' @{ path = $canonicalKey; items = $configItems.Count }

$seenIds = @{}
$selectedFields = 0
$idx = 0
foreach ($item in $configItems) {
    Assert-OnlyJsonKeys $item @('id', 'name', 'directory', 'command', 'selected', 'updatedAtUtc') "Config item $idx"
    foreach ($field in @('id', 'name', 'directory', 'command')) {
        if ([string]::IsNullOrWhiteSpace([string]$item.$field)) {
            throw "Config item $idx is missing field: $field"
        }
    }
    if ($item.PSObject.Properties.Name -contains 'confirm') {
        throw "Config item $idx must not use obsolete per-entry confirm field; settings.json requireConfirmationForLaunches is the single authority"
    }
    if (-not ($item.PSObject.Properties.Name -contains 'selected')) {
        throw "Config item $idx is missing persistent selected field"
    }
    if (-not ($item.selected -is [bool])) {
        throw "Config item $idx selected field must be boolean"
    }
    if (-not (Test-Path -LiteralPath ([string]$item.directory) -PathType Container)) {
        throw "Config item $idx directory does not exist: $($item.directory)"
    }
    $selectedFields++
    if ($seenIds.ContainsKey([string]$item.id)) { throw "Duplicate config id: $($item.id)" }
    $seenIds[[string]$item.id] = $true
    $idx++
}
Add-Check 'config-schema' @{
    uniqueIds = $seenIds.Count
    selectedFields = $selectedFields
    selectedCapable = ($selectedFields -eq $configItems.Count)
    existingDirectories = $configItems.Count
}

$presetPath = Join-Path (Split-Path -Parent $rootConfigPath) 'presets.json'
if (-not (Test-Path -LiteralPath $presetPath)) { throw "Missing portable launcher presets: $presetPath" }
$presetDoc = Get-Content -Raw -Encoding UTF8 -LiteralPath $presetPath | ConvertFrom-Json
if ($null -eq $presetDoc) { throw "Presets parsed to null: $presetPath" }
Assert-OnlyJsonKeys $presetDoc @('schemaVersion', 'defaultPresetId', 'presets') 'presets.json'
if ([int]$presetDoc.schemaVersion -ne 1) { throw "Unsupported presets schemaVersion: $($presetDoc.schemaVersion)" }
$defaultPresetId = [string]$presetDoc.defaultPresetId
if ([string]::IsNullOrWhiteSpace($defaultPresetId)) { throw 'presets.json defaultPresetId is required' }
$presetSeen = @{}
$referencedEntryIds = @{}
$presetCount = 0
$nonEmptyPresetCount = 0
foreach ($preset in @($presetDoc.presets)) {
    $presetCount++
    $presetId = [string]$preset.id
    Assert-OnlyJsonKeys $preset @('id', 'name', 'entryIds', 'locked') "Preset $presetCount"
    if ([string]::IsNullOrWhiteSpace($presetId)) { throw "Preset $presetCount is missing id" }
    if ([string]::IsNullOrWhiteSpace([string]$preset.name)) { throw "Preset $presetId is missing name" }
    if ($presetSeen.ContainsKey($presetId)) { throw "Duplicate preset id: $presetId" }
    $presetSeen[$presetId] = $true
    if ($preset.PSObject.Properties.Name -contains 'locked') {
        if (-not ($preset.locked -is [bool])) { throw "Preset $presetId locked field must be boolean" }
    }
    if (($presetId -eq 'blank-manual') -and ($preset.locked -ne $true)) {
        throw 'Preset blank-manual must stay locked so manual mode cannot be deleted in the UI'
    }

    $entrySeen = @{}
    $entryIds = @($preset.entryIds)
    if ($entryIds.Count -gt 0) { $nonEmptyPresetCount++ }
    foreach ($entryIdValue in $entryIds) {
        $entryId = [string]$entryIdValue
        if ([string]::IsNullOrWhiteSpace($entryId)) { throw "Preset $presetId has blank entry id" }
        if ($entrySeen.ContainsKey($entryId)) { throw "Preset $presetId has duplicate entry id: $entryId" }
        $entrySeen[$entryId] = $true
        if (-not $seenIds.ContainsKey($entryId)) { throw "Preset $presetId references missing config entry id: $entryId" }
        $referencedEntryIds[$entryId] = $true
    }
}
if ($presetCount -lt 1) { throw 'presets.json must contain at least one preset' }
if (-not $presetSeen.ContainsKey($defaultPresetId)) { throw "defaultPresetId does not match any preset: $defaultPresetId" }
if ($nonEmptyPresetCount -lt 1) { throw 'presets.json must contain at least one preset with entryIds' }
Add-Check 'presets-schema' @{
    path = 'config\presets.json'
    presets = $presetCount
    defaultPresetId = $defaultPresetId
    referencedEntries = $referencedEntryIds.Count
}

$settingsPath = Get-LauncherSettingsPath
if (-not (Test-Path -LiteralPath $settingsPath)) { throw "Missing portable settings: $settingsPath" }
$settings = Get-Content -Raw -Encoding UTF8 -LiteralPath $settingsPath | ConvertFrom-Json
if ($null -eq $settings) { throw "Settings parsed to null: $settingsPath" }
Assert-OnlyJsonKeys $settings @('lastFilterText', 'sortMode', 'entryViewMode', 'requireConfirmationForLaunches', 'customEntryOrder', 'window') 'settings.json'
if ($null -eq $settings.lastFilterText) { throw 'settings.json lastFilterText is required' }
if (-not ($settings.lastFilterText -is [string])) { throw 'settings.json lastFilterText must be string' }
if ($null -eq $settings.customEntryOrder) { throw 'settings.json customEntryOrder is required' }
if ($null -eq $settings.window) { throw 'settings.json window is required' }
Assert-OnlyJsonKeys $settings.window @('width', 'height') 'settings.json window'
Assert-PositiveJsonNumber $settings.window.width 'settings.json window width'
Assert-PositiveJsonNumber $settings.window.height 'settings.json window height'
$settingsOrder = @($settings.customEntryOrder)
$settingsOrderSeen = @{}
foreach ($id in $settingsOrder) {
    if (-not $seenIds.ContainsKey([string]$id)) { throw "settings.json customEntryOrder references missing config entry id: $id" }
    if ($settingsOrderSeen.ContainsKey([string]$id)) { throw "settings.json customEntryOrder has duplicate config entry id: $id" }
    $settingsOrderSeen[[string]$id] = $true
}
if ($settingsOrder.Count -ne $seenIds.Count) { throw "settings.json customEntryOrder should contain every config entry id once" }
if ([string]::IsNullOrWhiteSpace([string]$settings.sortMode)) { throw 'settings.json sortMode is required' }
if ([string]::IsNullOrWhiteSpace([string]$settings.entryViewMode)) { throw 'settings.json entryViewMode is required' }
if (-not ($settings.requireConfirmationForLaunches -is [bool])) { throw 'settings.json requireConfirmationForLaunches must be boolean' }
Add-Check 'settings-json' @{
    path = 'config\settings.json'
    sortMode = $settings.sortMode
    entryViewMode = $settings.entryViewMode
    requireConfirmationForLaunches = $settings.requireConfirmationForLaunches
    customEntryOrder = $settingsOrder.Count
    hasWindow = $null -ne $settings.window
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
Add-Check 'hta-retired-config' @{
    path = 'retired\config\config.json'
    items = $retiredItems.Count
    hash = Get-ShortHash $retiredConfigPath
    activeSource = $false
}

$mainlineVerify = Join-Path $sourceRoot 'verify.ps1'
if (-not (Test-Path -LiteralPath $mainlineVerify)) { throw "Missing mainline verify script: $mainlineVerify" }
$mainlineJson = & $mainlineVerify
$mainline = ($mainlineJson | Select-Object -Last 1) | ConvertFrom-Json
foreach ($requiredField in @('schemaVersion', 'kind', 'status', 'repoHead', 'worktreeDirty', 'worktreeStatus')) {
    if (-not ($mainline.PSObject.Properties.Name -contains $requiredField)) { throw "Avalonia mainline receipt missing field: $requiredField" }
}
if ([int]$mainline.schemaVersion -ne 1) { throw "Avalonia mainline receipt schemaVersion mismatch: $($mainline.schemaVersion)" }
if ($mainline.kind -ne 'avalonia-mainline-verify') { throw "Avalonia mainline receipt kind mismatch: $($mainline.kind)" }
if ([string]::IsNullOrWhiteSpace([string]$mainline.repoHead)) { throw 'Avalonia mainline receipt repoHead is required' }
if ($mainline.status -ne 'PASS') { throw 'Avalonia mainline verify did not pass' }
if ([string]::IsNullOrWhiteSpace([string]$mainline.publishedDll)) { throw 'Avalonia mainline verify publishedDll is required' }
Add-Check 'avalonia-mainline-verify' @{
    schemaVersion = $mainline.schemaVersion
    kind = $mainline.kind
    status = $mainline.status
    repoHead = $mainline.repoHead
    worktreeDirty = $mainline.worktreeDirty
    tests = $mainline.tests
    guiSmoke = $mainline.guiSmoke
    publishedExe = $mainline.publishedExe
    publishedDll = $mainline.publishedDll
    sha256 = $mainline.sha256
    dllSha256 = $mainline.dllSha256
}

New-ToolReceipt 'root-verify' @{
    root = $root
    checks = $checks
} | ConvertTo-Json -Depth 8 -Compress
