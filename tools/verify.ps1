
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
foreach ($requiredObjectiveBoundary in @(
    'all 21 active feature mappings as narrow-domain north-star',
    'portable Windows Terminal workspace-launcher scope only',
    'full absolute objective remains not complete as written',
    'unimprovable-product, stronger-than-every-GitHub-similar, and absolute zero-side-effect claims'
)) {
    if (-not $readme.Contains($requiredObjectiveBoundary)) {
        throw "README missing current objective boundary: $requiredObjectiveBoundary"
    }
}
Add-Check 'readme' @{
    sections = 6
    fullObjectiveBoundary = 'not-complete-as-written'
}

$featureAuditPath = Join-Path $root 'docs\feature-reality-north-star-audit.md'
if (-not (Test-Path -LiteralPath $featureAuditPath)) { throw "Missing feature reality/north-star audit: $featureAuditPath" }
$featureAudit = Get-Content -Raw -Encoding UTF8 -LiteralPath $featureAuditPath
foreach ($requiredObjectiveBoundary in @(
    'Full objective result: not-complete-as-written',
    'Feature north-star coverage: current-active-features=21,narrow-north-star=21,non-north-star=0',
    'Blocked absolute requirements: unimprovable-product,stronger-than-every-github-similar,absolute-confidence-zero-side-effects',
    'All 21 current feature mappings classify as narrow-domain north-star',
    'It is not a broad-domain north star for every launcher or automation product'
)) {
    if (-not $featureAudit.Contains($requiredObjectiveBoundary)) {
        throw "Feature reality/north-star audit missing objective boundary: $requiredObjectiveBoundary"
    }
}
$featureHeadingCount = [regex]::Matches($featureAudit, '(?m)^### \d+\. ').Count
$narrowNorthStarVerdictCount = [regex]::Matches($featureAudit, '(?m)^- Verdict level: narrow-north-star$').Count
if ($featureHeadingCount -ne 21) { throw "Feature reality/north-star audit must cover 21 active feature mappings; found $featureHeadingCount" }
if ($narrowNorthStarVerdictCount -ne $featureHeadingCount) {
    throw "Feature reality/north-star audit has non-north-star feature verdicts: narrow=$narrowNorthStarVerdictCount total=$featureHeadingCount"
}
Add-Check 'feature-objective-boundary' @{
    path = 'docs\feature-reality-north-star-audit.md'
    activeFeatureMappings = $featureHeadingCount
    narrowNorthStarVerdicts = $narrowNorthStarVerdictCount
    fullObjectiveResult = 'not-complete-as-written'
    blockedAbsoluteRequirements = @(
        'unimprovable-product',
        'stronger-than-every-github-similar',
        'absolute-confidence-zero-side-effects'
    )
}

$claimLedgerStart = $featureAudit.IndexOf('## Claim boundary ledger', [System.StringComparison]::Ordinal)
$finalAnswerStart = $featureAudit.IndexOf('## Final answer', [System.StringComparison]::Ordinal)
if ($claimLedgerStart -lt 0) { throw 'Feature reality/north-star audit missing claim boundary ledger' }
if ($finalAnswerStart -le $claimLedgerStart) { throw 'Feature reality/north-star audit claim boundary ledger must appear before final answer' }
$claimLedger = $featureAudit.Substring($claimLedgerStart, $finalAnswerStart - $claimLedgerStart)
$expectedClaimStatuses = [ordered]@{
    'narrow-domain-north-star' = 'allowed'
    'narrow-slice-stronger' = 'allowed'
    'small-beautiful-preserved' = 'allowed'
    'pre-handoff-zero-side-effect-boundary' = 'allowed'
    'broad-launcher-platform-north-star' = 'forbidden'
    'unimprovable-product' = 'forbidden'
    'stronger-than-every-github-similar' = 'forbidden'
    'absolute-confidence-zero-side-effects' = 'forbidden'
    'post-wt-terminal-health' = 'forbidden'
}
foreach ($claimId in $expectedClaimStatuses.Keys) {
    $status = $expectedClaimStatuses[$claimId]
    $pattern = "(?ms)^- Claim id: $([regex]::Escape($claimId))\s*\r?\n\s+- Claim status: $([regex]::Escape($status))\b"
    if (-not [regex]::IsMatch($claimLedger, $pattern)) {
        throw "Feature reality/north-star audit claim boundary mismatch: $claimId must be $status"
    }
}
$allowedClaimCount = [regex]::Matches($claimLedger, '(?m)^\s+- Claim status: allowed$').Count
$forbiddenClaimCount = [regex]::Matches($claimLedger, '(?m)^\s+- Claim status: forbidden$').Count
if ($allowedClaimCount -ne 4) { throw "Feature reality/north-star audit allowed claim count must be 4; found $allowedClaimCount" }
if ($forbiddenClaimCount -ne 5) { throw "Feature reality/north-star audit forbidden claim count must be 5; found $forbiddenClaimCount" }
Add-Check 'claim-boundary-ledger' @{
    path = 'docs\feature-reality-north-star-audit.md'
    allowedClaims = $allowedClaimCount
    forbiddenClaims = $forbiddenClaimCount
    forbiddenAbsoluteClaims = @(
        'unimprovable-product',
        'stronger-than-every-github-similar',
        'absolute-confidence-zero-side-effects',
        'post-wt-terminal-health'
    )
}

$githubRefreshScript = Join-Path $root 'tools\update-github-similar-projects-snapshot.ps1'
if (-not (Test-Path -LiteralPath $githubRefreshScript)) { throw "Missing GitHub similar-project snapshot refresh script: $githubRefreshScript" }
$githubSnapshotPath = Join-Path $root 'docs\reference\github-similar-projects-snapshot.json'
if (-not (Test-Path -LiteralPath $githubSnapshotPath)) { throw "Missing GitHub similar-project snapshot: $githubSnapshotPath" }
$githubSnapshot = Get-Content -Raw -Encoding UTF8 -LiteralPath $githubSnapshotPath | ConvertFrom-Json
if ($null -eq $githubSnapshot) { throw "GitHub similar-project snapshot parsed to null: $githubSnapshotPath" }
Assert-OnlyJsonKeys $githubSnapshot @('schemaVersion', 'capturedAtUtc', 'evidenceCommand', 'refreshCommand', 'queries', 'scope', 'summary', 'repositories') 'github-similar-projects-snapshot.json'
if ([int]$githubSnapshot.schemaVersion -ne 3) { throw "Unsupported GitHub similar-project snapshot schemaVersion: $($githubSnapshot.schemaVersion)" }
if ([string]::IsNullOrWhiteSpace([string]$githubSnapshot.capturedAtUtc)) { throw 'GitHub similar-project snapshot capturedAtUtc is required' }
$githubSnapshotMaxAgeDays = 30
try {
    $githubCapturedAt = [datetimeoffset]::Parse([string]$githubSnapshot.capturedAtUtc, [System.Globalization.CultureInfo]::InvariantCulture)
} catch {
    throw "GitHub similar-project snapshot capturedAtUtc is not parseable: $($githubSnapshot.capturedAtUtc)"
}
$githubSnapshotAge = (Get-Date).ToUniversalTime() - $githubCapturedAt.UtcDateTime
if ($githubSnapshotAge.TotalSeconds -lt 0) { throw "GitHub similar-project snapshot capturedAtUtc is in the future: $($githubSnapshot.capturedAtUtc)" }
if ($githubSnapshotAge.TotalDays -gt $githubSnapshotMaxAgeDays) {
    throw "GitHub similar-project snapshot is stale: $($githubSnapshot.capturedAtUtc). Refresh with: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\update-github-similar-projects-snapshot.ps1"
}
if (-not ([string]$githubSnapshot.evidenceCommand).Contains('gh repo view <owner>/<repo>')) {
    throw 'GitHub similar-project snapshot must record the gh repo view evidence command'
}
if (-not ([string]$githubSnapshot.refreshCommand).Contains('tools\update-github-similar-projects-snapshot.ps1')) {
    throw 'GitHub similar-project snapshot must record its repo-local refresh command'
}
if ($null -eq $githubSnapshot.summary) { throw 'GitHub similar-project snapshot summary is required' }
Assert-OnlyJsonKeys $githubSnapshot.summary @('repositories', 'directNarrowWeaker', 'adjacentAxisStronger', 'broadMarketStronger', 'flowStars', 'woxStars') 'GitHub similar-project snapshot summary'

$githubQueries = @($githubSnapshot.queries)
foreach ($query in @('Windows Terminal workspace launcher', 'Windows Terminal session manager', 'Claude Code launcher')) {
    if ($githubQueries -notcontains $query) { throw "GitHub similar-project snapshot missing query: $query" }
}

$githubRepos = @($githubSnapshot.repositories)
if ($githubRepos.Count -lt 12) { throw "GitHub similar-project snapshot should track at least 12 representative repositories; found $($githubRepos.Count)" }
$githubRepoByName = @{}
$githubAllowedYardstickVerdicts = @('direct-narrow-weaker', 'adjacent-axis-stronger', 'broad-market-stronger')
foreach ($repo in $githubRepos) {
    Assert-OnlyJsonKeys $repo @('fullName', 'url', 'description', 'language', 'stars', 'forks', 'pushedAtUtc', 'updatedAtUtc', 'archived', 'classification', 'yardstickVerdict', 'strongerAxes', 'wtLauncherAxes') "GitHub similar-project snapshot repository"
    $fullName = [string]$repo.fullName
    if ([string]::IsNullOrWhiteSpace($fullName)) { throw 'GitHub similar-project snapshot repository fullName is required' }
    if ($githubRepoByName.ContainsKey($fullName)) { throw "Duplicate GitHub similar-project snapshot repository: $fullName" }
    if (-not ([string]$repo.url).StartsWith('https://github.com/', [StringComparison]::Ordinal)) { throw "GitHub similar-project snapshot repository url is invalid: $fullName" }
    if ([int]$repo.stars -lt 0) { throw "GitHub similar-project snapshot repository stars must be non-negative: $fullName" }
    if ([int]$repo.forks -lt 0) { throw "GitHub similar-project snapshot repository forks must be non-negative: $fullName" }
    if (-not ($repo.archived -is [bool])) { throw "GitHub similar-project snapshot repository archived must be boolean: $fullName" }
    if ([string]::IsNullOrWhiteSpace([string]$repo.classification)) { throw "GitHub similar-project snapshot repository classification is required: $fullName" }
    if ($githubAllowedYardstickVerdicts -notcontains [string]$repo.yardstickVerdict) { throw "GitHub similar-project snapshot repository yardstickVerdict is unsupported for $fullName`: $($repo.yardstickVerdict)" }
    if (@($repo.strongerAxes).Count -lt 1) { throw "GitHub similar-project snapshot repository strongerAxes is required: $fullName" }
    if (@($repo.wtLauncherAxes).Count -lt 1) { throw "GitHub similar-project snapshot repository wtLauncherAxes is required: $fullName" }
    $githubRepoByName[$fullName] = $repo
}
foreach ($requiredRepo in @(
    'SyDroX/dev-layout',
    'augustdragon/terminal-manager',
    'noambrand/Launchpad-CLI',
    'kevinkicho/win-screen-setup',
    'ewdieckman/DevWorkspace',
    'jsoulet42/claude-launcher-app',
    'A-ema755/claude-dashboard',
    'adityacodepublic/wtsm-cli',
    'pramin/AgentSpace',
    'joyahmed/devgo-app',
    'Flow-Launcher/Flow.Launcher',
    'Wox-launcher/Wox'
)) {
    if (-not $githubRepoByName.ContainsKey($requiredRepo)) {
        throw "GitHub similar-project snapshot missing representative repo: $requiredRepo"
    }
}
$flowStars = [int]$githubRepoByName['Flow-Launcher/Flow.Launcher'].stars
$woxStars = [int]$githubRepoByName['Wox-launcher/Wox'].stars
if ($flowStars -lt 10000) { throw "Flow Launcher market-proof stars unexpectedly low in snapshot: $flowStars" }
if ($woxStars -lt 10000) { throw "Wox market-proof stars unexpectedly low in snapshot: $woxStars" }
if ([string]$githubRepoByName['Flow-Launcher/Flow.Launcher'].yardstickVerdict -ne 'broad-market-stronger') { throw 'Flow Launcher must remain classified as broad-market-stronger' }
if ([string]$githubRepoByName['Wox-launcher/Wox'].yardstickVerdict -ne 'broad-market-stronger') { throw 'Wox must remain classified as broad-market-stronger' }
$directNarrowWeakerCount = @($githubRepos | Where-Object { [string]$_.yardstickVerdict -eq 'direct-narrow-weaker' }).Count
$adjacentAxisStrongerCount = @($githubRepos | Where-Object { [string]$_.yardstickVerdict -eq 'adjacent-axis-stronger' }).Count
$broadMarketStrongerCount = @($githubRepos | Where-Object { [string]$_.yardstickVerdict -eq 'broad-market-stronger' }).Count
if ($broadMarketStrongerCount -ne 2) { throw "GitHub similar-project snapshot broad-market-stronger count must be 2; found $broadMarketStrongerCount" }
if ([int]$githubSnapshot.summary.repositories -ne $githubRepos.Count) { throw "GitHub similar-project snapshot summary repositories mismatch: $($githubSnapshot.summary.repositories) vs $($githubRepos.Count)" }
if ([int]$githubSnapshot.summary.directNarrowWeaker -ne $directNarrowWeakerCount) { throw "GitHub similar-project snapshot summary directNarrowWeaker mismatch: $($githubSnapshot.summary.directNarrowWeaker) vs $directNarrowWeakerCount" }
if ([int]$githubSnapshot.summary.adjacentAxisStronger -ne $adjacentAxisStrongerCount) { throw "GitHub similar-project snapshot summary adjacentAxisStronger mismatch: $($githubSnapshot.summary.adjacentAxisStronger) vs $adjacentAxisStrongerCount" }
if ([int]$githubSnapshot.summary.broadMarketStronger -ne $broadMarketStrongerCount) { throw "GitHub similar-project snapshot summary broadMarketStronger mismatch: $($githubSnapshot.summary.broadMarketStronger) vs $broadMarketStrongerCount" }
if ([int]$githubSnapshot.summary.flowStars -ne $flowStars) { throw "GitHub similar-project snapshot summary flowStars mismatch: $($githubSnapshot.summary.flowStars) vs $flowStars" }
if ([int]$githubSnapshot.summary.woxStars -ne $woxStars) { throw "GitHub similar-project snapshot summary woxStars mismatch: $($githubSnapshot.summary.woxStars) vs $woxStars" }
Add-Check 'github-similar-projects-snapshot' @{
    path = 'docs\reference\github-similar-projects-snapshot.json'
    refreshScript = 'tools\update-github-similar-projects-snapshot.ps1'
    capturedAtUtc = $githubSnapshot.capturedAtUtc
    maxAgeDays = $githubSnapshotMaxAgeDays
    ageDays = [math]::Round($githubSnapshotAge.TotalDays, 2)
    repositories = $githubSnapshot.summary.repositories
    directNarrowWeaker = $githubSnapshot.summary.directNarrowWeaker
    adjacentAxisStronger = $githubSnapshot.summary.adjacentAxisStronger
    broadMarketStronger = $githubSnapshot.summary.broadMarketStronger
    flowStars = $flowStars
    woxStars = $woxStars
}

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
