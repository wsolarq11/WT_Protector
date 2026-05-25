using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class MainWindowDragGhostFactoryTests
{
    [Fact]
    public void CreateAppliesLayoutAndCardShell()
    {
        var ghost = MainWindowDragGhostFactory.Create(
            name: "Alpha",
            directory: "D:\\Alpha",
            titleFontSize: 18,
            sourceWidth: 120,
            sourceHeight: 40,
            pointerX: 320,
            pointerY: 240);

        Assert.Equal(LauncherDragGhostLayoutCalculator.MinWidth, ghost.Control.Width);
        Assert.Equal(LauncherDragGhostLayoutCalculator.MinHeight, ghost.Control.Height);
        Assert.Equal(336, ghost.Left);
        Assert.Equal(256, ghost.Top);
        Assert.Contains("card", ghost.Control.Classes);
        Assert.False(ghost.Control.IsHitTestVisible);
        Assert.Equal(0.86, ghost.Control.Opacity);
    }

    [Fact]
    public void CreateBuildsNonInteractiveEntryPreviewContent()
    {
        var ghost = MainWindowDragGhostFactory.Create(
            name: "Alpha",
            directory: "D:\\Alpha",
            titleFontSize: 18,
            sourceWidth: 300,
            sourceHeight: 164,
            pointerX: 0,
            pointerY: 0);

        var content = Assert.IsType<StackPanel>(ghost.Control.Child);
        Assert.False(content.IsHitTestVisible);
        Assert.Equal(2, content.Children.Count);

        var title = Assert.IsType<TextBlock>(content.Children[0]);
        Assert.Equal("Alpha", title.Text);
        Assert.Equal(18, title.FontSize);
        Assert.Equal(FontWeight.SemiBold, title.FontWeight);
        Assert.Equal(TextTrimming.CharacterEllipsis, title.TextTrimming);
        Assert.False(title.IsHitTestVisible);

        var directory = Assert.IsType<TextBlock>(content.Children[1]);
        Assert.Equal("D:\\Alpha", directory.Text);
        Assert.Equal(12, directory.FontSize);
        Assert.Equal(TextTrimming.CharacterEllipsis, directory.TextTrimming);
        Assert.False(directory.IsHitTestVisible);
    }
}
