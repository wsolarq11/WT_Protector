
# Retired artifacts

This directory is for launcher artifacts that are intentionally no longer on the active path.

- `WT Launcher.hta` — archived legacy HTA launcher.
- `config/config.json` — HTA-local config for the archived launcher. The HTA resolves this file under its local `config/` directory, so the retired snapshot stays self-contained for manual/reference use.
- `design/winforms-launcher.md` — absorbed design lessons and receipts from the retired WinForms executable/source line.

`retired/config/config.json` is not the active launcher config and not a fallback for the Avalonia mainline. The active config remains `config/config.json` at the repository root.
