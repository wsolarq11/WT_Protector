using System.IO;
using System.Text.Json;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

[Trait("Category", TestCategories.Persistence)]
public sealed class LauncherStoreLoadFailureTests
{
    [Fact]
    public void CorruptJson_Load_ThrowsJsonException()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, "{invalid json}");

        Assert.Throws<JsonException>(() => LauncherJson.Load<LauncherStoreFile>(path));
    }

    [Fact]
    public void UnknownJsonKey_Load_LooseReadSkipsUnknownKey()
    {
        // Loose Read: unknown JSON keys are silently skipped for forward compat.
        // Write-time source-gen guarantees only known fields are serialized,
        // catching developer mistakes at write time, not at read time.
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path,
            "{\"schemaVersion\":2," +
            "\"settings\":{\"lastFilterText\":\"\",\"sortMode\":\"custom\",\"entryViewMode\":\"cards\"," +
            "\"requireConfirmationForLaunches\":false,\"customEntryOrder\":[]," +
            "\"commandTemplates\":{},\"searchHistory\":[],\"window\":{\"width\":1180,\"height\":760}}," +
            "\"entries\":[],\"presets\":[],\"defaultPresetId\":\"\"," +
            "\"unknownTopLevelKey\":42}");

        var store = LauncherJson.Load<LauncherStoreFile>(path);
        Assert.NotNull(store);
        Assert.Equal(2, store.SchemaVersion);
    }

    [Fact]
    public void MissingStoreFile_Load_ReturnsDefaultStoreFile()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, LauncherStore.ConfigDirectoryName);
        Directory.CreateDirectory(configDir);
        var path = Path.Combine(configDir, LauncherStore.StoreFileName);

        var result = LauncherStore.Load(path);

        Assert.NotNull(result);
        Assert.Equal(LauncherStoreValidator.ExpectedSchemaVersion, result.SchemaVersion);
        Assert.Empty(result.Entries);
        Assert.Empty(result.Presets);
    }

    [Fact]
    public void EmptyFile_Load_ThrowsJsonException()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, "");

        Assert.Throws<JsonException>(() =>
            LauncherJson.LoadRequired<LauncherStoreFile>(path, "test document"));
    }

    [Fact]
    public void CorruptFileWithValidBackup_LoadWithRecovery_RecoversFromBackup()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        var backupPath = path + ".bak";

        var store = new LauncherStoreFile
        {
            Entries = [LauncherPersistenceTestFixtures.Entry("recovered-entry")],
        };
        LauncherJson.Save(store, backupPath, backup: false);
        File.WriteAllText(path, "{invalid json}");

        LauncherStoreFile LoadAndValidate(string p) =>
            LauncherJson.LoadRequired<LauncherStoreFile>(p, "store");

        var result = LauncherJson.LoadWithRecovery(
            path, LoadAndValidate, "test", new LauncherStoreFile());

        Assert.NotNull(result);
        Assert.NotEmpty(result.Entries);
        Assert.Equal("recovered-entry", result.Entries[0].Id);
    }

    [Fact]
    public void FileLockedByAnotherProcess_Load_ThrowsIOException()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, "{}");

        using var lockStream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.Throws<IOException>(() => LauncherJson.Load<LauncherStoreFile>(path));
    }

    [Fact]
    public void FileExceeds50MB_Load_ThrowsInvalidDataException()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, LauncherStore.ConfigDirectoryName);
        Directory.CreateDirectory(configDir);
        var path = Path.Combine(configDir, LauncherStore.StoreFileName);

        using (var fs = new FileStream(path, FileMode.CreateNew))
        {
            fs.SetLength(51 * 1024 * 1024);
        }

        var ex = Assert.Throws<InvalidDataException>(() => LauncherStore.Load(path));
        Assert.Contains("50 MB", ex.Message);
    }

    [Fact]
    public void LauncherSettings_NoSchemaVersionField_SingleSourceOfTruth()
    {
        // ponytail: SchemaVersion lives only at LauncherStoreFile top level.
        // Settings had a dead SchemaVersion=1 field removed 2026-07-09 (task 07-09-schema-first-persistence).
        // This test pins the removal — re-adding it would create dual-source drift (pitfalls §6.1).
        var settings = new LauncherSettings();
        var json = JsonSerializer.Serialize(settings, LauncherJson.Options);
        var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("schemaVersion", out _),
            "LauncherSettings must NOT serialize schemaVersion — single source is LauncherStoreFile.SchemaVersion.");
    }

    [Fact]
    public void LauncherSettings_LegacySchemaVersionInJson_LooseReadSkipsUnknownKey()
    {
        // Loose Read: deprecated settings.schemaVersion is silently skipped.
        // Single source of truth is LauncherStoreFile.SchemaVersion.
        // Write-time source-gen removes stale keys on next save.
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path,
            "{\"schemaVersion\":2,\"lastFilterText\":\"legacy\",\"sortMode\":\"custom\"," +
            "\"entryViewMode\":\"cards\",\"requireConfirmationForLaunches\":true," +
            "\"customEntryOrder\":[],\"commandTemplates\":{},\"searchHistory\":[]," +
            "\"window\":{\"width\":1180,\"height\":760}}");

        var settings = LauncherJson.LoadRequired<LauncherSettings>(path, "legacy settings");
        Assert.Equal("legacy", settings.LastFilterText);
    }
}
