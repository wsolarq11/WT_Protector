# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command.

## What this project contains

- `WT Launcher.exe` — active built WinForms executable at the repository root.
- `src/winforms-launcher/Program.cs` — current WinForms launcher implementation.
- `config/config.json` — canonical human-readable launcher configuration with launch entries and persistent checkbox state.
- `config/settings.json` — portable UI preferences such as theme, last search text, and window size.
- `retired/WT Launcher.hta` — retired legacy HTA snapshot kept only for archive/reference.
- `retired/config/config.json` — retired HTA-local config used only when manually opening the archived HTA.
- `docs/reference/shortcut-backup-manifest.json` — human-readable replacement for the deleted `WT_Shortcuts_Backup/` binary shortcut tree.
- `tools/verify.ps1` — no-interaction local verification gate.
- `tools/config-lib.ps1` — shared helper functions used by the verification and config tools.
- `docs/giants-shoulders/` — "巨人肩膀库": curated external project lessons and source links for future stack/reflection decisions.
- `spikes/avalonia-launcher/` — non-active Avalonia/.NET 10 launcher spike for experience testing before any active-path migration.

The old `WT Launcher/` and `WT_Shortcuts_Backup/` directories are intentionally removed. Recreate neither as an active source of truth.

## Configuration schema

Each active config file is a JSON array of launch items:

```json
[
  {
    "id": "example",
    "name": "example",
    "directory": "D:\\path\\to\\workspace",
    "command": "codex --enable goals --dangerously-bypass-approvals-and-sandbox",
    "confirm": true,
    "selected": false
  }
]
```

Fields:

- `id` — stable human-readable identity for future grouping/sorting/migration.
- `name` — display name in the launcher UI.
- `directory` — working directory passed to `wt.exe -d`.
- `command` — command passed to `pwsh -NoExit -Command`.
- `confirm` — when `true`, the UI asks before launching.
- `selected` — persistent checkbox state used by the one-click/batch launch flow.

Portable UI settings live beside the launch config:

```json
{
  "theme": "light",
  "lastFilterText": "",
  "window": {
    "width": 1180,
    "height": 760
  }
}
```

Normal layout is therefore simply:

```text
WT Launcher.exe
config/
  config.json
  settings.json
```

If `WT Launcher.exe` is copied away from this repository, copy a sibling `config/` directory with it. The executable first looks upward for repository-root `config/config.json` next to `README.md`, then falls back to executable-local `config/config.json` for standalone portable copies.

## Build and verification

Run the project gate from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

The verifier currently checks:

- `src/winforms-launcher/Program.cs` compiles with `.NET Framework` `csc.exe`.
- shipped `WT Launcher.exe` is up to date with `Program.cs`.
- launch argument validation/quoting smoke tests pass without opening the GUI.
- root `config/` is the only active portable config directory.
- deleted debt directories `WT Launcher/` and `WT_Shortcuts_Backup/` do not return.
- shortcut-backup knowledge has been converted into `docs/reference/shortcut-backup-manifest.json` and selected unique entries in `config/config.json`.
- JSON configs parse successfully and config item `id` values are unique.
- entries that combine `dangerously*` commands with `confirm: false` are reported without modifying user data.
- the retired HTA remains archived under `retired/` and has its own retired-local config.
- stale risk patterns such as duplicate event-handler unregister attempts do not return.
- HTA-derived UX invariants stay present in the active executable: collapsed sidebar by default, two high-signal stat cards, ellipsis-truncated card text, no hover layout shift, and a roomier edit dialog.
- this README keeps the core operator sections present.

Build the shipped executable from current source without interactive pause:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

The release build compiles to a temporary executable first, backs up the current root `WT Launcher.exe`, then replaces it. Backup files match `*.bak-*` and are ignored as local rollback scratch.

## Config hardening helper

Check canonical `config/config.json` entries that would be changed from `confirm: false` to `confirm: true` because they contain a `dangerously*` command. A healthy hardened config reports zero changes needed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\harden-config.ps1
```

Apply that hardening only when you intentionally want the extra launch confirmation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\harden-config.ps1 -Apply
```

`-Apply` backs up `config/config.json` before rewriting it.

## Shortcut backup retirement

`WT_Shortcuts_Backup/` used to hold binary `.lnk` files and an HTA/config snapshot. It is now removed.

The useful parts were converted as follows:

- executable HTA snapshot -> `retired/WT Launcher.hta`;
- retired HTA config -> `retired/config/config.json`;
- unique launch targets worth keeping active -> appended to `config/config.json` with stable `id` values;
- remaining `.lnk` target/argument details -> `docs/reference/shortcut-backup-manifest.json`.

Do not reintroduce `WT_Shortcuts_Backup/`. If a shortcut contains a launch target worth using, add it directly to `config/config.json`; if it is historical evidence only, update the reference manifest.

## Safety notes

This launcher intentionally starts arbitrary local commands. Treat every config file as trusted local input.

Important risks:

- Commands such as `codex --dangerously-bypass-approvals-and-sandbox` and `claude --dangerously-skip-permissions` are powerful and should normally keep `confirm: true` unless the entry is intentionally trusted.
- `tools/verify.ps1` reports entries where a `dangerously*` command is paired with `confirm: false`; after hardening this should stay at zero.
- `directory` must exist before the WinForms launcher starts an item.
- Command text is quoted before being passed to `wt.exe`, but it is still a shell command; review entries before launching.
- The root `config/` directory is the canonical source.
- `retired/config/config.json` belongs only to `retired/WT Launcher.hta`. It may be kept with the archived HTA for historical/manual reference, but it is not read by the active WinForms executable.

## Current implementation status

The WinForms app supports:

- add/edit/delete launch items;
- single launch and batch launch;
- launch confirmation;
- JSON persistence with `.bak` backup;
- persistent selected checkbox state for one-click/batch launch;
- portable `config/settings.json` UI preferences without AppData or registry writes;
- no-interaction compile/config verification via `tools/verify.ps1`;
- root `config/` as the active source of truth, with dangerous commands requiring launch confirmation;
- retired HTA under `retired/`, outside the active launcher path;
- HTA-derived lower-friction feel: less chrome, larger card text, clipped long paths/commands, stable hover, and a roomier edit dialog.

Known remaining work:

- add UI-level smoke tests if the tool becomes shared beyond this machine;
- decide whether the Avalonia spike should replace WinForms only after it clearly wins the same gate.
