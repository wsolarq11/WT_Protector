using System.Collections.ObjectModel;
using Avalonia;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class MainWindowReorderAnimationScheduler
{
    private IReadOnlyDictionary<string, Rect>? _pendingSnapshot;

    public bool Schedule(
        IReadOnlyDictionary<string, Rect> before,
        EventHandler handler,
        Action<EventHandler> unsubscribe,
        Action<EventHandler> subscribe)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ArgumentNullException.ThrowIfNull(subscribe);

        if (before.Count == 0)
            return false;

        _pendingSnapshot = new ReadOnlyDictionary<string, Rect>(
            new Dictionary<string, Rect>(before, LauncherEntryIds.Comparer));
        unsubscribe(handler);
        subscribe(handler);
        return true;
    }

    public IReadOnlyDictionary<string, Rect>? Consume(EventHandler handler, Action<EventHandler> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(unsubscribe);

        unsubscribe(handler);
        var snapshot = _pendingSnapshot;
        _pendingSnapshot = null;
        return snapshot;
    }
}
