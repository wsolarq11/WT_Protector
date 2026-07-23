namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Generic memoized selector — skips computation when input hasn't changed.
/// Replaces hand-written _lastApplied* guard patterns.
/// </summary>
public class MemoizedSelector<TInput, TOutput> where TInput : notnull
{
    private TInput _lastInput = default!;
    private TOutput _lastOutput = default!;
    private bool _hasCached;

    public TOutput Get(Func<TInput, TOutput> compute, TInput input)
    {
        if (_hasCached && EqualityComparer<TInput>.Default.Equals(_lastInput, input))
            return _lastOutput;

        _lastInput = input;
        _lastOutput = compute(input);
        _hasCached = true;
        return _lastOutput;
    }

    public void Invalidate()
    {
        _hasCached = false;
    }
}
