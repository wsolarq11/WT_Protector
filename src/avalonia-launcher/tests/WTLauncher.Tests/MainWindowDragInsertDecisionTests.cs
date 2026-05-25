using Avalonia;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowDragInsertDecisionTests
{
    [Fact]
    public void ShouldInsertAfterUsesHorizontalHalfForCardAndNameViews()
    {
        Assert.False(MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(49, 90),
            new Size(100, 200),
            useVerticalInsertPreview: false));
        Assert.True(MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(50, 0),
            new Size(100, 200),
            useVerticalInsertPreview: false));
    }

    [Fact]
    public void ShouldInsertAfterUsesVerticalHalfForDetailsView()
    {
        Assert.False(MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(90, 49),
            new Size(100, 100),
            useVerticalInsertPreview: true));
        Assert.True(MainWindowDragInsertDecision.ShouldInsertAfter(
            new Point(0, 50),
            new Size(100, 100),
            useVerticalInsertPreview: true));
    }
}
