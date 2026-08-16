# Avalonia Launcher Mainline

This is the active source line for WT Launcher. It was promoted from the former spike after the WinForms executable/source line was retired into `retired/design/winforms-launcher.md`.

## Run for experience

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

The app reads the repository-root `config/launcher.json` (entries + presets + settings + defaultPresetId + runHistory as one atomic unit) and persists all mutations there. The header batch controls can select all visible filtered entries, clear the saved selection, or one-click launch the saved checked set.

## Verify

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

Or run only the Avalonia mainline gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\verify.ps1
```

TypeScript parity entry points (repository root):

```powershell
npm run verify
npm run verify:mainline
npm run test:fast
npm run test:coverage
npm run test:launch-args
npm run smoke
```

The local mainline gate runs:

1. `dotnet restore -r win-x64 -p:EnableNativeAot=true --locked-mode`
2. `dotnet test -c Release`
3. `dotnet publish -c Release -r win-x64 --self-contained true` into isolated verification output
4. a hidden temp-config startup smoke of the published exe on Windows; non-Windows hosts skip that GUI-only step after tests and win-x64 publish verification

Verification publish output is ignored under `artifacts/verify-publish/win-x64/`. Stable release output from `tools/build-release.ps1` is ignored under `artifacts/publish/win-x64/`. The central release contract is `tools/release-contract.ps1` from the repository root: locked-mode AOT restore, `--no-restore` publish, clean-worktree assertion, and receipt/manifest validation. The TypeScript authority is `npm run release:contract` (CI entry point).

## Current acceptance question

This line has replaced the WinForms source tree as the mainline. Further work should improve the Avalonia UX directly while preserving the root `tools/verify.ps1` gate and the portable config contract.

## Architecture layers

The codebase is organized in five logical layers. Each layer depends only on the layers below it.

### Layer 0 — Pure data & contracts (Core)

`src/WTLauncher.Core/`

- `LaunchEntry`, `LaunchPlan`, `LauncherPreset` — immutable-shaped records
- `LaunchArgumentBuilder` — `wt.exe` argument construction with Windows-style quoting
- `LauncherJson` — atomic temp-file + `File.Replace` JSON persistence with `.bak` backup (static facade delegating to `ILauncherJsonIO`)
- `LauncherEntryOrder`, `LauncherEntryLayoutCalculator` — pure ordering/layout rules reused outside one ViewModel branch

### Layer 1 — Command-local mutations (ViewModels)

`src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`

- Entry add/edit/delete command handlers own the short validation → mutate → save sequence.
- Shared rules stay in narrow helpers such as `LauncherEntryDrafts`; avoid a second ViewModel workflow when there is only one caller.

### Layer 2 — Persistence + rollback (StoreGrain + ViewModels)

`src/WTLauncher.App/Infrastructure/StoreGrain.cs` + `MainWindowViewModel.RestoreVmStateFromStore()`

- `StoreGrain.Dispatch` handles rollback via save-first-then-commit: the reducer result is persisted (through `FileStorePersistence` → `FileLauncherJsonIO`, temp-file + `File.Replace` + `.bak`) before memory is updated; on save failure memory is unchanged and the caller sees the error.
- `RestoreVmStateFromStore()` re-syncs ViewModel state after a rollback.
- Coordinates `launcher.json` (entries + presets + settings + defaultPresetId + runHistory) as one atomic unit.

> The former `AppStore` class was removed by the VTuber-Killer migration (2026-07-24); `StoreGrain.Dispatch` is the only write path.

### Layer 3 — ViewModel orchestration

`src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`

- The single writable owner of launcher UI state
- Delegates reusable rules to Core helpers and persistence/rollback boundaries
- Receives dialog confirmation and services (`IDialogService`, `IEntryService`, `IPresetService`) through its constructor; `MainWindow` receives the ViewModel

### Layer 4 — Views & drag-drop

`src/WTLauncher.App/Views/`

- XAML with three view modes: Cards / Details / Names (`MainWindow.axaml` hosts the modes; the view bodies live in `LauncherEntriesView.axaml`)
- Drag reorder: `MainWindow.Drag.cs` (`ShowDragGhost`/`MoveDragGhost`/`HideDragGhost` with an OverlayLayer ghost + insert preview), layout math in Core (`LauncherDragGhostLayoutCalculator`), reorder-session state in the ViewModel (`LauncherDragReorderWorkflow`)
- Focused view helpers (`MainWindowEntryCardResolver`, `FastWrapPanel`) handle shared drag hit-test and layout; one-off menu guards stay in `MainWindow`

### Key invariants

- One source per state: view modes, sort mode, drag session, card density each have a single owner
- Core never references Avalonia or CommunityToolkit.Mvvm
- Workflows return explicit result data; UI callbacks stay at the window/dialog seam
