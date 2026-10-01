using System.Diagnostics;
using ErrorOr;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Thread-safe store grain. Owns in-memory <see cref="LauncherStoreFile"/> state
/// and persists all mutations through <see cref="Dispatch"/> (pure reducer), which
/// saves to disk before applying to memory. On save failure, the in-memory state
/// is NOT updated — the caller sees the exception.
/// </summary>
public sealed class StoreGrain : IDisposable
{
    private readonly object _lock = new();
    private LauncherStoreFile _state = null!;
    private readonly string _storePath;
    private readonly IStorePersistence _persistence;

    /// <summary>Fires when state changes.</summary>
    public event Action<LauncherStoreFile>? StateChanged;

    /// <summary>Fires after ReloadFromDisk replaces the in-memory state.</summary>
    public event Action<LauncherStoreFile>? Reloaded;

    public string StorePath => _storePath;

    /// <summary>Create a store grain backed by the given path.
    /// State is loaded synchronously from disk via <paramref name="persistence"/>.</summary>
    public StoreGrain(string storePath, IStorePersistence persistence)
    {
        _storePath = storePath;
        _persistence = persistence;
        _state = LoadInitialState(storePath, persistence);
    }

    /// <summary>Test-only: creates a store with the given initial state.
    /// No file I/O — state starts from <paramref name="initialState"/>.
    /// Falls back to <see cref="FileStorePersistence"/> when <paramref name="persistence"/> is null.</summary>
    internal StoreGrain(string storePath, LauncherStoreFile initialState, IStorePersistence? persistence = null)
    {
        _storePath = storePath;
        _persistence = persistence ?? new FileStorePersistence();
        _state = initialState;
    }

    /// <summary>Read a projection from current state (thread-safe).</summary>
    public T Select<T>(Func<LauncherStoreFile, T> selector)
    {
        lock (_lock) { return selector(_state); }
    }

    /// <summary>Reload state from disk via persistence. Does not notify subscribers.
    /// Used by <see cref="MainWindowViewModel.Refresh"/> to synchronize with external changes.
    /// Load and memory update are serialized under the same lock as Dispatch's
    /// save + memory update, eliminating the reload/dispatch TOCTOU window.</summary>
    public void ReloadFromDisk()
    {
        LauncherStoreFile fresh;
        lock (_lock)
        {
            fresh = _persistence.Load(_storePath);
            _state = fresh;
        }
        try { Reloaded?.Invoke(fresh); }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "StoreGrain.Reloaded");
        }
    }

    /// <summary>Apply a reducer and persist. On save failure, state is unchanged.
    /// Serializes save + memory update under a single lock to eliminate the TOCTOU window.
    /// Subscriber exceptions are caught and logged to prevent Dispatch failure cascades.</summary>
    public void Dispatch(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        LauncherStoreFile after;
        lock (_lock)
        {
            var newState = reducer(_state);
            _persistence.Save(newState, _storePath, backup: true);
            _state = newState;
            after = newState;
        }
        try { StateChanged?.Invoke(after); }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "StoreGrain.StateChanged");
        }
    }

    /// <summary>Dispatch with ErrorOr return. Catches all recoverable save failures.</summary>
    public ErrorOr<Success> DispatchErrorOr(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        try { Dispatch(reducer); return Result.Success; }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
        { return LauncherErrors.ConfigSaveFailed(ex.Message); }
    }

    /// <summary>Subscribe to state changes.</summary>
    public IDisposable Subscribe(Action<LauncherStoreFile> onChanged)
    {
        StateChanged += onChanged;
        return new Subscription(() => StateChanged -= onChanged);
    }

    public void Dispose()
    {
        if (_persistence is IDisposable disposable) disposable.Dispose();
    }

    private static LauncherStoreFile LoadInitialState(string storePath, IStorePersistence persistence)
    {
        try
        {
            return File.Exists(storePath)
                ? persistence.Load(storePath)
                : new LauncherStoreFile();
        }
        catch (IOException ex) when (ex is not FileNotFoundException)
        {
            // Config exists but can't be read — log and return empty for graceful fallback.
            Debug.WriteLine($"[StoreGrain] LoadInitialState IO error: {ex.Message}");
            Trace.WriteLine($"[StoreGrain] LoadInitialState IO error: {ex.Message}");
            return new LauncherStoreFile();
        }
        catch (FileNotFoundException)
        {
            // Fresh install — no config file yet; return empty state silently.
            return new LauncherStoreFile();
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
