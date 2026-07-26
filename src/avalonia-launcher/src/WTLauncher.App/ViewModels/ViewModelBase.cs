using CommunityToolkit.Mvvm.ComponentModel;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Base class for all ViewModels. Owns a collection of <see cref="IDisposable"/>
/// resources for deterministic cleanup of event subscriptions.
///
/// Subclasses call <see cref="AddDisposable"/> to register resources. On <see cref="Dispose"/>,
/// all registered disposables are released in reverse order. Safe to call multiple times.
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly object _lock = new();
    private List<IDisposable> _disposables = [];
    private bool _disposed;

    /// <summary>Register a disposable resource for cleanup on Dispose.</summary>
    protected void AddDisposable(IDisposable disposable)
    {
        lock (_lock)
        {
            if (_disposed) { disposable.Dispose(); return; }
            _disposables.Add(disposable);
        }
    }

    /// <summary>Dispose all registered resources in reverse order.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Standard Dispose(bool) pattern for unsealed classes.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            List<IDisposable> toDispose;
            lock (_lock)
            {
                toDispose = _disposables;
                _disposables = [];
                _disposed = true;
            }
            for (var i = toDispose.Count - 1; i >= 0; i--)
                toDispose[i].Dispose();
        }
    }
}
