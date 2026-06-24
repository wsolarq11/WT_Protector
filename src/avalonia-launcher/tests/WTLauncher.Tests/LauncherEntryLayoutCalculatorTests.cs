using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryLayoutCalculatorTests
{
    [Fact]
    public void Calculate_KeepsCardRatioNearGoldenRhythmWhenSpaceAllows()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(900, 520, 6);

        var actualRatio = layout.CardWidth / layout.CardHeight;
        // Epsilon 0.02 accounts for rounding from integer pixel arithmetic in
        // LauncherEntryLayoutCalculator. The calculator works in integral pixel
        // values (card width/height are rounded to integers), so the ratio cannot
        // exactly match the floating-point CardGoldenRatio constant. 0.02 is wide
        // enough to tolerate the worst case across all supported column counts
        // (1–6) and screen widths (800–2560) while still catching algorithm drift.
        Assert.InRange(
            actualRatio,
            LauncherEntryLayoutCalculator.CardGoldenRatio - 0.02,
            LauncherEntryLayoutCalculator.CardGoldenRatio + 0.02);
    }

    [Fact]
    public void Calculate_UsesReadableMinimumBeforeScrollFallback()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(892, 330, 19);

        Assert.True(layout.CardHeight >= LauncherEntryLayoutCalculator.CardMinHeight);
        var rows = (int)Math.Ceiling(19 / 4d);
        var usedHeight = (rows * layout.CardHeight) + ((rows - 1) * LauncherEntryLayoutCalculator.CardGap);
        Assert.True(usedHeight > 330.001);
    }

    [Fact]
    public void Calculate_FitsNameWallInMultipleColumns()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(892, 330, 19);

        Assert.Equal(LauncherEntryLayoutCalculator.NameTileHeight, layout.NameTileHeight);
        Assert.InRange(
            layout.NameTileWidth,
            LauncherEntryLayoutCalculator.NameTileMinWidth,
            LauncherEntryLayoutCalculator.NameTilePreferredMaxWidth);

        var columns = Math.Max(
            1,
            (int)Math.Floor(
                (892 + LauncherEntryLayoutCalculator.NameTileGap)
                / (layout.NameTileWidth + LauncherEntryLayoutCalculator.NameTileGap)));
        var rows = (int)Math.Ceiling(19 / (double)columns);
        var usedHeight = (rows * layout.NameTileHeight) + ((rows - 1) * LauncherEntryLayoutCalculator.NameTileRowGap);
        Assert.True(columns >= 4);
        Assert.True(usedHeight <= 330.001);
    }
}
