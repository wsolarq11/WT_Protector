# Research: Sort/Filter Feature Audit for Features 3, 11, 12

- **Query**: Audit 3 sort/filter features across 5 criteria (real problem, data ownership, user path, failure contract, test coverage)
- **Scope**: internal
- **Date**: 2026-06-10

## Feature 3: Search/Filter

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryVisibility.cs` | Core filter engine — `MatchesFilter()` multi-word token AND, `FilterAndSort()` |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Layout.cs` | FilterText property with `OnFilterTextChanged` → `ApplyFilter()`, searchHistory recording |
| `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs` | Ctrl+F handler (lines 85-91) focuses SearchTextBox + SelectAll |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs` | `SearchHistory` field (line 30-31), `List<string>` type |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsStore.cs` | `RequireSearchHistory()` normalizes, deduplicates, rejects null (lines 98-115) |
| `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryVisibilityTests.cs` | 15 test cases — filter, null rejection, multi-token, projection validation |
| `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.FilterTests.cs` | 4 test cases — FilterText property, persistence, edge cases |

### Key Patterns

**Multi-word filter (MatchesFilter, LauncherEntryVisibility.cs:124-141):**
- Single token: substring match on name, directory, command (case-insensitive)
- Multiple tokens (space-split): ALL tokens must match (AND semantics), each token matches against any of name/directory/command
- Tokens are whitespace-trimmed, empty tokens discarded

**Ctrl+F (MainWindow.axaml.cs:85-91):**
- Simple focus + select-all on the search TextBox
- No toggle behavior (Ctrl+F does not close search if already focused)
- Works across all views (cards/details/names)

**SearchHistory (LauncherSettings.cs:30-31, MainWindowViewModel.Layout.cs:81-92):**
- `List<string>` stored in settings JSON
- Max 20 entries, most recent first
- Deduplicates (case-insensitive), skips blank entries
- Recorded on every FilterText change — NOT just on explicit "submit"
- Normalized and validated on store load by `RequireSearchHistory()`

### Failure Contracts
- `FilterAndSort` throws on null filterText, null entry fields, invalid sort mode, missing/duplicate custom order entries
- `FilterText` setter: on save failure, rolls back to snapshot's LastFilterText and re-applies filter
- `RequireSearchHistory`: null list → `InvalidDataException`; blank entries silently skipped; dedup applied

### Gaps
- SearchHistory is recorded but there is no visible search history dropdown/suggestion UI in the analyzed code — the data is collected but not surfaced to the user
- Ctrl+F: no Escape-to-defocus behavior; focus stays in search box until manual interaction
- No debouncing on FilterText change — every keystroke triggers full filter + settings save

---

## Feature 11: Custom Order and Drag/Non-Drag Reorder

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryOrder.cs` | Core reorder engine — `SyncWithEntries()`, `BuildIndex()`, `MoveRelative()` |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherDragReorderSession.cs` | Drag session state — preview dedup, target tracking, drag cue management |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherDragReorderWorkflow.cs` | Workflow orchestration — Begin/Preview/Commit/Cancel with snapshot rollback |
| `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs` | Drag UI handlers — ghost, FLIP, lazy preview (WouldChangePreview guard), card cache |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Reorder.cs` | Context-menu reorder — `MoveUp()`, `MoveDown()` with guards |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Drag.cs` | Drag commands — `BeginDragReorder`, `PreviewMoveEntry`, `Commit/Cancel` |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs` | `CustomEntryOrder` field (line 23-25), `List<string>` |
| `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryOrderTests.cs` | 19 test cases |
| `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherDragReorderSessionTests.cs` | 5 test cases |
| `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherDragReorderWorkflowTests.cs` | 1 test case (constructor validation only) |
| `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.DragTests.cs` | 13 test cases |

### Key Patterns

**Data ownership:**
- `customEntryOrder: List<string>` in `LauncherSettings` — the settings object owns the order
- `LauncherSettingsSnapshot` captures pre-mutation state for rollback
- `LauncherDragReorderSession` is a SINGLE `_session` field on `LauncherDragReorderWorkflow` — no scattered state

**Drag path (MainWindow.Drag.cs:137-157):**
- `WouldChangePreview()` is a cheap O(1) guard before expensive visual tree traversal
- "Lazy preview" — lazy capture only when the target actually changed
- Ghost overlay + XAML transition suppression (class `suppress-drag-transitions`)
- Card cache (`_dragCardCache`) built once at drag start, avoids repeated visual tree walks

**Context-menu reorder (MainWindowViewModel.Reorder.cs):**
- `CanMoveUp` / `CanMoveDown` guards: only in custom sort mode, not at boundaries
- Snapshot-based: capture → mutate → save → rollback on failure
- During active drag preview, returns `MoveUnavailable` status

**Commit/Cancel (LauncherDragReorderWorkflow.cs:109-158):**
- Commit: persists settings, clears preview state
- Cancel: restores `LauncherSettingsSnapshot.CustomEntryOrder`, returns `Canceled` status
- Both paths clear drag cues on all entries

### Failure Contracts
- `MoveRelative` throws if dragged/target entry not in custom order (defensive coding)
- `SaveSettingsChange()` failure → rollback snapshot + status text
- `BeginDragReorder` with unknown source → silently restores preview state, returns false
- `PreviewMoveEntry` with unknown target → clears hover, no mutation
- All drag state is cleared on session end (Clear() walks _target, _source, _lastPreview)
- File-system save failures (e.g., path is a directory) are explicitly tested

### Gaps
- `LauncherDragReorderWorkflow` test coverage is thin (1 test — constructor only); Preview/Commit/Cancel logic is exercised through MainWindowViewModel integration tests
- No test for concurrent drag sessions (two users dragging at once is impossible in single-window app, but the code handles it via `.IsFor(draggedEntryId)`)
- Drag ghost is not tested in unit tests (visual-only)

---

## Feature 12: Sort Modes

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryVisibility.cs` | Sort mode constants (custom/recent/updated/alphabetical), `NormalizeSortModeId()`, `FilterAndSort()` sort dispatch |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Layout.cs` | Sort mode dropdown, `OnSelectedSortModeChanged()`, `ApplyFilter()` |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherOrderExtensions.cs` (if exists) | N/A — order logic is in `LauncherEntryOrder.cs` |

### Key Patterns

**4 sort modes (LauncherEntryVisibility.cs:5-8):**
| Mode ID | Label (Chinese) | Sort key | Tiebreaker 1 | Tiebreaker 2 |
|---|---|---|---|---|
| `custom` | 自定义拖拽 | `customEntryOrder` index | name (case-insensitive) | entry ID |
| `recent` | 最近启动 | `launchedAtUtc` DESC | name | entry ID |
| `updated` | 更新时间 | `updatedAtUtc` DESC | name | entry ID |
| `alphabetical` | 首字母顺序 | name (case-insensitive) | — | entry ID |

**Mode switch (MainWindowViewModel.Layout.cs:104-124):**
- Snapshot-based: capture → mutate `_settings.SortMode` → save → rollback on failure
- Calls `EnsureCustomOrderContainsEntries()` to sync order with entries before applying
- Status text updates: "排序模式: {label}（已保存）" on success, error message on failure

**recent mode and launchedAtUtc:**
- `ParseLaunchedAt()` (LauncherEntryVisibility.cs:180-193) treats null/empty/parse-failure as `DateTimeOffset.MinValue`
- Sort direction: descending (most recent first)
- `launchedAtUtc` is NOT validated upfront like `updatedAtUtc` — silently falls back to MinValue
- No `ValidateSortProjection` check for `launchedAtUtc` (unlike `updatedAtUtc` at line 90-91)

**updated mode:**
- `ValidateSortProjection` requires valid `updatedAtUtc` for every entry (line 79-92)
- `ParseUpdatedAt()` throws on null (line 197-198) and on parse failure (line 207)
- Strictest mode: demands data integrity

**NormalizeSortModeId (LauncherEntryVisibility.cs:10-21):**
- Only accepts the 4 known IDs — everything else throws `InvalidDataException`
- Case-sensitive exact match after trim

### Failure Contracts
- Unknown sort mode → `InvalidDataException` with descriptive message
- Null `selectedSortMode` → `InvalidOperationException` ("Launcher sort mode selection is required.")
- Custom sort with missing/duplicate order → `InvalidDataException` / `ArgumentException` before projection
- Alphabetical sort with null name → `InvalidDataException` ("Launcher entry name is required for projection.")
- `OnSelectedSortModeChanged` save failure → rollback to snapshot SortMode, re-apply filter

### Gaps
- `launchedAtUtc` is not validated in `ValidateSortProjection` — silently tolerates garbage data
- recent sort treats null launch timestamps as equal (all `DateTimeOffset.MinValue`), which creates an undefined ordering among never-launched entries
- No "last used" vs "most launched" distinction — `launchedAtUtc` is a single per-entry field
- Sort mode is not observable per-entry (e.g., no visual indicator of which entry was launched most recently)
- Test coverage: 6 sort-specific tests in `LauncherEntryVisibilityTests.cs` (alphabetical tiebreak, updated sort, updated validation, custom order normalization, read-only snapshot, custom validation edge cases)
