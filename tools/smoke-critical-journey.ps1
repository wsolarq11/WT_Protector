param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [string]$ConfigPath = '',
    [string]$WorkingDirectory = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Exe)) { throw "Published exe missing: $Exe" }
if (-not $WorkingDirectory) { $WorkingDirectory = Split-Path -Parent $Exe }

$smokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('wt-launcher-e2e-' + [guid]::NewGuid().ToString('N'))
$smokeStore = $ConfigPath
if (-not $smokeStore) {
    $smokeStore = Join-Path $smokeRoot 'config\launcher.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $smokeStore) -Force | Out-Null
    $smokeStoreJson = @{
        schemaVersion = 2
        entries = @(@{id='smoke';name='smoke';directory='.';command='echo smoke';selected=$false;updatedAtUtc='';launchedAtUtc=''})
        settings = @{lastFilterText='';sortMode='custom';entryViewMode='cards';requireConfirmationForLaunches=$true;customEntryOrder=@('smoke');searchHistory=@();commandTemplates=@{};window=@{width=1180;height=760;windowState='Normal'}}
        presets = @(@{id='smoke-group';name='Smoke';entryIds=@('smoke')})
        defaultPresetId = 'smoke-group'
    } | ConvertTo-Json -Depth 6
    Set-Content -Encoding UTF8 -LiteralPath $smokeStore -Value $smokeStoreJson
}
if (-not (Test-Path -LiteralPath $smokeStore)) { throw "Smoke config missing: $smokeStore" }

$sp = Start-Process -FilePath $Exe -ArgumentList @('--config', $smokeStore) -WorkingDirectory $WorkingDirectory -WindowStyle Hidden -PassThru
$checks = @()
$exitedClean = $true

try {
    $booted = $false
    for ($i = 0; $i -lt 100; $i++) {
        Start-Sleep -Milliseconds 100
        $sp.Refresh()
        if ($sp.HasExited) { throw "Published app exited with code $($sp.ExitCode) before main window appeared" }
        if ($sp.MainWindowHandle -ne 0) { $booted = $true; break }
    }
    if (-not $booted) { throw 'Main window handle did not appear within 10 seconds' }
    $checks += @{ name = 'launch'; pass = $true; detail = 'process-booted-main-window' }

    $title = ''
    for ($i = 0; $i -lt 30; $i++) {
        $sp.Refresh()
        $title = $sp.MainWindowTitle
        if ($title) { break }
        Start-Sleep -Milliseconds 100
    }
    if ($title -notlike '*WT Launcher*') { throw "Main window title did not match WT Launcher: '$title'" }
    $checks += @{ name = 'title'; pass = $true; detail = "window-title: $title" }
}
finally {
    if (-not $sp.HasExited) {
        $null = $sp.CloseMainWindow()
        if (-not $sp.WaitForExit(5000)) {
            $exitedClean = $false
            Stop-Process -Id $sp.Id -Force
            $sp.WaitForExit(2000) | Out-Null
        }
    }
}

if (-not $exitedClean) { throw 'Published app did not exit gracefully after CloseMainWindow' }

[pscustomobject]@{
    smoke = 'critical-journey'
    exe = $Exe
    config = $smokeStore
    checks = $checks
} | ConvertTo-Json -Depth 4
