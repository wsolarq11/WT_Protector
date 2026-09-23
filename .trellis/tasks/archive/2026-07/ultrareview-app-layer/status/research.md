# Ultrareview: App/ViewModel Layer -- Code Quality Audit

- **Scope**: `src/avalonia-launcher/src/WTLauncher.App/` (all .cs files)
- **Date**: 2026-07-26
- **Files reviewed**: 40+ files across ViewModels, Infrastructure, Features, Views

## Summary

- **1 critical** (infinite loop in exception handling)
- **3 high** (state machine config lost, Rx subscription never disposed, StoreGrain subscription leaks)
- **7 medium** (fragile contracts, stale projection, UI-blocking I/O, null delegatse, etc.)
- **4 low** (unsubscribed events, unreachable states, TOCTOU window, keyboard localization)

---

## Critical (1)

### C1. Infinite loop in ExceptionFilters.IsRecoverable DEBUG block

**File**: `ViewModels/ExceptionFilters.cs`, lines 19-20

```csharp
var inner = ex;
while (inner is TypeInitializationException or AggregateException)
    inner = inner.InnerException ?? inner;
```

**Description**: When `ex` is a `TypeInitializationException` or `AggregateException` with `InnerException == null`, the expression `inner.InnerException ?? inner` returns `inner` itself (unchanged). The while-condition is still true, so the loop never terminates. This hangs the calling thread permanently.

**Reproduction scenario**: Any code path that calls `ExceptionFilters.IsRecoverable(ex)` where `ex` is a `TypeInitializationException` whose inner is null (e.g., a static constructor that threw `NullReferenceException` would be wrapped but Unity's TypeInitializationException could have no inner). Same for `AggregateException` created with no inner exceptions -- e.g., `new AggregateException()`.

**Fix**: Change to `inner = inner.InnerException; if (inner is null) break;` or add the guard in the while condition.

**Severity**: Critical -- process hang / thread hang

---

## High (3)

### H1. AppStateMachine.Saving state: conflicting Permit overrides previous transitions

**File**: `ViewModels/AppStateMachine.cs`, lines 77-79

```csharp
.Configure(AppUIState.Saving)
    .Permit(AppUITrigger.EndSave, AppUIState.Normal)
    .Permit(AppUITrigger.EndSave, AppUIState.Filtering)
    .Permit(AppUITrigger.EndSave, AppUIState.DialogOpen);
```

**Description**: The Stateless state machine's `Permit()` for the same trigger from the same state replaces the previous permit silently. Only the last `.Permit(AppUITrigger.EndSave, AppUIState.DialogOpen)` is effective. The transitions to `Normal` and `Filtering` are lost. This means when a save operation completes, the state machine always transitions to `DialogOpen` regardless of where it came from.

**Impact**: After saving from `Normal` state, the state machine goes to `DialogOpen` instead of back to `Normal`. Since there is no code that reads `CurrentState`, this bug may be invisible in practice today, but it defeats the purpose of the state machine and will cause bugs once `CanFire` / `FireIfPossible` guards are used.

**Fix**: Use `.PermitIf()` with guard conditions, or use internal helper states to track the pre-save origin.

---

### H2. EntryProjection DynamicData pipeline never disposed

**File**: `Features/Entries/EntryProjection.cs`, line 79

```csharp
_cleanup = pipeline
    .Bind(out _visibleEntries)
    .DisposeMany()
    .Subscribe();
```

**Description**: `_cleanup` is an `IDisposable` that represents the entire DynamicData+Rx subscription chain. `EntryProjection` implements `IDisposable` and exposes `Dispose()`. However:

1. `EntryListViewModel` is **not** `IDisposable` (it has `[SuppressMessage("CA1001")]` on line 17 claiming "EntryProjection lifecycle matches this VM")
2. `EntryListViewModel` never calls `_projection.Dispose()`
3. No caller of `EntryListViewModel` ever disposes it
4. `MainWindowViewModel` is not `IDisposable` either

The DynamicData pipeline -- including the filter/sort subscription that runs on the SynchronizationContext -- leaks for the entire app lifetime. Every call to `ReplaceEntries` (which calls `_projection.LoadEntries`) creates new ViewModel instances via `EditDiff` but the old ones inside the cache are replaced by reference comparison, not disposed.

**Impact**: Memory grows with each `Refresh()` / `ReplaceEntries` call. Rx subscriptions accumulate on the SynchronizationContext.

---

### H3. StoreGrain subscription (in StoreGrain.Subscribe) never disposed

**File**: `ViewModels/MainWindowViewModel.cs`, lines 22, 195-200

```csharp
private readonly IDisposable? _storeSubscription;
...
_storeSubscription = _storeGrain.Subscribe(state =>
{
    _presetManager.Load(
        new LauncherPresetFile { Presets = state.Presets, DefaultPresetId = state.DefaultPresetId },
        _presetManager.SelectedPreset?.Id);
});
```

**Description**: `_storeSubscription` is stored as a field but **never disposed**. `MainWindowViewModel` does not implement `IDisposable`. The `StoreGrain.Subscribe` method returns an `IDisposable` that unsubscribes from `StateChanged`, but this is never called. The subscription:

- Captures `_presetManager` and the lambda closure captures `this` (MWVM)
- Prevents GC of the MWVM if `StoreGrain` outlives it
- Calls `_presetManager.Load` which modifies `_presets` ObservableCollection -- if `StateChanged` fires from a background thread, this is a cross-thread violation

Additionally, `_list.EntriesChanged += OnEntriesChanged` (line 207) and `_list.CountChanged += OnEntryCountChanged` (line 147) are also never unsubscribed.

---

## Medium (7)

### M1. ReplaceEntrySelectionFromOwner -- fragile dual-update protocol

**File**: `ViewModels/LauncherEntryViewModel.cs`, lines 91-122

**Description**: The selection update uses a two-phase protocol:
1. `IsSelected` setter calls `SetProperty` which fires PropertyChanged and calls `_owner.UpdateSelection`
2. `UpdateSelection` calls `ReplaceEntrySelectionFromOwner` to sync the backing data

The contract requires the caller to have already updated `_isSelected` before calling `ReplaceEntrySelectionFromOwner`. If a future developer calls `ReplaceEntrySelectionFromOwner` directly without pre-setting `_isSelected`, the backing `_entry.Selected` disagrees with `_isSelected`, causing persistent state inconsistency across serialization.

The comment on lines 91-115 documents this extensively, which is itself a code smell. The protocol is inherently fragile.

---

### M2. Bridge delegates in EntryListViewModel are null-initialized

**File**: `Features/Entries/EntryListViewModel.cs`, lines 55-65

```csharp
internal Action<LauncherStatusContent> StatusApplier = null!;
internal Action DragPreviewDiscarder = null!;
internal Action<LauncherEntryViewModel, bool> SelectionUpdater = null!;
internal Func<bool> DetailedListModeChecker = null!;
internal Func<bool> NamesModeChecker = null!;
// ... etc
```

**Description**: All bridge delegates use `= null!` suppressing null warnings. If `MainWindowViewModel` forgets to set any of these during construction (or a future refactor removes one assignment), calling the delegate throws `NullReferenceException`. There is no guard at the call sites.

---

### M3. ReplaceEntries creates duplicate ViewModels via EntryProjection

**File**: `Features/Entries/EntryListViewModel.cs`, lines 86-100

**Description**: `ReplaceEntries` creates `LauncherEntryViewModel` instances for `_entries` and `_visibleEntries`. Then `_projection.LoadEntries(entries, this)` at line 99 calls `_cache.EditDiff(viewModels, ...)` which creates **another** set of `LauncherEntryViewModel` instances in the DynamicData cache. The DynamicData cache has a different set of VM instances from `_entries`/`_visibleEntries`. This means:

- The DynamicData projection's output (`_projection.VisibleEntries`) references different VM instances than the manually-maintained `_visibleEntries`
- The manual `_visibleEntries` is used for rendering, while the projection's count is used for `VisibleCount`
- If `ReplaceEntries` is called multiple times, old VM instances in the DynamicData cache are orphaned with no Dispose call

---

### M4. FileLauncherJsonIO.Save blocks the UI thread with SemaphoreSlim.Wait()

**File**: `Infrastructure/FileLauncherJsonIO.cs`, line 111

```csharp
_saveLock.Wait();  // synchronously blocks
```

**Description**: `Save` is called from `StoreGrain.Dispatch` which is called from UI-thread command handlers (`OnLayoutFilterChanged`, `OnLayoutSortChanged`, `OnLayoutViewModeChanged`, `ApplyPreset`, etc.). The `Wait()` blocks the UI thread during the entire file I/O (serialize + temp file + File.Replace + backup rotation). For small config files this is nearly instant, but during backup rotation with 3 backup files it could be several hundred milliseconds.

The `CleanupStaleTempFiles` method (line 182) similarly blocks with `Wait()`.

---

### M5. ObservableCollection mutation during drag preview not guarded against reentrancy

**File**: `ViewModels/MainWindowViewModel.Drag.cs`, lines 90-113

```csharp
_list.MutableVisibleEntries.Move(dragIdx, newIdx);
```

**Description**: `ReorderVisibleEntryForDrag` directly mutates `_list.MutableVisibleEntries` (an `ObservableCollection`). This fires `CollectionChanged` synchronously, which may trigger UI callbacks. If any callback triggers a filter/sort/refresh (which calls `ApplyFilter` -> `DiscardDragReorderPreview`), the code re-enters drag operations on a partially-mutated collection. The debounce guard on lines 56-63 only prevents redundant previews, not full reentrancy.

---

### M6. FileStorePersistence.Load reads full file before size validation

**File**: `Infrastructure/FileStorePersistence.cs`, lines 32-40

```csharp
LauncherStoreFile LoadAndValidate(string path)
{
    var fileInfo = new FileInfo(path);
    if (fileInfo.Length > 50 * 1024 * 1024)
        throw new InvalidDataException("...");
    var store = _io.LoadRequired<LauncherStoreFile>(path, "Launcher store");
    ...
}
```

**Description**: The size check happens inside `LoadAndValidate`, which is called as a callback to `_io.LoadWithRecovery`. But `LoadWithRecovery` may have already attempted to load the file before calling this callback (it handles backup recovery before calling `loadAndValidate`). The backup recovery code at line 64 does `loadAndValidate(path)` which means the file is already fully deserialized by the time the size check runs. For a 50MB JSON file, this is a large memory allocation that could have been avoided.

---

### M7. `LauncherLaunchWorkflow.StartBatch` accepts `CancellationToken` but `PreflightBatch` does not

**File**: `ViewModels/LauncherLaunchWorkflow.cs`, lines 48 and 73

```csharp
public LauncherBatchLaunchPreflight PreflightBatch(
    IEnumerable<LaunchEntry> entries, bool requireConfirmation)
...
public LauncherBatchLaunchReport StartBatch(
    LauncherBatchLaunchPreflight preflight, CancellationToken cancellationToken = default)
```

**Description**: The preflight phase (which builds launch plans for each entry) has no cancellation support. Each call to `BuildPlan` involves template resolution and directory validation. If the user cancels during preflight, the cancellation is only effective once execution reaches `StartBatch`. The `cancellationToken` is also not propagated to `BuildPlan` calls inside `PreflightBatch`.

---

## Low (4)

### L1. CountChanged event never raised

**File**: `Features/Entries/EntryListViewModel.cs`, line 51-52, 103

```csharp
public event Action? CountChanged;
...
internal void NotifyCountChanged() => CountChanged?.Invoke();
```

**Description**: `NotifyCountChanged()` is never called from anywhere. The subscriber in `MainWindowViewModel` constructor line 147 (`_list.CountChanged += OnEntryCountChanged`) is dead code. `TotalCount` and `VisibleCount` are only updated explicitly (e.g., `ApplyFilter` line 146, `ReplaceEntries` line 99).

---

### L2. OnClosing unsubscribes events that are on the Window itself

**File**: `Views/MainWindow.axaml.cs`, lines 296-329

**Description**: The `OnClosing` handler unsubscribes from Window-level events (`PointerMoved`, `SizeChanged`, `Opened`, `KeyDown`, etc.). Since the Window is about to be garbage collected, these unsubscriptions are unnecessary. The event subscriptions on the Window would not leak even if not explicitly unsubscribed. However, this is harmless defensive code.

---

### L3. StoreGrain.StateChanged fires outside the Grain lock

**File**: `Infrastructure/StoreGrain.cs`, lines 58-71

```csharp
public void Dispatch(...)
{
    var after = Mutate(state => { ... });
    try { StateChanged?.Invoke(after); }  // outside lock
    catch (...)
}
```

**Description**: `StateChanged` is invoked outside the `Grain._lock`. Between `Mutate` returning (lock released) and `StateChanged` being invoked, a second `Dispatch` call could mutate the state again. Subscribers see a potentially stale snapshot if they re-query `Select()` from the callback.

---

### L4. Hardcoded Key.F for Ctrl+F

**File**: `Views/MainWindow.axaml.cs`, line 193

```csharp
if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
```

**Description**: Uses `Key.F` directly. On non-QWERTY keyboard layouts (AZERTY, QWERTZ), the physical key position for the find action does not correspond to `Key.F`. Cross-platform frameworks typically keybind by virtual key code rather than logical key. Avalonia's `Key` enum maps to physical keys. On AZERTY, Ctrl+F is still `Key.F` since it's the same physical key position, but the key labeled "A" on QWERTY is labeled "Q" on AZERTY. This is unlikely to be an issue for the target Windows market.

---

## Not checked

- **Tests** -- test files are not in `WTLauncher.App/` but in `WTLauncher.Tests/`. Not in scope.
- **XAML files** -- only .cs code-behind was reviewed.
- **Native AOT compatibility** -- not in scope.
- **DI container wiring** -- `App.axaml.cs` was not fully analyzed for singleton lifetimes.

---

## Summary table

| ID | Severity | File | Line | Issue |
|---|---|---|---|---|
| C1 | Critical | ExceptionFilters.cs | 19-20 | Infinite loop when TypeInitializationException/AggregateException has null InnerException |
| H1 | High | AppStateMachine.cs | 78-79 | Last Permit overrides previous two -- EndSave only goes to DialogOpen |
| H2 | High | EntryProjection.cs | 79 | DynamicData Rx pipeline never disposed (EntryListViewModel not IDisposable) |
| H3 | High | MainWindowViewModel.cs | 195-200 | _storeSubscription never disposed; also _list.EntriesChanged/CountChanged subs leak |
| M1 | Medium | LauncherEntryViewModel.cs | 91-122 | Fragile dual-update protocol between IsSelected setter and ReplaceEntrySelectionFromOwner |
| M2 | Medium | EntryListViewModel.cs | 55-65 | Bridge delegates initialized to null! -- NRE if not set by MWVM |
| M3 | Medium | EntryListViewModel.cs | 86-100 | Duplicate VM instances in _entries vs _projection cache; orphans on ReplaceEntries |
| M4 | Medium | FileLauncherJsonIO.cs | 111 | SemaphoreSlim.Wait() on UI thread blocks during file I/O |
| M5 | Medium | MainWindowViewModel.Drag.cs | 90-113 | ObservableCollection.Move() during drag not guarded against reentrant refresh |
| M6 | Medium | FileStorePersistence.cs | 32-40 | Size check runs after backup recovery may have already loaded the file |
| M7 | Medium | LauncherLaunchWorkflow.cs | 48,73 | CancellationToken not propagated to PreflightBatch |
| L1 | Low | EntryListViewModel.cs | 51-52 | CountChanged event declared and subscribed but never raised |
| L2 | Low | MainWindow.axaml.cs | 296-329 | Unnecessary event unsubscription on closing window |
| L3 | Low | StoreGrain.cs | 58-71 | StateChanged fires outside the lock, potential TOCTOU |
| L4 | Low | MainWindow.axaml.cs | 193 | Hardcoded Key.F for Ctrl+F -- potential layout issue |
