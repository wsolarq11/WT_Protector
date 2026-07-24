using Avalonia.Threading;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Bridges event-sourced state changes to the UI thread.
///
/// Subscribes to <see cref="StoreGrain.StateChanged"/> and invokes the
/// callback on the Avalonia UI thread via <see cref="Dispatcher.UIThread"/>.
///
/// This eliminates the need for manual `Dispatcher.Invoke` calls when
/// responding to state changes from background-persisted events.
/// </summary>
public sealed class UiStateProjection : IDisposable
{
    private readonly StoreGrain _grain;
    private readonly Action<LauncherStoreFile> _onChanged;
    private bool _disposed;

    public UiStateProjection(StoreGrain grain, Action<LauncherStoreFile> onChanged)
    {
        _grain = grain ?? throw new ArgumentNullException(nameof(grain));
        _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        _grain.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(LauncherStoreFile state)
    {
        if (_disposed) return;

        // Always dispatch to UI thread — Avalonia ObservableCollection
        // and ObservableObject mutations must happen on the UI thread.
        if (Dispatcher.UIThread.CheckAccess())
        {
            _onChanged(state);
        }
        else
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_disposed) _onChanged(state);
            });
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _grain.StateChanged -= OnStateChanged;
    }
}
