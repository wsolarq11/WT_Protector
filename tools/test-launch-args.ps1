
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$solution = Get-AvaloniaSolutionPath
$tests = Get-AvaloniaTestsProjectPath
if (-not (Test-Path -LiteralPath $solution)) { throw "Missing solution: $solution" }
if (-not (Test-Path -LiteralPath $tests)) { throw "Missing tests project: $tests" }

Invoke-NativeStep 'dotnet restore solution native-aot-locked' { dotnet restore $solution -r win-x64 -p:EnableNativeAot=true --locked-mode }
Invoke-NativeStep 'dotnet test LaunchArgumentBuilder' { dotnet test $tests -c Release --no-restore --filter FullyQualifiedName~LaunchArgumentBuilder --verbosity minimal }

[pscustomobject]@{
    status = 'PASS'
    tests = 'FullyQualifiedName~LaunchArgumentBuilder'
    project = $tests
} | ConvertTo-Json -Depth 4 -Compress
