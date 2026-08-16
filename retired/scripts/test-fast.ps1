param(
    [string]$Configuration = 'Release',
    [string]$Logger = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$filter = 'Category=CoreUnit|Category=PropertyTest|Category=Architecture|Category=Analyzer'
$dotnetArgs = @(
    'test',
    (Join-Path $PSScriptRoot '..\src\avalonia-launcher\WTLauncher.Avalonia.sln'),
    '-c', $Configuration,
    '--no-restore',
    '--filter', $filter
)
if ($Logger) {
    $dotnetArgs += '--logger'
    $dotnetArgs += $Logger
}

& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output "Fast lane PASS: $filter"
