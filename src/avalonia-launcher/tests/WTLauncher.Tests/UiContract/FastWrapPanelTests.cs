using Avalonia;
using Avalonia.Controls;
using WTLauncher.App.Views;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class FastWrapPanelTests
{
    // ponytail: adversarial wrap-consistency check. Three children per row
    // (child=60, spacing=8, width=200), 10 children produce 4 rows: 3+3+3+1.
    [Fact]
    public void MeasureArrange_ChildrenWithinDesiredSize_AfterWrap()
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
    public void Measure_ExactFit_DoesNotWrap()
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
    public void Measure_HandlesPositiveInfinityWidthWithoutCrash()
    {
        // PositiveInfinity is passed through by Avalonia's Layoutable.Measure
        // (no NaN check). FastWrapPanel.MeasureOverride uses 900px fallback.
        var panel = new FastWrapPanel();
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.True(double.IsFinite(panel.DesiredSize.Width));
        Assert.True(double.IsFinite(panel.DesiredSize.Height));
    }

    [Fact]
    public void Measure_HandlesNegativeInfinityWidthWithoutCrash()
    {
        var panel = new FastWrapPanel();
        // NegativeInfinity bypasses both the NaN guard (Layoutable) and the
        // IsInfinity guard (FastWrapPanel). The key invariant: no exception.
        panel.Measure(new Size(double.NegativeInfinity, double.NegativeInfinity));
    }

    [Fact]
    public void Arrange_DoesNotCrashOnEmptyPanel()
    {
        var panel = new FastWrapPanel();
        panel.Measure(new Size(640, 480));
        panel.Arrange(new Rect(0, 0, 640, 480));
    }

    [Fact]
    public void Measure_NaNIsHandledByAvaloniaBaseClass()
    {
        // Avalonia's Layoutable.Measure throws InvalidOperationException on NaN
        // BEFORE FastWrapPanel.MeasureOverride is called. The IsNaN guard in
        // MeasureOverride is defense-in-depth (cannot be reached in current
        // Avalonia version, but protects against future changes).
        var panel = new FastWrapPanel();
        Assert.Throws<InvalidOperationException>(
            () => panel.Measure(new Size(double.NaN, double.NaN)));
    }
}
