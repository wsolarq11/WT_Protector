using System.Text.Json;
using ErrorOr;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Thread-safe store grain. Backed by <see cref="LauncherStore"/> for persistence.
///
/// All mutations go through <see cref="Dispatch"/> (pure reducer), which saves
/// to disk before applying to in-memory state. On save failure, the in-memory
/// state is NOT updated — the caller sees the exception.
/// </summary>
public sealed class StoreGrain : Grain<LauncherStoreFile>, IDisposable
{
    private readonly string _storePath;

    /// <summary>Fires when state changes.</summary>
    public event Action<LauncherStoreFile>? StateChanged;

    public string StorePath => _storePath;

    /// <summary>Create a store grain backed by the given path.
    /// State is loaded synchronously from disk.</summary>
    public StoreGrain(string storePath)
        : base(LoadInitialState(storePath))
    {
        _storePath = storePath;
    }

    /// <summary>Test-only: creates a store with the given initial state.
    /// No file I/O — state starts from <paramref name="initialState"/>.</summary>
    internal StoreGrain(string storePath, LauncherStoreFile initialState)
        : base(initialState)
    {
        _storePath = storePath;
    }

    /// <summary>Read a projection from current state (thread-safe).</summary>
    public T Select<T>(Func<LauncherStoreFile, T> selector) => ReadState(selector);

    /// <summary>Apply a reducer and persist. On save failure, state is unchanged.</summary>
    public void Dispatch(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        var current = ReadState(s => s);
        var after = reducer(current);
        LauncherStore.Save(after, _storePath, backup: true);
        SetState(after);
        StateChanged?.Invoke(after);
    }

    /// <summary>Dispatch with ErrorOr return. Catches recoverable save failures.</summary>
    public ErrorOr<Success> DispatchErrorOr(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        try { Dispatch(reducer); return Result.Success; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or JsonException)
        { return LauncherErrors.ConfigSaveFailed(ex.Message); }
    }

    /// <summary>Subscribe to state changes.</summary>
    public IDisposable Subscribe(Action<LauncherStoreFile> onChanged)
    {
        StateChanged += onChanged;
        return new Subscription(() => StateChanged -= onChanged);
    }

    public void Dispose() { }

    private static LauncherStoreFile LoadInitialState(string storePath)
    {
        try
        {
            return File.Exists(storePath)
                ? LauncherStore.Load(storePath)
                : new LauncherStoreFile();
        }
        catch (IOException)
        {
            return new LauncherStoreFile();
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
