# WT Launcher / WT Protector

Local Windows Terminal workspace launcher: open saved folders with a chosen shell command. Avalonia/.NET 10, win-x64 self-contained.

Documentation map: [`docs/README.md`](docs/README.md).

## Configuration schema

Single authoritative store: `config/launcher.json` (`LauncherStoreFile`, `schemaVersion: 2`). Top level: `entries[]`, `presets[]`, `settings{}`, `defaultPresetId`, `runHistory[]`.

```json
{
  "schemaVersion": 2,
  "entries": [{
    "id": "example",
    "name": "example",
    "directory": "D:\\path\\to\\workspace",
    "command": "$codex",
    "selected": false,
    "updatedAtUtc": "2026-05-18T09:30:00.0000000Z",
    "launchedAtUtc": ""
  }],
  "settings": {},
  "presets": [],
  "defaultPresetId": "blank-manual"
}
```

Key rules:

- `directory` is the working directory passed to `wt.exe -d`; it must exist on this machine.
- `command` is passed through `pwsh -NoExit -EncodedCommand` (base64 quoting). `$key` references resolve against `settings.commandTemplates`; a missing key is a launch validation error, not silent.
- `selected` is persistent checkbox state used by one-click/batch launch.
- No obsolete per-entry `confirm` key: launch confirmation has one authority, `settings.requireConfirmationForLaunches`.
- `presets[].entryIds` reference known entry ids only; duplicate or unknown ids are rejected.
- `settings.sortMode ∈ {custom, recent, updated, alphabetical, frequent}`; `settings.entryViewMode ∈ {cards, details, names}`.
- Unknown JSON keys are skipped at load (forward compat); source-gen writes only known fields (strict write).
- `settings.window.width` / `height` must be positive.

Live `config/launcher.json` is git-ignored runtime state; `config/launcher.example.json` is the machine-agnostic template. No AppData, no registry, no DB.

## Build and verification

```bash
npm run verify            # full gate: static blocks; local and snapshot are advisory; native skips without .NET SDK
npm run verify:mainline   # Avalonia gate only
npm run test:fast         # CoreUnit + PropertyTest + Architecture + Analyzer
npm run test:coverage
npm run test:launch-args
npm run smoke             # published exe critical journey
npm run release:build     # AOT exe -> artifacts/publish/win-x64/
npm run release:contract  # CI release contract: locked restore + AOT publish + clean tree + receipt
```

Distributable: `src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe`. `artifacts/verify-publish/` is the isolated verifier output, not the distributable.

The verifier finishes with a JSON receipt: `schemaVersion`, `kind`, `status`, `repoHead`, `worktreeDirty`, `worktreeStatus`.

## Run the mainline app

```bash
npm run start
```

Or explicit config:

```powershell
.\src\avalonia-launcher\artifacts\publish\win-x64\WTLauncher.App.exe --config .\config\launcher.json
```

Startup args are strict: one optional `--config <path>` pair (`--config path` and `--config=path` both accepted). Duplicate `--config`, missing value, and positional arguments are rejected; unknown `--` flags are warned and ignored.

## WinForms retirement

The former WinForms mainline is retired to documentation, not kept as a second source tree. Old root executable and `src/winforms-launcher/Program.cs` are deleted; retained lessons live in `retired/design/winforms-launcher.md`. Do not restore retired paths unless the Avalonia line fails the same native gate and there is an explicit rollback decision.

## Safety notes

This launcher intentionally starts arbitrary local commands. Treat every config file as trusted local input.

- Launch confirmation is global: on means every item asks; off means every item launches directly.
- Single/batch launch preflight every target before starting. Invalid plans are excluded and shown as failures; external start failures continue to remaining items and are collected.
- "已请求启动" means the `wt.exe` process start was requested, not that the terminal session finished healthy.
- "预演" shows generated `wt.exe` command lines without starting processes or rewriting selection.
- Applying a preset only rewrites checkbox state; the group launch button launches selected entries directly.
- Applying/previewing/launching a preset discards any uncommitted drag-sort preview first.
- JSON saves go through temp-file + `File.Replace`; `StoreGrain.Dispatch` saves before committing memory, so a failed save leaves memory unchanged.
- Config save failures stay visible as `配置保存失败` and roll back selection plus tracked config.
- App usage writes only the git-ignored `config/launcher.json`; tracked files stay clean.

## Current implementation status

The Avalonia mainline supports search, single/batch launch, adaptive cards, custom drag reorder, three density states, Cards/Details/Names views, add/edit/delete/clone, AI Agent CLI discovery, per-agent badges, launch groups/presets, five sort modes, command templates, global confirmation, quick-launch hotkey, tray icon, config export/import, run history, update checks, and a portable installer/uninstaller.

Claim boundary:

- The feature audit classifies all 22 active feature mappings as narrow-domain north-star for the portable Windows Terminal workspace-launcher scope only.
- The full absolute objective remains not complete as written because unimprovable-product, stronger-than-every-GitHub-similar, and absolute zero-side-effect claims are still blocked by current evidence.
- This project does not claim to be unimprovable, stronger than every GitHub similar, or free of side effects after starting arbitrary external processes.
