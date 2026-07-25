namespace WTLauncher.App.Infrastructure;

/// <summary>Wraps an Action as an IDisposable. Runs the action on Dispose.</summary>
public sealed class DisposableAction(Action dispose) : IDisposable
{
    private Action? _dispose = dispose;
    public void Dispose()
    {
        Interlocked.Exchange(ref _dispose, null)?.Invoke();
        GC.SuppressFinalize(this);
    }
}
