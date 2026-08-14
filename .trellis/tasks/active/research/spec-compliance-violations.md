# Research: Spec Compliance Violations

- **Query**: Compare code against project documentation to verify spec compliance
- **Scope**: Mixed - all 13 spec/docs files vs code across all layers
- **Date**: 2026-07-26

## Findings

### Critical (1)

#### C1. Infinite loop in ExceptionFilters.IsRecoverable -- violates max-iteration guard spec

**Spec source:** quality-guidelines.md "Exception unwrap loop: Must have max-iteration guard (`for i<16`) -- null InnerException causes infinite loop."

**Code:**
`src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs` lines 19-20:
```csharp
var inner = ex;
while (inner is TypeInitializationException or AggregateException)
    inner = inner.InnerException ?? inner;
```

When `inner.InnerException` is null, `inner.InnerException ?? inner` returns `inner` unchanged. The while-condition is still true, so the loop never terminates. The spec says "Must have max-iteration guard (`for i<16`)" but the code uses a `while` loop with `??` fallback that creates an infinite loop on null InnerException.

The Core layer's `LauncherErrors.IsRecoverable` properly uses a `for` loop with max 16 iterations. But `ExceptionFilters.IsRecoverable` does its own unwrapping before delegating to Core, and this version lacks the guard.

**Reproduction:** Any code path calling `ExceptionFilters.IsRecoverable(ex)` where `ex` is a `TypeInitializationException` or `AggregateException` with null `InnerException` hangs the calling thread permanently.

---

### Medium (7)

#### M1. `DispatchAsync` referenced in specs but does not exist in code

**Spec source (both incorrect):**
- CLAUDE.md: "All callers use Dispatch/DispatchAsync/DispatchErrorOr."
- persistence.md: "Rule WT1003 (compile-time error): LauncherStore.Save must only be called from AppStore.Dispatch, DispatchAsync, or DispatchErrorOr."

**Code reality:**
`src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs` has `Dispatch`, `DispatchErrorOr`, and `ReloadFromDisk` -- but NO `DispatchAsync` method exists anywhere in the codebase. There is no async dispatch path.

**Severity:** Two spec documents reference a method that was planned but never implemented or was removed. Developers following the spec will look for a non-existent method.

---

#### M2. `AppStore` referenced in persistence.md and mvvm-pattern.md but does not exist in code

**Spec source (persistence.md):**
- "AppStore already serializes per-instance access via its own SemaphoreSlim."
- "AppStore.Mutate captures before state before invoking transform."
- "AppStore handles rollback internally"

**Code reality:**
`AppStore` class does not exist. The code uses `StoreGrain` (`WTLauncher.App.Infrastructure.StoreGrain`) which extends `Grain<LauncherStoreFile>`. The entire `AppStore` abstraction was replaced by `StoreGrain` but persistence.md and mvvm-pattern.md were never updated.

mvvm-pattern.md Section "AppStore mutation with automatic rollback" describes `AppStore.Mutate(Func<LauncherStoreFile, LauncherStoreFile> transform)` which no longer exists. All callers use `StoreGrain.Dispatch`.

---

#### M3. `LauncherStore.ExpectedStoreSchemaVersion` referenced in persistence.md but does not exist

**Spec source (persistence.md API table):**
```csharp
public static class LauncherStore
{
    public const int ExpectedStoreSchemaVersion = 2;
    ...
}
```

**Code reality:**
`src/avalonia-launcher/src/WTLauncher.Core/LauncherStore.cs` does NOT have `ExpectedStoreSchemaVersion`. The schema version constant lives in `LauncherStoreValidator.cs:13` as:
```csharp
public const int ExpectedSchemaVersion = 2;
```
With a companion `MinimumSupportedSchemaVersion = 1` that is not documented in persistence.md.

---

#### M4. persistence.md `LauncherStore` API surface does not match current code

**Differences between persistence.md and LauncherStore.cs:**
1. `ExpectedStoreSchemaVersion` is documented in spec but is in `LauncherStoreValidator` instead
2. `RepositoryMarkerFileName` is in code but undocumented in spec
3. `DefaultPersistence` delegate injection pattern is undocumented
4. `LoadErrorOr()` method is in code but undocumented in spec
5. `MigrateFromLegacyFiles()` is documented in spec API table but no longer exists on `LauncherStore` (lives in `LegacyMigration.cs` instead)
6. `Load(string storePath, Action<Exception, string>? onRecovery)` has an extra `onRecovery` parameter not in spec

---

#### M5. mvvm-pattern.md scenario describes old `AppStore.Mutate` + `_settings` pattern

**Spec source:** mvvm-pattern.md Section "AppStore mutation with automatic rollback":
```csharp
// ViewModel-level sync after AppStore rolls back
private void RestoreVmStateFromStore();
```

And Section "State Single-Sourcing Pattern":
```csharp
// Wrong: local field duplicates store state
private LauncherSettings _settings;
```
with comment: "**Transition state:** `_settings` field temporarily retained as working copy."

**Code reality:**
`MainWindowViewModel.cs:193` shows: `// _settings field eliminated. All state reads use CurrentSettings (StoreGrain.Select).`
The `_settings` field has been fully eliminated. The spec still describes it as transition state.

---

#### M6. `StoreGrain.ReloadFromDisk` loads file outside lock without subscriber notification

**Code:**
`StoreGrain.cs:52-56`:
```csharp
public void ReloadFromDisk()
{
    var fresh = _persistence.Load(_storePath);  // outside lock
    SetState(fresh);                             // inside lock
    // NO StateChanged notification!
}
```

`_persistence.Load` is called OUTSIDE the `Grain._lock`. Between load and `SetState`, a concurrent `Dispatch` could mutate state -- the loaded state overwrites the concurrent mutation. Additionally, no `StateChanged?.Invoke()` after the reload, so subscribers (ViewModel projections) are never notified.

**Spec source:** The thread-safety contract implied by `Grain` requires atomic state access within the lock.

---

#### M7. `StoreGrain.StateChanged` fires outside the Grain lock

**Spec source:** quality-guidelines.md "Concurrency: StoreGrain internal SemaphoreSlim serializes state mutations" -- implies atomic state operations.

**Code:**
`StoreGrain.cs:58-71`:
```csharp
public void Dispatch(...)
{
    var after = Mutate(state => { ... });  // Mutate acquires and releases lock
    try { StateChanged?.Invoke(after); }   // outside lock!
    ...
}
```

A concurrent `Dispatch` could mutate state between `Mutate` returning and `StateChanged` being invoked. Subscribers that re-query `Select()` from their callback see the NEXT state, not the just-changed state.

---

### Low (6)

#### L1. README schema does not document `launchCount` field

`LaunchEntry.cs:50-51` has `[JsonPropertyName("launchCount")] public int LaunchCount { get; init; }` and it is serialized in launcher.json, but the README "Fields:" section does not list it.

#### L2. README schema does not document `backgroundMode` field

`LauncherSettings.cs:46-47` has `[JsonPropertyName("backgroundMode")] public string BackgroundMode { get; init; } = "solid";` and it is serialized, but the README "Additional settings fields:" section does not list it.

#### L3. `LauncherDesignTokens.NameTileGap` and `NameTileRowGap` re-declared as `const` in LauncherEntryLayoutCalculator

`LauncherEntryLayoutCalculator.cs:21,25` have:
```csharp
public const double NameTileGap = LauncherDesignTokens.NameTileGap;
public const double NameTileRowGap = LauncherDesignTokens.NameTileRowGap;
```

While these reference the original tokens, they create a second reference point that could diverge if someone updates `LauncherDesignTokens` but not the calculator's re-export. The spec says "All design constants live in LauncherDesignTokens -- single source of truth."

#### L4. `LauncherJsonContext` registers `List<LaunchEntry>` but actual types use `ImmutableList`

`LauncherJsonContext.cs:15` has `[JsonSerializable(typeof(List<LaunchEntry>))]` but `LauncherStoreFile.Entries` is `ImmutableList<LaunchEntry>`. The spec says "All generic types used (e.g., List<LaunchEntry>) must have explicit [JsonSerializable] attributes" -- the exact type used should be `ImmutableList<LaunchEntry>`, not `List<LaunchEntry>`.

Similarly, `List<LauncherPreset>` is registered but the actual type is `ImmutableList<LauncherPreset>`.

#### L5. `LauncherSettings.cardsGroupKey` is not `[JsonRequired]` but code depends on valid values

`LauncherSettings.cs:28`:
```csharp
[JsonPropertyName("cardsGroupKey")]
public string CardsGroupKey { get; init; } = LauncherCardsGroupKeys.None;
```

The spec says "`[JsonRequired]` on all non-optional fields." This field lacks `[JsonRequired]`. While it has a reasonable default, the code in LayoutManager depends on it having valid grouping values -- an invalid value from manual JSON editing would not fail fast.

#### L6. `LauncherLaunchWorkflow.StartBatch(entries)` does not pass CancellationToken to PreflightBatch

`LauncherLaunchWorkflow.cs:43-46`:
```csharp
public LauncherBatchLaunchReport StartBatch(
    IEnumerable<LaunchEntry> entries, bool requireConfirmation) =>
    StartBatch(PreflightBatch(entries, requireConfirmation));
```

The overload with `CancellationToken` is only applied to `StartBatch(LauncherBatchLaunchPreflight)`. The `entries`-accepting overload has no cancellation support at all -- neither during preflight (plan building) nor during the batch start.

---

## Rules Verified as Clean

The following documented rules were checked and found to have NO violations:

| Rule | Status |
|------|--------|
| No bare `catch (Exception)` without `when` filter | CLEAN -- all 54+ catch blocks have proper filters |
| No per-entry `confirm` key in launcher.json | CLEAN -- only global `requireConfirmationForLaunches` present |
| No duplicate projection fields (`_isCardsViewMode` etc.) | CLEAN -- all projections computed from single writable owner |
| No hardcoded hex colors in .axaml files | CLEAN -- all colors use `{StaticResource}` |
| `File.WriteAllText` only for temp files in correct save pattern | CLEAN -- only `FileLauncherJsonIO.cs:122` (temp write) and `App.axaml.cs:134` (smoke test) |
| `_settings` field eliminated from MainWindowViewModel | CLEAN -- confirmed eliminated, all reads via `CurrentSettings` |
| Drag state discard before Refresh | CLEAN -- `Refresh()` calls `DiscardDragReorderPreview()` first |
| Four-box control row in single Grid row (DESIGN.md P6) | CLEAN -- `LauncherControlBar.axaml` `ControlBoxesGrid` uses one Grid row |
| `[JsonIgnore]` not used on SchemaVersion | CLEAN -- no forbidden pattern found |
| No `TranslateTransform` usage | CLEAN |
| 32767-char command line validation | CLEAN -- `LaunchArgumentBuilder.cs:74-78` validates before Process.Start |
| `catch` blocks in finally have their own try/catch | CLEAN |
| Exception message sanitization via `ExceptionMessageFormatter.ForUser` | CLEAN |
| Design tokens all have consumers | CLEAN -- verified all tokens are referenced in App.axaml.cs or code-behind |
| No opaque internal marker comments (`// H4`, etc.) | CLEAN -- all markers carry descriptive comments |
