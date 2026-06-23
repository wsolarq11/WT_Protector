## PURPOSE
WT_Protector is a C# Avalonia-based launcher for managing and launching game/application entries with presets, drag-reorder, batch launch, and configuration management.

## STACK
- **UI**: Avalonia (cross-platform XAML-based)
- **Language**: C# (.NET 10)
- **Architecture**: MVVM with partial class decomposition
- **Persistence**: JSON file-based (config, presets, settings)
- **Testing**: xUnit
- **Build**: PowerShell scripts + GitHub Actions CI

## ARCHITECTURE
- **WTLauncher.Core**: Domain logic — LaunchEntry, LaunchPlan, LauncherPreset, LauncherSettings, JSON stores
- **WTLauncher.App**: Avalonia UI — MainWindow, ViewModels, Views, XAML bindings
- **WTLauncher.Tests**: Comprehensive test suite (556 tests, xUnit)
- MainWindowViewModel split across ~15 partial class files by concern (Drag, Entries, Layout, Presets, Selection, etc.)
- Snapshot-based mutation rollback: LauncherEntryMutationSnapshot / LauncherSettingsSnapshot capture state before mutations; on failure, in-memory state and files are rolled back
- Drag-reorder: BeginDragReorder → PreviewMoveEntry → CommitDragReorder workflow
- Preset system: Save/load entry selections as named presets

## PATTERNS
- Partial class decomposition for ViewModels
- **Immutable data carriers → `sealed record`**: LaunchPlan, LauncherBatchLaunchReport, LauncherBatchLaunchPreflight, LauncherBatchLaunchConfirmation, LauncherEntryDeleteTarget, LauncherPresetDeleteTarget, LauncherPresetSaveTarget, LauncherSingleLaunchConfirmation, EntryDraft — all get-only properties, value equality, `with` cloning
- **Mutable persisted entities → `sealed class`**: LaunchEntry, LauncherPreset, LauncherPresetFile, LauncherSettings, LauncherWindowSettings — all have `Clone()` methods for snapshot/rollback
- **ViewModel/workflow classes → `sealed class`**: LauncherEntryViewModel, LauncherPresetViewModel, LauncherDragReorderSession, LauncherLaunchWorkflow, etc.
- Async command execution with ConfigureAwait
- File-scoped namespaces throughout
- Atomic temp-file writes with guaranteed cleanup — `.tmp-{Guid}` files used during `LauncherJson.Save` for crash-safe atomic writes; `finally` block ensures cleanup; tests verify no `.tmp` files are left behind
- Collection expressions (`[]`, `[.. items]`) preferred over `new List<T>()`
- `OrdinalIgnoreCase` for all internal string comparisons (sort, ID matching)

## TRADEOFFS
- Partial classes keep files small but increase file count and navigation overhead
- Avalonia provides cross-platform but adds AOT complexity vs WinForms
- JSON persistence is simple but lacks transactional guarantees (compensated by snapshots)
- Snapshot rollback adds memory overhead but avoids complex transaction logic

## PHILOSOPHY
- KISS: Consolidate and simplify, remove unnecessary abstractions
- No regressions: Tests guard against breaking existing behavior
- Fail-fast on unexpected errors; narrow catch blocks to specific I/O exceptions; ViewModel command handlers filter fatal exceptions via `catch when (ex is not (OutOfMemoryException or StackOverflowException))`
- Atomic temp-file writes — `.tmp-{Guid}` files enable crash-safe atomic persistence with guaranteed `finally` cleanup; not a forbidden pattern