using Avalonia;

namespace WTLauncher.App.Views;

public static class MainWindowDragInsertDecision
{
    public static bool ShouldInsertAfter(Point positionInCard, Size cardSize, bool useVerticalInsertPreview)
    {
        return useVerticalInsertPreview
            ? positionInCard.Y >= (cardSize.Height * 0.5)
            : positionInCard.X >= (cardSize.Width * 0.5);
    }
}
