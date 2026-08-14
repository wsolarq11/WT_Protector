# WT Launcher / WT Protector

A local Windows Terminal launcher for quickly opening saved workspaces with a chosen shell command.

## What this project contains

- `src/avalonia-launcher/` — active mainline Avalonia/.NET launcher source, tests, run script, and publish gate.
- `config/launcher.example.json` — machine-agnostic template of the unified launcher store (zero entries, locked blank-manual preset). The live `config/launcher.json` is git-ignored runtime state (desensitized 2026-08-02: it carries local absolute paths); friends and fresh clones start from the example.
- `docs/使用说明.txt` — friend-facing quick start (zero jargon, three steps to first launch); packaged into the friend zip.
- `retired/WT Launcher.hta` — retired legacy HTA snapshot kept only for archive/reference.
- `retired/config/config.json` — retired HTA-local config used only when manually opening the archived HTA.
- `retired/design/winforms-launcher.md` — retired WinForms executable/source design lessons and receipts.
- `docs/reference/shortcut-backup-manifest.json` — human-readable replacement for the deleted `WT_Shortcuts_Backup/` binary shortcut tree.
- `docs/feature-reality-north-star-audit.md` — feature-by-feature reality mapping and north-star audit for the active launcher scope.
- `docs/feature-first-class-upgrade-audit.md` — current all-capability real-problem and first-class upgrade audit.
- `.snow/` — ignored local runtime/index state; Git and verification intentionally do not track or validate it.
- `.gitattributes` — repository line-ending policy; text is normalized as LF to avoid CRLF-only churn.
- `.wt-protector.dsc.yaml` — WinGet Configuration file declaring fresh-machine prerequisites (Windows Terminal + PowerShell 7); applied via `winget configure` or `tools/apply-machine-config.ps1`.
- `tools/verify.ps1` — no-interaction local verification gate.
- `tools/config-lib.ps1` — shared helper functions used by verification and config tools.
- `tools/apply-machine-config.ps1` — thin wrapper that applies (or `-DryRun` validates) `.wt-protector.dsc.yaml` for fresh-machine setup.
- `docs/reference/github-similar-projects-snapshot.json` — current GitHub similar-project evidence used by the feature north-star audit.
- `tools/update-github-similar-projects-snapshot.ps1` — refreshes that market snapshot through `gh repo view`; the root gate rejects stale snapshots.
- `docs/giants-shoulders/` — "巨人肩膀库": curated external project lessons and source links for future stack/reflection decisions.
  The old root `WT Launcher.exe`, old `src/winforms-launcher/`, and old `spikes/` directory are intentionally retired. The former Avalonia spike is now the source mainline under `src/avalonia-launcher/`.

## Architecture

The project uses a 5-layer architecture under `src/avalonia-launcher/`:

| Layer | Project | Responsibilities |
|-------|---------|-----------------|
| Core | `WTLauncher.Core` | Data records, `LaunchArgumentBuilder`, `LauncherJson` (atomic JSON persistence), `LauncherStore` (schema validation), `AgentDiscoveryScanner`, design tokens |
| Commands | `WTLauncher.App/ViewModels/` | `MainWindowViewModel` + partials handle add/edit/delete/launch via `StoreGrain` |
| Persistence | `StoreGrain` | Single state container, SemaphoreSlim thread safety, rollback on save failure |
| Orchestration | `MainWindowViewModel` | Single writable owner of UI state, delegates to Core rules and Services |
| Views | `WTLauncher.App/Views/` | XAML, drag ghost, code-behind windowing only |

Key architecture decisions:

- **State management**: `StoreGrain` (single state container with `Dispatch`/`DispatchAsync`/`DispatchErrorOr` for mutations and `Select`/`SelectAsync` for reads). CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` for ViewModel bindings. Fluxor was removed 2026-07-23.
- **Persistence**: Single `config/launcher.json` file. Atomic writes via temp-file + `File.Replace`. Loose Read (unknown keys silently skipped for forward compatibility) + Strict Write (source-gen only writes known fields). AOT-safe via `JsonTypeInfo<T>` source-generated `LauncherJsonContext`.
- **Exception handling**: `catch when (ExceptionFilters.IsRecoverable(ex))` for recoverable IO/security failures. `ErrorOr<T>` for domain-expected errors (validation, not-found). No bare `catch (Exception)`.
- **Concurrency**: `StoreGrain` internal SemaphoreSlim serializes state mutations. `FileLauncherJsonIO` has an instance-level SemaphoreSlim on `Save` for defense-in-depth. `MemoizedSelector._hasCached` is volatile. `AgentDiscoveryScanner.Scan()` accepts CancellationToken.

## Configuration schema

The unified store (`config/launcher.json`) uses `LauncherStoreFile` schema:

```json
{
  "schemaVersion": 2,
  "entries": [
    {
      "id": "example",
      "name": "example",
      "directory": "D:\\path\\to\\workspace",
      "command": "$codex",
      "selected": false,
      "updatedAtUtc": "2026-05-18T09:30:00.0000000Z",
      "launchedAtUtc": ""
    }
  ],
  "settings": {
    /* ... */
  },
  "presets": [
    /* ... */
  ],
  "defaultPresetId": "core-projects"
}
```

Fields:

- `id` — stable human-readable identity for future grouping/sorting/migration.
- `name` — display name in the launcher UI.
- `directory` — working directory passed to `wt.exe -d`.
- `command` — command passed to `pwsh -NoExit -EncodedCommand`（通过 base64 编码防注入）。Supports `$key` template references resolved from `settings.commandTemplates` (see below).
- `selected` — persistent checkbox state used by the one-click/batch launch flow.
- `updatedAtUtc` — optional ISO-8601 UTC timestamp refreshed when the app adds or edits a button, and used by the "更新时间" sort mode.
- `launchedAtUtc` — optional ISO-8601 UTC timestamp written after each successful launch, and used by the "最近启动" sort mode.

Active config entries must not contain the obsolete per-entry `confirm` key. Launch confirmation has one authority: `settings.requireConfirmationForLaunches` (nested in `launcher.json`).

All active JSON files must exist, require the documented camelCase keys, and reject missing, null, or blank mode keys. Unknown JSON keys are silently skipped at load time (Loose Read for forward compatibility). Source-gen serialization writes only known fields (Strict Write), making the C# record the effective schema contract.

Nested `settings.window.width` and `settings.window.height` must be positive numbers; the gate (verify.ps1) rejects non-positive geometry. At runtime, invalid values clamp to minimums via Math.Max.

Named launch groups live inside the store under the `presets` array:

```json
{
  "id": "core-projects",
  "name": "主线维护",
  "entryIds": ["WT_Protector"],
  "locked": false
}
```

Preset fields:

- `id` — stable human-readable launch group identity.
- `entryIds` — stable entry ids; `tools/verify.ps1` rejects unknown or duplicate references.
- `locked` — optional UI guard for system groups such as `blank-manual`; locked groups can still be applied but cannot be updated or deleted in the app.
- `defaultPresetId` (top-level) — startup UI selection; applying it is explicit.

Portable UI settings live inside the store under the `settings` object:

```json
{
  "schemaVersion": 2,
  "lastFilterText": "",
  "sortMode": "custom",
  "entryViewMode": "cards",
  "requireConfirmationForLaunches": false,
  "customEntryOrder": ["启动脚本", "WT_Protector"],
  "commandTemplates": {
    "codex": "codex --enable goals --dangerously-bypass-approvals-and-sandbox"
  },
  "searchHistory": ["w", "wt"],
  "quickLaunchHotkey": "Ctrl+Alt+Space",
  "runInTray": true,
  "updateManifestUrl": "",
  "window": {
    "width": 1180,
    "height": 760
  }
}
```

Additional settings fields:

- `sortMode` — `custom`, `recent`, `updated`, or `alphabetical`.
- `entryViewMode` — `cards`, `details`, or `names`.
- `requireConfirmationForLaunches` — global launch-confirmation toggle.
- `customEntryOrder` — stable entry-id order used by the "自定义拖拽" mode and persisted after drag-drop reorder.
- `commandTemplates` — optional dictionary of reusable command templates. When a config entry's `command` starts with `$` followed by a key (e.g. `$codex`), the launcher resolves it to the template value from this map. Missing template keys cause a launch validation error.
- `searchHistory` — optional deduplicated list of recent filter-text terms. Maintained as a JSON array of strings; the launcher validates the array must not be null. Search history is exposed in the UI via an AutoCompleteBox dropdown on the search input.
- `quickLaunchHotkey` — optional global hotkey text such as `Ctrl+Alt+Space`; `Ctrl`, `Alt`, `Shift`, `Win` plus `Space`/`A-Z`/`0-9` are accepted.
- `runInTray` — optional boolean; when true, closing the main window hides it to the tray and the tray menu is the explicit quit path.
- `updateManifestUrl` — optional HTTP(S) or local file URL to an `update-manifest.json`; the management menu's `检查更新` reads it and never rewrites `config/launcher.json`.

## Friend handoff (2026-08-02)

A ready-to-share zip is built with two commands (repo root, PowerShell):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1     # AOT single exe → artifacts/publish/win-x64/
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\package-friend.ps1    # friend zip → artifacts/friend/WTLauncher-<version>.zip
```

The zip contains the AOT exe, `使用说明.txt` (three-step quick start, SmartScreen bypass included), and `config/launcher.example.json`. A friend unzips to any folder and double-clicks the exe; the app boots with an empty store and the first-run welcome panel guides adding the first workspace. The exe is unsigned — the quick start documents "更多信息 → 仍要运行". No installer, no registry, no AppData (see Safety notes).

## Run the mainline app

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

Optional explicit config path:

```powershell
.\src\avalonia-launcher\artifacts\publish\win-x64\WTLauncher.App.exe --config .\config\launcher.json
```

Startup arguments are intentionally strict: the app accepts only one optional `--config <path>` pair. Unknown `--`-prefixed arguments are silently ignored for forward compatibility. Duplicate `--config` flags, missing config values, and non-`--`-prefixed positional arguments are rejected instead of silently choosing the wrong config.

The app reads and writes `config/launcher.json` as the unified store (entries, presets, settings all in one file). It does not use AppData or the registry for launcher state.

## Fresh-machine setup

The launcher needs Windows Terminal + PowerShell 7 on the host. A WinGet Configuration file at repo root (`.wt-protector.dsc.yaml`) declares these prerequisites declaratively so a fresh Windows machine converges to the declared state in one command:

```powershell
winget configure --file .\.wt-protector.dsc.yaml
```

Or the thin wrapper (resolves the path, exposes a `-DryRun` validate-only mode):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-machine-config.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-machine-config.ps1 -DryRun
```

What the configuration does:

- Asserts Windows 10 1809+ via `Microsoft.Windows.Developer/OsVersion` before applying anything.
- Installs `Microsoft.WindowsTerminal` and `Microsoft.PowerShell` through the `Microsoft.WinGet.DSC/WinGetPackage` resource (idempotent — safe to re-run).
- Optional AI agent CLIs (`opencode`, `aider`) are commented out; uncomment only after verifying the winget package id is current at `microsoft/winget-pkgs`, since publisher manifests change.

WinGet Configuration vs the launcher — different concerns, complementary layers:

- **WinGet Configuration** sets up the _machine_: "what's installed on this Windows host". Run once on a fresh machine.
- **The launcher** manages the _workspace_: "which project folders open in `wt.exe` with which command". Daily use.

Agent CLIs that ship via `irm | iex` (`claude`, `codex`, `cursor`, `antigravity`, `gemini`, `qwen-code`) are not winget packages today. Install them per the publisher's documented method, then run the launcher's "发现" (discover) button to import them as launch entries — the two layers compose: WinGet/irm installs the binary, the launcher wires it into a workspace button.

## Build and verification

Run the project gate from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

The verifier currently enforces a growing set of checks — see the `Add-Check` calls in `tools/verify.ps1` for the canonical list. Summary by category:

- **repo hygiene**: line-ending policy (LF), `.snow/` ignored local state, retired paths (`WT Launcher.exe`, `src/winforms-launcher/`, `spikes/`) do not return;
- **mainline layout**: `src/avalonia-launcher/` has the solution, app project, tests project, and mainline verify script;
- **documentation**: `README.md` readability, `docs/feature-reality-north-star-audit.md` keeps 22/22 narrow-domain north-star coverage and the `not-complete-as-written` boundary, claim-boundary ledger separates allowed narrow-scope claims from forbidden broad/unimprovable/universal/absolute-side-effect/post-`wt.exe` claims;
- **market evidence**: `docs/reference/github-similar-projects-snapshot.json` schema, representative repos, freshness metadata;
- **launcher.json contract**: top-level `LauncherStoreFile` (schemaVersion=2) parses, unique entry `id`s, preset `entryIds` reference known entries only, every active entry `directory` exists on this machine;
- **schema invariants**: `config-schema`, `presets-schema`, `settings-json`, `settings-single-source-buildstorefile` (single canonical assembler), `no-stale-three-file-refs` (active docs must not reference abandoned three-file architecture);
- **retirement hygiene**: shortcut-backup retirement, WinForms retired design (`retired/design/winforms-launcher.md`), HTA retired config;
- **machine setup**: `.wt-protector.dsc.yaml` exists at repo root and carries the required DSC v3 schema markers (`configuration-dsc-schema/0.2`, `configurationVersion: 0.2.0`) plus the two required `WinGetPackage` resources (`Microsoft.WindowsTerminal`, `Microsoft.PowerShell`); structural validation only — run `tools/apply-machine-config.ps1 -DryRun` for winget's own schema validation when DSC v3 is installed;
- **Avalonia mainline**: restore/test/isolated verification publish passes; on Windows a hidden temp-config startup smoke of the published exe; non-Windows hosts skip GUI smoke after win-x64 publish verification.

The final verifier line is a JSON receipt with `schemaVersion`, `kind`, `status`, `repoHead`, `worktreeDirty`, and `worktreeStatus` fields before tool-specific evidence. The Avalonia mainline receipt uses the same schema and is checked by the root gate.

Canonical distributable output:

- executable: `src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe`
- folder: `src/avalonia-launcher/artifacts/publish/win-x64/`
- builder: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1`

Note: `src/avalonia-launcher/artifacts/verify-publish/win-x64/WTLauncher.App.exe` is only the isolated verifier publish target used by `verify.ps1`, not the canonical distributable path.

The verifier publishes into isolated ignored output under `src/avalonia-launcher/artifacts/verify-publish/win-x64/` so a running release executable does not block the gate. Build/publish the stable mainline executable without running the whole root gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

The publish output is ignored local build output under:

```text
src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe
```

If that stable executable is already running, `tools/build-release.ps1` stops before publishing and reports the blocking PID(s); keep using `tools/verify.ps1` for routine gates, or close the running app before a stable release publish.

## Testing

Tests live in one `WTLauncher.Tests` project with physical layers under `src/avalonia-launcher/tests/WTLauncher.Tests/` and `[Trait("Category", ...)]` markers. The canonical map is `docs/test-structure.md`.

```powershell
# Fast lane: CoreUnit + PropertyTest + Architecture + Analyzer
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-fast.ps1

# Critical-path coverage gate
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-coverage.ps1

# Temp-config critical-journey smoke (published exe)
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\smoke-critical-journey.ps1 -Exe .\src\avalonia-launcher\artifacts\verify-publish\win-x64\WTLauncher.App.exe

# Complete native gate
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

CI uses `build-and-test` for build + fast lane and `full-gate` for the single complete verify/publish pass.

## WinForms retirement

The former WinForms mainline has been retired into documentation instead of kept as a second source tree:

- old root executable: deleted;
- old `src/winforms-launcher/Program.cs`: deleted;
- useful design lessons and receipts: `retired/design/winforms-launcher.md`.

Do not restore the old WinForms path unless the Avalonia mainline fails the same native gate and there is an explicit rollback decision.

## Safety notes

This launcher intentionally starts arbitrary local commands. Treat every config file as trusted local input.

Important risks:

- Launch confirmation is a global UI setting: when enabled, every item asks before launch; when disabled, every item launches directly.
- `directory` must exist before the launcher starts an item.
- Selected-item and current-group batch launch preflight every target before confirmation or process start; the confirmation dialog shows generated `wt.exe` command lines. If any local launch plan is invalid, no process starts. If an external start fails after earlier starts, remaining items are still attempted (continue-on-failure); failures are collected and reported.
- Single and batch launch write transient `启动回执` text for canceled, requested, or failed launch attempts; batch receipts show requested, failed, and skipped plans; `已请求启动` means `wt.exe` process start was requested, not that the terminal session finished healthy.
- "预演当前组" runs the same current-group preflight and shows generated `wt.exe` command lines without starting processes or rewriting checkbox selection; if any plan is invalid, the preview still shows every valid generated command line as not started plus the failure list.
- Command text is quoted before being passed to `wt.exe`, but it is still a shell command; review entries before launching.
- Applying a preset only rewrites checkbox state; "启动当前组" launches the selected preset entries directly without rewriting checkbox state first.
- Applying, previewing, or launching a preset discards any uncommitted drag-sort preview first; a ghost reorder is never silently saved by a launch-group action.
- The root `config/` directory is the canonical source.
- JSON saves are written through temporary files, normal config/preset writes keep `.bak` backups, and failed multi-file entry edits or launch-group save/update/delete writes attempt to roll back both memory and already-written portable files.
- Checkbox-selection and preset-apply config save failures stay visible as `配置保存失败` and roll back both in-memory selection and the tracked config file instead of reporting a successful apply/select/clear action.
- Add/edit/delete and launch-group dialogs fail visibly and leave portable files unchanged if the dialog boundary itself is unavailable.
- Deleting the current launch group selects the next default/current group explicitly and reports that group in status text instead of hiding the fallback selection.
- Using the app can legitimately dirty tracked `config/launcher.json`; review that user/app working set before committing or reverting it.
- `retired/config/config.json` belongs only to `retired/WT Launcher.hta` and is not read by the Avalonia mainline.

## Current implementation status

The Avalonia mainline supports:

- search with multi-word tokenized matching (space-separated keywords all must match) and direct card launch;
- single launch and batch launch, with `launchedAtUtc` timestamp recorded on each successful start for "最近启动" sort;
- adaptive card sizing so the visible buttons shrink to fit the current window region instead of forcing a second scroll path;
- custom-order dragging with a cursor-following ghost and live reorder preview so the redistribution is visible before drop;
- context-menu `上移排序` / `下移排序` actions so reorder is not trapped behind drag gestures; if a drag preview is active, menu reorder first discards that preview and resolves against the last committed order;
- three density states (Normal / Compact / Micro) that keep card content readable while the scroll view remains the fallback;
- a Cards / Details (“详细列表”) / Names (“名称墙”) view mode toggle for readable actions, dense scanning, and overview;
- header actions for selecting all visible items or clearing the saved selection;
- add/edit/delete/clone launch buttons inside the app, with every editable field saved back to portable config; right-click "复制副本" creates a deep copy of an entry;
- **AI Agent CLI auto-discovery** ("发现" button): scans PATH for installed agent CLIs (claude / codex / cursor / antigravity / gemini / opencode / aider / qwen-code), detects versions, and imports selected ones as launcher entries in one click — already-added agents are greyed out and deduped;
- **Per-agent brand badges**: each catalog-known agent shows a small colored badge (approximate brand color + 2-letter glyph) on its card and in the discovery dialog, so Claude / Codex / Cursor / etc. are visually distinguishable at a glance;
- named launch groups/presets that replace the saved selection through explicit apply;
- first-class launch group actions: save current selection as a new group, update the current group from current selection, and delete editable groups;
- direct launch of the current group without rewriting checkbox selection first;
- live preset-vs-selection difference feedback before applying a launch group, with no-op apply disabled when already matched;
- four persisted sort modes: `自定义拖拽`, `最近启动`, `更新时间`, and `首字母顺序`;
- command templates from `launcher.json` `settings.commandTemplates` for repeated shell commands, with unknown template keys rejected at launch-plan build time;
- global launch confirmation;
- global quick-launch hotkey with a dedicated overlay window (type-to-filter, Enter launch, Esc dismiss);
- system tray icon with Open/Quit actions and close-to-tray behavior;
- config export/import from the management menu: export all or currently selected entries as readable unified-store JSON, then import on another machine with explicit path mapping and add/replace conflict handling;
- persistent recent-run history from the management menu with requested/failed/skipped/canceled status (100-entry cap, no terminal health claims);
- explicit update checks through the management menu against a versioned `update-manifest.json` (local or `settings.updateManifestUrl`), with no automatic overwrite of portable config;
- a portable installer script that creates Start Menu/optional Desktop shortcuts, writes an uninstaller, and preserves user `config` on uninstall;

- JSON persistence with temporary-file writes, `.bak` backup for normal config/preset saves, and rollback compensation for failed multi-file entry edits;
- persistent selected checkbox state for one-click/batch launch;
- portable UI preferences in `launcher.json` `settings` object without AppData or registry writes;
- Core/App/Tests separation around launch argument rules and config behavior;
- self-contained `win-x64` publish through `dotnet publish`.

Current gate state:

- the feature audit currently classifies all 22 active feature mappings as narrow-domain north-star for the portable Windows Terminal workspace-launcher scope only;
- the full absolute objective remains not complete as written because unimprovable-product, stronger-than-every-GitHub-similar, and absolute zero-side-effect claims are still blocked by current evidence;
- no current native-gate blocker is known after a fresh `tools/verify.ps1` PASS;
- this is not a claim that the launcher is unimprovable, stronger than every GitHub alternative, or capable of 0% side effects after starting arbitrary external processes;
- for future visual-heavy layout changes, repeat screenshot-backed comfort passes without weakening the root `tools/verify.ps1` gate.
