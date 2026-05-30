using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void Load_ReadsRootConfigShape()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
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
    public void Load_IgnoresNullEntryRowsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          null,
          {
            "id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex"
          }
        ]
        """);

        var entry = Assert.Single(LauncherConfigStore.Load(config));

        Assert.Equal("demo", entry.Id);
    }

    [Fact]
    public void Load_NormalizesNullEntryFieldsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": null,
            "name": null,
            "directory": null,
            "command": null,
            "updatedAtUtc": null
          }
        ]
        """);

        var entry = Assert.Single(LauncherConfigStore.Load(config));

        Assert.Equal(string.Empty, entry.Id);
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.Directory);
        Assert.Equal(string.Empty, entry.Command);
        Assert.Equal(string.Empty, entry.UpdatedAtUtc);
    }

    [Fact]
    public void Load_NormalizesEntryIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": " one ",
            "name": "One",
            "directory": "D:\\one",
            "command": "codex"
          }
        ]
        """);

        var entry = Assert.Single(LauncherConfigStore.Load(config));

        Assert.Equal("one", entry.Id);
    }

    [Fact]
    public void Save_RejectsNullEntryCollection()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);

        var exception = Assert.Throws<ArgumentNullException>(
            () => LauncherConfigStore.Save(null!, config, backup: false));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void Save_NormalizesEntryDataBeforeWriting()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);
        var entries = new LaunchEntry[]
        {
            null!,
            new()
            {
                Id = " one ",
                Name = null!,
                Directory = null!,
                Command = null!,
                UpdatedAtUtc = null!,
            },
        };

        LauncherConfigStore.Save(entries, config, backup: false);

        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.Equal("one", entry.Id);
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.Directory);
        Assert.Equal(string.Empty, entry.Command);
        Assert.Equal(string.Empty, entry.UpdatedAtUtc);
        var savedJson = File.ReadAllText(config);
        Assert.DoesNotContain("null", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
    }

    [Fact]
    public void Save_PersistsSelectionAndCreatesBackup()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);
        var configDir = Path.GetDirectoryName(config)!;

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
        Assert.Empty(Directory.EnumerateFiles(configDir, $"{LauncherConfigStore.ConfigFileName}.tmp-*"));
    }

    [Fact]
    public void Save_NormalizesDirtyEntriesBeforeWriting()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);

        LauncherConfigStore.Save(
            [
                null!,
                new LaunchEntry
                {
                    Id = " one ",
                    Name = null!,
                    Directory = null!,
                    Command = null!,
                    UpdatedAtUtc = null!,
                },
            ],
            config);

        var savedJson = File.ReadAllText(config);
        Assert.DoesNotContain(" one ", savedJson);
        Assert.DoesNotContain("null", savedJson);
        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.Equal("one", entry.Id);
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.Directory);
        Assert.Equal(string.Empty, entry.Command);
        Assert.Equal(string.Empty, entry.UpdatedAtUtc);
    }

    [Fact]
    public void Save_CanSkipBackupForRollbackWrites()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);
        var configDir = Path.GetDirectoryName(config)!;

        LauncherConfigStore.Save(
            [
                Entry(),
            ],
            config,
            backup: false);

        Assert.False(File.Exists(config + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, $"{LauncherConfigStore.ConfigFileName}.tmp-*"));
    }
}
