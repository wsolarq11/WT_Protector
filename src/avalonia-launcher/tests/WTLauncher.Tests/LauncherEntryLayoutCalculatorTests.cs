using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryLayoutCalculatorTests
{
    [Fact]
    public void Calculate_KeepsCardRatioNearGoldenRhythmWhenSpaceAllows()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(900, 520, 6);

        const double goldenRatio = 21d / 13d;
        var actualRatio = layout.CardWidth / layout.CardHeight;
        Assert.InRange(actualRatio, goldenRatio - 0.02, goldenRatio + 0.02);
    }

    [Fact]
    public void Calculate_UsesReadableMinimumBeforeScrollFallback()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(892, 330, 19);

        Assert.True(layout.CardHeight >= 96);
        var rows = (int)Math.Ceiling(19 / 4d);
        var usedHeight = (rows * layout.CardHeight) + ((rows - 1) * 8);
        Assert.True(usedHeight > 330.001);
    }

    [Fact]
    public void Calculate_FitsNameWallInMultipleColumns()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(892, 330, 19);

        Assert.Equal(24, layout.NameTileHeight);
        Assert.InRange(layout.NameTileWidth, 132, 220);

        const double gap = 6;
        const double rowGap = 4;
        var columns = Math.Max(1, (int)Math.Floor((892 + gap) / (layout.NameTileWidth + gap)));
        var rows = (int)Math.Ceiling(19 / (double)columns);
        var usedHeight = (rows * layout.NameTileHeight) + ((rows - 1) * rowGap);
        Assert.True(columns >= 4);
        Assert.True(usedHeight <= 330.001);
    }
}
