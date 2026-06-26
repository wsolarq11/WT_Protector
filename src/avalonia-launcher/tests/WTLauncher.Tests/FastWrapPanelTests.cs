using Avalonia;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class FastWrapPanelTests
{
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
