using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class MainWindowEntryCardResolverTests
{
    [Fact]
    public void ResolveFromSourceReturnsDirectCard()
    {
        using var temp = new TempDirectory();
        var entry = CreateEntryViewModel(temp.Path, "direct");
        var card = new Border
        {
            Classes = { "card" },
            DataContext = entry,
        };

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
        var innerCard = new Border
        {
            Classes = { "card" },
            DataContext = innerEntry,
            Child = source,
        };
        var outerCard = new Border
        {
            Classes = { "card" },
            DataContext = outerEntry,
            Child = innerCard,
        };

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

    private static LauncherEntryViewModel CreateEntryViewModel(string rootPath, string id)
    {
        var configPath = Path.Combine(rootPath, $"{id}-config.json");
        var settingsPath = Path.Combine(rootPath, $"{id}-settings.json");
        var entry = CreateEntry(id);

        LauncherConfigStore.Save([entry], configPath);

        var owner = new MainWindowViewModel(
            configPath,
            settingsPath,
            new LauncherSettings(),
            new LauncherProcess());

        return new LauncherEntryViewModel(entry, owner);
    }

    private static LaunchEntry CreateEntry(string id)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
        };
    }
}
