using Avalonia;

namespace WTLauncher.App.Views;

public static class MainWindowLayoutMetrics
{
    private const double HorizontalChromeWidth = 48;
    private const double VerticalChromeHeight = 270;
    private const double MinimumCardsWidth = 220;
    private const double MinimumCardsHeight = 140;

    public static Size CalculateCardsAvailableSize(Size windowSize)
    {
        return new Size(
            Math.Max(MinimumCardsWidth, windowSize.Width - HorizontalChromeWidth),
            Math.Max(MinimumCardsHeight, windowSize.Height - VerticalChromeHeight));
    }
}
