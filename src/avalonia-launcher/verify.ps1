
param()
Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'tools\config-lib.ps1')

$sourceRoot = Get-AvaloniaRoot
$solution = Get-AvaloniaSolutionPath
$tests = Get-AvaloniaTestsProjectPath
$app = Get-AvaloniaAppProjectPath
$publishDir = Join-Path $sourceRoot 'artifacts\verify-publish\win-x64'
$testResultsDir = Join-Path $sourceRoot 'artifacts\test-results'
$testResultsFile = Join-Path $testResultsDir 'wtlauncher-tests.trx'
$smokeRoot = Join-Path $sourceRoot 'artifacts\gui-smoke'

Invoke-NativeStep 'dotnet restore solution win-x64' { dotnet restore $solution -r win-x64 }
New-Item -ItemType Directory -Path $testResultsDir -Force | Out-Null
Remove-Item -LiteralPath $testResultsFile -Force -ErrorAction SilentlyContinue
# ── Performance baseline: record elapsed time for key steps ────────────
$perfTimings = @{}

$testElapsed = Measure-Command {
    Invoke-NativeStep 'dotnet test' { dotnet test $tests -c Release --no-restore --verbosity minimal --logger "trx;LogFileName=wtlauncher-tests.trx" --results-directory $testResultsDir }
}
$perfTimings.testSeconds = [math]::Round($testElapsed.TotalSeconds, 2)

$publishElapsed = Measure-Command {
    Invoke-NativeStep 'dotnet publish app win-x64' { dotnet publish $app -c Release -r win-x64 --self-contained true --no-restore -o $publishDir }
}
$perfTimings.publishSeconds = [math]::Round($publishElapsed.TotalSeconds, 2)

$exe = Join-Path $publishDir 'WTLauncher.App.exe'
$dll = Join-Path $publishDir 'WTLauncher.App.dll'
if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }
if (-not (Test-Path -LiteralPath $dll)) { throw "Published app dll missing: $dll" }
if (-not (Test-Path -LiteralPath $testResultsFile)) { throw "Test result file missing: $testResultsFile" }

[xml]$testResults = Get-Content -Raw -Encoding UTF8 -LiteralPath $testResultsFile
$counters = $testResults.TestRun.ResultSummary.Counters
$totalTests = [int]$counters.total
$failedTests = [int]$counters.failed
if ($totalTests -lt 1) { throw 'No tests were reported in the TRX result' }
if ($failedTests -ne 0) { throw "TRX reported failed tests: $failedTests" }

# ── Contract validation (preventive checks) ──────────────────────────
# P0#2: Verify smoke fixtures include all [JsonRequired] fields.
$modelSources = Get-ChildItem -Path (Join-Path $sourceRoot 'src\WTLauncher.Core') -Filter '*.cs' -Recurse -Exclude '*AssemblyInfo*'
$jsonRequiredFields = @{}
foreach ($src in $modelSources) {
    $content = Get-Content -Raw -Encoding UTF8 -LiteralPath $src.FullName
    # Extract type name + [JsonPropertyName] + [JsonRequired] patterns.
    # Allow optional modifiers (sealed / readonly / etc.) between "public" and "record".
    if ($content -match 'public\s+(?:\w+\s+)*record\s+(\w+)') {
        $typeName = $Matches[1]
        $fields = [regex]::Matches($content, '\[JsonPropertyName\("(\w+)"\)\]\s*\[JsonRequired\]\s*public \S+ (\w+)')
        foreach ($f in $fields) { $jsonRequiredFields[$f.Groups[1].Value] = $typeName }
    }
}
if ($jsonRequiredFields.Count -eq 0) { throw 'Contract validation: could not extract any [JsonRequired] fields' }
Write-Host "Contract validation: found $($jsonRequiredFields.Count) [JsonRequired] fields across models"

# P1#1: No SaveConfig() / Save*() call discards its return value.
$saveDiscards = Select-String -Path (Join-Path $sourceRoot 'src\WTLauncher.App\ViewModels\*.cs') -Pattern '^\s+Save\w+\(\)'
$saveDiscards += Select-String -Path (Join-Path $sourceRoot 'src\WTLauncher.App\ViewModels\*.cs') -Pattern '^\s+if\s*\([^)]*\.Save\w+\(\)' -NotMatch | Select-String -Pattern '\.Save\w+\(\)' | Where-Object { $_.Line -notmatch '=>' }
if ($saveDiscards) {
    $lines = ($saveDiscards | ForEach-Object { "$($_.Filename):$($_.LineNumber)" }) -join ', '
    throw "Contract violation: Save*() return value discarded at: $lines"
}

# P1#2: No bare integers in XAML TimeSpan properties.
$timeSpanBare = Select-String -Path (Join-Path $sourceRoot 'src\WTLauncher.App\Views\*.axaml') -Pattern '(MinimumPopulateDelay|Duration)="\d+"'
if ($timeSpanBare) {
    $lines = ($timeSpanBare | ForEach-Object { "$($_.Filename):$($_.LineNumber)" }) -join ', '
    throw "Contract violation: bare integer in XAML TimeSpan at: $lines. Use explicit h:m:s.ms format."
}

$isWindowsPlatform = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

# ── Process-level smoke (all platforms, including CI) ──────────────────
# Validates that the published exe can boot, load config, and not crash.
# Catches stacked startup bugs (missing [JsonRequired] fields, TimeSpan
# parsing errors in XAML) that unit tests cannot detect.
# Does NOT require a display — safe for CI / headless environments.
$processSmoke = 'skipped-non-windows'
if ($isWindowsPlatform) {
    Remove-Item -LiteralPath $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
    $smokeStore = Join-Path $smokeRoot 'config\launcher.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $smokeStore) -Force | Out-Null
    $smokeStoreJson = @{
        schemaVersion = 2
        entries = @(@{id="smoke";name="smoke";directory=".";command="echo smoke";selected=$false;updatedAtUtc="";launchedAtUtc=""})
        # runInTray must be explicitly false: with the declared default (true),
        # closing the main window hides to tray and the close-exit smoke assertion
        # would never fire.
        settings = @{lastFilterText="";sortMode="custom";entryViewMode="cards";requireConfirmationForLaunches=$true;runInTray=$false;customEntryOrder=@("smoke");searchHistory=@();commandTemplates=@{};window=@{width=1180;height=760;windowState="Normal"}}
        presets = @(@{id="smoke-group";name="Smoke";entryIds=@("smoke")})
        defaultPresetId = "smoke-group"
    } | ConvertTo-Json -Depth 6
    Set-Content -Encoding UTF8 -LiteralPath $smokeStore -Value $smokeStoreJson
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $smokeScript = Join-Path $repoRoot 'tools\smoke-critical-journey.ps1'
    if (-not (Test-Path -LiteralPath $smokeScript)) { throw "Critical journey smoke script missing: $smokeScript" }
    $smokeOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File $smokeScript -Exe $exe -ConfigPath $smokeStore -WorkingDirectory $smokeRoot
    if ($LASTEXITCODE -ne 0) { throw 'Critical journey smoke failed' }
    $smokeReceipt = $smokeOutput | ConvertFrom-Json
    $processSmoke = (($smokeReceipt.checks | ForEach-Object { $_.name }) -join '+')
    if ($processSmoke -ne 'launch+title') { throw "Critical journey smoke reported unexpected checks: $processSmoke" }
}

# ── GUI title check (Windows non-CI only) ─────────────────────────────
# Full interactive smoke: validates the main window title is correct.
# Skipped in CI because it requires a real display.
$isCi = $env:CI -eq 'true'
$guiSmoke = if (-not $isWindowsPlatform) { 'skipped-non-windows' } elseif ($isCi) { 'skipped-ci-headless' } else { $processSmoke }
if ($guiSmoke -ne 'skipped-non-windows' -and $guiSmoke -ne 'skipped-ci-headless') {
    $guiSmoke = 'hidden-temp-config-window-title'
}

New-ToolReceipt 'avalonia-mainline-verify' @{
    sourceRoot = $sourceRoot
    tests = $totalTests
    guiSmoke = $guiSmoke
    publishedExe = $exe
    publishedDll = $dll
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-FileSha256 $exe).Substring(0, 16)
    dllBytes = (Get-Item -LiteralPath $dll).Length
    dllSha256 = (Get-FileSha256 $dll).Substring(0, 16)
    perf = $perfTimings
} | ConvertTo-Json -Depth 4 -Compress
