# Implementation Plan: Rewiring Production

## Phase A: Context and Ports

1. Add IStatusSink.cs and ICommandExecutionContext.cs.
2. Make MainWindowViewModel implement ICommandExecutionContext.
3. Refactor ILaunchCoordinatorContext to inherit ICommandExecutionContext and remove duplicates.
4. Add context contract tests; update structure baseline.

Validation: build, targeted tests, full gate.

## Phase B: Preset and Layout

1. Extract Presets.cs command bodies into LauncherPresetCommandWorkflow.
2. Extract Layout.cs persistence into LauncherLayoutCommandWorkflow.
3. Keep command/CanExecute surface on MainWindowViewModel.
4. Add workflow tests; reduce MainWindowViewModel by 600+ lines.

## Phase C: Selection, Maintenance, Entry

1. Extract Batch selection bodies into LauncherSelectionCommandWorkflow.
2. Extract CommandManagement/DirectoryManagement into LauncherMaintenanceCommandWorkflow.
3. Extract Entries/Discovery persistence into LauncherEntryCommandWorkflow.
4. Add tests; MainWindowViewModel total <= 1800.

## Phase D: Exchange

1. Extract Migration/Update into LauncherExchangeCommandWorkflow.
2. MainWindowViewModel no longer touches IConfigExchangeService/IUpdateManifestReader.
3. Add tests; MainWindowViewModel total <= 1200.

## Phase E: Ledger and Gate

1. Create docs/wiring-map.md port ledger.
2. Add wiring-map-consistency, main-vm-line-budget, workflow-no-callback-bag checks.
3. Update AGENTS.md/CLAUDE.md/README and baseline constants.

## Validation Commands

```powershell
git diff --check
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore -warnaserror --nologo
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore --nologo
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## Rollback

Each phase is an independent commit. If a phase fails gate, revert only that phase commit.

## Landing

- Phase A `33577f2e` (context and ports)
- Phase B `4cbf9590` (preset and layout)
- Phase C `5a5bca54` (selection, maintenance, entry)
- Phase D `eef7d4ad` (exchange and update)
- Phase E `e5fc23dc` (wiring map and 43-check gate)
- Final verification: `dotnet build -warnaserror` 0 warnings, 832 tests passed, `tools/verify.ps1` PASS
