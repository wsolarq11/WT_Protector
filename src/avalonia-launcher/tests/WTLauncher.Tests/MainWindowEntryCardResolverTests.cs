using Avalonia.Controls;
using WTLauncher.App.Views;
using static WTLauncher.Tests.MainWindowEntryCardTestFactory;

namespace WTLauncher.Tests;

public sealed class MainWindowEntryCardResolverTests
{
    [Fact]
    public void ResolveFromSourceReturnsDirectCard()
    {
        using var temp = new TempDirectory();
        var entry = CreateEntryViewModel(temp.Path, "direct");
        var card = Card(entry);

        var resolved = MainWindowEntryCardResolver.ResolveFromSource(card);

        Assert.NotNull(resolved);
        Assert.Same(entry, resolved.Value.Entry);
        Assert.Same(card, resolved.Value.Card);
    }

    [Fact]
    public void ResolveFromSourceWalksVisualAncestorsToNearestCard()
    {
        using var temp = new TempDirectory();
        var outerEntry = CreateEntryViewModel(temp.Path, "outer");
        var innerEntry = CreateEntryViewModel(temp.Path, "inner");
        var source = new Button();
        var innerCard = Card(innerEntry);
        innerCard.Child = source;
        var outerCard = Card(outerEntry);
        outerCard.Child = innerCard;

        var resolved = MainWindowEntryCardResolver.ResolveFromSource(source);

        Assert.NotNull(resolved);
        Assert.Same(innerEntry, resolved.Value.Entry);
        Assert.Same(innerCard, resolved.Value.Card);
        Assert.Same(outerCard, MainWindowEntryCardResolver.ResolveFromSource(outerCard)?.Card);
    }

    [Fact]
    public void ResolveFromSourceReturnsNullWithoutLauncherEntryCard()
    {
        var source = new Button();
        var cardWithoutEntry = new Border
        {
            Classes = { "card" },
            DataContext = new object(),
            Child = source,
        };

        Assert.Null(MainWindowEntryCardResolver.ResolveFromSource(source));
        Assert.Null(MainWindowEntryCardResolver.ResolveFromSource(cardWithoutEntry));
        Assert.Null(MainWindowEntryCardResolver.ResolveFromSource(new object()));
    }
}
