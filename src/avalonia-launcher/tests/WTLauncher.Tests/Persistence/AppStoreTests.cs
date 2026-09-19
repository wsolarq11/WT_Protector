using System.Collections.Immutable;
using ErrorOr;
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

    [Fact]
    public void Reloaded_NotifiesAfterReloadFromDisk()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());
        LauncherStoreFile? reloaded = null;
        store.Reloaded += state => reloaded = state;

        LauncherStore.Save(SampleStore() with { DefaultPresetId = "changed" }, path, backup: false);
        store.ReloadFromDisk();

        Assert.NotNull(reloaded);
        Assert.Equal("changed", reloaded!.DefaultPresetId);
        Assert.Equal("changed", store.Select(s => s.DefaultPresetId));
    }

    [Fact]
    public void ReloadFromDisk_CorruptPrimary_Throws()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path, backup: false);
        var store = new StoreGrain(path, new FileStorePersistence());
        File.WriteAllText(path, "not-json{");

        Assert.Throws<System.Text.Json.JsonException>(() => store.ReloadFromDisk());
    }

    [Fact]
    public void Reloaded_DoesNotFireOnDispatch()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        LauncherStore.Save(SampleStore(), path);
        var store = new StoreGrain(path, new FileStorePersistence());
        var notified = false;
        store.Reloaded += _ => notified = true;

        store.Dispatch(state => state with { DefaultPresetId = "changed" });

        Assert.False(notified);
    }

    [Fact]
    public void Constructor_UnreadableExistingFile_ReturnsEmptyStateWithoutThrowing()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        File.WriteAllText(path, "{}");
        var persistence = new ThrowingLoadPersistence(new IOException("unreadable"));

        var store = new StoreGrain(path, persistence);

        Assert.Empty(store.Select(s => s.Entries));
        Assert.Empty(store.Select(s => s.Presets));
    }

    [Fact]
    public void Constructor_FileNotFoundDuringLoad_ReturnsEmptyStateWithoutThrowing()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        File.WriteAllText(path, "{}");
        var persistence = new ThrowingLoadPersistence(new FileNotFoundException("gone"));

        var store = new StoreGrain(path, persistence);

        Assert.Empty(store.Select(s => s.Entries));
    }

    [Fact]
    public void Constructor_WhenLoadFails_DispatchOverwritesPersistedStore()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        File.WriteAllText(path, "{}");
        var persistence = new RecordingLoadFailurePersistence(new IOException("unreadable"));
        var store = new StoreGrain(path, persistence);

        store.Dispatch(state => state with { DefaultPresetId = "overwritten" });

        Assert.Equal(1, persistence.SaveCount);
        Assert.Equal("overwritten", persistence.LastSaved!.DefaultPresetId);
    }

    private static string HashFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }
}

public sealed class ThrowingLoadPersistence(Exception exception) : IStorePersistence
{
    public LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null) => throw exception;

    public void Save(LauncherStoreFile store, string storePath, bool backup = true) => throw new NotSupportedException();

    public ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath) => throw exception;

    public string ResolveStorePath(string? explicitPath, string? currentDirectory = null, string? appBaseDirectory = null)
        => explicitPath ?? string.Empty;
}

public sealed class RecordingLoadFailurePersistence(Exception loadException) : IStorePersistence
{
    public int SaveCount { get; private set; }

    public LauncherStoreFile? LastSaved { get; private set; }

    public LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null) => throw loadException;

    public void Save(LauncherStoreFile store, string storePath, bool backup = true)
    {
        SaveCount++;
        LastSaved = store;
    }

    public ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath) => throw loadException;

    public string ResolveStorePath(string? explicitPath, string? currentDirectory = null, string? appBaseDirectory = null)
        => explicitPath ?? string.Empty;
}

public sealed class StoreMutationSessionTests
{
    [Fact]
    public void Commit_FailureInvokesRestoreAndReturnsError()
    {
        var restored = false;
        using var session = new StoreMutationSession(_ => "save failed", () => restored = true);

        var failure = session.Commit(new LauncherStoreFile());

        Assert.Equal("save failed", failure);
        Assert.True(restored);
    }

    [Fact]
    public void Commit_SuccessDoesNotRestore()
    {
        var restored = false;
        using var session = new StoreMutationSession(_ => null, () => restored = true);

        var failure = session.Commit(new LauncherStoreFile());

        Assert.Null(failure);
        Assert.False(restored);
    }

    [Fact]
    public void DisposeWithoutCommit_Restores()
    {
        var restored = false;

        using (new StoreMutationSession(_ => null, () => restored = true))
        {
            Assert.False(restored);
        }

        Assert.True(restored);
    }

    [Fact]
    public void CommitAfterCommit_Throws()
    {
        using var session = new StoreMutationSession(_ => null, () => { });

        session.Commit(new LauncherStoreFile());

        Assert.Throws<InvalidOperationException>(() => session.Commit(new LauncherStoreFile()));
    }
}

