
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
$app = Get-AvaloniaAppProjectPath
$publishDir = Get-AvaloniaPublishDir

if (-not (Test-Path -LiteralPath $solution)) { throw "Missing solution: $solution" }
if (-not (Test-Path -LiteralPath $app)) { throw "Missing app project: $app" }

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Invoke-NativeStep 'dotnet restore solution' { dotnet restore $solution }
Invoke-NativeStep 'dotnet restore app win-x64' { dotnet restore $app -r win-x64 }
Invoke-NativeStep 'dotnet publish app win-x64' { dotnet publish $app -c Release -r win-x64 --self-contained true --no-restore -o $publishDir }

$exe = Join-Path $publishDir 'WTLauncher.App.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }

[pscustomobject]@{
    status = 'PASS'
    sourceRoot = Get-AvaloniaRoot
    publishedExe = Resolve-Path -LiteralPath $exe | ForEach-Object { $_.Path }
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-ShortHash $exe)
    builtAt = (Get-Item -LiteralPath $exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
} | ConvertTo-Json -Depth 4 -Compress
