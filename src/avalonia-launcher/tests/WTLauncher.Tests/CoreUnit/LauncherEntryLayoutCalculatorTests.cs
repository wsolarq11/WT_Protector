using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherEntryLayoutCalculatorTests
{
    // ── Legacy tests ───────────────────────────────────────────

    [Fact]
    public void Calculate_KeepsCardRatioNearGoldenRhythmWhenSpaceAllows()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(900, 520, 6);

        var actualRatio = layout.CardWidth / layout.CardHeight;
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

    // ── Responsive visual matrix ──────────────────────────────────
    // Verifies layout correctness across the three canonical window
    // widths: Min (940), Default (1180), Wide (1600).
    // Available width = window width - CardsRegionHorizontalChromeWidth (48).

    public static TheoryData<string, double, int, double, double> CardLayoutMatrix => new()
    {
        // Available widths: Min(892), Default(1132), Wide(1552).
        // 6 entries → prefers fewer larger columns; 20 entries → fills available columns.
        { "Min-6",  892, 6,   200,  96 },
        { "Min-20", 892, 20,  200,  96 },
        { "Default-6",  1132, 6,  200, 96 },
        { "Default-20", 1132, 20, 200, 96 },
        { "Wide-6",  1552, 6,  200,  96 },
        { "Wide-20", 1552, 20, 200, 96 },
    };

    [Theory, MemberData(nameof(CardLayoutMatrix))]
    public void Calculate_Cards_ResponsiveMatrix(
        string label, double availableWidth, int itemCount,
        double expectedMinCardWidth, double expectedMinCardHeight)
    {
        _ = label;
        var layout = LauncherEntryLayoutCalculator.Calculate(availableWidth, 600, itemCount);

        // Cards respect minimum dimensions
        Assert.True(layout.CardWidth >= expectedMinCardWidth,
            $"{label}: CardWidth {layout.CardWidth} < {expectedMinCardWidth}");
        Assert.True(layout.CardHeight >= expectedMinCardHeight,
            $"{label}: CardHeight {layout.CardHeight} < {expectedMinCardHeight}");

        // Cards fill available width without overflow
        var gap = LauncherEntryLayoutCalculator.CardGap;
        var columns = (int)Math.Floor((availableWidth + gap) / (layout.CardWidth + gap));
        var usedWidth = (columns * layout.CardWidth) + ((columns - 1) * gap);
        Assert.True(usedWidth <= availableWidth + 0.001,
            $"{label}: used {usedWidth} > available {availableWidth}");
        Assert.True(columns >= 2, $"{label}: only {columns} column — should at least be 2 at this width");

        // More entries produce at least as many columns as fewer entries
        // (monotonicity check — column count should not decrease with more items)
        var layout20 = LauncherEntryLayoutCalculator.Calculate(availableWidth, 600, 20);
        var columns20 = (int)Math.Floor((availableWidth + gap) / (layout20.CardWidth + gap));
        Assert.True(columns20 >= columns,
            $"{label}: 20 entries produce {columns20} columns but 6 entries produce {columns}");
    }

    public static TheoryData<string, double, int, double> NameTileMatrix => new()
    {
        { "Min-6",  892, 6,   132 },
        { "Min-20", 892, 20,  132 },
        { "Default-6",  1132, 6,  132 },
        { "Default-20", 1132, 20, 132 },
        { "Wide-6",  1552, 6,  132 },
        { "Wide-20", 1552, 20, 132 },
    };

    [Theory, MemberData(nameof(NameTileMatrix))]
    public void Calculate_NameTiles_ResponsiveMatrix(
        string label, double availableWidth, int itemCount, double expectedMinTileWidth)
    {
        _ = label;
        var layout = LauncherEntryLayoutCalculator.Calculate(availableWidth, 600, itemCount);

        // Tiles respect minimum width
        Assert.True(layout.NameTileWidth >= expectedMinTileWidth,
            $"{label}: NameTileWidth {layout.NameTileWidth} < {expectedMinTileWidth}");

        // Tile height is fixed
        Assert.Equal(LauncherEntryLayoutCalculator.NameTileHeight, layout.NameTileHeight);

        // Tiles fill available width without overflow
        var columns = Math.Max(1,
            (int)Math.Floor((availableWidth + LauncherEntryLayoutCalculator.NameTileGap)
                / (layout.NameTileWidth + LauncherEntryLayoutCalculator.NameTileGap)));
        var usedWidth = (columns * layout.NameTileWidth)
            + ((columns - 1) * LauncherEntryLayoutCalculator.NameTileGap);
        Assert.True(usedWidth <= availableWidth + 0.001,
            $"{label}: used {usedWidth} > available {availableWidth}");

        // At these widths, should have at least 3 tile columns
        Assert.True(columns >= 3,
            $"{label}: only {columns} tile columns — expected ≥ 3");
    }

    // ── Narrow-width edge cases ──────────────────────────────────

    [Fact]
    public void Calculate_NarrowWidth_StillProducesReadableCards()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(892, 520, 20);

        Assert.True(layout.CardWidth >= LauncherEntryLayoutCalculator.CardMinWidth);
        var gap = LauncherEntryLayoutCalculator.CardGap;
        var columns = (int)Math.Floor((892 + gap) / (layout.CardWidth + gap));
        var usedWidth = (columns * layout.CardWidth) + ((columns - 1) * gap);
        Assert.True(usedWidth <= 892.001);
    }

    [Fact]
    public void Calculate_VeryNarrowWidth_ForcesSingleColumn()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(300, 600, 5);

        var gap = LauncherEntryLayoutCalculator.CardGap;
        var columns = (int)Math.Floor((300 + gap) / (layout.CardWidth + gap));
        Assert.Equal(1, columns);
        // At 300px available, single column uses full width (not clamped to CardMinWidth)
        Assert.True(layout.CardWidth > LauncherEntryLayoutCalculator.CardMinWidth,
            $"CardWidth {layout.CardWidth} should exceed {LauncherEntryLayoutCalculator.CardMinWidth} in single-column mode");
        Assert.True(layout.CardHeight >= LauncherEntryLayoutCalculator.CardMinHeight);
    }

    // ── Details view column proportions ───────────────────────────

    [Fact]
    public void DetailsColumnProportions_AreInExpectedRatio()
    {
        var totalWeight = LauncherDesignTokens.DetailsNameColumnWeight
            + LauncherDesignTokens.DetailsDirectoryColumnWeight
            + LauncherDesignTokens.DetailsCommandColumnWeight;

        Assert.Equal(7, totalWeight);
        Assert.Equal(2, LauncherDesignTokens.DetailsNameColumnWeight);
        Assert.Equal(2, LauncherDesignTokens.DetailsDirectoryColumnWeight);
        Assert.Equal(3, LauncherDesignTokens.DetailsCommandColumnWeight);
    }

    // ── Edge cases ───────────────────────────────────────────────

    [Fact]
    public void Calculate_SingleEntry_ProducesOneColumn()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(892, 600, 1);

        Assert.True(layout.CardWidth >= LauncherEntryLayoutCalculator.CardMinWidth);
        Assert.True(layout.CardHeight >= LauncherEntryLayoutCalculator.CardMinHeight);
        Assert.True(layout.NameTileWidth >= LauncherEntryLayoutCalculator.NameTileMinWidth);
        Assert.Equal(LauncherEntryLayoutCalculator.NameTileHeight, layout.NameTileHeight);
    }

    [Fact]
    public void Calculate_ManyEntries_LayoutCompletes()
    {
        var layout = LauncherEntryLayoutCalculator.Calculate(1132, 600, 100);
        Assert.True(layout.CardWidth >= LauncherEntryLayoutCalculator.CardMinWidth);
        Assert.True(layout.NameTileWidth >= LauncherEntryLayoutCalculator.NameTileMinWidth);
    }
}
