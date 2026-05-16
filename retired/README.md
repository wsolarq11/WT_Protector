# Retired artifacts

This directory is for launcher artifacts that are intentionally no longer on the active path.

- `WT Launcher.hta` — archived legacy HTA launcher. The active launcher is `WT Launcher/WT Launcher.exe`.
- `config.json` — HTA-local config for the archived launcher. The HTA resolves `config.json` beside itself, so this file is kept here only to make the retired snapshot self-contained for manual/reference use.

`retired/config.json` is not the active launcher config, not a fallback for the WinForms executable, and not synced by `tools/sync-config.ps1`. The active config remains the repository-root `config.json`.
