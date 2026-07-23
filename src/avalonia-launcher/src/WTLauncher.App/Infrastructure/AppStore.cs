using System.Text.Json;
using System.Threading;
using ErrorOr;
using WTLauncher.Core;

// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace WTLauncher.App.Infrastructure;

/// <summary>Single source of truth for launcher state. Exposes read-only
/// projections via <see cref="Select{T}"/>, mutations via <see cref="Dispatch"/>
/// or the legacy <see cref="Mutate"/>/<see cref="MutateAsync"/> methods.
/// Thread-safe: all state mutations are protected by a SemaphoreSlim.
/// Rollback on failure is automatic — the caller never sees a half-persisted state.</summary>
public sealed class AppStore : IDisposable
{
    private readonly string _storePath;
    private LauncherStoreFile _state;
    private readonly Action<Exception, string>? _onRecovery;
    private event Action<LauncherStoreFile>? _stateChanged;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppStore(string storePath, Action<Exception, string>? onRecovery = null)
    {
        _storePath = storePath;
        _onRecovery = onRecovery;
        _state = LauncherStore.Load(storePath, onRecovery);
    }

    /// <summary>ponytail: test-only — creates a store with the given state
    /// without touching the filesystem. Prefer loading from real path in
    /// production code.</summary>
    internal AppStore(string storePath, LauncherStoreFile initialState)
    {
        _storePath = storePath;
        _state = initialState;
    }

    public string StorePath => _storePath;

    // ── Read ──────────────────────────────────────────────────────

    /// <summary>Project a read-only view of the current state.</summary>
    public T Select<T>(Func<LauncherStoreFile, T> selector) => selector(_state);

    /// <summary>Project a read-only view of the current state (async-safe).</summary>
    public async Task<T> SelectAsync<T>(Func<LauncherStoreFile, T> selector)
    {
        var before = _state;
        Action<LauncherStoreFile>? handler = null;
        LauncherStoreFile snapshot = default!;
        try
        {
            _state = transform(_state);
            LauncherStore.Save(_state, _storePath, backup: true);
            handler = _stateChanged;
            snapshot = _state;
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.Mutate rolled back");
            _onRecovery?.Invoke(ex, "AppStore.Mutate rolled back");
            throw;
        }
        handler?.Invoke(snapshot);
    }

    // ── Dispatch (Reducer pattern) ────────────────────────────────

    /// <summary>Apply a pure reducer function to the state, persist, and notify.
    /// Rollback on failure: the store is reverted to the pre-mutation state.</summary>
    public void Dispatch(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        Action<LauncherStoreFile>? handler = null;
        LauncherStoreFile snapshot = default!;

        _lock.Wait();
        var before = _state;
        Action<LauncherStoreFile>? handler = null;
        LauncherStoreFile snapshot = default!;
        try
        {
            _state = await transform(_state).ConfigureAwait(true);
            LauncherStore.Save(_state, _storePath, backup: true);
            handler = _stateChanged;
            snapshot = _state;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or JsonException)
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.Dispatch rolled back");
            _onRecovery?.Invoke(ex, "AppStore.Dispatch rolled back");
            throw;
        }
        handler?.Invoke(snapshot);
    }

    /// <summary>Async variant of Dispatch for reducers that need async operations.</summary>
    public async Task DispatchAsync(Func<LauncherStoreFile, Task<LauncherStoreFile>> reducer)
    {
        Action<LauncherStoreFile>? handler = null;
        LauncherStoreFile snapshot = default!;

        await _lock.WaitAsync().ConfigureAwait(false);
        var before = _state;
        Action<LauncherStoreFile>? handler = null;
        LauncherStoreFile snapshot = default!;
        try
        {
            _state = transform(_state);
            LauncherStore.Save(_state, _storePath, backup: true);
            handler = _stateChanged;
            snapshot = _state;
            return Result.Success;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or JsonException)
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.DispatchAsync rolled back");
            _onRecovery?.Invoke(ex, "AppStore.DispatchAsync rolled back");
            throw;
        }
        finally { _lock.Release(); }

        // Notify outside lock to prevent deadlock if subscriber calls Dispatch
        handler?.Invoke(snapshot);
    }

    /// <summary>Dispatch with ErrorOr return — allows the caller to handle
    /// persistence failures without try/catch.</summary>
    public ErrorOr<Success> DispatchErrorOr(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        Action<LauncherStoreFile>? handler = null;
        LauncherStoreFile snapshot = default!;

        _lock.Wait();
        var before = _state;
        try
        {
            var after = reducer(_state);
            LauncherStore.Save(after, _storePath, backup: true);
            _state = after;
            handler = _stateChanged;
            snapshot = _state;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or JsonException)
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.DispatchErrorOr rolled back");
            _onRecovery?.Invoke(ex, "AppStore.DispatchErrorOr rolled back");
            return LauncherErrors.ConfigSaveFailed(ex.Message);
        }
        finally
        {
            handler?.Invoke(snapshot);
        }
    }

    // ── Legacy Mutate (backward compatible) ────────────────────────

    public void Mutate(Func<LauncherStoreFile, LauncherStoreFile> transform) =>
        Dispatch(transform);

    public async Task MutateAsync(Func<LauncherStoreFile, Task<LauncherStoreFile>> transform) =>
        await DispatchAsync(transform).ConfigureAwait(false);

    public ErrorOr<Success> MutateErrorOr(Func<LauncherStoreFile, LauncherStoreFile> transform) =>
        DispatchErrorOr(transform);

    // ── Subscription ──────────────────────────────────────────────

    public IDisposable Subscribe(Action<LauncherStoreFile> onChanged)
    {
        _stateChanged += onChanged;
        return new Subscription(() => _stateChanged -= onChanged);
    }

    public void Dispose() => _lock.Dispose();

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
