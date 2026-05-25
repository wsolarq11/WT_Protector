using Avalonia.Controls;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowEntryRowInteractionTests
{
    [Fact]
    public void IsInteractiveSourceTreatsButtonsAndCheckboxesAsInteractive()
    {
        Assert.True(MainWindowEntryRowInteraction.IsInteractiveSource(new Button()));
        Assert.True(MainWindowEntryRowInteraction.IsInteractiveSource(new CheckBox()));
    }

    [Fact]
    public void IsInteractiveSourceTreatsDragHandleAsInteractive()
    {
        var handle = new Border
        {
            Classes = { "entry-drag-handle" },
        };

        Assert.True(MainWindowEntryRowInteraction.IsInteractiveSource(handle));
    }

    [Fact]
    public void IsInteractiveSourceLeavesPlainTextAsRowSurface()
    {
        Assert.False(MainWindowEntryRowInteraction.IsInteractiveSource(new TextBlock()));
    }
}
