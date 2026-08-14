param(
    [switch]$NoBuild
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'coverage-gate.json'
$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json

$solution = Join-Path $repoRoot 'src\avalonia-launcher\WTLauncher.Avalonia.sln'
$resultsDir = Join-Path $repoRoot 'src\avalonia-launcher\artifacts\coverage'
Remove-Item -LiteralPath $resultsDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

$dotnetArgs = @(
    'test',
    $solution,
    '-c', 'Release',
    '--no-restore',
    '--filter', $manifest.filter,
    '--collect', 'XPlat Code Coverage',
    '--results-directory', $resultsDir
)
if ($NoBuild) { $dotnetArgs += '--no-build' }
& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$coverageFile = Get-ChildItem -LiteralPath $resultsDir -Filter 'coverage.cobertura.xml' -Recurse | Sort-Object LastWriteTime | Select-Object -Last 1
if (-not $coverageFile) { throw 'Coverage collector produced no coverage.cobertura.xml' }
[xml]$xml = Get-Content -Raw -Encoding UTF8 -LiteralPath $coverageFile.FullName

$reported = @()
$failures = @()
foreach ($entry in $manifest.criticalPaths) {
    $class = $xml.coverage.packages.package.classes.class | Where-Object { $_.name -eq $entry.className } | Select-Object -First 1
    if (-not $class) {
        $failures += "$($entry.className): class not found in coverage report"
        continue
    }
    $rate = [double]$class.'line-rate'
    $reported += [pscustomobject]@{ className = $entry.className; lineRate = $rate; minLineRate = [double]$entry.minLineRate; pass = ($rate -ge [double]$entry.minLineRate) }
    if ($rate -lt [double]$entry.minLineRate) {
        $failures += "$($entry.className): $([math]::Round($rate, 3)) below $($entry.minLineRate)"
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

$receipt = [pscustomobject]@{
    status = 'PASS'
    report = $coverageFile.FullName
    criticalPaths = $reported
}
$receiptPath = Join-Path $resultsDir 'coverage-gate-receipt.json'
$receipt | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -LiteralPath $receiptPath
Write-Output $receiptPath
