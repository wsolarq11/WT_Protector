param()

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot "config-lib.ps1")

$root = Get-RepoRoot
$exe = Get-RepoPath "WT Launcher\WT Launcher.exe"
if (-not (Test-Path -LiteralPath $exe)) { throw "Missing shipped exe: $exe" }

$asm = [Reflection.Assembly]::LoadFile((Resolve-Path -LiteralPath $exe).Path)
$itemType = $asm.GetType("WTLauncher.ButtonItem", $true)
$formType = $asm.GetType("WTLauncher.MainForm", $true)
$method = $formType.GetMethod("TryBuildWtArguments", [Reflection.BindingFlags]"NonPublic, Static")
if ($null -eq $method) { throw "TryBuildWtArguments method not found" }
$resolveConfigPath = $formType.GetMethod("ResolveConfigPath", [Reflection.BindingFlags]"NonPublic, Static")
if ($null -eq $resolveConfigPath) { throw "ResolveConfigPath method not found" }

function New-ItemForTest([string]$Directory, [string]$Command) {
    $item = [Activator]::CreateInstance($itemType)
    $item.name = "test"
    $item.directory = $Directory
    $item.command = $Command
    $item.confirm = $true
    return $item
}

$tests = New-Object System.Collections.Generic.List[object]

$valid = New-ItemForTest $root "Write-Output `"hi`""
$call = @($valid, "", "")
$ok = [bool]$method.Invoke($null, $call)
if (-not $ok) { throw "valid launch item was rejected: $($call[2])" }
if ((([string]$call[1]) -notmatch "pwsh -NoExit -Command")) { throw "valid arguments missing pwsh command: $($call[1])" }
if ((([string]$call[1]) -notmatch '\\"hi\\"')) { throw "valid arguments did not escape embedded quotes: $($call[1])" }
$tests.Add([pscustomobject]@{ name = "valid-quoted-command"; ok = $true }) | Out-Null

$missingDir = Join-Path $root "__missing_launch_dir__"
$invalidDir = New-ItemForTest $missingDir "Write-Output hi"
$call = @($invalidDir, "", "")
$ok = [bool]$method.Invoke($null, $call)
if ($ok) { throw "missing directory launch item was accepted" }
if ([string]::IsNullOrWhiteSpace([string]$call[2])) { throw "missing directory error was empty" }
$tests.Add([pscustomobject]@{ name = "missing-directory-rejected"; ok = $true }) | Out-Null

$emptyCommand = New-ItemForTest $root ""
$call = @($emptyCommand, "", "")
$ok = [bool]$method.Invoke($null, $call)
if ($ok) { throw "empty command launch item was accepted" }
if ([string]::IsNullOrWhiteSpace([string]$call[2])) { throw "empty command error was empty" }
$tests.Add([pscustomobject]@{ name = "empty-command-rejected"; ok = $true }) | Out-Null

$releaseStartupPath = Get-RepoPath "WT Launcher"
$expectedRootConfig = Get-LauncherConfigPath
$resolved = [string]$resolveConfigPath.Invoke($null, [object[]]@([string]$releaseStartupPath))
if ($resolved -ne $expectedRootConfig) {
    throw "release layout did not resolve canonical config: expected=$expectedRootConfig actual=$resolved"
}
$tests.Add([pscustomobject]@{ name = "release-layout-uses-root-config"; ok = $true }) | Out-Null

$standaloneDir = Join-Path ([IO.Path]::GetTempPath()) ("wt-launcher-standalone-" + [Guid]::NewGuid().ToString("N"))
try {
    New-Item -ItemType Directory -Force -Path $standaloneDir | Out-Null
    $standaloneConfigDir = Join-Path $standaloneDir "config"
    New-Item -ItemType Directory -Force -Path $standaloneConfigDir | Out-Null
    $standaloneConfig = Join-Path $standaloneConfigDir "config.json"
    "[]" | Set-Content -Encoding UTF8 -LiteralPath $standaloneConfig
    $resolved = [string]$resolveConfigPath.Invoke($null, [object[]]@([string]$standaloneDir))
    if ($resolved -ne $standaloneConfig) {
        throw "standalone layout did not resolve local config: expected=$standaloneConfig actual=$resolved"
    }
    $tests.Add([pscustomobject]@{ name = "standalone-layout-uses-local-config"; ok = $true }) | Out-Null
}
finally {
    if (Test-Path -LiteralPath $standaloneDir) {
        Remove-Item -LiteralPath $standaloneDir -Recurse -Force
    }
}

[pscustomobject]@{
    status = "PASS"
    exe = Resolve-Path -LiteralPath $exe | ForEach-Object { $_.Path }
    tests = $tests
} | ConvertTo-Json -Depth 5
