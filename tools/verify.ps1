
param()
Set-StrictMode -Version Latest

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

# Spec invariant .trellis/spec/guides/index.md §5: "Never catch (Exception) — Narrow It".
# A bare catch swallows unrecoverable exceptions (OOM/SEH/AccessViolation) behind a
# status string, hiding real bugs. Enforcing here prevents future regressions of the
# bug fixed in 266aa6f. Filtered catches (`catch (Exception ex) when (...)`) are allowed.
$srcRootForCatchCheck = Join-Path $root 'src/avalonia-launcher/src'
$csFilesForCatchCheck = Get-ChildItem -LiteralPath $srcRootForCatchCheck -Recurse -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '\\(obj|bin|WTLauncher.Analyzers)\\' }
$bareCatchViolations = @()
foreach ($file in $csFilesForCatchCheck) {
    $lineNum = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
        $lineNum++
        if ($line -match 'catch\s*\(\s*Exception\b' -and $line -notmatch '\bwhen\b') {
            $bareCatchViolations += "$($file.FullName.Substring($root.Length)):$lineNum"
        }
    }
}
if ($bareCatchViolations.Count -gt 0) {
    throw "Bare catch(Exception) found (use catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) or specific types): $($bareCatchViolations -join '; ')"
}
Add-Check 'no-bare-catch-exception' @{
    path = 'src/avalonia-launcher/src'
    violations = 0
    invariant = '.trellis/spec/guides/index.md §5'
}

# App views consume semantic resources/tokens; raw color literals belong only in
# WTLauncher.Core/LauncherDesignTokens.cs, the framework-neutral authority.
$rawHexPattern = '(?<![0-9A-Fa-f])#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})(?![0-9A-Fa-f])'
function Get-AppRawHexViolations([System.IO.FileInfo[]]$Files) {
    $violations = @()
    foreach ($file in $Files) {
        $lineNum = 0
        foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
            $lineNum++
            if ($line -match $rawHexPattern) {
                $displayPath = if ($file.FullName.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $file.FullName.Substring($root.Length)
                } else {
                    $file.FullName
                }
                $violations += "${displayPath}:$lineNum"
            }
        }
    }
    return $violations
}

$appSourceRoot = Join-Path $root 'src/avalonia-launcher/src/WTLauncher.App'
$appVisualSourceFiles = @(Get-ChildItem -LiteralPath $appSourceRoot -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '\\(obj|bin)\\' -and
        $_.Extension -in @('.cs', '.axaml')
    })
$rawHexViolations = @(Get-AppRawHexViolations $appVisualSourceFiles)
if ($rawHexViolations.Count -gt 0) {
    throw "Raw hex color found in WTLauncher.App (use LauncherDesignTokens/resource): $($rawHexViolations -join '; ')"
}

$rawHexFixturePath = Join-Path ([System.IO.Path]::GetTempPath()) "wt-launcher-raw-hex-$PID.axaml"
try {
    Set-Content -LiteralPath $rawHexFixturePath -Encoding UTF8 -NoNewline -Value '<Border Background="#123456"/>'
    $fixtureViolations = @(Get-AppRawHexViolations @([System.IO.FileInfo]$rawHexFixturePath))
    if ($fixtureViolations.Count -ne 1) { throw 'Raw-hex gate self-check failed to reject fixture' }
}
finally {
    Remove-Item -LiteralPath $rawHexFixturePath -Force -ErrorAction SilentlyContinue
}
Add-Check 'app-raw-hex-zero' @{
    path = 'src/avalonia-launcher/src/WTLauncher.App'
    violations = 0
    fixtureRejected = $true
}

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
    'all 22 active feature mappings as narrow-domain north-star',
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
    'Feature north-star coverage: current-active-features=22,narrow-north-star=22,non-north-star=0',
    'Blocked absolute requirements: unimprovable-product,stronger-than-every-github-similar,absolute-confidence-zero-side-effects',
    'The full objective remains not complete as written.',
    'All 22 current feature mappings classify as narrow-domain north-star',
    'It is not a broad-domain north star for every launcher or automation product'
)) {
    if (-not $featureAudit.Contains($requiredObjectiveBoundary)) {
        throw "Feature reality/north-star audit missing objective boundary: $requiredObjectiveBoundary"
    }
}
$featureHeadingCount = [regex]::Matches($featureAudit, '(?m)^### \d+\. ').Count
$narrowNorthStarVerdictCount = [regex]::Matches($featureAudit, '(?m)^- Verdict level: narrow-north-star$').Count
if ($featureHeadingCount -ne 22) { throw "Feature reality/north-star audit must cover 22 active feature mappings; found $featureHeadingCount" }
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
    finalAnswer = 'not-complete-as-written'
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

$storePath = Get-LauncherStorePath
$canonicalKey = 'config\launcher.json'
if (-not (Test-Path -LiteralPath $storePath)) { throw "Missing portable launcher store: $storePath" }
$store = Get-Content -Raw -Encoding UTF8 -LiteralPath $storePath | ConvertFrom-Json
if ($null -eq $store) { throw "Launcher store parsed to null: $storePath" }
# Unknown-key and type/required-field validation is enforced by LauncherJson's
# UnmappedMemberHandling=Disallow contract: the C# record shapes ARE the schema.
# The dotnet test run earlier in this gate covers the contract end-to-end
# (UnknownJsonKey_Load_RejectedByStrictSchema etc.). PowerShell keeps only the
# referential-integrity and business-rule checks below.
if ([int]$store.schemaVersion -ne 2) { throw "Unsupported launcher.json schemaVersion: $($store.schemaVersion)" }

$configItems = @($store.entries)
Add-Check 'json' @{ path = $canonicalKey; items = $configItems.Count }

$seenIds = @{}
$selectedFields = 0
$idx = 0
foreach ($item in $configItems) {
    # CI runners lack user workspace directories — skip existence check in CI
    if ($env:CI -ne 'true' -and -not (Test-Path -LiteralPath ([string]$item.directory) -PathType Container)) {
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

$defaultPresetId = [string]$store.defaultPresetId
if ([string]::IsNullOrWhiteSpace($defaultPresetId)) { throw 'launcher.json defaultPresetId is required' }
$presetSeen = @{}
$referencedEntryIds = @{}
$presetCount = 0
$nonEmptyPresetCount = 0
foreach ($preset in @($store.presets)) {
    $presetCount++
    $presetId = [string]$preset.id
    if ([string]::IsNullOrWhiteSpace($presetId)) { throw "Preset $presetCount is missing id" }
    if ($presetSeen.ContainsKey($presetId)) { throw "Duplicate preset id: $presetId" }
    $presetSeen[$presetId] = $true
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
if ($presetCount -lt 1) { throw 'launcher.json must contain at least one preset' }
if (-not $presetSeen.ContainsKey($defaultPresetId)) { throw "defaultPresetId does not match any preset: $defaultPresetId" }
if ($nonEmptyPresetCount -lt 1) { throw 'launcher.json must contain at least one preset with entryIds' }
Add-Check 'presets-schema' @{
    path = 'config\launcher.json'
    presets = $presetCount
    defaultPresetId = $defaultPresetId
    referencedEntries = $referencedEntryIds.Count
}

$settings = $store.settings
if ($null -eq $settings) { throw 'launcher.json settings is required' }
Assert-PositiveJsonNumber $settings.window.width 'launcher.json settings window width'
Assert-PositiveJsonNumber $settings.window.height 'launcher.json settings window height'
$settingsOrder = @($settings.customEntryOrder)
$settingsOrderSeen = @{}
foreach ($id in $settingsOrder) {
    if (-not $seenIds.ContainsKey([string]$id)) { throw "launcher.json settings customEntryOrder references missing config entry id: $id" }
    if ($settingsOrderSeen.ContainsKey([string]$id)) { throw "launcher.json settings customEntryOrder has duplicate config entry id: $id" }
    $settingsOrderSeen[[string]$id] = $true
}
if ($settingsOrder.Count -ne $seenIds.Count) { throw 'launcher.json settings customEntryOrder should contain every config entry id once' }
Add-Check 'settings-json' @{
    path = 'config\launcher.json'
    sortMode = $settings.sortMode
    entryViewMode = $settings.entryViewMode
    requireConfirmationForLaunches = $settings.requireConfirmationForLaunches
    customEntryOrder = $settingsOrder.Count
    hasWindow = $null -ne $settings.window
}

# commandTemplates object shape is enforced by the C# record contract
# (ImmutableDictionary<string,string>). The non-empty value constraint is
# a business rule (an empty template value would surface as "empty command"
# at launch time), so it stays here as a static gate.
$commandTemplates = $settings.commandTemplates
foreach ($templateKey in @($commandTemplates.PSObject.Properties.Name)) {
    $templateValue = $commandTemplates.$templateKey
    if ([string]::IsNullOrWhiteSpace([string]$templateValue)) {
        throw "launcher.json settings commandTemplates key '$templateKey' must be a non-empty string"
    }
}
# Cross-validate: every $key command reference in entries must exist in commandTemplates.
$templateKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($templateKey in @($commandTemplates.PSObject.Properties.Name)) { [void]$templateKeys.Add($templateKey) }
foreach ($item in $configItems) {
    $cmd = [string]$item.command
    if ($cmd.Length -gt 1 -and $cmd[0] -eq '$' -and $cmd[1] -ne '$') {
        $refKey = $cmd.Substring(1)
        if (-not $templateKeys.Contains($refKey)) {
            throw "Config item '$($item.id)' command references undefined template key: $refKey"
        }
    }
}

# R8 (friend-handoff): the shipped example store must stay machine-agnostic —
# zero entries, locked blank-manual preset, no absolute paths. The canonical
# config/launcher.json is git-ignored (desensitized); friends start from this file.
$examplePath = 'config\launcher.example.json'
if (-not (Test-Path -LiteralPath $examplePath)) { throw "Missing example launcher store: $examplePath" }
$example = Get-Content -Raw -Encoding UTF8 -LiteralPath $examplePath | ConvertFrom-Json
if ($null -eq $example) { throw "Example launcher store parsed to null: $examplePath" }
if ([int]$example.schemaVersion -ne 2) { throw "Example launcher store schemaVersion != 2: $examplePath" }
if (@($example.entries).Count -ne 0) { throw "Example launcher store must ship with zero entries: $examplePath" }
if ([string]::IsNullOrWhiteSpace([string]$example.defaultPresetId)) { throw 'Example launcher store defaultPresetId is required' }
foreach ($examplePreset in @($example.presets)) {
    if (($examplePreset.id -eq 'blank-manual') -and ($examplePreset.locked -ne $true)) {
        throw 'Example launcher store blank-manual preset must stay locked'
    }
}
$exampleText = Get-Content -Raw -Encoding UTF8 -LiteralPath $examplePath
if ($exampleText -match '([A-Za-z]:\\)|(/Users/)') {
    throw "Example launcher store must not contain absolute machine paths: $examplePath"
}
Add-Check 'config-example-schema' @{
    path = $examplePath
    schemaVersion = [int]$example.schemaVersion
    entries = @($example.entries).Count
    defaultPresetId = [string]$example.defaultPresetId
    noAbsolutePaths = $true
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

$retiredDesign = Get-RepoPath 'retired\design\winforms-launcher.md'
if (-not (Test-Path -LiteralPath $retiredDesign)) { throw "Missing WinForms retirement design: $retiredDesign" }
$retiredDesignText = Get-Content -Raw -Encoding UTF8 -LiteralPath $retiredDesign
foreach ($required in @('Retired WinForms launcher design', 'Portable config discovery', 'Launch safety boundary', 'Successor directive')) {
    if (-not $retiredDesignText.Contains($required)) { throw "WinForms retirement design missing section: $required" }
}
Add-Check 'winforms-retired-design' @{ path = 'retired\design\winforms-launcher.md' }

$retiredConfigPath = Get-RepoPath 'retired\config\config.json'
if (-not (Test-Path -LiteralPath $retiredConfigPath)) { throw "Missing retired config: $retiredConfigPath" }
$retiredItems = @(Read-ConfigItems $retiredConfigPath)
Add-Check 'hta-retired-config' @{
    path = 'retired\config\config.json'
    items = $retiredItems.Count
    hash = Get-ShortHash $retiredConfigPath
    activeSource = $false
}

# --- Distribution/update contract ---
$installerScript = Get-RepoPath 'tools\install-portable.ps1'
if (-not (Test-Path -LiteralPath $installerScript)) { throw "Missing portable installer script: $installerScript" }
$installerText = Get-Content -Raw -Encoding UTF8 -LiteralPath $installerScript
foreach ($installerMarker in @('WTLauncher.App.exe', 'uninstall.ps1', 'config', 'Start Menu')) {
    if (-not $installerText.Contains($installerMarker)) { throw "Portable installer missing marker: $installerMarker" }
}
Add-Check 'portable-installer-contract' @{
    path = 'tools\install-portable.ps1'
    preservesPortableConfig = $true
    uninstallScript = 'uninstall.ps1'
}

$releaseScript = Get-RepoPath 'tools\build-release.ps1'
if (-not (Test-Path -LiteralPath $releaseScript)) { throw "Missing release build script: $releaseScript" }
$releaseText = Get-Content -Raw -Encoding UTF8 -LiteralPath $releaseScript
if (-not $releaseText.Contains('update-manifest.json')) { throw 'build-release.ps1 must emit update-manifest.json' }
Add-Check 'update-manifest-generation' @{
    path = 'tools\build-release.ps1'
    manifest = 'update-manifest.json'
}

# --- Test structure layering ---
$testRoot = Get-RepoPath 'src\avalonia-launcher\tests\WTLauncher.Tests'
$testLayers = @('Support','CoreUnit','Persistence','ViewModelBehavior','Analyzer','Architecture','UiContract','PropertyTest')
foreach ($layer in $testLayers) {
    $layerDir = Join-Path $testRoot $layer
    if (-not (Test-Path -LiteralPath $layerDir)) { throw "Missing test layer directory: $layerDir" }
    if (@(Get-ChildItem -LiteralPath $layerDir -Filter '*.cs' -ErrorAction SilentlyContinue).Count -lt 1) { throw "Empty test layer directory: $layerDir" }
}
Add-Check 'test-structure-layering' @{ layers = ($testLayers -join ',') }

# --- CI fast lane must not duplicate the full native gate ---
$workflowFile = Get-RepoPath '.github\workflows\verify.yml'
$workflowText = Get-Content -Raw -Encoding UTF8 -LiteralPath $workflowFile
if (-not $workflowText.Contains('test-fast.ps1')) { throw 'CI build-and-test must run tools/test-fast.ps1' }
if ($workflowText.Contains('Publish (smoke check)')) { throw 'CI build-and-test must not duplicate publish; full-gate owns publish' }
Add-Check 'ci-fast-lane-no-publish-duplication' @{ pass = $true }

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

# --- Coverage gate for critical paths ---
$coverageScript = Join-Path $PSScriptRoot 'test-coverage.ps1'
if (-not (Test-Path -LiteralPath $coverageScript)) { throw "Missing coverage script: $coverageScript" }
& powershell -NoProfile -ExecutionPolicy Bypass -File $coverageScript -NoBuild
if ($LASTEXITCODE -ne 0) { throw 'Coverage gate failed' }
$coverageReceiptFile = Join-Path $sourceRoot 'artifacts\coverage\coverage-gate-receipt.json'
if (-not (Test-Path -LiteralPath $coverageReceiptFile)) { throw "Coverage receipt missing: $coverageReceiptFile" }
$coverage = Get-Content -Raw -Encoding UTF8 -LiteralPath $coverageReceiptFile | ConvertFrom-Json
Add-Check 'coverage-gate' @{
    status = $coverage.status
    criticalPaths = $coverage.criticalPaths.Count
}

# --- Architecture rule check ---
# ponytail: Runs architecture tests (ArchUnitNET / NetArchTest) as a fast pre-check.
# If they fail, run `dotnet test --filter "FullyQualifiedName~ArchitectureRules"` locally.
# The --no-build flag is safe because the mainline verify above already compiled everything.
$archTestResult = & dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~ArchitectureRules" --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Architecture rule violations detected. Run dotnet test locally to see details."
}
Add-Check 'architecture-rule-check' @{ pass = $true }

# --- Analyzer wiring verification ---
# Phase A regression gate: verifies that code analyzers are wired and active.
# Build with -warnaserror to catch any analyzer-generated diagnostics.
$buildLog = dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror 2>&1
if ($LASTEXITCODE -ne 0) { throw "Build failed (warnings as errors)" }
Add-Check 'analyzer-wiring' @{ pass = $true }

# ponytail: Negative gate — active docs must not reference the abandoned three-file architecture.
# Catches `config/(config|presets|settings).json` not preceded by retired/. Does NOT catch
# Windows Terminal's own settings.json (lives in docs/research/, not in $activeDocs).
# Add new active docs here when they describe the current single-file launcher.json contract.
$activeDocs = @(
    'README.md', 'AGENTS.md', 'DESIGN.md',
    'docs\feature-reality-north-star-audit.md',
    'docs\feature-first-class-upgrade-audit.md',
    'docs\giants-shoulders\launcher-first-class-patterns.md'
)
foreach ($doc in $activeDocs) {
    $docPath = Get-RepoPath $doc
    if (-not (Test-Path -LiteralPath $docPath)) { continue }
    $docText = Get-Content -Raw -Encoding UTF8 -LiteralPath $docPath
    # Negative lookbehind: config/(config|presets|settings).json not preceded by retired[/\]
    $staleMatches = [regex]::Matches($docText, '(?<!retired[/\\])config[/\\](?:config|presets|settings)\.json')
    if ($staleMatches.Count -gt 0) {
        $examples = ($staleMatches | Select-Object -First 3 | ForEach-Object { $_.Value }) -join ', '
        throw "Stale three-file reference in ${doc}: $examples (use config/launcher.json)"
    }
}
Add-Check 'no-stale-three-file-refs' @{ docs = ($activeDocs -join ', ') }

# 2026-07-09 task 07-09-settings-single-source R3: prevent dual-path regression.
# MainWindowViewModel.cs should have exactly 1 LauncherStoreFile hand-assembly site
# (BuildStoreFile body). SaveSettingsToStore must call BuildStoreFile, not hand-assemble.
# ponytail: this signature (`Entries = _entries.Select... Settings = `) is the unique
# fingerprint of LauncherStoreFile hand-assembly; verifying count = 1 prevents the
# dual-path drift that existed pre-2026-07-09 from being reintroduced.
$vmSourcePath = Get-RepoPath 'src\avalonia-launcher\src\WTLauncher.App\ViewModels\MainWindowViewModel.cs'
$vmSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $vmSourcePath
$signatureCount = ([regex]::Matches($vmSource, 'Entries = _list\.MutableEntries\.Select\(ToLaunchEntry\)\.ToImmutableList\(\),\s*\r?\n\s+Settings =')).Count
if ($signatureCount -ne 1) {
    throw "MainWindowViewModel.cs should have exactly 1 LauncherStoreFile hand-assembly site (BuildStoreFile); found $signatureCount. SaveSettingsToStore must call BuildStoreFile, not hand-assemble. See task 07-09-settings-single-source R1."
}
Add-Check 'settings-single-source-buildstorefile' @{ signatureCount = $signatureCount }

# task 07-10-winget-machine-ready R4: structural gate for the DSC yaml.
# ponytail: `winget configure validate` requires DSC v3 on the host — that's a
# machine-setup dependency, not a dev dependency, so the gate does structural
# validation only. Run `.\tools\apply-machine-config.ps1 -DryRun` for the full
# winget validation when DSC v3 is installed.
$dscPath = Get-RepoPath '.wt-protector.dsc.yaml'
if (-not (Test-Path -LiteralPath $dscPath)) { throw "Missing .wt-protector.dsc.yaml at repo root (task 07-10-winget-machine-ready R1)" }
$dscText = Get-Content -Raw -Encoding UTF8 -LiteralPath $dscPath
$dscRequiredMarkers = @(
    'configuration-dsc-schema/0.2',
    'configurationVersion: 0.2.0',
    'id: Microsoft.WindowsTerminal',
    'id: Microsoft.PowerShell'
)
foreach ($required in $dscRequiredMarkers) {
    if (-not $dscText.Contains($required)) { throw ".wt-protector.dsc.yaml missing required marker: $required" }
}
Add-Check 'winget-config-schema-valid' @{
    path = '.wt-protector.dsc.yaml'
    requiredMarkers = @('schema-0.2', 'configVersion-0.2.0', 'WindowsTerminal', 'PowerShell')
}

# Views directory lint: no imperative UI control construction in .cs files
$viewsDir = Get-AvaloniaRoot
$violations = @()
$whitelisted = @('FastWrapPanel.cs', 'LauncherDialogText.cs', 'MainWindowEntryCardResolver.cs', 'MainWindow.Drag.cs')
$pattern = '\bnew\s+(TextBlock|Grid|StackPanel|Border|Button|TextBox|CheckBox|ComboBox|ScrollViewer)\b'
Get-ChildItem -Path "$viewsDir\src\WTLauncher.App\Views" -Filter '*.cs' -Recurse |
    Where-Object { $_.Name -notmatch '\.axaml\.cs$' -and $_.Name -notin $whitelisted } |
    ForEach-Object {
        $content = Get-Content -Raw -LiteralPath $_.FullName
        if ($content -match $pattern) {
            $violations += $_.Name
        }
    }
if ($violations.Count -gt 0) {
    throw "Imperative UI control construction found in: $($violations -join ', ')"
}
Add-Check 'no-imperative-view-ui' @{ viewsScanned = $true; violations = $violations.Count; allowedExceptions = $whitelisted }

# ponytail: Scans for `Path.GetFullPath(` in DirectoryPath.cs only. If the
# normalization moves to a different class or method (e.g. LaunchEntry.cs
# ExpandEnvironmentVariables), this gate misses the regression. Upgrade path:
# add a second scan pattern for the new location.
# Security audit F1 (2026-07-23): DirectoryPath deserialization must normalize via Path.GetFullPath.
# Regression gate: the JSON converter Read method must contain "Path.GetFullPath".
$directoryPathConverterPath = Get-RepoPath 'src\avalonia-launcher\src\WTLauncher.Core\DirectoryPath.cs'
$directoryPathConverterSrc = Get-Content -Raw -Encoding UTF8 -LiteralPath $directoryPathConverterPath
if ($directoryPathConverterSrc -notmatch 'Path\.GetFullPath\(') {
    throw 'DirectoryPathJsonConverter must call Path.GetFullPath after ExpandEnvironmentVariables (security audit F1)'
}
Add-Check 'security-audit-f1-path-getfullpath' @{
    path = 'src\avalonia-launcher\src\WTLauncher.Core\DirectoryPath.cs'
    invariant = 'DirectoryPathJsonConverter.Read normalizes via Path.GetFullPath'
    pass = $true
}

# ponytail: Checks that `knownEntryIds` string appears in IPresetService.cs.
# A rename of the parameter or migration to a different interface (e.g.
# IPresetValidator) would bypass this gate. Upgrade path: add the new interface
# name to the scan pattern.
# Security audit F2 (2026-07-23): IPresetService.AddPreset must validate entryIds against knownEntryIds.
# Regression gate: IPresetService interface must have a knownEntryIds parameter on AddPreset.
$presetServiceInterfacePath = Get-RepoPath 'src\avalonia-launcher\src\WTLauncher.App\Features\Presets\IPresetService.cs'
$presetServiceInterfaceSrc = Get-Content -Raw -Encoding UTF8 -LiteralPath $presetServiceInterfacePath
if ($presetServiceInterfaceSrc -notmatch 'knownEntryIds') {
    throw 'IPresetService.AddPreset/UpdatePreset must accept knownEntryIds parameter (security audit F2)'
}
Add-Check 'security-audit-f2-preset-entryid-validate' @{
    path = 'src\avalonia-launcher\src\WTLauncher.App\Features\Presets\IPresetService.cs'
    invariant = 'AddPreset/UpdatePreset validate entryIds against knownEntryIds'
    pass = $true
}

# ponytail: Checks that `LatinScript` appears in EntryId.cs. If the whitelist
# is renamed (e.g. LatinScriptRanges -> AllowedScriptRanges) or moved to a
# helper class, this gate misses the regression. Upgrade path: add the new
# class/member name to the scan pattern.
# Security audit F3 (2026-07-23): EntryId must enforce Latin script whitelist.
# Regression gate: EntryId.cs must contain EnsureLatinScript or LatinScriptRanges.
$entryIdPath = Get-RepoPath 'src\avalonia-launcher\src\WTLauncher.Core\EntryId.cs'
$entryIdSrc = Get-Content -Raw -Encoding UTF8 -LiteralPath $entryIdPath
if ($entryIdSrc -notmatch 'LatinScript') {
    throw 'EntryId must enforce Latin script whitelist (security audit F3)'
}
Add-Check 'security-audit-f3-homoglyph-protection' @{
    path = 'src\avalonia-launcher\src\WTLauncher.Core\EntryId.cs'
    invariant = 'EntryId.Parse rejects non-Latin scripts to prevent homoglyph attacks'
    pass = $true
}

# --- Method-level dead-code gate: thin wrapper delegation ---
# ponytail: Regex-based — catches `public T M(a) { Other(a); }` where the ENTIRE
# body is a single delegation to a same-class method (body starts and ends with
# the call). Does NOT catch `_service.Method()` (legitimate encapsulation),
# expression-bodied members (`=>`), multi-statement methods, or methods with
# control flow. Catches the ReplaceAllEntries → ReplaceEntries pattern.
# Upgrade path: replace with a Roslyn analyzer (e.g. WT1007) for semantic detection.
$thinWrapperPath = Get-RepoPath 'src\avalonia-launcher\src'
$thinWrapperRegex = [regex]'(?m)^\s*public\s+\w+\s+(?<wrapMethod>\w+)\s*\([^)]*\)\s*\{\s*(?:return\s+(?:this\.)?)?(?<wrapTarget>\w+)\s*\([^)]*\)\s*;\s*\}'
$thinWrapperFiles = [System.Collections.ArrayList]@()
foreach ($file in (Get-ChildItem -Recurse -LiteralPath $thinWrapperPath -Filter '*.cs')) {
    $lines = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
    $m = $thinWrapperRegex.Match($lines)
    if (-not $m.Success) { continue }
    # Exempt method overloads (same name, different params) — convenience overload, not dead code.
    if ($m.Groups['wrapMethod'].Value -eq $m.Groups['wrapTarget'].Value) { continue }
    $null = $thinWrapperFiles.Add($file)
}
if ($thinWrapperFiles.Count -gt 0) {
    $names = ($thinWrapperFiles | ForEach-Object { $_.Name }) -join ', '
    throw "Thin wrapper delegations detected (potential dead code): $names"
}
Add-Check 'no-thin-wrapper-delegation' @{
    violations = $thinWrapperFiles.Count
    invariant = 'Public methods must not be single-delegation wrappers to same-class methods'
}

# --- VTuber-Killer architecture integrity gate ---
# ponytail: Scans file existence only. Does not check that the types are actually
# referenced or used. A rename or move that preserves the file path bypasses this gate.
# Upgrade path: add content-based scanning (e.g. check class name in file).
# Trinity Phase 2: EventJournal and UiStateProjection were intentionally removed
# (VTuber-Killer simplification). ErrorSupervisor deleted by Phase C dead-code cleanup (07-26-arch-cut-root-causes).
$vtkComponents = @(
    @{ Name = 'StoreGrain'; Path = 'WTLauncher.App\Infrastructure\StoreGrain.cs' }
)
$vtkMissing = @()
foreach ($component in $vtkComponents) {
    $fullPath = Get-RepoPath "src\avalonia-launcher\src\$($component.Path)"
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $vtkMissing += $component.Name
    }
}
if ($vtkMissing.Count -gt 0) {
    throw "VTuber-Killer architecture components missing: $($vtkMissing -join ', ')"
}
Add-Check 'vtk-architecture-integrity' @{
    components = ($vtkComponents.Name -join ', ')
    missing = $vtkMissing
    pass = ($vtkMissing.Count -eq 0)
}

# --- Trinity architecture component gate ---
$triComponents = @(
    @{ Name = 'LauncherJsonSerializer'; Path = 'WTLauncher.Core\LauncherJsonSerializer.cs' }
    @{ Name = 'LauncherStoreValidator'; Path = 'WTLauncher.Core\LauncherStoreValidator.cs' }
    @{ Name = 'ILauncherJsonIO'; Path = 'WTLauncher.Core\ILauncherJsonIO.cs' }
    @{ Name = 'IStorePersistence'; Path = 'WTLauncher.Core\IStorePersistence.cs' }
    @{ Name = 'EntryProjection'; Path = 'WTLauncher.App\Features\Entries\EntryProjection.cs' }
    @{ Name = 'AppStateMachine'; Path = 'WTLauncher.App\ViewModels\AppStateMachine.cs' }
)
$triMissing = @()
foreach ($component in $triComponents) {
    $fullPath = Get-RepoPath "src\avalonia-launcher\src\$($component.Path)"
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $triMissing += $component.Name
    }
}
if ($triMissing.Count -gt 0) {
    throw "Trinity architecture components missing: $($triMissing -join ', ')"
}
Add-Check 'trinity-architecture-integrity' @{
    components = ($triComponents.Name -join ', ')
    missing = $triMissing
    pass = ($triMissing.Count -eq 0)
}

# --- AppStore dead-code regression gate ---
# ponytail: Scans for AppStore references OUTSIDE comments (// or /// lines).
# An AppStore mention in a doc comment or migration note is legitimate;
# an AppStore mention in active code is a regression.
# Upgrade path: add Roslyn analyzer for semantic reference detection.
$appStoreRefs = [System.Collections.ArrayList]@()
foreach ($file in (Get-ChildItem -Recurse -LiteralPath $thinWrapperPath -Filter '*.cs')) {
    $lines = Get-Content -Encoding UTF8 -LiteralPath $file.FullName
    foreach ($line in $lines) {
        if ($line -match '\bAppStore\b' -and $line -notmatch '^\s*//') {
            $null = $appStoreRefs.Add($file)
            break
        }
    }
}
if ($appStoreRefs.Count -gt 0) {
    $files = ($appStoreRefs | ForEach-Object { $_.FullName.Replace($root, '') }) -join ', '
    throw "AppStore references found in active code: $files (was deleted by VTuber-Killer)"
}
Add-Check 'vtk-no-appstore-residue' @{
    violations = $appStoreRefs.Count
    invariant = 'AppStore was deleted by VTuber-Killer migration; zero references allowed in src/'
}

New-ToolReceipt 'root-verify' @{
    root = $root
    checks = $checks
} | ConvertTo-Json -Depth 8 -Compress
