# Research: Infrastructure Code and Edge Case Analysis

- **Query**: Deep code review of infrastructure files -- exception handling, memoization, result types, enums, text formatting, architecture constraints, batch launch, search history, persistence test fixtures
- **Scope**: Internal (codebase)
- **Date**: 2026-07-23

## Findings

### 1. ExceptionBoundary (`src/avalonia-launcher/src/WTLauncher.App/Infrastructure/ExceptionBoundary.cs`)

#### 1.1 Usage Consistency -- NOT USED (Confidence: 100)

`ExceptionBoundary` defines 4 methods (`Run<T>`, `RunAsync<T>`, `RunAction`, `RunActionAsync`) but is **never called anywhere** in the codebase. The search for `ExceptionBoundary.` returned 0 matches outside the definition file itself.

All 34 catch clauses across the codebase use the older pattern directly: `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) { ErrorLog.Write(ex, "context"); return ...; }`. This is exactly the boilerplate that `ExceptionBoundary` was designed to eliminate.

Files that use the manual pattern instead of `ExceptionBoundary`:
- `src/WTLauncher.App/Infrastructure/AppStore.cs`:80,109,138
- `src/WTLauncher.App/Features/Entries/EntryService.cs`:29-31
- `src/WTLauncher.App/Features/Entries/EntryListViewModel.cs`:201-203
- `src/WTLauncher.App/Features/Presets/PresetService.cs`:52-54
- `src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`:251-253
- `src/WTLauncher.App/ViewModels/LaunchOrchestrator.cs`:72-74
- And many more.

Some of these handlers do more than just log-and-return (e.g., they return `ServiceCanceled<T>` on dialog dismissal, or handle specific known exceptions differently). For those, `ExceptionBoundary`'s one-size-fits-all `$"操作失败: {context}"` error message would be too inflexible. But there are several sites (e.g., `EntryListViewModel.cs` lines 201, 242, 273, 301) that do fit the pattern exactly.

#### 1.2 Context Capture (Confidence: 80)

Captures the `context` string parameter and passes it to `ErrorLog.Write`. The context string is the only identifier for where the exception occurred -- no call stack enrichment, no structured logging tags, no activity IDs. `ErrorLog.Write` outputs: `{DateTimeOffset:O} [{context}] {exception}{Environment.NewLine}`. This is adequate for a desktop launcher but lacks structured fields for automated log analysis.

#### 1.3 Reentrancy Safety (Confidence: 100)

`ExceptionBoundary` itself has no state -- it is a static class with static methods. The `catch` block runs synchronously before returning. `ErrorLog.Write` internally uses `Interlocked.Exchange` to guard concurrent writes. No reentrancy issue.

#### 1.4 Error Message Hardcoding (Confidence: 100)

All four methods hardcode the Chinese error message `$"操作失败: {context}"`. The `RunAction`/`RunActionAsync` variants return this as a `string?`, while `Run`/`RunAsync` wrap it in `ServiceFailure<T>`. Even if this file were used, the error message format lacks the user-friendly sanitization that `ExceptionMessageFormatter.ForUser()` provides elsewhere.

---

### 2. MemoizedSelector (`src/avalonia-launcher/src/WTLauncher.App/Infrastructure/MemoizedSelector.cs`)

#### 2.1 Thread Safety -- NOT Thread-Safe (Confidence: 100)

Instance fields `_lastInput`, `_lastOutput`, `_hasCached` are accessed with zero synchronization. Under concurrent calls to `Get()`:

| Scenario | Outcome |
|---|---|
| Two threads with different inputs race on write | Returned `_lastOutput` may not correspond to the returned `_lastInput` value. Type mismatch is impossible (generic) but the wrong output is returned. |
| Thread A writes `_lastInput` but not yet `_lastOutput`; Thread B reads | Thread B sees `_hasCached == true` but stale/mismatched `_lastOutput`. |
| Write to reference-type `_lastOutput` without volatile | Other threads may never observe the new value. |

Currently used in `LayoutManager.cs:17` as `MemoizedSelector<(Size availableSize, int visibleCount), bool>` -- the value tuple `(Size, int)` is a struct, so reading a partially-written struct can yield a corrupted value (e.g., `availableSize` from one call and `visibleCount` from another). This call is on the Avalonia UI thread only, so in practice races do not occur. But nothing prevents calling from a background thread.

#### 2.2 Memory Leak Risk (Confidence: 100)

None. Only caches the **last** (input, output) pair -- O(1) fixed memory. `_lastInput` and `_lastOutput` are overwritten on each cache miss. If the cached `TOutput` is a large object that is no longer referenced elsewhere, it becomes eligible for GC on the next write.

#### 2.3 Key Equality (Confidence: 95)

Uses `EqualityComparer<TInput>.Default.Equals(_lastInput, input)`. For the current usage with the value tuple `(Size, int)`, this calls the compiler-generated `Equals` which compares each component by value. This is correct.

For any future use with a reference-type `TInput`, `EqualityComparer<T>.Default` uses `Object.Equals` -- which means reference equality for classes without overridden `Equals`. This could cause false cache misses if semantically equal instances are distinct objects. The `where TInput : notnull` constraint prevents null keys at compile time but does not enforce value equality semantics.

#### 2.4 Null Keys (Confidence: 100)

`where TInput : notnull` prevents null at the type level. The `_lastInput = default!` initialization could theoretically be null for nullable value types, but `(Size, int)` is a non-nullable value type so `default!` is `(default(Size), default(int))` which is a valid value.

#### 2.5 Usage (Confidence: 100)

Used in exactly one place: `LayoutManager.cs:17` (`_cardSizeSelector`). Called at `LayoutManager.cs:159` in `TryRefreshCardSize`. The memoization avoids redundant card-size recalculations when the available size and visible count have not changed.

---

### 3. ServiceResult (`src/avalonia-launcher/src/WTLauncher.App/Features/ServiceResult.cs`)

#### 3.1 Error Pattern (Confidence: 100)

Custom discriminated union with three sealed record subclasses:

| Subclass | Semantics |
|---|---|
| `ServiceSuccess<T>` | Operation completed with a result value. `Value` is non-null (enforced by `ArgumentNullException.ThrowIfNull` in constructor). |
| `ServiceCanceled<T>` | User dismissed/canceled the operation. Carries `LauncherStatusContent` for the status bar message. |
| `ServiceFailure<T>` | Operation failed due to an exception or validation error. Carries `LauncherStatusContent` for the error message. |

This is not the AnyOrNothing pattern (no implicit `None` state). It is not the ErrorOr pattern used in Core. It is a custom three-state result that is specific to the App layer. Core uses the `ErrorOr<T>` library from NuGet, while App uses this custom `ServiceResult<T>`.

#### 3.2 Implicit Conversion Pitfalls (Confidence: 100)

None. `ServiceResult<T>` is an abstract record with a `private protected` constructor -- consumers cannot derive new variants. No implicit operators exist. Consumers must pattern match explicitly:

```csharp
if (result is ServiceSuccess<T> success) { /* use success.Value */ }
```

There is a consistent pattern across consumers: `EntryListViewModel.cs` and `MainWindowViewModel.*.cs` files all use switch expressions or `is ServiceSuccess<T> success` checks. No implicit conversion trap exists.

#### 3.3 Usage (Confidence: 100)

Used extensively:
- `IPresetService` / `PresetService` -- 7 methods returning `ServiceResult<T>`
- `IEntryService` / `EntryService` -- 5 methods
- `EntryListViewModel` -- pattern-matches results in 4 methods
- `MainWindowViewModel.*.cs` -- pattern-matches in multiple locations (Grouping, Discovery, Presets partials)

---

### 4. LauncherSearchHistory (`src/avalonia-launcher/src/WTLauncher.Core/LauncherSearchHistory.cs`)

#### 4.1 Max Size Enforcement (Confidence: 95)

`Record()` method trims to `MaximumTerms` (20) after insert:

```csharp
return updated.Count > MaximumTerms
    ? updated.RemoveRange(MaximumTerms, updated.Count - MaximumTerms)
    : updated;
```

The `StoreReducers.AddSearchTerm()` also caps at 20, but it has a subtle dedup difference (see 4.2).

**Gap (Confidence: 90)**: There is **no trimming on load**. When `LauncherSettings.SearchHistory` (line 44 of LauncherSettings.cs) is deserialized from JSON, if the `config/launcher.json` file has a `searchHistory` array with more than 20 entries, all entries survive in memory. Trimming only happens when the next `Record()` call is made. If the user never searches again, the oversized array persists in memory and will be re-serialized to disk on the next settings save, perpetuating the bloated array.

#### 4.2 Deduplication (Confidence: 100)

Uses `StringComparison.OrdinalIgnoreCase` for:
- "Already newest" check (line 21): `string.Equals(history[0], normalized, StringComparison.OrdinalIgnoreCase)`
- Dedup removal (line 27): `.RemoveAll(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase))`

This means "Demo Project" and "demo project" are treated as the same term. The normalized value ("demo project") wins regardless of original casing. This is consistent with the `EntryName.Normalize` call that lowercases via Unicode normalization FormKC (which folds case in a locale-aware way for most scripts).

**Divergent implementation (Confidence: 95)**: `StoreReducers.AddSearchTerm()` at `Infrastructure/StoreReducers.cs:75-88` has a parallel implementation that uses `ImmutableList.Remove(term)` which uses the default `EqualityComparer<string>.Default` (ordinal, case-sensitive). This means:
- `LauncherSearchHistory.Record("Demo")` deduplicates "demo" case-insensitively
- `StoreReducers.AddSearchTerm("Demo")` would NOT deduplicate "demo" -- it would add a second entry

However, `StoreReducers.AddSearchTerm` is **never called** from anywhere (confirmed: only defined, never referenced). The actual save path goes through `MainWindowViewModel.Layout.cs:163-168` which uses `LauncherSearchHistory.Record`. So the divergent behavior is a maintenance hazard but not a live bug.

#### 4.3 Persistence Structure (Confidence: 100)

Saved through `TryPersistSettingsChange` in `MainWindowViewModel.Layout.cs:164-167`:

```csharp
var newHistory = LauncherSearchHistory.Record(_settings.SearchHistory, _layoutManager.FilterText);
if (ReferenceEquals(newHistory, _settings.SearchHistory))
    return;
TryPersistSettingsChange(s => s with { SearchHistory = newHistory });
```

The `ReferenceEquals` optimization correctly avoids save when the term is already newest (the `Record` method returns the same instance). The JSON serialization uses `ImmutableList<string>` which serializes as a JSON array under the `searchHistory` key. Structure is correct.

---

### 5. LauncherStatusText / LauncherDialogText / Status Text Classes

#### 5.1 Hardcoded Chinese Strings (Confidence: 100)

All user-facing strings in `LauncherStatusText.cs` (364 lines) and `ExceptionMessageFormatter` are hardcoded Chinese. String interpolation uses `$""` syntax throughout, e.g.:

```
$"已加载 {entryCount} 个启动项、{presetCount} 个启动组"
$"已新增按钮: {entryName}（已保存）"
$"启动组\u201c{presetName}\u201d启动失败: {ExceptionMessageFormatter.ForUser(exception)}"
```

**Localization impact**: All placeholders use string interpolation (`$""`) rather than indexed format strings (`string.Format("{0}", arg)`). Simple string extraction via regex is possible (`\$"([^"]*)"`), but tooling that expects `{0}`-style placeholders will not parse these files directly. A future localization pass would need to convert each `$""` to either `string.Format` or a resource string builder.

**No format string vulnerability**: Format arguments are trusted domain values (entry counts, preset names, entry names validated by `EntryName.RequireNonEmpty`, or exception messages sanitized by `ExceptionMessageFormatter.ForUser`). No user-supplied raw text reaches a format string.

#### 5.2 ExceptionMessageFormatter.ForUser (Confidence: 95)

Method at `LauncherStatusText.cs:126-142` sanitizes exception messages for user display:

| Exception Type | Output |
|---|---|
| `FileNotFoundException` | `"找不到文件"` (replaces message, which leaks path) |
| `DirectoryNotFoundException` | `"找不到目录"` (replaces message) |
| `UnauthorizedAccessException` | `"没有访问权限"` (replaces message) |
| `IOException` | `"文件操作失败"` (replaces message) |
| `InvalidDataException`, `ArgumentException`, `InvalidOperationException` | **Passes through original `exception.Message`** |
| Any other type | `"操作失败，请查看错误日志了解详情"` |

**Risk (Confidence: 70)**: Passing through the original `Message` for `InvalidDataException` carries a small risk. If an `InvalidDataException` wraps a lower-level error and its message includes a file path (e.g., `"The file 'C:\Users\...\secret.txt' contains invalid data."`), that path leaks to the user. This is by design -- the comment says app-layer exceptions carry "intentionally user-safe messages crafted by the throwing code." The risk is low because the throwing code is within the same application, not from untrusted input.

#### 5.3 LauncherDialogText Usage (Confidence: 100)

`LauncherDialogText` defines 6 factory methods (`Title`, `Body`, `Label`, `Subtle`, `Error`, `Input`) plus `SafeParseBrush`. Only `SafeParseBrush` is used externally -- 4 calls in `MainWindow.Drag.cs:311,312,337,346`. The `Title`, `Body`, `Label`, `Subtle`, `Error`, `Input` methods are never called. The `ponytail:` comment on the class (line 13) acknowledges this: "retained for SafeParseBrush used by MainWindow.Drag.cs; dialog-specific factory methods migrated to XAML."

---

### 6. ArchitectureRules (`tests/WTLauncher.Tests/ArchitectureRules.cs`)

#### 6.1 Constraints Tested (Confidence: 100)

12 architecture rules (R1-R12) enforced via ArchUnitNET:

| Rule | Constraint | Method |
|---|---|---|
| R1 | Core must not reference Avalonia | Namespace-based (`Avalonia`) |
| R2 | Core must not reference CommunityToolkit.Mvvm | Namespace-based (`CommunityToolkit`) |
| R3 | Core must not depend on App | Namespace-based (`WTLauncher.App`) |
| R4 | ViewModels must not depend on `System.IO.File` | Type-level (`HaveFullName("System.IO.File")`) |
| R5 | Only LauncherJson and 3 known readers may depend on `System.IO.File` in Core | Type-level exclusion list |
| R6 | Core must not reference App.ViewModels | Namespace-based |
| R7 | Features.Presets must not reference Views | Namespace-based |
| R8 | Features.Entries must not reference Views | Namespace-based |
| R9 | Features.Presets must not depend on `System.IO.File` | Type-level |
| R10 | Features.Entries must not depend on `System.IO.File` | Type-level |
| R11 | Infrastructure must not depend on ViewModels | Namespace-based |
| R12 | Features.Dialogs must not reference Views | Namespace-based |

#### 6.2 Coverage Gaps (Confidence: 85)

Notable constraints NOT tested:

| Missing Constraint | Why It Matters |
|---|---|
| **Views must not depend on Infrastructure** | Views (`MainWindow.axaml.cs`, `MainWindow.Drag.cs`) directly use `ExceptionFilters.IsRecoverable` and `ErrorLog.Write`. If Infrastructure layering should be below Views, this is a violation. Currently Views reference: `ExceptionFilters` (via `ExceptionFilters.IsRecoverable`), `ErrorLog` (via `ErrorLog.Write`), `LauncherDialogText`. |
| **Infrastructure must not depend on Features** | `ExceptionBoundary` depends on `LauncherStatusContent` (in Features.Status). `AppStore` depends on `LauncherStoreFile` and mutations from Core. This dependency currently exists but is not tested. |
| **Views must not depend directly on ViewModels** (beyond binding) | Views reference ViewModels for cast operations and property access. This is expected but not constrained. |
| **Features must not depend on ViewModels** | `EntryService` and `PresetService` return `ServiceResult<T>` which is consumed by ViewModels, but Features layer must not take a dependency on ViewModel types. Not explicitly tested. |
| **No Core-to-Core circular dependency test** | Detecting `A → B → A` within Core or App. |

**False positive risk for R5 (Confidence: 80)**: R5 uses type-level `NotDependOnAnyTypesThat().HaveFullName("System.IO.File")` with an exclusion list. The ponytail comment at line 103-107 explains that method-level checks are a no-op in ArchUnitNET. This means:
- Any new Core type that uses `File.Exists` (a read) but is not in the exemption list will **falsely fail** R5.
- Conversely, a new Core type that uses `File.WriteAllText` directly **will** be correctly caught.
- The exemption list for read-only access will need maintenance.

#### 6.3 Assembly Loading Scope (Confidence: 100)

Line 22: Only two assemblies are loaded:
- `typeof(LauncherStore).Assembly` -- Core layer
- `typeof(MainWindowViewModel).Assembly` -- App layer

This means rules that reference the WTTests assembly or other assemblies would not see them. This is fine for these rules since they all constrain Core or App.

---

### 7. LauncherEntryViewMode (`src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryViewMode.cs`)

#### 7.1 Completeness of View Mode Switching (Confidence: 100)

Three values: `Cards`, `Details`, `Names`. All three are:

| Concern | Coverage |
|---|---|
| Enum definition | All 3 present (line 5-7) |
| String IDs | `cards`, `details`, `names` (lines 12-14) |
| `NormalizeId()` | Exhaustive switch over all 3, with `_ => throw` (lines 21-28) |
| `ToId()` | Exhaustive switch over all 3, with `_ => throw` (lines 30-36) |
| `LayoutManager` projections | `IsCardsViewMode`, `IsDetailedListMode`, `IsNamesViewMode` cover all 3 (lines 120-122) |
| `LayoutManager` display mode strings | "卡片", "详细列表", "名称墙" (lines 34-36) |
| XAML views | LauncherEntriesView.axaml references all 3 via `Is*ViewMode` |

No gaps. If a fourth mode were added, both `NormalizeId` and `ToId` would throw at runtime until updated -- a deliberate fail-fast approach.

---

### 8. LauncherStatusTone (`src/avalonia-launcher/src/WTLauncher.App/Features/Status/LauncherStatusTone.cs`)

#### 8.1 Unused Variants (Confidence: 100)

All 4 variants are used:

| Variant | Factory Method | Used In |
|---|---|---|
| `Neutral` | `LauncherStatusContent.Neutral()` | `LauncherMainWindowText.Ready`, `Loaded`, many more |
| `Success` | `LauncherStatusContent.Success()` | `LauncherEntryStatus.Added`, `Updated`, many more |
| `Warning` | `LauncherStatusContent.Warning()` | `LauncherEntryStatus.AddCanceled`, many more |
| `Error` | `LauncherStatusContent.Error()` | `ExceptionBoundary`, config failures, many more |

`StatusPresenter.cs:32-35` creates boolean projections for all 4. `LauncherStatusPresentation.IndicatorFor` handles all 4 with `_ => "•"` as fallback for any future variants.

---

### 9. LauncherIcons (`src/avalonia-launcher/src/WTLauncher.Core/LauncherIcons.cs`)

#### 9.1 Usage (Confidence: 95)

Defines 5 SVG path data strings (`LaunchIcon`, `EditIcon`, `DeleteIcon`, `MoreIcon`, `SearchIcon`) as static properties. **Never referenced** in the codebase -- searched for `LauncherIcons.` across all source files and got 0 matches outside the definition.

The icons may be intended for XAML usage via `x:Static` or similar mechanism, but a grep of `.axaml` files for "edit", "delete", "launch" found no references to `LauncherIcons`. These appear to be dead code -- either from an earlier approach before icons were moved to XAML directly, or as a planned feature that was never wired up.

---

### 10. LauncherBatchLaunchReport (`src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherBatchLaunchReport.cs`)

#### 10.1 Data Integrity (Confidence: 100)

`FailedCount` is a computed property (line 38): `public int FailedCount => _failureMessages.Count;` -- derived from the list, not stored separately. The constructor receives `requestedCount` and `skippedCount` as explicit parameters but constructs `FailedCount` from the failure messages list. The builder (lines 72-150) correctly accumulates failures and passes them to `ToReport()`.

**Edge case -- constructor parameter mismatch (Confidence: 95)**: The constructor accepts `requestedCount` and `skippedCount` independently from `failureMessages`. If a caller passes `requestedCount=5` with only 2 failure messages, `FailedCount` returns 2 and `RequestedCount` returns 5. This is consistent (the calls that succeeded are implied). But there is no validation that `failureMessages.Count + ... <= requestedCount` because skip logic is orthogonal.

#### 10.2 FailureIndex Semantics (Confidence: 100)

`FailureIndex` is set only by the first `RecordFailure` call that provides an index (line 140: `FailureIndex ??= failureIndex`). Subsequent calls with different failure indices do not overwrite. This means `FailureIndex` always points to the **first** failure index encountered, not the last. The `FailureIndices` list accumulates all indices. This is by design -- the first failure is the most relevant for diagnostic purposes.

#### 10.3 Cloning Guard (Confidence: 100)

`CloneFailureMessages` (line 54-69) validates that no failure message is null and creates a defensive copy via `Array.AsReadOnly`. The builder's copy constructor (line 81-91) correctly copies all fields from an existing report.

---

### 11. ExceptionFilters (`src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs`)

#### 11.1 DEBUG Behavior (Confidence: 100)

In DEBUG builds, unwraps `TypeInitializationException`/`AggregateException` and crashes on `NullReferenceException`, `IndexOutOfRangeException`, or `ArgumentNullException`. In Release, delegates to `LauncherErrors.IsRecoverable`. This is the correct pattern -- catch logic errors during development, treat them as recoverable in production (where crashing would lose user data).

#### 11.2 Unwrap Logic Duplication (Confidence: 95)

Both `ExceptionFilters.IsRecoverable` (App layer, lines 25-33) and `LauncherErrors.IsRecoverable` (Core layer, `ErrorOrExtensions.cs` lines 24-31) contain **identical** unwrap logic for `TypeInitializationException`/`AggregateException`:

```csharp
for (var i = 0; i < 16 && ex is TypeInitializationException or AggregateException; i++)
{
    if (ex.InnerException is null) break;
    ex = ex.InnerException;
    if (ex is AggregateException agg)
        ex = agg.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
}
```

This is deliberate: `ExceptionFilters.IsRecoverable` unwraps first so the DEBUG crash check operates on the root cause, then delegates to `LauncherErrors.IsRecoverable` which unwraps again (a no-op on the already-unwrapped exception). The double unwrap is redundant but safe.

#### 11.3 Usage (Confidence: 100)

Used by 34 catch clauses across the codebase:
- `App.axaml.cs:187` (startup)
- `EntryService.cs` (4 sites)
- `PresetService.cs` (5 sites)
- `EntryListViewModel.cs` (4 sites)
- `LaunchOrchestrator.cs` (5 sites)
- `MainWindowViewModel*.cs` (8 sites)
- `MainWindow.axaml.cs` (1), `MainWindow.Drag.cs` (4)
- `LauncherLaunchWorkflow.cs` (2)
- `LauncherEntryViewModel.cs` (2)
- And others.

---

### 12. LauncherPersistenceTestFixtures (`tests/WTLauncher.Tests/LauncherPersistenceTestFixtures.cs`)

Simple test data builders. Noteworthy edge case:

- `RootDrive` detection (line 7-8): `Path.GetPathRoot(Environment.CurrentDirectory) ?? Path.DirectorySeparatorChar.ToString()`. On Windows this returns `C:\`, on Unix `/`. The fallback handles the edge case where `Path.GetPathRoot` returns null (documented as possible for relative paths).
- `Entry()` creates entries with `Directory = Path.Combine(RootDrive, id)` -- ensures the `directory` field always matches a valid root path to satisfy the gate invariant about stale launch buttons.
- No edge case issues. Confidence: 100.

---

### 13. LauncherStatusPresentation (`src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherStatusPresentation.cs`)

Single method `IndicatorFor` mapping tone to a Unicode symbol:
- `Success` => `"✓"` (U+2713)
- `Warning` => `"!"`
- `Error` => `"×"` (U+00D7)
- `_` (default, including `Neutral`) => `"•"` (U+2022)

Used once at `StatusPresenter.cs:31`. The default branch means any future variant gets a bullet. No issues. Confidence: 100.

---

### 14. LauncherBatchLaunchPreflight / LauncherBatchLaunchConfirmation (`src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherBatchLaunchPreflight.cs`)

#### 14.1 LauncherBatchLaunchItem (Confidence: 100)
- Constructor validates `entry` non-null, `EntryId` via `EntryId.RequireEntryId`, `EntryName` via `_EntryName.RequireNonEmpty`, `Plan` non-null.
- No edge cases.

#### 14.2 LauncherBatchLaunchPreflight (Confidence: 100)
- Accepts `IReadOnlyList<LauncherBatchLaunchItem>` and `LauncherBatchLaunchReport`.
- Constructor creates a defensive array copy (`Items.ToArray()`), validates each item non-null.
- No edge cases.

#### 14.3 LauncherBatchLaunchConfirmation (Confidence: 100)
- Accepts items, targetLabel, failedCount, failureMessages.
- Validates `failedCount >= 0` (rejects negative with `ArgumentOutOfRangeException`).
- `targetLabel` is normalized via `EntryName.Normalize` with explicit empty check.
- Defensive copies both `Items` and `FailureMessages`.

---

### 15. ServiceResultTests (`tests/WTLauncher.Tests/ServiceResultTests.cs`)

Three tests covering:
- `ServiceSuccess<string>` rejects null value
- `ServiceCanceled<string>` and `ServiceFailure<string>` reject null status
- `NonSuccess` preserves the same `LauncherStatusContent` reference (reference equality check via `Assert.Same`)

Coverage gaps (Confidence: 85):
- No test for `ServiceResult` type hierarchy (e.g., that `is ServiceSuccess<T>` pattern matching works)
- No test for the `LauncherStatusContent` text/tone validation (empty string rejection)
- No test for `ServiceCanceled` vs `ServiceFailure` distinction semantics

---

### 16. LauncherSearchHistoryTests (`tests/WTLauncher.Tests/LauncherSearchHistoryTests.cs`)

Four tests covering:
- Normalization and newest-first ordering
- Case-insensitive deduplication
- Already-newest returns same instance (`ReferenceEquals`)
- Max size enforcement (drops oldest when exceeding 20)

Coverage gaps (Confidence: 85):
- **No test for initial load exceeding max size**: Tests always start from an empty or small history. No test verifies behavior when `Record()` is called on a list with >20 entries that was deserialized from a corrupted config.
- **No test for whitespace-only/empty term**: The method returns `history` unchanged for `IsNullOrWhiteSpace` terms, but there is no explicit test for this.
- **No test for Unicode normalization** (e.g., full-width vs half-width characters)
- **No trim-on-load test**: Would verify behavior when `SearchHistory` is loaded from JSON with >20 entries and `Record()` is never called.

---

## Cross-Cutting Concerns

### Dead Code Found
| Component | Status | Confidence |
|---|---|---|
| `ExceptionBoundary` | All 4 methods never called | 100 |
| `LauncherIcons` | All 5 icon paths never referenced | 95 |
| `LauncherDialogText.Title/Body/Label/Subtle/Error/Input` | 6 factory methods never called | 100 |
| `StoreReducers.AddSearchTerm` | Method never called; divergent dedup logic | 100 |

### Data Integrity Warnings

| Issue | File | Confidence |
|---|---|---|
| `StoreReducers.AddSearchTerm` uses case-sensitive dedup vs `LauncherSearchHistory.Record` uses case-insensitive | `StoreReducers.cs:75` vs `LauncherSearchHistory.cs` | 95 |
| `LauncherSearchHistory` has no trim-on-load for >20 entries | `LauncherSearchHistory.cs` | 90 |
| `MemoizedSelector` is not thread-safe; used with a value tuple that could be torn on concurrent read | `MemoizedSelector.cs` | 100 |
| `LauncherErrors.IsRecoverable` unwrap logic duplicated in `ExceptionFilters.IsRecoverable` | `ErrorOrExtensions.cs:24-31` and `ExceptionFilters.cs:25-33` | 95 |
| `ExceptionMessageFormatter.ForUser` passes through `exception.Message` for app-layer exceptions, which could leak paths if an exception wraps IO | `LauncherStatusText.cs:138` | 70 |

---

## Related Specs

- `.trellis/spec/core/quality-guidelines.md` -- Layer invariants encoded as architecture rules
- `.trellis/spec/core/index.md` -- Core layer guidelines
- `.trellis/spec/views/index.md` -- View layer guidelines
- `docs/feature-reality-north-star-audit.md` -- Claim-boundary ledger

## Caveats

- The `ExceptionBoundary` class may have been deliberately kept as a migration target -- existing catch handlers may be gradually migrated to use it. However, there is no evidence of a migration plan or any partial migration.
- XAML icon references may use a different mechanism (e.g., `PathIcon Data="{x:Static}"`) that the grep pattern did not match. A thorough audit of all `.axaml` files would be needed to confirm `LauncherIcons` is completely unused.
- Architecture rule coverage analysis is based on what is tested vs. what could be tested relative to the documented 5-layer architecture. Some constraints may have been deliberately omitted because they are enforced at compile time by project references.
