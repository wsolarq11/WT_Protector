using Avalonia.Controls;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowEntryRowDoubleTapTests
{
    [Fact]
    public void ShouldLaunchFromSourceAllowsPlainRow()
    {
        var row = new Border();
        var source = new TextBlock();
        row.Child = source;

        Assert.True(MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(source));
    }

    [Fact]
    public void ShouldLaunchFromSourceBlocksInteractiveControls()
    {
        Assert.False(MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(new Button()));
        Assert.False(MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(new CheckBox()));
    }

    [Fact]
    public void ShouldLaunchFromSourceBlocksDragHandleAncestor()
    {
        var source = new TextBlock();
        var handle = new Border
        {
            Classes = { MainWindowEntryClasses.DragHandle },
            Child = source,
        };

        Assert.False(MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(source));
        Assert.False(MainWindowEntryRowDoubleTap.ShouldLaunchFromSource(handle));
    }

}
