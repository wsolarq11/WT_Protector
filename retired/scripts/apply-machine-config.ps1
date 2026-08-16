
param([switch]$DryRun)
# task 07-10-winget-machine-ready R2: thin wrapper over `winget configure`.
# Runs the DSC yaml at repo root. -DryRun validates without applying.
#
# Why a wrapper: resolves the yaml path from anywhere, documents the
# dry-run escape hatch, and keeps "how do I set up a fresh machine?"
# answerable in one command from the repo root.
#
# `winget configure` is interactive by design — it prompts the user to
# accept the configuration warning before applying. We do NOT pass
# --accept-configuration-agreements; the user sees every step.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$yaml = Join-Path (Split-Path -Parent $PSScriptRoot) '.wt-protector.dsc.yaml'
if (-not (Test-Path -LiteralPath $yaml)) { throw "Missing DSC yaml: $yaml" }

if ($DryRun) {
    winget configure validate -f $yaml
} else {
    winget configure -f $yaml
}
