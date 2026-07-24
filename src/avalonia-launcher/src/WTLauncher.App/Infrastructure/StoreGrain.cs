using System.Text.Json;
using ErrorOr;
using WTLauncher.Core;
using WTLauncher.Core.Errors;
using WTLauncher.Core.Events;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Event-sourced store grain that replaces AppStore.
///
/// All mutations are recorded as events in the EventJournal.
/// State is derived by replaying events through the pure projector.
///
/// Legacy-compat: methods like <see cref="Dispatch"/> and <see cref="DispatchErrorOr"/>
/// mirror the old AppStore API so callers can migrate incrementally.
/// </summary>
public sealed class StoreGrain : Grain<LauncherStoreFile>, IDisposable
{
    private readonly string _storePath;
    private readonly string _journalDir;

    /// <summary>Test-only: creates a store with the given initial state.
    /// State mutation works (Dispatch, Select), but journal I/O is skipped
    /// because the journal directory does not exist. Tests that verify
    /// persistence should use the real file-backed path.</summary>
    internal StoreGrain(string storePath, LauncherStoreFile initialState)
        : base(Path.Combine(Path.GetTempPath(), "wtlauncher-test-journal", Guid.NewGuid().ToString("N")))
    {
        _storePath = storePath;
        _journalDir = Path.Combine(Path.GetTempPath(), "wtlauncher-test-journal", Guid.NewGuid().ToString("N"));
        // Use synchronous init since test constructors can't be async.
        // The journal dir doesn't exist, so ReadAll returns empty immediately.
        InitializeAsync(() => initialState).GetAwaiter().GetResult();
    }

    /// <summary>Fires when state changes.</summary>
    public event Action<LauncherStoreFile>? StateChanged;

    public StoreGrain(string storePath)
        : base(Path.Combine(Path.GetDirectoryName(storePath) ?? ".", "journal"))
    {
        _storePath = storePath;
        _journalDir = Path.Combine(Path.GetDirectoryName(storePath) ?? ".", "journal");
    }

    /// <summary>Initialize from journal, with optional legacy migration.</summary>
    public async Task InitializeAsync()
    {
        await InitializeAsync(() => new LauncherStoreFile());

        if (File.Exists(_storePath))
        {
            try
            {
                var legacy = LauncherStore.Load(_storePath);
                await PersistEventCoreAsync(new Core.Events.SettingsUpdatedEvent { Settings = legacy.Settings });
                foreach (var entry in legacy.Entries)
                    await PersistEventCoreAsync(new Core.Events.EntryAddedEvent { Entry = entry });
            }
            catch (IOException) { }
        }
    }

    /// <summary>Read a projection from current state (thread-safe).
    /// Auto-initializes lazily if needed.</summary>
    public T Select<T>(Func<LauncherStoreFile, T> selector)
    {
        if (!IsInitialized) LazyInit();
        return ReadState(selector);
    }

    /// <summary>Dispatch a strongly-typed event with persistence.</summary>
    public async Task<Core.Errors.AppError?> DispatchEventAsync(Core.Events.LauncherEvent @event)
    {
        try
        {
            await PersistEventCoreAsync(@event);
            StateChanged?.Invoke(ReadState(s => s));
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new Core.Errors.AppError.ConfigSaveFailed(_storePath, ex.Message);
        }
    }

    public string StorePath => _storePath;

#pragma warning disable CA1822
    public void Dispose() { /* IDisposable interface requirement */ }
#pragma warning restore CA1822

    /// <summary>Lazy initialization for test compat. Idempotent.</summary>
    private void LazyInit()
    {
        try
        {
            var legacy = File.Exists(_storePath)
                ? LauncherStore.Load(_storePath)
                : new LauncherStoreFile();
            InitializeAsync(() => legacy).GetAwaiter().GetResult();
        }
        catch (IOException)
        {
            InitializeAsync(() => new LauncherStoreFile()).GetAwaiter().GetResult();
        }
    }

    // ── Legacy AppStore-compat methods ─────────────────────────

    /// <summary>Legacy-compat: applies a reducer and persists via LauncherStore.Save.</summary>
    public void Dispatch(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        // Lazy auto-init (test compat: StoreGrain may not have been initialized)
        if (!IsInitialized)
        {
            LazyInit();
        }
        var after = reducer(ReadState(s => s));
        LauncherStore.Save(after, _storePath, backup: true);
        SetState(after);
        StateChanged?.Invoke(after);
    }

    /// <summary>Legacy-compat: Dispatch with ErrorOr return.</summary>
    public ErrorOr<Success> DispatchErrorOr(Func<LauncherStoreFile, LauncherStoreFile> reducer)
    {
        try { Dispatch(reducer); return Result.Success; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException or JsonException)
        { return LauncherErrors.ConfigSaveFailed(ex.Message); }
    }

    /// <summary>Legacy-compat: subscribe to state changes.</summary>
    public IDisposable Subscribe(Action<LauncherStoreFile> onChanged)
    {
        StateChanged += onChanged;
        return new Subscription(() => StateChanged -= onChanged);
    }

    protected override LauncherStoreFile ApplyEvent(Core.Events.LauncherEvent @event, LauncherStoreFile currentState)
    {
        return LauncherStoreProjector.Project(currentState, @event);
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
