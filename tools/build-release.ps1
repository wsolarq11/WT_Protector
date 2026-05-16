param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$program = Get-RepoPath 'WT Launcher\Program.cs'
$target = Get-RepoPath 'WT Launcher\WT Launcher.exe'
$csc = Join-Path $env:windir 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$tempDir = Join-Path ([IO.Path]::GetTempPath()) ('wt-launcher-build-' + [Guid]::NewGuid().ToString('N'))
$tempExe = Join-Path $tempDir 'WT Launcher.exe'
$backupPath = $null
$oldHash = Get-ShortHash $target

try {
    if (-not (Test-Path -LiteralPath $program)) { throw "Missing Program.cs: $program" }
    if (-not (Test-Path -LiteralPath $csc)) { throw "Missing csc.exe: $csc" }
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

    & $csc /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /out:"$tempExe" "$program" | Out-String | ForEach-Object {
        if ($_.Trim()) { Write-Host $_ }
    }
    if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }
    if (-not (Test-Path -LiteralPath $tempExe)) { throw "Compile output missing: $tempExe" }

    if (Test-Path -LiteralPath $target) {
        $backupPath = Backup-File $target
    }
    Copy-Item -LiteralPath $tempExe -Destination $target -Force

    [pscustomobject]@{
        status = 'PASS'
        target = Resolve-Path -LiteralPath $target | ForEach-Object { $_.Path }
        backupPath = $backupPath
        oldHash = $oldHash
        newHash = Get-ShortHash $target
        bytes = (Get-Item -LiteralPath $target).Length
        builtAt = (Get-Item -LiteralPath $target).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
    } | ConvertTo-Json -Depth 4
}
finally {
    if (Test-Path -LiteralPath $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
