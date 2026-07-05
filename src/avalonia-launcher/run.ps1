
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$sourceRoot = $PSScriptRoot
$dir = Get-Item -LiteralPath $sourceRoot
$repoRoot = $null
while ($null -ne $dir) {
    $candidateStore = Join-Path $dir.FullName 'config\store.json'
    $candidateReadme = Join-Path $dir.FullName 'README.md'
    if ((Test-Path -LiteralPath $candidateStore) -and (Test-Path -LiteralPath $candidateReadme)) {
        $repoRoot = $dir.FullName
        break
    }
    $dir = $dir.Parent
}
if ([string]::IsNullOrWhiteSpace($repoRoot)) { throw "Could not locate repository root from $sourceRoot" }

$store = Join-Path $repoRoot 'config\store.json'
$project = Join-Path $sourceRoot 'src\WTLauncher.App\WTLauncher.App.csproj'
if (-not (Test-Path -LiteralPath $store)) { throw "Missing store: $store" }
if (-not (Test-Path -LiteralPath $project)) { throw "Missing app project: $project" }

dotnet run --project $project --configuration Release -- --config $store
