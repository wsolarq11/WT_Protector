
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$sourceRoot = $PSScriptRoot
$dir = Get-Item -LiteralPath $sourceRoot
$repoRoot = $null
while ($null -ne $dir) {
    $candidateConfig = Join-Path $dir.FullName 'config\config.json'
    $candidateReadme = Join-Path $dir.FullName 'README.md'
    if ((Test-Path -LiteralPath $candidateConfig) -and (Test-Path -LiteralPath $candidateReadme)) {
        $repoRoot = $dir.FullName
        break
    }
    $dir = $dir.Parent
}
if ([string]::IsNullOrWhiteSpace($repoRoot)) { throw "Could not locate repository root from $sourceRoot" }

$config = Join-Path $repoRoot 'config\config.json'
$project = Join-Path $sourceRoot 'src\WTLauncher.App\WTLauncher.App.csproj'
if (-not (Test-Path -LiteralPath $config)) { throw "Missing portable config: $config" }
if (-not (Test-Path -LiteralPath $project)) { throw "Missing app project: $project" }

dotnet run --project $project --configuration Release -- --config $config
