param(
    [string]$PublishDir = (Join-Path (Split-Path -Parent $PSScriptRoot) 'src\avalonia-launcher\artifacts\publish\win-x64'),
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\WTLauncher'),
    [switch]$Desktop,
    [switch]$Uninstall
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

if ($Uninstall) {
    $configSource = Join-Path $InstallDir 'config'
    $backupRoot = Join-Path $env:USERPROFILE 'Documents\WTLauncher-config-backup'
    if (Test-Path -LiteralPath $configSource) {
        $backup = Join-Path $backupRoot ('config-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        New-Item -ItemType Directory -Force -Path $backup | Out-Null
        Copy-Item -LiteralPath $configSource -Destination $backup -Recurse -Force
        Write-Host "User config preserved at: $backup"
    }

    $startMenuLink = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\WT Launcher\WT Launcher.lnk'
    if (Test-Path -LiteralPath $startMenuLink) { Remove-Item -LiteralPath $startMenuLink -Force }
    $desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'WT Launcher.lnk'
    if (Test-Path -LiteralPath $desktopLink) { Remove-Item -LiteralPath $desktopLink -Force }
    if (Test-Path -LiteralPath $InstallDir) { Remove-Item -LiteralPath $InstallDir -Recurse -Force }
    Write-Host "Uninstalled: $InstallDir"
    exit 0
}

if (-not (Test-Path -LiteralPath $PublishDir)) { throw "Missing publish directory: $PublishDir" }
$exe = Join-Path $PublishDir 'WTLauncher.App.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Missing published exe: $exe" }
$manifestPath = Join-Path $PublishDir 'update-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Missing update manifest: $manifestPath" }
$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace([string]$manifest.version)) { throw 'Update manifest has no version' }

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $InstallDir -Recurse -Force
$targetExe = Join-Path $InstallDir 'WTLauncher.App.exe'

$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\WT Launcher'
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null
$shell = New-Object -ComObject WScript.Shell
$startMenuShortcut = $shell.CreateShortcut((Join-Path $startMenuDir 'WT Launcher.lnk'))
$startMenuShortcut.TargetPath = $targetExe
$startMenuShortcut.WorkingDirectory = $InstallDir
$startMenuShortcut.Save()

if ($Desktop) {
    $desktopShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'WT Launcher.lnk'))
    $desktopShortcut.TargetPath = $targetExe
    $desktopShortcut.WorkingDirectory = $InstallDir
    $desktopShortcut.Save()
}

$installManifest = [ordered]@{
    schemaVersion = 1
    kind = 'portable-install'
    version = [string]$manifest.version
    installDir = $InstallDir
    installDateUtc = (Get-Date).ToUniversalTime().ToString('o')
    configPreserved = $true
} | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText(
    (Join-Path $InstallDir 'install-manifest.json'),
    $installManifest,
    [System.Text.UTF8Encoding]::new($false))

$template = @'
$ErrorActionPreference = 'Stop'
$installDir = '__INSTALL_DIR__'
$backupRoot = '__BACKUP_ROOT__'
$configSource = Join-Path $installDir 'config'
if (Test-Path -LiteralPath $configSource) {
    $backup = Join-Path $backupRoot ('config-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Force -Path $backup | Out-Null
    Copy-Item -LiteralPath $configSource -Destination $backup -Recurse -Force
    Write-Host "User config preserved at: $backup"
}
$startMenuLink = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\WT Launcher\WT Launcher.lnk'
if (Test-Path -LiteralPath $startMenuLink) { Remove-Item -LiteralPath $startMenuLink -Force }
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'WT Launcher.lnk'
if (Test-Path -LiteralPath $desktopLink) { Remove-Item -LiteralPath $desktopLink -Force }
if (Test-Path -LiteralPath $installDir) { Remove-Item -LiteralPath $installDir -Recurse -Force }
Write-Host "Uninstalled: $installDir"
'@
$uninstallPath = Join-Path $InstallDir 'uninstall.ps1'
[System.IO.File]::WriteAllText(
    $uninstallPath,
    $template.Replace('__INSTALL_DIR__', $InstallDir).Replace('__BACKUP_ROOT__', $backupRoot),
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Installed WT Launcher to $InstallDir"
Write-Host "Uninstall script: $uninstallPath"
Write-Host "User config remains portable and is not migrated by the installer."
