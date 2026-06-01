param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. (Join-Path $PSScriptRoot 'config-lib.ps1')

$root = Get-RepoRoot
$snapshotPath = Join-Path $root 'docs\reference\github-similar-projects-snapshot.json'
$fields = 'nameWithOwner,url,description,stargazerCount,forkCount,pushedAt,updatedAt,primaryLanguage,isArchived'
$evidenceCommand = "gh repo view <owner>/<repo> --json $fields"

$repoNotes = @(
    @{
        fullName = 'SyDroX/dev-layout'
        classification = 'direct WT workspace launcher'
        yardstickVerdict = 'direct-narrow-weaker'
        strongerAxes = @('fixed Claude Code multi-repo layout', 'session resume', 'cd blocking')
        wtLauncherAxes = @('editable portable UI entries', 'named launch groups', 'rollback-aware JSON persistence', 'native gate')
    },
    @{
        fullName = 'augustdragon/terminal-manager'
        classification = 'direct WT profile/session manager'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('WT profile editing', 'split-pane groups', 'import/export', 'WT settings sync')
        wtLauncherAxes = @('smaller local JSON-to-wt contract', 'no local web server', 'does not mutate Windows Terminal settings')
    },
    @{
        fullName = 'noambrand/Launchpad-CLI'
        classification = 'Claude Code launcher/installer'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('installer', 'folder picker', 'right-click launch', 'statusline distribution')
        wtLauncherAxes = @('portable editable WT workspace groups', 'rollback-aware config gate', 'smaller app scope')
    },
    @{
        fullName = 'kevinkicho/win-screen-setup'
        classification = 'Windows multi-monitor workspace launcher'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('visual screen/app positioning')
        wtLauncherAxes = @('narrower side-effect surface', 'portable launch groups', 'native repo gate')
    },
    @{
        fullName = 'ewdieckman/DevWorkspace'
        classification = 'PowerShell WT multi-pane workspace module'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('fixed split-pane layouts')
        wtLauncherAxes = @('editable GUI entries', 'rollback-aware persistence', 'native publish/verify gate')
    },
    @{
        fullName = 'jsoulet42/claude-launcher-app'
        classification = 'Claude Code WT TUI workspace launcher'
        yardstickVerdict = 'direct-narrow-weaker'
        strongerAxes = @('adjacent TUI flow')
        wtLauncherAxes = @('portable editable GUI entries', 'named groups', 'rollback-aware JSON', 'native gate')
    },
    @{
        fullName = 'A-ema755/claude-dashboard'
        classification = 'Claude Code CLI session launcher/preset manager'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('split-pane layouts', 'cost tracking')
        wtLauncherAxes = @('small local WT launcher contract', 'portable GUI config', 'native gate')
    },
    @{
        fullName = 'adityacodepublic/wtsm-cli'
        classification = 'Windows Terminal session manager CLI'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('named terminal sessions CLI')
        wtLauncherAxes = @('GUI editing', 'launch-group preview', 'repo-native rollback/verify evidence')
    },
    @{
        fullName = 'pramin/AgentSpace'
        classification = 'Avalonia agent workspace launcher'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('embedded PTY', 'SSH file tree', 'code preview')
        wtLauncherAxes = @('smaller WT handoff scope', 'portable JSON groups', 'native gate')
    },
    @{
        fullName = 'joyahmed/devgo-app'
        classification = 'WSL-first developer launcher'
        yardstickVerdict = 'adjacent-axis-stronger'
        strongerAxes = @('WSL', 'VS Code', 'tmux workflows', 'installer ambition')
        wtLauncherAxes = @('verified portable WT launch groups', 'rollback-aware JSON', 'narrower scope')
    },
    @{
        fullName = 'Flow-Launcher/Flow.Launcher'
        classification = 'broad Windows launcher'
        yardstickVerdict = 'broad-market-stronger'
        strongerAxes = @('market proof', 'plugins', 'search', 'packaging', 'community')
        wtLauncherAxes = @('narrow portable WT workspace-launcher slice only')
    },
    @{
        fullName = 'Wox-launcher/Wox'
        classification = 'broad cross-platform launcher'
        yardstickVerdict = 'broad-market-stronger'
        strongerAxes = @('market proof', 'cross-platform packaging', 'plugin/theme stores', 'AI/MCP direction')
        wtLauncherAxes = @('narrow portable WT workspace-launcher slice only')
    }
)

$repositories = foreach ($note in $repoNotes) {
    $fullName = [string]$note.fullName
    $raw = & gh repo view $fullName --json $fields
    if ($LASTEXITCODE -ne 0) { throw "gh repo view failed for $fullName with exit code $LASTEXITCODE" }

    $repo = $raw | ConvertFrom-Json
    $language = $null
    if ($null -ne $repo.primaryLanguage) { $language = [string]$repo.primaryLanguage.name }

    [ordered]@{
        fullName = [string]$repo.nameWithOwner
        url = [string]$repo.url
        description = [string]$repo.description
        language = $language
        stars = [int]$repo.stargazerCount
        forks = [int]$repo.forkCount
        pushedAtUtc = [string]$repo.pushedAt
        updatedAtUtc = [string]$repo.updatedAt
        archived = [bool]$repo.isArchived
        classification = [string]$note.classification
        yardstickVerdict = [string]$note.yardstickVerdict
        strongerAxes = @($note.strongerAxes)
        wtLauncherAxes = @($note.wtLauncherAxes)
    }
}

$capturedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$snapshot = [ordered]@{
    schemaVersion = 2
    capturedAtUtc = $capturedAtUtc
    evidenceCommand = $evidenceCommand
    refreshCommand = 'powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\update-github-similar-projects-snapshot.ps1'
    queries = @(
        'Windows Terminal workspace launcher',
        'Windows Terminal session manager',
        'Claude Code launcher'
    )
    scope = "Representative similar GitHub projects for WT Launcher's market/north-star audit; not a universal ranking of every launcher repository."
    repositories = @($repositories)
}

$json = ($snapshot | ConvertTo-Json -Depth 8) -replace "`r`n", "`n"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($snapshotPath, "$json`n", $utf8NoBom)

New-ToolReceipt 'github-similar-projects-snapshot-refresh' @{
    path = 'docs\reference\github-similar-projects-snapshot.json'
    capturedAtUtc = $capturedAtUtc
    repositories = @($repositories).Count
    flowStars = [int](@($repositories) | Where-Object { $_.fullName -eq 'Flow-Launcher/Flow.Launcher' } | Select-Object -First 1).stars
    woxStars = [int](@($repositories) | Where-Object { $_.fullName -eq 'Wox-launcher/Wox' } | Select-Object -First 1).stars
} | ConvertTo-Json -Depth 4 -Compress
