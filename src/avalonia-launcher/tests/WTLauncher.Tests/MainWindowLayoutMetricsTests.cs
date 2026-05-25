using Avalonia;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowLayoutMetricsTests
{
    [Fact]
    public void CalculateCardsAvailableSizeSubtractsStableWindowChrome()
    {
        var size = MainWindowLayoutMetrics.CalculateCardsAvailableSize(new Size(900, 520));

        Assert.Equal(new Size(852, 250), size);
    }

    [Fact]
    public void CalculateCardsAvailableSizeClampsToReadableMinimums()
    {
        var size = MainWindowLayoutMetrics.CalculateCardsAvailableSize(new Size(120, 120));

        Assert.Equal(new Size(220, 140), size);
    }
}
