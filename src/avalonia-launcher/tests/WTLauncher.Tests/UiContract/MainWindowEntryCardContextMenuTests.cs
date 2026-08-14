using Avalonia.Controls;
using WTLauncher.App.Views;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class MainWindowEntryCardContextMenuTests
{
    [Fact]
    public void ResolveFromSourceReturnsNearestCardMenu()
    {
        var source = new Button();
        var innerMenu = new ContextMenu();
        var innerCard = Card(innerMenu, source);
        var outerMenu = new ContextMenu();
        var outerCard = Card(outerMenu, innerCard);

        var resolved = MainWindowEntryCardContextMenu.ResolveFromSource(source);

        Assert.NotNull(resolved);
        Assert.Same(innerMenu, resolved.Value.Menu);
        Assert.Same(innerCard, resolved.Value.Card);
        Assert.Same(outerCard, MainWindowEntryCardContextMenu.ResolveFromSource(outerCard)?.Card);
    }

    [Fact]
    public void ResolveFromSourceStopsAtNearestCardWithoutMenu()
    {
        var source = new Button();
        var innerCard = new Border
        {
            Classes = { MainWindowEntryClasses.Card },
            Child = source,
        };
        var outerCard = Card(new ContextMenu(), innerCard);

        Assert.Null(MainWindowEntryCardContextMenu.ResolveFromSource(source));
        Assert.Same(outerCard, MainWindowEntryCardContextMenu.ResolveFromSource(outerCard)?.Card);
    }

    [Fact]
    public void ResolveFromSourceReturnsNullWithoutCardMenu()
    {
        Assert.Null(MainWindowEntryCardContextMenu.ResolveFromSource(new Button()));
        Assert.Null(MainWindowEntryCardContextMenu.ResolveFromSource(new object()));
        Assert.Null(MainWindowEntryCardContextMenu.ResolveFromSource(null));
    }

    private static Border Card(ContextMenu menu, Control child) =>
        new()
        {
            Classes = { MainWindowEntryClasses.Card },
            ContextMenu = menu,
            Child = child,
        };
}
