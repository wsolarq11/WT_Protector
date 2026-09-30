using Avalonia;
using WTLauncher.App.Views;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class MainWindowDragThresholdTests
{
    [Fact]
    public void ShouldStartDragReturnsFalseWithinThreshold()
    {
        var start = new Point(10, 10);

        Assert.False(MainWindowDragThreshold.ShouldStartDrag(start, new Point(10, 10)));
        Assert.False(MainWindowDragThreshold.ShouldStartDrag(start, new Point(13, 10)));
        Assert.False(MainWindowDragThreshold.ShouldStartDrag(start, new Point(10, 13)));
        Assert.False(MainWindowDragThreshold.ShouldStartDrag(start, new Point(12, 12)));
    }

    [Fact]
    public void ShouldStartDragReturnsTrueAtOrBeyondThreshold()
    {
        var start = new Point(10, 10);

        Assert.True(MainWindowDragThreshold.ShouldStartDrag(start, new Point(14, 10)));
        Assert.True(MainWindowDragThreshold.ShouldStartDrag(start, new Point(10, 14)));
        Assert.True(MainWindowDragThreshold.ShouldStartDrag(start, new Point(13, 13)));
    }
}
