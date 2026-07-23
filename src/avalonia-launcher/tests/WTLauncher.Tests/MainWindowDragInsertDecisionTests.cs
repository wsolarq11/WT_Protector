using Avalonia;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowDragInsertDecisionTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(49, false)]
    [InlineData(50, true)]
    [InlineData(51, true)]
    [InlineData(100, true)]
    public void ShouldInsertAfterUsesHorizontalMidpointForCardAndNameModes(double x, bool expected)
    {
        var result = MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(x, 0),
            new Size(100, 40),
            isDetailedListMode: false);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(21, true)]
    [InlineData(40, true)]
    public void ShouldInsertAfterUsesVerticalMidpointForDetailedListMode(double y, bool expected)
    {
        var result = MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(0, y),
            new Size(100, 40),
            isDetailedListMode: true);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 50, true)]
    [InlineData(null, 49, false)]
    [InlineData(true, 45, true)]
    [InlineData(true, 44, false)]
    [InlineData(false, 56, true)]
    [InlineData(false, 54, false)]
    public void ShouldInsertAfterRespectsHysteresisThreshold(bool? currentInsertAfter, double x, bool expected)
    {
        var result = MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(x, 0),
            new Size(100, 40),
            isDetailedListMode: false,
            currentInsertAfter: currentInsertAfter);

        Assert.Equal(expected, result);
    }
}
