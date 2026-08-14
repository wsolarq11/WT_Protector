# Research: Data Flow Trace -- Add Entry User Action

- **Query**: Trace how a user action ("Add Entry") flows through all 5 layers. Identify any layer bypasses and state consistency issues.
- **Scope**: internal
- **Date**: 2026-07-26

## Data Flow Trace: Add Entry

### Layer 4 (Views) -> Layer 3 (Orchestration)

1. User clicks "Add Entry" button in MainWindow.axaml
2. XAML binding: `Command="{Binding AddEntryCommand}"`
3. `MainWindowViewModel.AddEntryAsync()` (MainWindowViewModel.Entries.cs:12) is invoked via `[RelayCommand]`
4. Delegates to `_list.AddEntryAsync()` -- the `EntryListViewModel`

### Layer 3 (Orchestration) -> Layer 2 (Service) -> Layer 1 (Dialog)

5. `EntryListViewModel.AddEntryAsync()` (EntryListViewModel.cs:119-142):
   - Calls `DiscardDragReorderPreview()` first (invariant enforcement)
   - Calls `_entryService.AddEntryAsync(...)` which is `IEntryService`

6. `EntryService.AddEntryAsync()` (EntryService.cs:17-43):
   - Calls `_dialog.RequestEntryDraftAsync(null, ...)` which is `IDialogService`

7. `DialogService.RequestEntryDraftAsync()` (DialogService.cs:33-39):
   - Creates `EntryDetailsWindow.EntryPrefill` from prefill data
   - Creates `new EntryDetailsWindow(...)` and shows as dialog
   - **This is the View layer boundary** -- DialogService lives in Views

### Layer 4 (Views -- dialog code-behind)

8. `EntryDetailsWindow` code-behind:
   - User fills form, clicks save
   - `Submit()` validates and normalizes via `LauncherEntryDrafts.TryNormalizeRequiredDraft()`
   - **VIOLATION**: Contains business logic (template preview, draft normalization) in code-behind
   - Returns `EntryDraft?` via `Close(draft)`

### Back up through layers

9. `EntryService.CreateEntryFromDraft()` (EntryService.cs:138-146):
   - Calls `LauncherEntryDrafts.CreateEntry(draft, previousEntry)` -- pure Core function
   - Calls `LauncherEntryDrafts.ValidateEntry(entry, originalId, existingEntries)` -- validation
   - Returns `ServiceResult<LaunchEntry>`
   - **This is the correct boundary** -- Service layer owns the create+validate flow

### Layer 2 (Persistence) -> Layer 0 (Core)

10. `EntryListViewModel.HandleAddOrCloneResult()` (EntryListViewModel.cs:261-291):
    - Creates updated settings with new CustomEntryOrder
    - Calls `_storeGrain.Dispatch(state => state with { Entries = ..., Settings = ... })`

11. `StoreGrain.Dispatch()` (StoreGrain.cs:58-71):
    - Inside grain lock: applies reducer, calls `_persistence.Save()`, updates state
    - **This is single atomic operation** -- save and update are atomic
    - On save failure: state is NOT updated (exception propagates)

12. `FileStorePersistence.Save()` delegates to `FileLauncherJsonIO.Save()`:
    - Temp file + File.Replace (atomic write)
    - Backup rotation

### Reverse through layers (UI update)

13. After successful dispatch, `EntryListViewModel`:
    - Creates new `LauncherEntryViewModel` and adds to `_entries` ObservableCollection
    - Calls `SyncProjection()` to update visible entries filter/sort
    - Fires `EntriesChanged` event -> MWVM `OnEntriesChanged` -> `RefreshAllProjections`

14. Avalonia data binding picks up the new entry in `VisibleEntries` ObservableCollection
    - Cards/Details/Names view automatically renders new item

## Layer Boundary Verification

| Step | From Layer | To Layer | Bypass? |
|---|---|---|---|
| 1-3 | Views (XAML) | Orchestration (MWVM) | No -- standard binding |
| 4 | Orchestration (MWVM) | Service (EntryService) | No -- via IEntryService |
| 5-6 | Service (EntryService) | Dialog (DialogService) | No -- via IDialogService |
| 7-8 | Dialog (DialogService) | Views (EntryDetailsWindow) | No -- creates Window instance |
| 8 | Views (EntryDetailsWindow) | Core (LauncherEntryDrafts) | **Yes -- code-behind calls Core directly** |
| 9 | Service (EntryService) | Core (LauncherEntryDrafts) | No -- via IEntryService |
| 10-11 | Service (EntryListVM) | Infrastructure (StoreGrain) | No -- via StoreGrain |
| 11-12 | Infrastructure (StoreGrain) | Persistence (FileLauncherJsonIO) | No -- via IStorePersistence |
| 13-14 | Orchestration (MWVM) | Views (XAML) | No -- ObservableCollection binding |

### Direct Core call from EntryDetailsWindow code-behind

EntryDetailsWindow (Views layer, code-behind) calls `LauncherEntryDrafts.TryNormalizeRequiredDraft()` (a Core function) directly at line 132. While this doesn't violate the architecture (Views can technically call Core since Core is the lowest layer), it bypasses the intended IEntryService layer that owns the validation workflow. This creates two validation paths: one in the View (code-behind) and one in the service. Duplicate validation is a code debt.

## State Consistency After Mutation

After Add Entry succeeds:
1. StoreGrain in-memory state has new entry (guaranteed by atomic dispatch)
2. Disk state has new entry (saved before in-memory update)
3. ObservableCollection in EntryListViewModel has new ViewModel
4. PresetManager state unchanged
5. Drag session is discarded (first action in AddEntry)

All state is consistent. If save fails:
- StoreGrain state is unchanged (exception pattern)
- EntryListViewModel _entries is NOT modified (exception caught before it gets there)
- No phantom entries in UI

## Summary

- Add Entry data flow follows the intended 5-layer architecture
- One noted concern: code-behind (Views) calls Core directly for validation, bypassing the service layer
- State is consistent across layers on both success and failure paths
- All invariants (drag discard, atomic save) are enforced before mutation begins
