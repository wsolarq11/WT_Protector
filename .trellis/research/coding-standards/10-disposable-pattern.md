# Research: Disposable Pattern

- **Query**: Check all .cs files for correct IDisposable implementation, Dispose(bool) pattern, SuppressFinalize usage
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### IDisposable Implementations

Three classes implement `IDisposable`:

**1. ViewModelBase.cs (line 12-43)**
```csharp
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly object _lock = new();
    private List<IDisposable> _disposables = [];
    private bool _disposed;

    public void Dispose()
    {
        List<IDisposable> toDispose;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            toDispose = _disposables;
            _disposables = [];
        }
        for (var i = toDispose.Count - 1; i >= 0; i--)
            toDispose[i].Dispose();
        GC.SuppressFinalize(this);
    }
}
```

Pattern analysis:
- Uses simple `Dispose()` method (no `Dispose(bool)` pattern)
- This is acceptable because `ViewModelBase` is `abstract` and has no unmanaged resources directly. Derived classes add managed disposables only.
- `_disposed` guard correctly prevents double-dispose
- `GC.SuppressFinalize(this)` called — correct (though no finalizer exists, it is standard practice)
- Thread-safe with `lock (_lock)` for disposables collection access
- **No violations found**

**2. DisposableAction.cs (line 4-12)**
```csharp
public sealed class DisposableAction(Action dispose) : IDisposable
{
    private Action? _dispose = dispose;
    public void Dispose()
    {
        Interlocked.Exchange(ref _dispose, null)?.Invoke();
        GC.SuppressFinalize(this);
    }
}
```

Pattern analysis:
- Simple `Dispose()` method — acceptable for this lightweight wrapper
- `Interlocked.Exchange` for thread-safe null check — correct
- `GC.SuppressFinalize(this)` called — correct
- **No violations found**

**3. EntryProjection.cs (line 15-97)**
```csharp
public sealed class EntryProjection : IDisposable
{
    // ...
    private readonly IDisposable _cleanup;
    public void Dispose() => _cleanup.Dispose();
}
```

Pattern analysis:
- Simple `Dispose()` method — acceptable since it owns only managed resources
- Does **NOT** call `GC.SuppressFinalize(this)`. Since the class has no finalizer, this is not a bug, but it is inconsistent with the other IDisposable implementations which do call `SuppressFinalize`.

**V: EntryProjection.cs line 96 — Missing `GC.SuppressFinalize(this)`**
```csharp
public void Dispose() => _cleanup.Dispose();
```
Should be:
```csharp
public void Dispose() { _cleanup.Dispose(); GC.SuppressFinalize(this); }
```
This is an inconsistency: ViewModelBase and DisposableAction both call `SuppressFinalize`, but EntryProjection does not.

**4. FileLauncherJsonIO.cs (line 13-222)**
```csharp
public sealed class FileLauncherJsonIO : ILauncherJsonIO, IDisposable
{
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    public void Dispose() => _saveLock.Dispose();
}
```

Pattern analysis:
- Simple `Dispose()` method — correct for single-resource disposal
- Does **NOT** call `GC.SuppressFinalize(this)` — same issue as EntryProjection

**V: FileLauncherJsonIO.cs line 222 — Missing `GC.SuppressFinalize(this)`**
```csharp
public void Dispose() => _saveLock.Dispose();
```

**5. Subscription (inside StoreGrain.cs line 115-...)**

```csharp
private sealed class Subscription(Action unsubscribe) : IDisposable
{
    public void Dispose() => _unsubscribe();
}
```
- `Subscription` has no SuppressFinalize — same issue

### Dispose(bool) Pattern

**No class uses the Dispose(bool) pattern.** This is acceptable because:
- The project has no unmanaged resources
- All classes are `sealed` or `abstract` with no derived classes owning native handles
- The simpler `Dispose()` method is sufficient for managed-only disposal

**No violations found.**

### SuppressFinalize Consistency

Three IDisposable implementations call `GC.SuppressFinalize(this)`:
1. ViewModelBase.cs:41 — calls GC.SuppressFinalize(this)
2. DisposableAction.cs:10 — calls GC.SuppressFinalize(this)

Three IDisposable implementations do NOT call `GC.SuppressFinalize(this)`:
1. EntryProjection.cs:96 — missing
2. FileLauncherJsonIO.cs:222 — missing
3. StoreGrain.Subscription — missing

While `GC.SuppressFinalize()` is technically unnecessary when there is no finalizer, calling it is standard practice for all `IDisposable` implementations and creates consistency.

## Summary

| Category | Violations |
|---|---|
| Dispose(bool) pattern correctness | N/A (not used, correctly so) |
| Disposed guard (double-dispose prevention) | 0 violations |
| Missing GC.SuppressFinalize | 3 instances: EntryProjection.cs:96, FileLauncherJsonIO.cs:222, StoreGrain.Subscription |
| IDisposable implementation correctness | 0 violations |
