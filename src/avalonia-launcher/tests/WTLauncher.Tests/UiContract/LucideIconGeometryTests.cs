using Avalonia;

using WTLauncher.App.Infrastructure;

using WTLauncher.Tests;

namespace WTLauncher.Tests.UiContract;

/// <summary>
/// Locks the centring math behind <see cref="LucideIconGeometry"/>. Lucide icons
/// live on a 24×24 viewbox whose centre is (12,12); the uniform scale
/// targetSize/24 must land that centre on the target centre exactly, which is
/// what makes the geometry self-centring with no translation. These assertions
/// stay on the transform matrix (pure math) so they run headless without a
/// rendering platform.
/// </summary>
[Trait("Category", TestCategories.UiContract)]
public sealed class LucideIconGeometryTests
{
    private static readonly Point ViewBoxCentre = new(LucideIconGeometry.ViewBoxSize / 2, LucideIconGeometry.ViewBoxSize / 2);

    [Theory]
    [InlineData(16)]
    [InlineData(14)]
    [InlineData(12)]
    public void UniformScale_MapsViewBoxCentreOntoTargetCentre(double targetSize)
    {
        var scale = LucideIconGeometry.ScaleFor(targetSize);
        var transform = Matrix.CreateScale(scale, scale);

        var mapped = ViewBoxCentre.Transform(transform);

        Assert.Equal(targetSize / 2, mapped.X, 6);
        Assert.Equal(targetSize / 2, mapped.Y, 6);
    }

    [Fact]
    public void ScaleFor_IsTargetOverViewBox()
    {
        Assert.Equal(16.0 / 24.0, LucideIconGeometry.ScaleFor(16), 12);
        Assert.Equal(14.0 / 24.0, LucideIconGeometry.ScaleFor(14), 12);
        Assert.Equal(12.0 / 24.0, LucideIconGeometry.ScaleFor(12), 12);
    }

    [Fact]
    public void StrokeFor_ScalesStrokeBySameFactor()
    {
        // 2-unit stroke on a 24 viewbox, rendered at 16px → 2 * 16/24 = 4/3.
        Assert.Equal(4.0 / 3.0, LucideIconGeometry.StrokeFor(16), 6);
        Assert.Equal(7.0 / 6.0, LucideIconGeometry.StrokeFor(14), 6);
        Assert.Equal(1.0, LucideIconGeometry.StrokeFor(12), 6);
    }
}
