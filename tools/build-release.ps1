param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

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
Invoke-NativeStep 'dotnet publish app win-x64' { dotnet publish $app -c Release -r win-x64 --self-contained true --no-restore -o $publishDir -p:Version=$version -p:InformationalVersion=$version }

if (-not (Test-Path -LiteralPath $exe)) { throw "Published exe missing: $exe" }
if (-not (Test-Path -LiteralPath $dll)) { throw "Published app dll missing: $dll" }

$receipt = New-ToolReceipt 'release-build' @{
    sourceRoot = Get-AvaloniaRoot
    publishedExe = Resolve-Path -LiteralPath $exe | ForEach-Object { $_.Path }
    publishedDll = Resolve-Path -LiteralPath $dll | ForEach-Object { $_.Path }
    bytes = (Get-Item -LiteralPath $exe).Length
    sha256 = (Get-ShortHash $exe)
    dllBytes = (Get-Item -LiteralPath $dll).Length
    dllSha256 = (Get-ShortHash $dll)
    builtAt = (Get-Item -LiteralPath $exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
} | ConvertTo-Json -Depth 4 -Compress

$receiptPath = Join-Path $publishDir 'receipt.json'
$receipt | Set-Content -LiteralPath $receiptPath -Encoding UTF8 -NoNewline
$receipt
