# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command.

## What this project contains

- `WT Launcher/Program.cs` — current WinForms launcher implementation.
- `WT Launcher/WT Launcher.exe` — built WinForms executable.
- `retired/WT Launcher.hta` — retired legacy HTA snapshot kept only for archive/reference.
- `retired/config.json` — retired HTA-local config used only when manually opening the archived HTA; it is not an active executable fallback.
- `config.json` — canonical launcher configuration with multiple launch entries.
- `WT Launcher/config.json` — legacy executable-local fallback/export config for standalone copies; the normal release layout uses the root `config.json`.
- `WT_Shortcuts_Backup/` — backup `.lnk` shortcuts that inspired the launcher; these are data artifacts, not source code.
- `tools/verify.ps1` — no-interaction local verification gate.
- `tools/config-lib.ps1` — shared helper functions used by the verification and config tools.
- `docs/giants-shoulders/` — "巨人肩膀库": curated external project lessons and source links for future stack/reflection decisions.

## Configuration schema

Each config file is a JSON array of launch items:

```json
[
  {
    "name": "example",
    "directory": "D:\\path\\to\\workspace",
    "command": "codex --enable goals --dangerously-bypass-approvals-and-sandbox",
    "confirm": true
  }
]
```

Fields:

- `name` — display name in the launcher UI.
- `directory` — working directory passed to `wt.exe -d`.
- `command` — command passed to `pwsh -NoExit -Command`.
- `confirm` — when `true`, the UI asks before launching.

## Build and verification

Run the project gate from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

The verifier currently checks:

- `Program.cs` compiles with `.NET Framework` `csc.exe`.
- shipped `WT Launcher.exe` is up to date with `Program.cs`.
- launch argument validation/quoting smoke tests pass without opening the GUI.
- the legacy HTA is no longer active at the repository root and is archived under `retired/`.
- JSON configs parse successfully.
- root config is treated as the canonical source; the executable-local fallback/export status is reported without modifying user data.
- entries that combine `dangerously*` commands with `confirm: false` are reported without modifying user data.
- the archived HTA and historical backup HTA have not drifted when both are present.
- the retired HTA-local `retired/config.json` parses successfully and keeps dangerous commands behind confirmation.
- stale risk patterns such as duplicate event-handler unregister attempts do not return.
- HTA-derived UX invariants stay present in the active executable: collapsed sidebar by default, two high-signal stat cards, ellipsis-truncated card text, no hover layout shift, and a roomier edit dialog.
- this README keeps the core operator sections present.

Manual build is still available through `WT Launcher/build.bat`, but the script pauses for interactive use. Prefer `tools/verify.ps1` for repeatable validation.

Build the shipped executable from current source without interactive pause:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

The release build compiles to a temporary executable first, backs up the current `WT Launcher/WT Launcher.exe`, then replaces it.

## Config sync helper

The WinForms executable first looks for the root `config.json` next to this README and falls back to `WT Launcher/config.json` only when it is copied away from the release layout. Root config is the single active source in the normal layout.

Use the helper only when you intentionally want to export the root config into the executable-local fallback file for a standalone copy.

Check what would happen without changing files:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\sync-config.ps1
```

Apply/export the root config to the executable-local fallback only when you intentionally want that standalone snapshot:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\sync-config.ps1 -Apply
```

`-Apply` backs up the current executable-local fallback before copying when that fallback already exists.

## Config hardening helper

Check root config entries that would be changed from `confirm: false` to `confirm: true` because they contain a `dangerously*` command. A healthy hardened config reports zero changes needed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\harden-config.ps1
```

Apply that hardening only when you intentionally want the extra launch confirmation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\harden-config.ps1 -Apply
```

`-Apply` backs up the root config before rewriting it. Run `tools/sync-config.ps1 -Apply` afterwards only if you also need to refresh the executable-local fallback/export copy.

## Safety notes

This launcher intentionally starts arbitrary local commands. Treat every config file as trusted local input.

Important risks:

- Commands such as `codex --dangerously-bypass-approvals-and-sandbox` are powerful and should normally keep `confirm: true` unless the entry is intentionally trusted.
- `tools/verify.ps1` reports entries where a `dangerously*` command is paired with `confirm: false`; after hardening this should stay at zero.
- `directory` must exist before the WinForms launcher starts an item.
- Command text is quoted before being passed to `wt.exe`, but it is still a shell command; review entries before launching.
- The root `config.json` is the canonical source. `WT Launcher/config.json` is a legacy fallback/export snapshot, not a second active source.
- `retired/config.json` belongs only to `retired/WT Launcher.hta`. It may be kept with the archived HTA for historical/manual reference, but it is not read by the active WinForms executable.

## Current implementation status

The WinForms app supports:

- add/edit/delete launch items;
- single launch and batch launch;
- launch confirmation;
- JSON persistence with `.bak` backup;
- light/dark theme preference;
- no-interaction compile/config verification via `tools/verify.ps1`.
- root config is the active source of truth, with dangerous commands requiring launch confirmation.
- the legacy HTA is retired under `retired/` and is not part of the active launcher path.
- the active WinForms UI now ports the retired HTA's lower-friction feel: less chrome, larger card text, clipped long paths/commands, stable hover, and a roomier edit dialog.

Known remaining work:

- add UI-level smoke tests if the tool becomes shared beyond this machine;
- replace the interactive `build.bat` with a release-oriented build script if distribution is needed.
