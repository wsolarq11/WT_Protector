using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class MainWindowDragEventCardResolverTests
{
    [Fact]
    public void ResolveReturnsDirectSourceCardWithoutHitTesting()
    {
        using var temp = new TempDirectory();
        var entry = CreateEntryViewModel(temp.Path, "direct");
        var directCard = Card(entry);
        var hitTestCalled = false;

        var resolved = MainWindowDragEventCardResolver.Resolve(
            directCard,
            () =>
            {
                hitTestCalled = true;
                return null;
            });

        Assert.NotNull(resolved);
        Assert.Same(entry, resolved.Value.Entry);
        Assert.Same(directCard, resolved.Value.Card);
        Assert.False(hitTestCalled);
    }

    [Fact]
    public void ResolveFallsBackToHitTestWhenSourceDoesNotResolve()
    {
        using var temp = new TempDirectory();
        var entry = CreateEntryViewModel(temp.Path, "fallback");
        var fallbackCard = Card(entry);

        var resolved = MainWindowDragEventCardResolver.Resolve(
            eventSource: new object(),
            hitTestFallback: () => fallbackCard);

        Assert.NotNull(resolved);
        Assert.Same(entry, resolved.Value.Entry);
        Assert.Same(fallbackCard, resolved.Value.Card);
    }

    [Fact]
    public void ResolveReturnsNullWhenNeitherSourceNorHitTestResolve()
    {
        var resolved = MainWindowDragEventCardResolver.Resolve(
            eventSource: new object(),
            hitTestFallback: () => new Border { Classes = { "card" }, DataContext = new object() });

        Assert.Null(resolved);
    }

    private static Border Card(LauncherEntryViewModel entry)
    {
        return new Border
        {
            Classes = { "card" },
            DataContext = entry,
        };
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
