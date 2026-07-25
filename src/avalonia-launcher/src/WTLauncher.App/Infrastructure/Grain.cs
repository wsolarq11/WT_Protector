namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Lightweight actor base class for state isolation.
///
/// Every <see cref="Grain{TState}"/> instance guarantees single-threaded
/// state access via <c>ReadState</c> and <c>SetState</c> — all mutations
/// are serialized through a lock.
///
/// Usage: subclass and expose typed Dispatch/Select methods.
/// </summary>
public abstract class Grain<TState> where TState : class
{
    private readonly object _lock = new();
    private TState _state = null!;

    protected Grain() { }

    protected Grain(TState initialState)
    {
        _state = initialState;
    }

    /// <summary>The current state (read inside the lock).</summary>
    protected TState State
    {
        get { lock (_lock) { return _state; } }
    }

    /// <summary>Read state inside the lock (thread-safe).</summary>
    protected T ReadState<T>(Func<TState, T> selector)
    {
        lock (_lock) { return selector(_state); }
    }

    /// <summary>Set state inside the lock (thread-safe).</summary>
    protected void SetState(TState newState)
    {
        lock (_lock) { _state = newState; }
    }

    /// <summary>Apply a mutation inside the lock and return the new value.</summary>
    protected TState Mutate(Func<TState, TState> mutator)
    {
        lock (_lock)
        {
            _state = mutator(_state);
            return _state;
        }
    }
}
