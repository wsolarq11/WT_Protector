param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$spikeRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $spikeRoot)
$config = Join-Path $repoRoot 'config.json'
$project = Join-Path $spikeRoot 'src\WTLauncher.App\WTLauncher.App.csproj'

if (-not (Test-Path -LiteralPath $config)) {
    throw "Missing root config: $config"
}

dotnet run --project $project --configuration Release -- --config $config
