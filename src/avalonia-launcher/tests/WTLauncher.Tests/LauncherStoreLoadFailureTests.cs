using System.IO;
using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

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
    public void MissingStoreFile_Load_ReturnsDefaultStoreFile()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, LauncherStore.ConfigDirectoryName);
        Directory.CreateDirectory(configDir);
        var path = Path.Combine(configDir, LauncherStore.StoreFileName);

        var result = LauncherStore.Load(path);

        Assert.NotNull(result);
        Assert.Equal(LauncherStore.ExpectedStoreSchemaVersion, result.SchemaVersion);
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
}
