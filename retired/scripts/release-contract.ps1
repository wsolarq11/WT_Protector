param()
Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

function Assert-RequiredField([object]$Object, [string]$Field, [string]$Context) {
    if (-not ($Object.PSObject.Properties.Name -contains $Field) -or [string]::IsNullOrWhiteSpace([string]$Object.$Field)) {
        throw "$Context missing required field: $Field"
    }
}

$repoRoot = Get-RepoRoot
$releaseScript = Join-Path $PSScriptRoot 'build-release.ps1'
if (-not (Test-Path -LiteralPath $releaseScript)) { throw "Missing release build script: $releaseScript" }

& powershell -NoProfile -ExecutionPolicy Bypass -File $releaseScript -LockedMode
if ($LASTEXITCODE -ne 0) { throw 'build-release.ps1 failed' }

$dirty = @(& git -C $repoRoot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'git status failed during release contract' }
if ($dirty.Count -gt 0) {
    throw "Release contract left worktree dirty: $($dirty -join '; ')"
}

$publishDir = Get-AvaloniaPublishDir
$exe = [IO.Path]::GetFullPath((Join-Path $publishDir 'WTLauncher.App.exe'))
$receiptPath = Join-Path $publishDir 'receipt.json'
$manifestPath = Join-Path $publishDir 'update-manifest.json'

if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }
if (-not (Test-Path -LiteralPath $receiptPath)) { throw "Missing receipt: $receiptPath" }
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Missing update manifest: $manifestPath" }

$receipt = Get-Content -Raw -Encoding UTF8 -LiteralPath $receiptPath | ConvertFrom-Json
Assert-RequiredField $receipt 'kind' 'receipt.json'
Assert-RequiredField $receipt 'status' 'receipt.json'
Assert-RequiredField $receipt 'publishedExe' 'receipt.json'
Assert-RequiredField $receipt 'bytes' 'receipt.json'
Assert-RequiredField $receipt 'sha256' 'receipt.json'
if ($receipt.kind -ne 'release-build') { throw "Unexpected receipt kind: $($receipt.kind)" }
if ($receipt.status -ne 'PASS') { throw "Receipt status is not PASS: $($receipt.status)" }
if ([IO.Path]::GetFullPath([string]$receipt.publishedExe) -ne $exe) { throw "Receipt publishedExe mismatch: $($receipt.publishedExe)" }
if ([int]$receipt.bytes -ne (Get-Item -LiteralPath $exe).Length) { throw 'Receipt bytes mismatch' }
if ($receipt.sha256 -ne (Get-ShortHash $exe)) { throw 'Receipt sha256 mismatch' }

$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
Assert-RequiredField $manifest 'schemaVersion' 'update-manifest.json'
Assert-RequiredField $manifest 'version' 'update-manifest.json'
Assert-RequiredField $manifest 'fileName' 'update-manifest.json'
Assert-RequiredField $manifest 'sha256' 'update-manifest.json'
Assert-RequiredField $manifest 'bytes' 'update-manifest.json'
Assert-RequiredField $manifest 'publishedAtUtc' 'update-manifest.json'
if ([int]$manifest.schemaVersion -ne 1) { throw "Unexpected manifest schemaVersion: $($manifest.schemaVersion)" }
if ($manifest.fileName -ne 'WTLauncher.App.exe') { throw "Unexpected manifest fileName: $($manifest.fileName)" }
if ([int]$manifest.bytes -ne (Get-Item -LiteralPath $exe).Length) { throw 'Manifest bytes mismatch' }
if ($manifest.sha256 -ne (Get-FileSha256 $exe)) { throw 'Manifest sha256 mismatch' }

$contractReceipt = New-ToolReceipt 'release-contract' @{
    publishedExe = $exe
    receiptPath = $receiptPath
    manifestPath = $manifestPath
    worktreeClean = $true
} | ConvertTo-Json -Depth 4 -Compress

$contractReceipt
