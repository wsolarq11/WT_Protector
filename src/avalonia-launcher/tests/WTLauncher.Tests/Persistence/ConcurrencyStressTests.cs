using ErrorOr;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

/// <summary>
/// Concurrency stress tests: StoreGrain concurrent Dispatch, ErrorLog concurrent writes.
/// Uses Parallel.For for stress testing.
/// </summary>
[Trait("Category", TestCategories.Persistence)]
public sealed class ConcurrencyStressTests
{
    // ── StoreGrain concurrent Dispatch ────────────────────────────────

    [Fact]
    public void StoreGrain_ConcurrentDispatch_MaintainsConsistency()
    {
        using var tmp = new TempDirectory();
        var path = Path.Combine(tmp.Path, "launcher.json");
        var storeFile = new LauncherStoreFile
        {
            SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
            Entries = [],
            Settings = new LauncherSettings(),
            Presets = [],
            DefaultPresetId = "",
        };
        LauncherStore.Save(storeFile, path);
        using var grain = new StoreGrain(path, new FileStorePersistence());

        // Fire 20 concurrent Dispatch calls
        int[] operations = [1, 2, 3, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1];
        Parallel.For(0, operations.Length, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
        {
            var op = operations[i];
            grain.Dispatch(state => op switch
            {
                0 => state with { DefaultPresetId = $"p-{Guid.NewGuid():N}" },
                1 => state with { Settings = state.Settings with { SortMode = "alphabetical" } },
                _ => state with { Settings = state.Settings with { RequireConfirmationForLaunches = !state.Settings.RequireConfirmationForLaunches } },
            });
        });

        var final = grain.Select(s => s);
        Assert.NotNull(final.Settings);
        Assert.NotNull(final.Entries);
    }

    [Fact]
    public void StoreGrain_ReloadFromDisk_SerializesWithDispatch_NoOverwrite()
    {
        var loadStarted = new ManualResetEventSlim(false);
        var releaseLoad = new ManualResetEventSlim(false);
        var freshState = NewStoreFile("fresh");
        var persistence = new BlockingLoadPersistence(freshState, loadStarted, releaseLoad);

        using var grain = new StoreGrain("unused.json", NewStoreFile("initial"), persistence);

        Exception? reloadError = null;
        var reloadThread = new Thread(() =>
        {
            try { grain.ReloadFromDisk(); }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) { reloadError = ex; }
        });
        reloadThread.Start();

        Assert.True(loadStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "Load should start and block under the lock.");

        Exception? dispatchError = null;
        var dispatchThread = new Thread(() =>
        {
            try { grain.Dispatch(s => s with { DefaultPresetId = "dispatched" }); }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) { dispatchError = ex; }
        });
        dispatchThread.Start();

        // Give Dispatch a moment to contend for the lock, then release the slow Load.
        Thread.Sleep(50);
        releaseLoad.Set();

        Assert.True(reloadThread.Join(TimeSpan.FromSeconds(5)), "Reload should complete.");
        Assert.True(dispatchThread.Join(TimeSpan.FromSeconds(5)), "Dispatch should complete.");
        Assert.Null(reloadError);
        Assert.Null(dispatchError);

        // Dispatch's write must survive the reload. A TOCTOU (Load outside the lock)
        // would let the stale reload overwrite "dispatched" back to "fresh".
        Assert.Equal("dispatched", grain.Select(s => s.DefaultPresetId));
    }

    // ── ErrorLog concurrent write never throws ────────────────────────

    [Fact]
    public void ErrorLog_ConcurrentWrite_NeverThrows()
    {
        using var tmp = new TempDirectory();
        var logDir = Path.Combine(tmp.Path, "logs");
        var logPath = Path.Combine(logDir, "errors.log");

        // 100 parallel writes, none should throw
        var exceptions = new List<Exception>();
        var parallelOpt = new ParallelOptions { MaxDegreeOfParallelism = 8 };
        Parallel.For(0, 100, parallelOpt, i =>
        {
            try
            {
                ErrorLog.Write(new InvalidOperationException($"thread-{i}"), "Stress", logDir, logPath);
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        });

        Assert.Empty(exceptions);
        Assert.True(File.Exists(logPath));
        var lineCount = File.ReadAllLines(logPath).Length;
        Assert.True(lineCount > 0, "At least one log entry should exist");
    }

    private static LauncherStoreFile NewStoreFile(string defaultPresetId) => new()
    {
        SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
        Entries = [],
        Settings = new LauncherSettings(),
        Presets = [],
        DefaultPresetId = defaultPresetId,
    };

    private sealed class BlockingLoadPersistence : IStorePersistence
    {
        private readonly LauncherStoreFile _freshState;
        private readonly ManualResetEventSlim _loadStarted;
        private readonly ManualResetEventSlim _releaseLoad;

        public BlockingLoadPersistence(
            LauncherStoreFile freshState,
            ManualResetEventSlim loadStarted,
            ManualResetEventSlim releaseLoad)
        {
            _freshState = freshState;
            _loadStarted = loadStarted;
            _releaseLoad = releaseLoad;
        }

        public LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null)
        {
            _loadStarted.Set();
            _releaseLoad.Wait(TimeSpan.FromSeconds(5));
            return _freshState;
        }

        public void Save(LauncherStoreFile store, string storePath, bool backup = true) { }

        public ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath) =>
            throw new NotSupportedException();

        public string ResolveStorePath(
            string? explicitPath,
            string? currentDirectory = null,
            string? appBaseDirectory = null) =>
            explicitPath ?? string.Empty;
    }
}
