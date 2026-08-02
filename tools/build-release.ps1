param()
Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

# Native AOT needs vswhere.exe to locate VS Build Tools C++ compiler
$vsInstaller = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'
if (Test-Path $vsInstaller) { $env:PATH = "$vsInstaller;$env:PATH" }

$solution = Get-AvaloniaSolutionPath
$app = Get-AvaloniaAppProjectPath
$publishDir = Get-AvaloniaPublishDir
$exe = [IO.Path]::GetFullPath((Join-Path $publishDir 'WTLauncher.App.exe'))
$dll = [IO.Path]::GetFullPath((Join-Path $publishDir 'WTLauncher.App.dll'))

if (-not (Test-Path -LiteralPath $solution)) { throw "Missing solution: $solution" }
if (-not (Test-Path -LiteralPath $app)) { throw "Missing app project: $app" }

$running = @(Get-CimInstance Win32_Process -Filter "name = 'WTLauncher.App.exe'" | Where-Object {
    $_.ExecutablePath -and [IO.Path]::GetFullPath([string]$_.ExecutablePath).Equals($exe, [StringComparison]::OrdinalIgnoreCase)
})
if ($running.Count -gt 0) { throw "Release publish target is running: $exe (pid(s): $($running.ProcessId -join ', '))" }

$version = & {
    try {
        $desc = git -C (Split-Path $solution -Parent) describe --tags --always --dirty 2>$null
        if ($LASTEXITCODE -eq 0 -and $desc) {
            $v = $desc.Trim()
            $v = $v -replace '^v', ''
            if ($v -match '^[0-9]+\.[0-9]+\.[0-9]+') {
                $v = $v -replace '-dirty$', '.dev'
            } else {
                $v = $v -replace '-dirty$', ''
                $v = "0.1.0-$v"
            }
            return $v
        }
    } catch { }
    return '0.1.0-dev'
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Invoke-NativeStep 'dotnet restore solution win-x64' { dotnet restore $solution -r win-x64 }
# 07-12-modernization: Native AOT publish (EnableNativeAot marker enables the
# project-scoped PublishAot in WTLauncher.App.csproj; passing PublishAot on the
# command line would propagate globally to the analyzer project → NETSDK1207).
Invoke-NativeStep 'dotnet publish app win-x64 native-aot' { dotnet publish $app -c Release -r win-x64 -p:EnableNativeAot=true -o $publishDir -p:Version=$version -p:InformationalVersion=$version }

if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }

# R2: keep the publish directory as a clean distributable — remove debug symbols
# (*.pdb) and the stale config/ copy from older publishes. IMPORTANT: *.dll files
# (libSkiaSharp.dll, libHarfBuzzSharp.dll, av_libglesv2.dll) are REQUIRED native
# runtime dependencies of the AOT exe — AOT does not default to single-file —
# and their old timestamps reflect nuget-package asset dates, not staleness.
# Deleting them broke exe startup (DllNotFoundException for libSkiaSharp,
# 2026-08-02) — keep them.
Get-ChildItem -LiteralPath $publishDir -File -Recurse |
    Where-Object { $_.Extension -eq '.pdb' } |
    Remove-Item -Force
$staleConfigDir = Join-Path $publishDir 'config'
if (Test-Path -LiteralPath $staleConfigDir) { Remove-Item -LiteralPath $staleConfigDir -Recurse -Force }

$receipt = New-ToolReceipt 'release-build' @{
    sourceRoot = Get-AvaloniaRoot
    publishedExe = Resolve-Path -LiteralPath $exe | ForEach-Object { $_.Path }
    publishedDll = $(if (Test-Path -LiteralPath $dll) { Resolve-Path -LiteralPath $dll | ForEach-Object { $_.Path } } else { $null })
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-ShortHash $exe)
    builtAt = (Get-Item -LiteralPath $exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
} | ConvertTo-Json -Depth 4 -Compress

$receiptPath = Join-Path $publishDir 'receipt.json'
$receipt | Set-Content -LiteralPath $receiptPath -Encoding UTF8 -NoNewline
$receipt
