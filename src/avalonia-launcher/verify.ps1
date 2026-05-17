
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Invoke-NativeStep([string]$Name, [scriptblock]$Command) {
    & $Command
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "$Name failed with exit code $code" }
}

$sourceRoot = $PSScriptRoot
$solution = Join-Path $sourceRoot 'WTLauncher.Avalonia.sln'
$tests = Join-Path $sourceRoot 'tests\WTLauncher.Tests\WTLauncher.Tests.csproj'
$app = Join-Path $sourceRoot 'src\WTLauncher.App\WTLauncher.App.csproj'
$publishDir = Join-Path $sourceRoot 'artifacts\publish\win-x64'

Invoke-NativeStep 'dotnet restore solution' { dotnet restore $solution }
Invoke-NativeStep 'dotnet restore app win-x64' { dotnet restore $app -r win-x64 }
Invoke-NativeStep 'dotnet test' { dotnet test $tests -c Release --no-restore --verbosity minimal }
Invoke-NativeStep 'dotnet publish app win-x64' { dotnet publish $app -c Release -r win-x64 --self-contained true --no-restore -o $publishDir }

$exe = Join-Path $publishDir 'WTLauncher.App.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }

[pscustomobject]@{
    status = 'PASS'
    sourceRoot = $sourceRoot
    tests = 13
    publishedExe = $exe
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash.Substring(0, 16)
} | ConvertTo-Json -Depth 4 -Compress
