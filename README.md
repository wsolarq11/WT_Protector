
# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command.

## What this project contains

- `src/avalonia-launcher/` — active mainline Avalonia/.NET launcher source, tests, run script, and publish gate.
- `config/config.json` — canonical human-readable launcher configuration with launch entries and persistent checkbox state.
- `config/presets.json` — canonical human-readable named launch groups built from `config/config.json` entry ids.
- `config/settings.json` — portable UI preferences such as theme, last search text, and window size.
- `retired/WT Launcher.hta` — retired legacy HTA snapshot kept only for archive/reference.
- `retired/config/config.json` — retired HTA-local config used only when manually opening the archived HTA.
- `retired/design/winforms-launcher.md` — retired WinForms executable/source design lessons and receipts.
- `docs/reference/shortcut-backup-manifest.json` — human-readable replacement for the deleted `WT_Shortcuts_Backup/` binary shortcut tree.
- `tools/verify.ps1` — no-interaction local verification gate.
- `tools/config-lib.ps1` — shared helper functions used by verification and config tools.
- `docs/giants-shoulders/` — "巨人肩膀库": curated external project lessons and source links for future stack/reflection decisions.
- `omx_wiki/` — project knowledge base for durable local decisions, patterns, and learning summaries.

The old root `WT Launcher.exe`, old `src/winforms-launcher/`, and old `spikes/` directory are intentionally retired. The former Avalonia spike is now the source mainline under `src/avalonia-launcher/`.

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

Named launch groups live beside the launch config:

```json
{
  "schemaVersion": 1,
  "defaultPresetId": "core-projects",
  "presets": [
    {
      "id": "core-projects",
      "name": "主线维护",
      "entryIds": ["WT_Protector"],
      "locked": false
    }
  ]
}
```

Preset fields:

- `schemaVersion` — currently `1`.
- `defaultPresetId` — startup UI selection; applying it is explicit.
- `presets[].id` — stable human-readable launch group identity.
- `presets[].entryIds` — stable `config/config.json` entry ids; `tools/verify.ps1` rejects unknown or duplicate references.
- `presets[].locked` — optional UI guard for system groups such as `blank-manual`; locked groups can still be applied but cannot be updated or deleted in the app.

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

## Run the mainline app

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

The app reads root `config/config.json`, applies and edits named launch groups from `config/presets.json`, persists selected checkboxes back to `config/config.json`, and stores portable UI preferences in `config/settings.json`. It does not use AppData or the registry for launcher state.

## Build and verification

Run the project gate from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

The verifier currently checks:

- old `WT Launcher.exe`, `src/winforms-launcher/`, and `spikes/` do not return;
- `src/avalonia-launcher/` has the solution, app project, tests project, and mainline verify script;
- root `config/` parses, item `id` values are unique, persistent `selected` state remains part of the contract, and `presets.json` references only known entry ids;
- dangerous commands keep `confirm: true`;
- `retired/design/winforms-launcher.md` exists with the absorbed WinForms design lessons;
- retired HTA and shortcut-backup reference material remain readable;
- Avalonia mainline restore/test/isolated verification publish passes, including launch-argument tests and a hidden temp-config startup smoke of the published exe.

The final verifier line is a JSON receipt with `schemaVersion`, `kind`, `status`, `repoHead`, `worktreeDirty`, and `worktreeStatus` fields before tool-specific evidence. The Avalonia mainline receipt uses the same schema and is checked by the root gate.

The verifier publishes into isolated ignored output under `src/avalonia-launcher/artifacts/verify-publish/win-x64/` so a running release executable does not block the gate. Build/publish the stable mainline executable without running the whole root gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

The publish output is ignored local build output under:

```text
src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe
```

If that stable executable is already running, `tools/build-release.ps1` stops before publishing and reports the blocking PID(s); keep using `tools/verify.ps1` for routine gates, or close the running app before a stable release publish.

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

## WinForms retirement

The former WinForms mainline has been retired into documentation instead of kept as a second source tree:

- old root executable: deleted;
- old `src/winforms-launcher/Program.cs`: deleted;
- useful design lessons and receipts: `retired/design/winforms-launcher.md`.

Do not restore the old WinForms path unless the Avalonia mainline fails the same native gate and there is an explicit rollback decision.

## Safety notes

This launcher intentionally starts arbitrary local commands. Treat every config file as trusted local input.

Important risks:

- Commands such as `codex --dangerously-bypass-approvals-and-sandbox` and `claude --dangerously-skip-permissions` are powerful and should normally keep `confirm: true` unless the entry is intentionally trusted.
- `tools/verify.ps1` reports entries where a `dangerously*` command is paired with `confirm: false`; after hardening this should stay at zero.
- `directory` must exist before the launcher starts an item.
- Command text is quoted before being passed to `wt.exe`, but it is still a shell command; review entries before launching.
- Applying a preset only rewrites checkbox state; launching remains a separate explicit action.
- The root `config/` directory is the canonical source.
- Using the app can legitimately dirty tracked `config/config.json`, `config/presets.json`, and `config/settings.json`; review that user/app working set before committing or reverting it.
- `retired/config/config.json` belongs only to `retired/WT Launcher.hta` and is not read by the Avalonia mainline.

## Current implementation status

The Avalonia mainline supports:

- search and direct card launch;
- single launch and batch launch;
- header actions for selecting all visible items or clearing the saved selection;
- named launch groups/presets that replace the saved selection through explicit apply;
- first-class launch group actions: save current selection as a new group, update the current group from current selection, and delete editable groups;
- live preset-vs-selection difference feedback before applying a launch group, with no-op apply disabled when already matched;
- launch confirmation;
- JSON persistence with `.bak` backup;
- persistent selected checkbox state for one-click/batch launch;
- portable `config/settings.json` UI preferences without AppData or registry writes;
- Core/App/Tests separation around launch argument rules and config behavior;
- self-contained `win-x64` publish through `dotnet publish`.

Known remaining work:

- add/edit/delete launch item UI if editing inside the app becomes more valuable than direct JSON editing;
- repeat screenshot-backed comfort passes only after UI changes, without weakening the root `tools/verify.ps1` gate.
