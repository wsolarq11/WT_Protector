using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowEntryCardBoundsCapture
{
    public static Dictionary<string, Rect> Capture(Control root, Visual relativeTo)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(relativeTo);

        return Capture(
            root.GetVisualDescendants().OfType<Border>(),
            card => card.TranslatePoint(new Point(0, 0), relativeTo),
            card => card.Bounds.Size);
    }

    public static Dictionary<string, Rect> Capture(
        IEnumerable<Border> cards,
        Func<Border, Point?> translateOrigin,
        Func<Border, Size> readSize)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(translateOrigin);
        ArgumentNullException.ThrowIfNull(readSize);

        var bounds = new Dictionary<string, Rect>(LauncherEntryIds.Comparer);
        foreach (var (entry, card) in MainWindowEntryCardResolver.ResolveCards(cards))
        {
            var origin = translateOrigin(card);
            if (origin is null)
                continue;

            bounds[entry.Id] = new Rect(origin.Value, readSize(card));
        }

        return bounds;
    }
}
