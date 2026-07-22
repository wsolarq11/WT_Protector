using System.Runtime.InteropServices;
using ErrorOr;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>Single source of truth for launcher state. Exposes read-only
/// projections via <see cref="Select{T}"/>, mutations via <see cref="Mutate"/>
/// / <see cref="MutateAsync"/>, and notifies subscribers on every committed
/// change. Rollback on failure is automatic inside Mutate — the caller never
/// sees a half-persisted state.</summary>
public sealed class AppStore
{
    private readonly string _storePath;
    private LauncherStoreFile _state;
    private readonly Action<Exception, string>? _onRecovery;
    private event Action<LauncherStoreFile>? _stateChanged;

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

    public T Select<T>(Func<LauncherStoreFile, T> selector) => selector(_state);

    public void Mutate(Func<LauncherStoreFile, LauncherStoreFile> transform)
    {
        var before = _state;
        try
        {
            _state = transform(_state);
            LauncherStore.Save(_state, _storePath, backup: true);
            _stateChanged?.Invoke(_state);
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.Mutate rolled back");
            _onRecovery?.Invoke(ex, "AppStore.Mutate rolled back");
            throw;
        }
    }

    public async Task MutateAsync(Func<LauncherStoreFile, Task<LauncherStoreFile>> transform)
    {
        var before = _state;
        try
        {
            _state = await transform(_state).ConfigureAwait(true);
            LauncherStore.Save(_state, _storePath, backup: true);
            _stateChanged?.Invoke(_state);
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.MutateAsync rolled back");
            _onRecovery?.Invoke(ex, "AppStore.MutateAsync rolled back");
            throw;
        }
    }

    /// <summary>Mutate state with ErrorOr return, rolling back on recoverable failures.</summary>
    public ErrorOr<Success> MutateErrorOr(Func<LauncherStoreFile, LauncherStoreFile> transform)
    {
        var before = _state;
        try
        {
            _state = transform(_state);
            LauncherStore.Save(_state, _storePath, backup: true);
            _stateChanged?.Invoke(_state);
            return Result.Success;
        }
        catch (Exception ex) when (IsRecoverable(ex))
        {
            _state = before;
            ErrorLog.Write(ex, "AppStore.MutateErrorOr rolled back");
            _onRecovery?.Invoke(ex, "AppStore.MutateErrorOr rolled back");
            return LauncherErrors.ConfigSaveFailed(ex.Message);
        }
    }

    public IDisposable Subscribe(Action<LauncherStoreFile> onChanged)
    {
        _stateChanged += onChanged;
        return new Subscription(() => _stateChanged -= onChanged);
    }

    /// <summary>ponytail: inlined recoverability check — matches
    /// ExceptionFilters.IsRecoverable without the cross-layer dependency.
    /// Upgrade to the shared utility if it moves to Infrastructure.</summary>
    private static bool IsRecoverable(Exception ex)
    {
        // Unwrap TypeInitializationException and AggregateException
        for (var i = 0; i < 16 && ex is TypeInitializationException or AggregateException; i++)
        {
            if (ex.InnerException is null) break;
            ex = ex.InnerException;
            if (ex is AggregateException agg)
                ex = agg.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
        }
        return ex is not (OutOfMemoryException or StackOverflowException
            or AccessViolationException or SEHException);
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
