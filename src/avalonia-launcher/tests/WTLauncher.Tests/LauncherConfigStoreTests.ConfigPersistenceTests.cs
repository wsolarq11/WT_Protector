using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void Load_ReadsRootConfigShape()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        File.WriteAllText(config, """
        [
          {
            "id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex",
            "confirm": true,
            "selected": true
          }
        ]
        """);

        var entries = LauncherConfigStore.Load(config);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Name);
        Assert.Equal("D:\\demo", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.True(entry.Confirm);
        Assert.True(entry.Selected);
    }

    [Fact]
    public void Save_PersistsSelectionAndCreatesBackup()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

        LauncherConfigStore.Save(
            [
                Entry(confirm: true, selected: true),
            ],
            config);

        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.True(entry.Selected);
        var savedJson = File.ReadAllText(config);
        Assert.Contains("演示", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
        Assert.True(File.Exists(config + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, "config.json.tmp-*"));
    }

    [Fact]
    public void Save_CanSkipBackupForRollbackWrites()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        File.WriteAllText(config, "[]");

        LauncherConfigStore.Save(
            [
                Entry(),
            ],
            config,
            backup: false);

        Assert.False(File.Exists(config + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, "config.json.tmp-*"));
    }
}
