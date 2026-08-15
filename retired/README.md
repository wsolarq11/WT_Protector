# Retired artifacts

This directory holds launcher artifacts that are intentionally off the active path — archive-only, never restore.

- `retired/config/config.json` — HTA-local config of the legacy launcher. The HTA launcher snapshot itself was
  removed 2026-07-06 (commit `825ecb5b`); this config remains as the only HTA-era artifact.
- `design/winforms-launcher.md` — absorbed design lessons and receipts from the retired WinForms
  executable/source line.

`retired/config/config.json` is not the active launcher config and not a fallback for the Avalonia
mainline. The active config is `config/launcher.json` at the repository root (entries + presets +
settings + `defaultPresetId` as one atomic unit).
