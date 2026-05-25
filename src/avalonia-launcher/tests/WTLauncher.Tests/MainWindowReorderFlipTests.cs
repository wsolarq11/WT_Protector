using Avalonia;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowReorderFlipTests
{
    [Fact]
    public void TryCalculateOffsetReturnsOldMinusNewPosition()
    {
        var calculated = MainWindowReorderFlip.TryCalculateOffset(
            "two",
            new Dictionary<string, Rect>
            {
                ["two"] = Rect(40, 120),
            },
            new Dictionary<string, Rect>
            {
                ["two"] = Rect(12.5, 180.25),
            },
            out var offset);

        Assert.True(calculated);
        Assert.Equal(27.5, offset.X);
        Assert.Equal(-60.25, offset.Y);
        Assert.Equal("translate(27.5px,-60.25px)", MainWindowReorderFlip.FormatTranslate(offset));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.49, 0)]
    [InlineData(0, -0.49)]
    [InlineData(0.49, -0.49)]
    public void TryCalculateOffsetIgnoresSubPixelNoiseBelowThreshold(double deltaX, double deltaY)
    {
        var calculated = MainWindowReorderFlip.TryCalculateOffset(
            "two",
            new Dictionary<string, Rect>
            {
                ["two"] = Rect(100 + deltaX, 200 + deltaY),
            },
            new Dictionary<string, Rect>
            {
                ["two"] = Rect(100, 200),
            },
            out _);

        Assert.False(calculated);
    }

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(0, -0.5)]
    public void TryCalculateOffsetKeepsMovementAtThreshold(double deltaX, double deltaY)
    {
        var calculated = MainWindowReorderFlip.TryCalculateOffset(
            "two",
            new Dictionary<string, Rect>
            {
                ["two"] = Rect(100 + deltaX, 200 + deltaY),
            },
            new Dictionary<string, Rect>
            {
                ["two"] = Rect(100, 200),
            },
            out var offset);

        Assert.True(calculated);
        Assert.Equal(deltaX, offset.X);
        Assert.Equal(deltaY, offset.Y);
    }

    [Fact]
    public void TryCalculateOffsetReturnsFalseWhenEntryIsMissingFromEitherSnapshot()
    {
        Assert.False(MainWindowReorderFlip.TryCalculateOffset(
            "missing-before",
            new Dictionary<string, Rect>(),
            new Dictionary<string, Rect> { ["missing-before"] = Rect(0, 0) },
            out _));
        Assert.False(MainWindowReorderFlip.TryCalculateOffset(
            "missing-after",
            new Dictionary<string, Rect> { ["missing-after"] = Rect(0, 0) },
            new Dictionary<string, Rect>(),
            out _));
    }

    private static Rect Rect(double x, double y)
    {
        return new Rect(x, y, 100, 60);
    }
}
