using System.Collections.Immutable;
using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public class MigrationToolTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "wt-migration-test-" + Guid.NewGuid().ToString("N"));

    public MigrationToolTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void MigrateIfNeeded_NoDbAndNoJson_ReturnsOk()
    {
        var result = MigrationTool.MigrateIfNeeded(_tempDir);
        Assert.True(result.IsOk);
    }

    [Fact]
    public void MigrateIfNeeded_DbExists_ReturnsOk()
    {
        File.WriteAllText(Path.Combine(_tempDir, "store.db"), "");
        var result = MigrationTool.MigrateIfNeeded(_tempDir);
        Assert.True(result.IsOk);
    }

    [Fact]
    public void MigrateIfNeeded_JsonExistsNoDb_MigratesAndRenamesJson()
    {
        var jsonPath = Path.Combine(_tempDir, LauncherStore.StoreFileName);
        var entry = new LaunchEntry { Id = "test", Name = "Test", Directory = ".", Command = "cmd" };
        var entries = new List<LaunchEntry> { entry }.ToImmutableList();
        var storeFile = new LauncherStoreFile { Entries = entries };
        var json = JsonSerializer.Serialize(storeFile);
        File.WriteAllText(jsonPath, json);

        var result = MigrationTool.MigrateIfNeeded(_tempDir);
        Assert.True(result.IsOk);

        Assert.False(File.Exists(jsonPath));
        Assert.True(File.Exists(jsonPath + ".bak"));
        Assert.True(File.Exists(Path.Combine(_tempDir, "store.db")));
    }
}
