using CsCheck;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.PropertyTest;

[Trait("Category", TestCategories.PropertyTest)]
public sealed class LauncherLayoutPbtTests
{
    /// <summary>
    /// Property: Ghost layout calculator always produces non-negative positions.
    /// </summary>
    [Fact]
    public void CalculateGhostPosition_ReturnsValidPosition()
    {
        Gen.Double[0, 2000]
            .SelectMany(x => Gen.Double[0, 2000], (x, y) => (x, y))
            .SelectMany(pos => Gen.Double[100, 2000], (pos, w) => (pos.x, pos.y, w))
            .SelectMany(all => Gen.Double[100, 2000], (all, h) => (all.x, all.y, all.w, h))
            .Sample(assert: input =>
            {
                var (px, py, cw, ch) = input;
                var result = LauncherDragGhostLayoutCalculator.CalculatePosition(
                    px, py, cw, ch, ghostWidth: 200, ghostHeight: 100);

                Assert.True(result.Left >= 0 || cw > px + 200,
                    $"Ghost left ({result.Left}) should be >= 0 or container wider than pointer");
                Assert.True(result.Top >= 0 || ch > py + 100,
                    $"Ghost top ({result.Top}) should be >= 0 or container taller than pointer");
            });
    }

    /// <summary>
    /// Property: Ghost layout calculator returns finite dimensions.
    /// </summary>
    [Fact]
    public void CalculateGhost_ReturnsFiniteDimensions()
    {
        Gen.Double[10, 2000]
            .SelectMany(sw => Gen.Double[10, 2000], (sw, sh) => (sw, sh))
            .SelectMany(dim => Gen.Double[0, 2000], (dim, px) => (dim.sw, dim.sh, px))
            .SelectMany(all => Gen.Double[0, 2000], (all, py) => (all.sw, all.sh, all.px, py))
            .Sample(assert: input =>
            {
                var (sw, sh, px, py) = input;
                var result = LauncherDragGhostLayoutCalculator.Calculate(sw, sh, px, py);

                Assert.True(double.IsFinite(result.Width), "Width must be finite");
                Assert.True(double.IsFinite(result.Height), "Height must be finite");
                Assert.True(double.IsFinite(result.Left), "Left must be finite");
                Assert.True(double.IsFinite(result.Top), "Top must be finite");
                Assert.True(result.Width > 0, "Width must be positive");
                Assert.True(result.Height > 0, "Height must be positive");
            });
    }
}
