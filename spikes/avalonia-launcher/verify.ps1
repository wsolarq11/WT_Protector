param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$spikeRoot = $PSScriptRoot
$solution = Join-Path $spikeRoot 'WTLauncher.Avalonia.sln'
$tests = Join-Path $spikeRoot 'tests\WTLauncher.Tests\WTLauncher.Tests.csproj'
$app = Join-Path $spikeRoot 'src\WTLauncher.App\WTLauncher.App.csproj'
$publishDir = Join-Path $spikeRoot 'artifacts\publish\win-x64'

dotnet restore $solution
dotnet restore $app -r win-x64
dotnet test $tests -c Release --no-restore --verbosity minimal
dotnet publish $app -c Release -r win-x64 --self-contained true --no-restore -o $publishDir

$exe = Join-Path $publishDir 'WTLauncher.App.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Published exe missing: $exe"
}

[pscustomobject]@{
    status = 'PASS'
    spikeRoot = $spikeRoot
    publishedExe = $exe
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash.Substring(0, 16)
} | ConvertTo-Json -Depth 4
