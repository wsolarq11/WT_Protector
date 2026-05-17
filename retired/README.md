# Retired artifacts

This directory is for launcher artifacts that are intentionally no longer on the active path.

- `WT Launcher.hta` — archived legacy HTA launcher. The active launcher is `WT Launcher/WT Launcher.exe`.
- `config/config.json` — HTA-local config for the archived launcher. The HTA resolves this file under its local `config/` directory, so the retired snapshot stays self-contained for manual/reference use.

`retired/config/config.json` is not the active launcher config, not a fallback for the WinForms executable, and not synced by `tools/sync-config.ps1`. The active config remains `config/config.json` at the repository root.
