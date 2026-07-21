# Avalonia Launcher Mainline

This is the active source line for WT Launcher. It was promoted from the former spike after the WinForms executable/source line was retired into `retired/design/winforms-launcher.md`.

## Run for experience

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

The app reads the repository-root `config/launcher.json` (entries + presets + settings + defaultPresetId as one atomic unit) and persists all mutations there. The header batch controls can select all visible filtered entries, clear the saved selection, or one-click launch the saved checked set.

## Verify

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

Or run only the Avalonia mainline gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\verify.ps1
```

The local mainline gate runs:

1. `dotnet restore -r win-x64`
2. `dotnet test -c Release`
3. `dotnet publish -c Release -r win-x64 --self-contained true` into isolated verification output
4. a hidden temp-config startup smoke of the published exe on Windows; non-Windows hosts skip that GUI-only step after tests and win-x64 publish verification

Verification publish output is ignored under `artifacts/verify-publish/win-x64/`. Stable release output from `tools/build-release.ps1` is ignored under `artifacts/publish/win-x64/`.

## Current acceptance question

This line has replaced the WinForms source tree as the mainline. Further work should improve the Avalonia UX directly while preserving the root `tools/verify.ps1` gate and the portable config contract.

## Architecture layers

The codebase is organized in five logical layers. Each layer depends only on the layers below it.

### Layer 0 — Pure data & contracts (Core)

`src/WTLauncher.Core/`

- `LaunchEntry`, `LaunchPlan`, `LauncherPreset` — immutable-shaped records
- `LaunchArgumentBuilder` — `wt.exe` argument construction with Windows-style quoting
- `LauncherJson` — atomic temp-file + `.bak` JSON persistence
- `LauncherEntryOrder`, `LauncherEntryLayoutCalculator` — pure ordering/layout rules reused outside one ViewModel branch

### Layer 1 — Command-local mutations (ViewModels)

`src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`

- Entry add/edit/delete command handlers own the short validation → mutate → save sequence.
- Shared rules stay in narrow helpers such as `LauncherEntryDrafts`; avoid a second ViewModel workflow when there is only one caller.

### Layer 2 — Persistence + rollback (AppStore + ViewModels)

`src/WTLauncher.App/Infrastructure/AppStore.cs` + `MainWindowViewModel.RestoreVmStateFromStore()`

- `AppStore.Mutate` handles automatic rollback: captures `before`, applies transform, persists via `LauncherStore.Save`, reverts on failure.
- `RestoreVmStateFromStore()` re-syncs ViewModel state after AppStore rollback.
- Coordinates `launcher.json` (entries + presets + settings + defaultPresetId) as one atomic unit

### Layer 3 — ViewModel orchestration

`src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`

- The single writable owner of launcher UI state
- Delegates reusable rules to Core helpers and persistence/rollback boundaries
- Injects dialog confirmation `Func<>` delegates through the `MainWindow` constructor

### Layer 4 — Views & drag-drop

`src/WTLauncher.App/Views/`

- XAML with three view modes: Cards / Details / Names
- Drag reorder: `MainWindowDragGhostFactory` with ghost overlay and insert preview
- Focused view helpers remain for shared drag and hit-test behavior; one-off menu guards stay in `MainWindow`

### Key invariants

- One source per state: view modes, sort mode, drag session, card density each have a single owner
- Core never references Avalonia or CommunityToolkit.Mvvm
- Workflows return explicit result data; UI callbacks stay at the window/dialog seam
