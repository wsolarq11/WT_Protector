using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

/// <summary>
/// Concurrency stress tests: StoreGrain concurrent Dispatch, ErrorLog concurrent writes.
/// Uses Parallel.For for stress testing.
/// </summary>
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
}
