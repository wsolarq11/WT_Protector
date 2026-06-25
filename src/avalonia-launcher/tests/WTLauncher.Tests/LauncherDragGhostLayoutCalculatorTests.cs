using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherDragGhostLayoutCalculatorTests
{
    [Fact]
    public void Calculate_ClampsGhostToReadableMinimumAndTracksPointer()
    {
        var layout = LauncherDragGhostLayoutCalculator.Calculate(
            sourceWidth: 120,
            sourceHeight: 40,
            pointerX: 320,
            pointerY: 240);

        Assert.Equal(LauncherDragGhostLayoutCalculator.MinWidth, layout.Width);
        Assert.Equal(LauncherDragGhostLayoutCalculator.MinHeight, layout.Height);
        Assert.Equal(336, layout.Left);
        Assert.Equal(256, layout.Top);
    }

    [Fact]
    public void Calculate_PreservesLargerSourceSize()
    {
        var layout = LauncherDragGhostLayoutCalculator.Calculate(
            sourceWidth: 284,
            sourceHeight: 128,
            pointerX: 11,
            pointerY: 17);

        Assert.Equal(284, layout.Width);
        Assert.Equal(128, layout.Height);
        Assert.Equal(27, layout.Left);
        Assert.Equal(33, layout.Top);
    }

    [Fact]
    public void Calculate_ClampsNegativeContainerSize()
    {
        // Negative container sizes should be clamped to 0, not produce
        // unexpected ghost positions.
        var layout = LauncherDragGhostLayoutCalculator.Calculate(
            sourceWidth: 284,
            sourceHeight: 128,
            pointerX: 50,
            pointerY: 50,
            windowWidth: -100,
            windowHeight: -200);

        // Ghost dimensions should be preserved.
        Assert.Equal(284, layout.Width);
        Assert.Equal(128, layout.Height);
        // With container clamped to 0 and ghost > container, the clamp logic
        // will produce predictable positions (no NaN or wildly negative values).
        Assert.True(double.IsFinite(layout.Left));
        Assert.True(double.IsFinite(layout.Top));
    }
}
