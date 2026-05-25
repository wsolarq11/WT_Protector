using Avalonia;

namespace WTLauncher.App.Views;

public static class MainWindowReorderFlip
{
    public const double MovementThreshold = 0.5;

    public static bool TryCalculateOffset(
        string entryId,
        IReadOnlyDictionary<string, Rect> before,
        IReadOnlyDictionary<string, Rect> after,
        out MainWindowReorderFlipOffset offset)
    {
        ArgumentNullException.ThrowIfNull(entryId);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        offset = default;
        if (!before.TryGetValue(entryId, out var oldRect))
            return false;
        if (!after.TryGetValue(entryId, out var newRect))
            return false;

        var deltaX = oldRect.X - newRect.X;
        var deltaY = oldRect.Y - newRect.Y;
        if (Math.Abs(deltaX) < MovementThreshold && Math.Abs(deltaY) < MovementThreshold)
            return false;

        offset = new MainWindowReorderFlipOffset(deltaX, deltaY);
        return true;
    }

    public static string FormatTranslate(MainWindowReorderFlipOffset offset)
    {
        return FormattableString.Invariant($"translate({offset.X:0.##}px,{offset.Y:0.##}px)");
    }
}

public readonly record struct MainWindowReorderFlipOffset(double X, double Y);
