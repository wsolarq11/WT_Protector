# Research: Thread Safety

- **Query**: Check all .cs files for thread safety patterns: volatile, lock usage, no lock(this) or lock(typeof(T))
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Volatile on Cross-Thread Flags

The codebase correctly uses `volatile` on a boolean flag read by multiple threads:

**V: MemoizedSelector.cs line 11**
```csharp
private volatile bool _hasCached;
```
This is mentioned in the project spec as a required pattern (`MemoizedSelector._hasCached is volatile`). Correct.

No other flags require `volatile` — other cross-thread state is protected by locks or `SemaphoreSlim`.

**No violations found.**

### lock Statement Usage

The codebase uses `lock` on private instance-level `object` fields:

| File | Lock Object | Purpose |
|---|---|---|
| Grain.cs:14 | `private readonly object _lock = new()` | State read/write serialization |
| ViewModelBase.cs:14 | `private readonly object _lock = new()` | _disposables thread safety |

Both use `private readonly object _lock = new()` — these are correctly scoped to the instance and are not shared or externally accessible.

### V: Property Getter with Lock Inside Body (Code Style)

**Grain.cs line 27**
```csharp
protected TState State
{
    get { lock (_lock) { return _state; } }
}
```
This is functionally correct (the lock protects concurrent reads) but has readability concerns:
- The property getter contains `{ lock (_lock) { return _state; } }` — braces inside braces.
- The explicit property syntax with inline lock is necessary because auto-properties cannot use locks, so this is an acceptable pattern.
- The lock object `_lock` is private to the `Grain<TState>` class.

**No violation** — this is the standard pattern for thread-safe lock-protected properties.

### V: lock in Expression-Bodied Members

**Grain.cs lines 33, 39**
```csharp
protected T ReadState<T>(Func<TState, T> selector)
{
    lock (_lock) { return selector(_state); }
}

protected void SetState(TState newState)
{
    lock (_lock) { _state = newState; }
}
```
These are short methods using inline lock expressions. While some style guides prefer the `lock` body on separate lines, this pattern is correct and concise for simple lock-then-return patterns.

**No violation.**

### No `lock(this)` or `lock(typeof(T))`

A search across all src/ .cs files for `lock(this` and `lock(typeof(` returned **no matches**. The codebase correctly avoids these dangerous patterns.

### SemaphoreSlim for Async Synchronization

**FileLauncherJsonIO.cs line 17**
```csharp
private readonly SemaphoreSlim _saveLock = new(1, 1);
```
`SemaphoreSlim` is correctly used for async-compatible synchronization (save path mixes async and sync operations). This is the recommended pattern over `lock()` when async code paths are involved, as `lock` cannot be used with `await`.

**No violations found.**

### No Global Bulk-Suppression Flags

The project spec specifically notes "No global bulk-suppression flag" for selection state. The codebase uses `ReplaceEntrySelectionFromOwner` (LauncherEntryViewModel.cs:116) for batch operations instead of a global flag. This is correct.

**No violations found.**

## Summary

| Category | Violations |
|---|---|
| volatile on cross-thread flags | 0 violations (MemoziedSelector correct) |
| lock(this) or lock(typeof(T)) | 0 violations |
| Private lock objects | 0 violations |
| SemaphoreSlim usage | 0 violations (correct in FileLauncherJsonIO) |
