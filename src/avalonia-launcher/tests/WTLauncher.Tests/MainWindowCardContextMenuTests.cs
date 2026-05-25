using Avalonia.Controls;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowCardContextMenuTests
{
    [Fact]
    public void TryOpenForOpensNearestCardContextMenu()
    {
        var leaf = new Button();
        var outerMenu = new ContextMenu();
        var innerMenu = new ContextMenu();
        var innerCard = new Border
        {
            Classes = { "card" },
            ContextMenu = innerMenu,
            Child = leaf,
        };
        var outerCard = new Border
        {
            Classes = { "card" },
            ContextMenu = outerMenu,
            Child = innerCard,
        };

        ContextMenu? openedMenu = null;
        Border? openedCard = null;
        var opened = MainWindowCardContextMenu.TryOpenFor(
            leaf,
            (menu, card) =>
            {
                openedMenu = menu;
                openedCard = card;
            });

        Assert.True(opened);
        Assert.Same(innerMenu, openedMenu);
        Assert.Same(innerCard, openedCard);
        Assert.Same(outerCard, MainWindowCardContextMenu.FindCard(outerCard));
    }

    [Fact]
    public void TryOpenForReturnsFalseWhenCardHasNoContextMenu()
    {
        var source = new Button();
        var card = new Border
        {
            Classes = { "card" },
            Child = source,
        };

        var opened = false;
        var result = MainWindowCardContextMenu.TryOpenFor(source, (_, _) => opened = true);

        Assert.False(result);
        Assert.False(opened);
        Assert.Same(card, MainWindowCardContextMenu.FindCard(source));
    }

    [Fact]
    public void TryOpenForReturnsFalseWithoutCardAncestor()
    {
        var opened = false;
        var result = MainWindowCardContextMenu.TryOpenFor(new Button(), (_, _) => opened = true);

        Assert.False(result);
        Assert.False(opened);
    }
}
