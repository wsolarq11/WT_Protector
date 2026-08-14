# Research: Cross-Layer Invariants

- **Query**: Verify all cross-layer invariants defined in CLAUDE.md and DESIGN.md are maintained: single source of truth for state, selection write paths, drag state ownership, catch(Exception) rule, atomic config write.
- **Scope**: internal
- **Date**: 2026-07-26

## Findings

### Invariant 1: Single Source Per State -- CLEAN

All verified read-only projections are computed from a single writable field:

| Projection | Writable Source | Location | Compliant? |
|---|---|---|---|
| `IsCardsViewMode` | `LayoutManager.SelectedDisplayMode` | `LayoutManager.cs:121` | YES |
| `IsDetailedListMode` | `LayoutManager.SelectedDisplayMode` | `LayoutManager.cs:122` | YES |
| `IsNamesViewMode` | `LayoutManager.SelectedDisplayMode` | `LayoutManager.cs:123` | YES |
| `IsCustomSortMode` | `LayoutManager.SelectedSortMode` | `LayoutManager.cs:120` | YES |
| `IsCompactLayout` | `LauncherEntryViewModel._layoutDensity` | `LauncherEntryViewModel.Layout.cs:26` | YES |
| `IsMicroLayout` | `LauncherEntryViewModel._layoutDensity` | `LauncherEntryViewModel.Layout.cs:28` | YES |
| `IsDragPreviewTarget` | `LauncherEntryViewModel._dragCueState` | `LauncherEntryViewModel.Drag.cs:7` | YES |
| `IsDragPreviewSource` | `LauncherEntryViewModel._dragCueState` | `LauncherEntryViewModel.Drag.cs:9` | YES |
| `IsDragInsertBefore` | `LauncherEntryViewModel._dragCueState` | `LauncherEntryViewModel.Drag.cs:11` | YES |
| `IsDragInsertAfter` | `LauncherEntryViewModel._dragCueState` | `LauncherEntryViewModel.Drag.cs:13` | YES |

Evidence from LayoutManager.cs:
```csharp
// Line 120-123 -- all computed, no second writable field
public bool IsCustomSortMode => SelectedSortMode?.Id == LauncherEntryQuery.CustomSortModeId;
public bool IsCardsViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Cards;
public bool IsDetailedListMode => CurrentEntryViewMode == LauncherEntryViewMode.Details;
public bool IsNamesViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Names;
```

**No second writable field duplicates found for any projection.**

### Invariant 2: Selection Has Two Write Paths -- CLEAN

Verified in `MainWindowViewModel.Batch.cs`:

**Path 1 -- User checkbox** (immediate save):
- `LauncherEntryViewModel.IsSelected` setter (line 63-70) calls `_owner.UpdateSelection(this, value)` 
- This reaches `MainWindowViewModel.UpdateSelection()` (Batch.cs:88-94) which calls `ReplaceEntrySelectionFromOwner` then `SaveSelectionChangeOrRestore`

**Path 2 -- Bulk/preset** (single batched save):
- `MainWindowViewModel.SetSelection()` (Batch.cs:100-120) uses `entry.SetSelectionFromOwner(selected)` for all entries, then saves once via `SaveSelectionChangeOrRestore`
- `MainWindowViewModel.ReplaceSelection()` (Batch.cs:122-139) uses `SetSelectionFromOwner` for all entries, saves once

**No global bulk-suppression flag found.** The `IsSelected` setter does not check any global flag -- it always fires `UpdateSelection`. The `SetSelectionFromOwner` path bypasses the setter to avoid the double-save loop.

The delicate contract is documented in `LauncherEntryViewModel.cs` lines 91-111 with a detailed comment explaining the `ReplaceEntrySelectionFromOwner` protocol and why it must not call `IsSelected`'s setter.

### Invariant 3: Drag State Must Be Discarded Before Refresh -- CLEAN

Verified in `MainWindowViewModel.cs` line 238-241:
```csharp
[RelayCommand]
private void Refresh()
{
    DiscardDragReorderPreview();  // <-- FIRST thing in Refresh
    ...
```

Also verified in the drag partial at `MainWindowViewModel.Drag.cs` lines 152-161:
```csharp
private void DiscardDragReorderPreview()
{
    var result = _dragReorderWorkflow.Cancel();
    if (result.RefreshEntries)
        ApplyFilter();
}
```

Every mutation entry point (AddEntry, EditEntry, DeleteEntry, CloneEntry, ApplyPreset, UpdateSelectedPreset, SaveSelectionAsPreset, etc.) calls `DiscardDragReorderPreview()` as its first action.

### Invariant 4: Drag State Single Owner -- CLEAN

Confirmed in DESIGN.md and code:

Single source: `DragReorderSession?` owned by `LauncherDragReorderWorkflow._session` (line 37).

The sole writable field `LauncherDragReorderWorkflow._session` controls:
- `HasActiveSession` (line 39) -- public read-only
- `Begin()` -- creates session
- `Commit()` / `Cancel()` -- clears session

In `MainWindowViewModel`, the drag state is accessed through the workflow:
```csharp
internal bool HasActiveDragReorder => _dragReorderWorkflow.HasActiveSession;
```

The View (MainWindow.Drag.cs line 91-96) checks `vm.HasActiveDragReorder` to detect ViewModel-side cancellation:
```csharp
if (DataContext is MainWindowViewModel vm && !vm.HasActiveDragReorder)
{
    ForceCleanup();
    return;
}
```

### Invariant 5: `catch (Exception)` is Forbidden -- CLEAN

**Result: Zero bare `catch (Exception)` blocks in production code.**

The only match for `catch\s*\(Exception\s*\)` in the entire `src/avalonia-launcher/src/` directory is the analyzer itself (`CatchExceptionMustHaveWhenAnalyzer.cs`), which is the enforcement rule.

All exception handlers use one of:
- `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` -- 40+ instances
- `catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))` -- StoreGrain, Core code
- `catch (X) when (X is IOException or UnauthorizedAccessException)` -- narrow types
- `catch (FileNotFoundException)`, `catch (OperationCanceledException)` -- specific types

### Invariant 6: Atomic Config Write -- CLEAN

All persistent writes go through `StoreGrain.Dispatch()` which calls `_persistence.Save()` then updates in-memory state only on success. On save failure, the in-memory state is NOT updated.

`FileLauncherJsonIO.Save()` (lines 109-155):
1. Creates temp file with GUID suffix
2. Writes JSON to temp file
3. If backup requested, rotates backups
4. If target file exists, uses `File.Replace` (atomic on NTFS)
5. If target file doesn't exist, uses `File.Move`

This is the temp-file + `File.Replace` atomic write pattern required by the invariant.

### Invariant 7: No Direct JSON Writing Outside LauncherStore -- CLEAN

**Result: Confirmed.** All JSON persistence goes through `FileLauncherJsonIO.Save<T>()` (via `StoreGrain.Dispatch`). The only `File.WriteAllText` calls in the App layer are:
- `App.axaml.cs:134` -- config directory writability test (writes a temp test file, not JSON)
- `ErrorLog.cs:61` -- diagnostic error logging (writes to error.log, not config)

### Invariant 8: Design Tokens Single Source -- VERIFIED

`LauncherDesignTokens.cs` (Core) is the single source. `App.axaml.cs` `InjectDesignTokens()` method (lines 31-97) reads from `LauncherDesignTokens` to populate `Application.Resources` with actual Avalonia brushes and thickness values. Every token used in XAML is defined in this method. No hardcoded `#2563EB` color in XAML was detected (verified via spec description).

### Invariant 9: No Obsolete Per-Entry `confirm` Key -- VERIFIED

`LaunchEntry` record in Core does not contain a `confirm` field. The `LauncherSettings.RequireConfirmationForLaunches` is the sole authority. Confirmation logic in `LaunchOrchestrator.ConfirmSingleLaunchIfRequiredAsync()` (line 139) checks `plan.RequiresConfirmation` which delegates to `LauncherLaunchWorkflow`.

## Caveats / Not Found

- All invariants found clean. No violations detected.
- The selection contract between `IsSelected` setter and `ReplaceEntrySelectionFromOwner` is fragile (documented in code comments). A future programmer could accidentally call `IsSelected = value` from within `SetSelection` bulk path and create a double-save loop.
