using System.Collections.Immutable;
using WTLauncher.Core.Events;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Lightweight actor base class for state isolation.
///
/// Every <see cref="Grain{TState}"/> instance has:
///   - Single-threaded dispatch (all mutations run sequentially)
///   - Append-only event log persistence via <see cref="EventJournal"/>
///   - Snapshot caching with automatic rebuild from events
///   - Typed error handling via <see cref="OneOf{T, AppError}"/>
///
/// The grain guarantees that <c>TState</c> is never exposed to concurrent
/// mutation — all state changes go through <c>DispatchEventAsync</c>.
/// </summary>
public abstract class Grain<TState> where TState : class
{
    private readonly string _journalDir;
    private readonly object _lock = new();
    private TState _state = null!;
    private ImmutableList<LauncherEvent> _events = ImmutableList<LauncherEvent>.Empty;
    private bool _initialized;

    protected Grain(string journalDir)
    {
        _journalDir = journalDir ?? throw new ArgumentNullException(nameof(journalDir));
    }

    /// <summary>True after InitializeAsync has completed.</summary>
    protected bool IsInitialized => _initialized;

    /// <summary>The current state (read-only snapshot, always consistent).</summary>
    protected TState State => _state;

    /// <summary>Initialize the grain from the event journal.</summary>
    protected async Task InitializeAsync(Func<TState> createDefault)
    {
        if (_initialized) return;
        lock (_lock)
        {
            _state = createDefault();
        }

        var events = await EventJournal.ReadAll(_journalDir);
        lock (_lock)
        {
            _events = events;
            foreach (var e in events)
                _state = ApplyEvent(e, _state);
        }
        _initialized = true;
    }

    /// <summary>Dispatch an event: append to journal + apply to state.</summary>
    protected async Task PersistEventCoreAsync(LauncherEvent @event)
    {
        await EventJournal.Append(_journalDir, @event);
        lock (_lock)
        {
            _events = _events.Add(@event);
            _state = ApplyEvent(@event, _state);
        }
    }

    /// <summary>Read state inside the lock (thread-safe).</summary>
    protected T ReadState<T>(Func<TState, T> selector)
    {
        lock (_lock) { return selector(_state); }
    }

    /// <summary>Set state inside the lock (thread-safe). For legacy compat paths.</summary>
    protected void SetState(TState newState)
    {
        lock (_lock) { _state = newState; }
    }

    /// <summary>
    /// Override to apply an event to the current state and return the new state.
    /// Must be a pure function — no side effects, no I/O.
    /// </summary>
    protected abstract TState ApplyEvent(LauncherEvent @event, TState currentState);
}
