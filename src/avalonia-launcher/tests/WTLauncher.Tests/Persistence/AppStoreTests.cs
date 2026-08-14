using System.Collections.Immutable;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

[Trait("Category", TestCategories.Persistence)]
public sealed class StoreGrainTests
{
    private static LauncherStoreFile SampleStore() => new()
    {
        Entries =
        [
            new LaunchEntry { Id = "one", Name = "One", Directory = (WTLauncher.Core.DirectoryPath)"D:\\one", Command = "pwsh" },
            new LaunchEntry { Id = "two", Name = "Two", Directory = (WTLauncher.Core.DirectoryPath)"D:\\two", Command = "codex" },
        ],
        Settings = new LauncherSettings(),
        Presets =
        [
            new LauncherPreset { Id = "default", Name = "Default", EntryIds = ["one", "two"] },
        ],
        DefaultPresetId = "default",
    };

    [Fact]
    public void Select_ReturnsProjectedValue()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());

        var ids = store.Select(s => s.Entries.Select(e => e.Id).ToImmutableList());

        Assert.Equal(["one", "two"], ids);
    }

    [Fact]
    public void Select_ReturnsSettings()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());

        var settings = store.Select(s => s.Settings);

        Assert.NotNull(settings);
    }

    [Fact]
    public void Mutate_PersistsTransformToDisk()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());

        store.Dispatch(state => state with
        {
            Settings = state.Settings with { SortMode = LauncherEntryQuery.AlphabeticalSortModeId },
        });

        var reloaded = LauncherStore.Load(path);
        Assert.Equal(LauncherEntryQuery.AlphabeticalSortModeId, reloaded.Settings.SortMode);
    }

    [Fact]
    public void Mutate_UpdatesInMemoryState()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());

        store.Dispatch(state => state with
        {
            Entries = state.Entries.Add(new LaunchEntry
                { Id = "three", Name = "Three", Directory = (WTLauncher.Core.DirectoryPath)"D:\\three", Command = "pwsh" }),
        });

        var count = store.Select(s => s.Entries.Count);
        Assert.Equal(3, count);
    }

    [Fact]
    public void Mutate_SkipNoopTransformDoesNotChangeFile()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var beforeHash = HashFile(path);
        var store = new StoreGrain(path, new FileStorePersistence());

        store.Dispatch(state => state);

        var afterHash = HashFile(path);
        Assert.Equal(beforeHash, afterHash);
    }

    [Fact]
    public void Mutate_RollsBackOnSaveFailure()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        // Make file read-only so the next save fails.
        File.SetAttributes(path, FileAttributes.ReadOnly);
        var store = new StoreGrain(path, new FileStorePersistence());

        try
        {
            Assert.ThrowsAny<Exception>(() =>
                store.Dispatch(state => state with { DefaultPresetId = "x" }));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }

        Assert.Equal("default", store.Select(s => s.DefaultPresetId));
    }

    [Fact]
    public void Subscribe_NotifiedOnMutate()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());
        var notified = false;

        using (store.Subscribe(_ => notified = true))
        {
            store.Dispatch(state => state with { DefaultPresetId = "changed" });
        }

        Assert.True(notified);
    }

    [Fact]
    public void Subscribe_NotNotifiedOnFailedMutate()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());
        var notified = false;

        using (store.Subscribe(_ => notified = true))
        {
            Assert.ThrowsAny<Exception>(() =>
                store.Dispatch(_ => throw new InvalidOperationException("boom")));
        }

        Assert.False(notified);
    }

    [Fact]
    public void Subscribe_DisposeStopsNotifications()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());
        var notified = false;

        var sub = store.Subscribe(_ => notified = true);
        sub.Dispose();
        store.Dispatch(state => state with { DefaultPresetId = "x" });

        Assert.False(notified);
    }

    [Fact]
    public async Task MutateAsync_PersistsAndUpdatesState()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());

        store.Dispatch(state => state with
        {
            Settings = state.Settings with { SortMode = LauncherEntryQuery.AlphabeticalSortModeId },
        });

        var reloaded = LauncherStore.Load(path);
        Assert.Equal(LauncherEntryQuery.AlphabeticalSortModeId, reloaded.Settings.SortMode);
        Assert.Equal(LauncherEntryQuery.AlphabeticalSortModeId, store.Select(s => s.Settings.SortMode));
    }

    [Fact]
    public void StorePath_ReturnsConstructorPath()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());

        Assert.Equal(path, store.StorePath);
    }

    private static string HashFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }
}

