# Packages the friend-handoff distributable: a single zip containing the
# AOT exe, the plain-language quick start, and the example config template.
#
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\package-friend.ps1
# Output: src/avalonia-launcher/artifacts/friend/WTLauncher-<version>.zip
#
# Run after tools/build-release.ps1; the script fails loudly rather than
# producing an incomplete package.

param()
Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot 'src\avalonia-launcher\artifacts\publish\win-x64'
$exe = Join-Path $publishDir 'WTLauncher.App.exe'
$receiptPath = Join-Path $publishDir 'receipt.json'
# Resolved by glob instead of a Chinese literal: Windows PowerShell 5.1 reads
# no-BOM UTF-8 scripts as ANSI, which corrupts non-ASCII string literals.
$quickStartFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'docs') -Filter '*.txt' -File)
if ($quickStartFiles.Count -ne 1) { throw "Expected exactly one quick-start .txt in docs/, found $($quickStartFiles.Count)" }
$quickStart = $quickStartFiles[0]
$exampleConfig = Join-Path $repoRoot 'config\launcher.example.json'

foreach ($required in @($exe, $quickStart.FullName, $exampleConfig)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing package input: $required" }
}

# Version comes from the exe's file version, which build-release.ps1 injects
# via -p:Version (git describe). No receipt dependency.
$fileVersion = (Get-Item -LiteralPath $exe).VersionInfo.FileVersion
$version = if ([string]::IsNullOrWhiteSpace($fileVersion)) { '0.1.0' } else { $fileVersion }
$version = $version -replace '[^\w\.\-]', '_'

$friendRoot = Join-Path $repoRoot 'src\avalonia-launcher\artifacts\friend'
$staging = Join-Path $friendRoot 'staging'
$bundle = Join-Path $staging 'WTLauncher'
New-Item -ItemType Directory -Force -Path $bundle | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $bundle 'config') | Out-Null

# Copy the exe plus its required native runtime dlls (AOT does not default to
# single-file — libSkiaSharp.dll etc. must sit next to the exe or startup fails
# with DllNotFoundException). receipt.json stays out of the friend zip.
Get-ChildItem -LiteralPath $publishDir -File |
    Where-Object { $_.Name -ne 'receipt.json' } |
    Copy-Item -Destination $bundle -Force
Copy-Item -LiteralPath $quickStart.FullName -Destination (Join-Path $bundle $quickStart.Name) -Force
Copy-Item -LiteralPath $exampleConfig -Destination (Join-Path $bundle 'config\launcher.example.json') -Force

$zipPath = Join-Path $friendRoot "WTLauncher-$version.zip"
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path $bundle -DestinationPath $zipPath -CompressionLevel Optimal

Remove-Item -LiteralPath $staging -Recurse -Force

$zip = Get-Item -LiteralPath $zipPath
Write-Output "Packaged $($zip.FullName) ($([math]::Round($zip.Length / 1MB, 1)) MB)"
