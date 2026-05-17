
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

function Invoke-NativeStep([string]$Name, [scriptblock]$Command) {
    & $Command
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "$Name failed with exit code $code" }
}

$solution = Get-AvaloniaSolutionPath
$tests = Get-AvaloniaTestsProjectPath
if (-not (Test-Path -LiteralPath $solution)) { throw "Missing solution: $solution" }
if (-not (Test-Path -LiteralPath $tests)) { throw "Missing tests project: $tests" }

Invoke-NativeStep 'dotnet restore solution' { dotnet restore $solution }
Invoke-NativeStep 'dotnet test LaunchArgumentBuilder' { dotnet test $tests -c Release --no-restore --filter FullyQualifiedName~LaunchArgumentBuilder --verbosity minimal }

[pscustomobject]@{
    status = 'PASS'
    tests = 'FullyQualifiedName~LaunchArgumentBuilder'
    project = $tests
} | ConvertTo-Json -Depth 4 -Compress
