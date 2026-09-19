# Avalonia Launcher Mainline

Active source line for WT Launcher. Promoted from the former spike after the WinForms line retired to `retired/design/winforms-launcher.md`.

## Run

```bash
npm run start
```

Reads and writes repository-root `config/launcher.json` as one atomic unit.

## Verify

```bash
npm run verify           # full repo gate
npm run verify:mainline  # Avalonia gate only
npm run test:fast
npm run test:coverage
npm run test:launch-args
npm run smoke
```

Mainline gate order: locked AOT restore → `dotnet test -c Release` → isolated AOT publish → hidden temp-config smoke on Windows. Stable output: `artifacts/publish/win-x64/`; verifier output: `artifacts/verify-publish/win-x64/`. Release contract: `npm run release:contract`.

## 5-Layer Architecture

Each layer depends only on layers below it.

### Layer 0 — Core

`src/WTLauncher.Core/`

- `LaunchEntry`, `LaunchPlan`, `LauncherPreset` records
- `LaunchArgumentBuilder` — `wt.exe` quoting
- `LauncherJson` — temp-file + `File.Replace` atomic persistence
- `LauncherEntryOrder`, `LauncherEntryLayoutCalculator` — pure ordering/layout rules

### Layer 1 — Commands

`ViewModels/MainWindowViewModel*.cs`

- add/edit/delete/launch handlers: validate → mutate → save
- shared rules in narrow helpers such as `LauncherEntryDrafts`

### Layer 2 — Persistence + Rollback

`Infrastructure/StoreGrain.cs` + `MainWindowViewModel.RestoreVmStateFromStore()`

- `StoreGrain.Dispatch`: reducer → save to disk → apply to memory; save failure leaves memory unchanged
- `RestoreVmStateFromStore()` re-syncs after rollback
- coordinates `launcher.json` as one atomic unit

### Layer 3 — Orchestration

`MainWindowViewModel`

- single writable owner of launcher UI state
- receives dialog/services through constructor; `MainWindow` receives the ViewModel

### Layer 4 — Views

`Views/`

- XAML three modes: Cards / Details / Names (`MainWindow.axaml` hosts, `LauncherEntriesView.axaml` holds bodies)
- drag reorder: `MainWindow.Drag.cs` + Core layout math + `LauncherDragReorderWorkflow`
- focused helpers: `MainWindowEntryCardResolver`, `FastWrapPanel`, `FillGridPanel`

### Key Invariants

- one source per state: view mode, sort mode, drag session, card density
- Core never references Avalonia or CommunityToolkit.Mvvm
- workflows return explicit result data; UI callbacks stay at window/dialog seam
