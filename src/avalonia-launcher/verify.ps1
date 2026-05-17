
param()

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
Invoke-NativeStep 'dotnet test' { dotnet test $tests -c Release --no-restore --verbosity minimal --logger "trx;LogFileName=wtlauncher-tests.trx" --results-directory $testResultsDir }
Invoke-NativeStep 'dotnet publish app win-x64' { dotnet publish $app -c Release -r win-x64 --self-contained true --no-restore -o $publishDir }

$exe = Join-Path $publishDir 'WTLauncher.App.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }
if (-not (Test-Path -LiteralPath $testResultsFile)) { throw "Test result file missing: $testResultsFile" }

[xml]$testResults = Get-Content -Raw -Encoding UTF8 -LiteralPath $testResultsFile
$counters = $testResults.TestRun.ResultSummary.Counters
$totalTests = [int]$counters.total
$failedTests = [int]$counters.failed
if ($totalTests -lt 1) { throw 'No tests were reported in the TRX result' }
if ($failedTests -ne 0) { throw "TRX reported failed tests: $failedTests" }

Remove-Item -LiteralPath $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
$smokeConfig = Join-Path $smokeRoot 'config\config.json'
New-Item -ItemType Directory -Path (Split-Path -Parent $smokeConfig) -Force | Out-Null
Set-Content -Encoding UTF8 -LiteralPath $smokeConfig -Value '[{"id":"smoke","name":"smoke","directory":".","command":"echo smoke","confirm":false,"selected":false}]'
$smokeProcess = Start-Process -FilePath $exe -ArgumentList @('--config', $smokeConfig) -WorkingDirectory $smokeRoot -WindowStyle Hidden -PassThru
try {
    $smokeTitle = $null
    for ($i = 0; $i -lt 30 -and $null -eq $smokeTitle; $i++) {
        Start-Sleep -Milliseconds 100; $smokeProcess.Refresh()
        if ($smokeProcess.HasExited) { throw "Published app exited during GUI smoke with code $($smokeProcess.ExitCode)" }
        if ($smokeProcess.MainWindowHandle -ne 0) { $smokeTitle = $smokeProcess.MainWindowTitle }
    }
    if ($smokeTitle -ne 'WT Launcher') { throw "Published app did not expose the expected main window title; title=$smokeTitle" }
}
finally {
    if (-not $smokeProcess.HasExited) {
        $null = $smokeProcess.CloseMainWindow()
        if (-not $smokeProcess.WaitForExit(2000)) { Stop-Process -Id $smokeProcess.Id -Force; $smokeProcess.WaitForExit(2000) | Out-Null }
    }
}

[pscustomobject]@{
    status = 'PASS'
    sourceRoot = $sourceRoot
    tests = $totalTests
    guiSmoke = 'hidden-temp-config-window-title'
    publishedExe = $exe
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash.Substring(0, 16)
} | ConvertTo-Json -Depth 4 -Compress
