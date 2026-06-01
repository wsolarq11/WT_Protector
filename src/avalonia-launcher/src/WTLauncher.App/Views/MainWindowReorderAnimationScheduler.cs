using Avalonia;

namespace WTLauncher.App.Views;

public sealed class MainWindowReorderAnimationScheduler
{
    private Dictionary<string, Rect>? _pendingSnapshot;

    public bool Schedule(
        Dictionary<string, Rect> before,
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

        _pendingSnapshot = new Dictionary<string, Rect>(before, before.Comparer);
        unsubscribe(handler);
        subscribe(handler);
        return true;
    }

    public Dictionary<string, Rect>? Consume(EventHandler handler, Action<EventHandler> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(unsubscribe);

        unsubscribe(handler);
        var snapshot = _pendingSnapshot;
        _pendingSnapshot = null;
        return snapshot;
    }
}
