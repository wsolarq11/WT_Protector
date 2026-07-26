using System.Text.Json;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.Tests;

/// <summary>
/// FileLauncherJsonIO tests: atomic write, backup recovery, temp cleanup, concurrency.
/// All tests use TempDirectory for hermetic file I/O.
/// </summary>
public sealed class FileLauncherJsonIOTests
{
    private static LauncherStoreFile TestStore(string id) => new()
    {
        SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
        Entries = [new LaunchEntry
        {
            Id = id, Name = "test",
            Directory = (DirectoryPath)@"C:\",
            Command = "pwsh",
        }],
        Settings = new LauncherSettings
        {
            SortMode = "custom",
            EntryViewMode = "cards",
            CustomEntryOrder = [id],
        },
        Presets = [],
        DefaultPresetId = "",
    };

    // ── Save → Load round-trip ────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        using var tmp = new TempDirectory();
        var path = Path.Combine(tmp.Path, "launcher.json");
        var io = new FileLauncherJsonIO();

        io.Save(TestStore("e1"), path, backup: true);
        var loaded = io.LoadRequired<LauncherStoreFile>(path, "test");

        Assert.Equal("e1", loaded.Entries[0].Id.ToString());
        Assert.Equal("pwsh", loaded.Entries[0].Command);
    }

    // ── Save creates .bak ──────────────────────────────────────────────

    [Fact]
    public void Save_CreatesBackup_WhenBackupEnabled()
    {
        using var tmp = new TempDirectory();
        var path = Path.Combine(tmp.Path, "launcher.json");
        var io = new FileLauncherJsonIO();

        // First save — no .bak expected
        io.Save(TestStore("e1"), path, backup: true);
        Assert.False(File.Exists(path + ".bak"));

        // Second save — .bak should now exist
        io.Save(TestStore("e2"), path, backup: true);
        Assert.True(File.Exists(path + ".bak"));

        // .bak contains the previous state
        var bakContent = File.ReadAllText(path + ".bak");
        Assert.Contains("e1", bakContent);
    }

    // ── LoadWithRecovery: backup fallback on corrupt file ──────────────

    [Fact]
    public void LoadWithRecovery_FallsBackToBackup_WhenPrimaryCorrupt()
    {
        using var tmp = new TempDirectory();
        var path = Path.Combine(tmp.Path, "launcher.json");
        var io = new FileLauncherJsonIO();

        io.Save(TestStore("e1"), path, backup: true);
        // Second save creates .bak
        io.Save(TestStore("e2"), path, backup: true);
        // Corrupt primary
        File.WriteAllText(path, "not-json{");

        var recovered = io.LoadWithRecovery(path,
            p => JsonSerializer.Deserialize<LauncherStoreFile>(
                File.ReadAllText(p),
                LauncherJsonContext.Default.LauncherStoreFile)!,
            "test-recovery",
            new LauncherStoreFile());

        Assert.NotNull(recovered);
        Assert.Single(recovered.Entries); // from backup (e1 or e2)
    }

    // ── CleanupStaleTempFiles removes orphan temp files ───────────────

    [Fact]
    public void CleanupStaleTempFiles_RemovesOrphanTempFiles()
    {
        using var tmp = new TempDirectory();
        var io = new FileLauncherJsonIO();

        // Create stale temp files matching the expected pattern
        var stale1 = Path.Combine(tmp.Path, $"config.tmp-{Guid.NewGuid():N}");
        var stale2 = Path.Combine(tmp.Path, $"config.bak-tmp-{Guid.NewGuid():N}");
        File.WriteAllText(stale1, "stale");
        File.WriteAllText(stale2, "stale");

        // Create a non-temp file that should NOT be cleaned
        var keep = Path.Combine(tmp.Path, "launcher.json");
        File.WriteAllText(keep, "{}");

        io.CleanupStaleTempFiles(tmp.Path);

        Assert.False(File.Exists(stale1), "tmp-* files should be cleaned");
        Assert.False(File.Exists(stale2), "bak-tmp-* files should be cleaned");
        Assert.True(File.Exists(keep), "non-temp files should be preserved");
    }

    // ── Disposed IO doesn't leak ──────────────────────────────────────

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var io = new FileLauncherJsonIO();
        io.Dispose();
        io.Dispose(); // Double dispose — must not throw
    }
}
