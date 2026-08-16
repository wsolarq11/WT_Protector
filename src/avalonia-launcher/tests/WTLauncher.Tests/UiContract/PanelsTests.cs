using Avalonia;
using Avalonia.Controls;
using WTLauncher.App.Views;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class PanelsTests
{
    // ponytail: adversarial wrap-consistency check. Three children per row
    // (child=60, spacing=8, width=200), 10 children produce 4 rows: 3+3+3+1.
    [Fact]
    public void FastWrapPanel_MeasureArrange_ChildrenWithinDesiredSize_AfterWrap()
    {
        var panel = new FastWrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        for (int i = 0; i < 10; i++)
            panel.Children.Add(new Border { Width = 60, Height = 40 });

        panel.Measure(new Size(200, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 200, panel.DesiredSize.Height));

        foreach (var child in panel.Children)
        {
            var b = child.Bounds;
            Assert.True(b.Right <= panel.DesiredSize.Width + 0.001,
                $"bounds.Right={b.Right} > panel.DesiredSize.Width={panel.DesiredSize.Width}");
            Assert.True(b.Bottom <= panel.DesiredSize.Height + 0.001,
                $"bounds.Bottom={b.Bottom} > panel.DesiredSize.Height={panel.DesiredSize.Height}");
        }

        // 3 per row: 3*60+2*8=196 width, 4*40+3*8=184 height
        Assert.Equal(196.0, panel.DesiredSize.Width, 3);
        Assert.Equal(184.0, panel.DesiredSize.Height, 3);
    }

    // ponytail: boundary case where last child on a row exactly fills available width.
    // child=96, spacing=8, width=200: 96+8+96=200 (exact fit, should NOT wrap between them).
    [Fact]
    public void FastWrapPanel_Measure_ExactFit_DoesNotWrap()
    {
        var panel = new FastWrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        panel.Children.Add(new Border { Width = 96, Height = 40 });
        panel.Children.Add(new Border { Width = 96, Height = 40 });

        panel.Measure(new Size(200, double.PositiveInfinity));

        // one row: 2*96+1*8=200, height=40
        Assert.Equal(200.0, panel.DesiredSize.Width, 2);
        Assert.Equal(40.0, panel.DesiredSize.Height, 2);
    }

    [Fact]
    public void FastWrapPanel_Measure_HandlesPositiveInfinityWidthWithoutCrash()
    {
        var panel = new FastWrapPanel();
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.True(double.IsFinite(panel.DesiredSize.Width));
        Assert.True(double.IsFinite(panel.DesiredSize.Height));
    }

    [Fact]
    public void FastWrapPanel_Measure_HandlesNegativeInfinityWidthWithoutCrash()
    {
        var panel = new FastWrapPanel();
        panel.Measure(new Size(double.NegativeInfinity, double.NegativeInfinity));
    }

    [Fact]
    public void FastWrapPanel_Arrange_DoesNotCrashOnEmptyPanel()
    {
        var panel = new FastWrapPanel();
        panel.Measure(new Size(640, 480));
        panel.Arrange(new Rect(0, 0, 640, 480));
    }

    [Fact]
    public void FastWrapPanel_Measure_NaNIsHandledByAvaloniaBaseClass()
    {
        var panel = new FastWrapPanel();
        Assert.Throws<InvalidOperationException>(
            () => panel.Measure(new Size(double.NaN, double.NaN)));
    }

    [Fact]
    public void FillGridPanel_MeasuresAndArrangesCellsToFillViewportHeight()
    {
        var panel = new FillGridPanel
        {
            Columns = 3,
            ViewportHeight = 520,
            ItemSpacing = 8,
            LineSpacing = 8,
            MinItemWidth = LauncherEntryLayoutCalculator.CardMinWidth,
            MinItemHeight = LauncherEntryLayoutCalculator.CardMinHeight,
        };
        for (int i = 0; i < 6; i++)
            panel.Children.Add(new Border());

        panel.Measure(new Size(900, double.PositiveInfinity));
        Assert.Equal(900.0, panel.DesiredSize.Width, 3);
        Assert.Equal(520.0, panel.DesiredSize.Height, 3);

        panel.Arrange(new Rect(0, 0, panel.DesiredSize.Width, panel.DesiredSize.Height));
        var expectedCellWidth = (900 - (2 * 8)) / 3d;
        var expectedCellHeight = (520 - 8) / 2d;
        foreach (var child in panel.Children)
        {
            Assert.InRange(child.Bounds.Width, expectedCellWidth - 1, expectedCellWidth + 1);
            Assert.Equal(expectedCellHeight, child.Bounds.Height, 3);
            Assert.True(child.Bounds.Right <= panel.Bounds.Width + 0.001);
            Assert.True(child.Bounds.Bottom <= panel.Bounds.Height + 0.001);
        }
    }

    [Fact]
    public void FillGridPanel_StretchesLastRowForPartialGroup()
    {
        var panel = new FillGridPanel
        {
            Columns = 4,
            ViewportHeight = 520,
            ItemSpacing = 8,
            LineSpacing = 8,
            MinItemWidth = LauncherEntryLayoutCalculator.CardMinWidth,
            MinItemHeight = LauncherEntryLayoutCalculator.CardMinHeight,
            StretchLastRow = true,
        };
        for (int i = 0; i < 7; i++)
            panel.Children.Add(new Border());

        var expectedGrid = LauncherEntryLayoutCalculator.CalculateCardGridWithColumns(4, 1132, 520, 7);
        Assert.Equal(2, expectedGrid.Rows);
        Assert.True(panel.StretchLastRow);

        panel.Measure(new Size(1132, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 1132, panel.DesiredSize.Height));

        var normalCellWidth = (1132 - (3 * 8)) / 4d;
        var lastRowWidth = panel.Children[4].Bounds.Width;
        Assert.True(lastRowWidth > normalCellWidth, $"last-row width {lastRowWidth} should exceed normal {normalCellWidth}");
        Assert.True(panel.Children[6].Bounds.Right <= 1132 + 0.001);
    }

    [Fact]
    public void FillGridPanel_ScrollFallback_ReturnsContentHeightAboveViewport()
    {
        var panel = new FillGridPanel
        {
            Columns = 4,
            ViewportHeight = 330,
            ItemSpacing = 8,
            LineSpacing = 8,
            MinItemWidth = LauncherEntryLayoutCalculator.CardMinWidth,
            MinItemHeight = LauncherEntryLayoutCalculator.CardMinHeight,
        };
        for (int i = 0; i < 19; i++)
            panel.Children.Add(new Border());

        panel.Measure(new Size(892, double.PositiveInfinity));

        Assert.True(panel.DesiredSize.Height > 330.001, "scroll fallback must report overflow content height");
        var expectedHeight = (5 * LauncherEntryLayoutCalculator.CardMinHeight) + (4 * 8);
        Assert.Equal(expectedHeight, panel.DesiredSize.Height, 3);
    }

    [Fact]
    public void FillGridPanel_GroupMode_UsesSharedRowHeightAndReturnsContentHeight()
    {
        var panel = new FillGridPanel
        {
            Columns = 4,
            ViewportHeight = 520,
            ItemHeight = 256,
            FillViewport = false,
            ItemSpacing = 8,
            LineSpacing = 8,
            MinItemWidth = LauncherEntryLayoutCalculator.CardMinWidth,
            MinItemHeight = LauncherEntryLayoutCalculator.CardMinHeight,
            StretchLastRow = true,
        };
        for (int i = 0; i < 3; i++)
            panel.Children.Add(new Border());

        panel.Measure(new Size(1132, double.PositiveInfinity));

        Assert.Equal(1132.0, panel.DesiredSize.Width, 3);
        Assert.Equal(256.0, panel.DesiredSize.Height, 3);

        panel.Arrange(new Rect(0, 0, 1132, panel.DesiredSize.Height));
        Assert.All(panel.Children, child => Assert.Equal(256.0, child.Bounds.Height, 3));
        Assert.InRange(panel.Children[2].Bounds.Right, 1130, 1134);
    }

    [Fact]
    public void FillGridPanel_AutoComputesColumnsFromSharedCoreRule()
    {
        var panel = new FillGridPanel
        {
            ViewportHeight = 520,
            ItemSpacing = 8,
            LineSpacing = 8,
            MinItemWidth = LauncherEntryLayoutCalculator.CardMinWidth,
            MinItemHeight = LauncherEntryLayoutCalculator.CardMinHeight,
        };
        for (int i = 0; i < 6; i++)
            panel.Children.Add(new Border());

        panel.Measure(new Size(900, double.PositiveInfinity));

        var expected = LauncherEntryLayoutCalculator.CalculateCardGrid(900, 520, 6);
        Assert.Equal(expected.Columns * expected.Width + ((expected.Columns - 1) * 8), panel.DesiredSize.Width, 3);
        Assert.Equal(520.0, panel.DesiredSize.Height, 3);
    }

    [Fact]
    public void FillGridPanel_Arrange_DoesNotCrashOnEmptyPanel()
    {
        var panel = new FillGridPanel { Columns = 4, ViewportHeight = 520 };
        panel.Measure(new Size(1132, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 1132, panel.DesiredSize.Height));
    }
}
