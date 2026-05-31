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
            "selected": true
          }
        ]
        """);

        var entries = LauncherConfigStore.Load(config);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Name);
        Assert.Equal("D:\\demo", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.True(entry.Selected);
    }

    [Fact]
    public void Load_IgnoresObsoleteConfirmFieldFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex",
            "confirm": false,
            "selected": true
          }
        ]
        """);

        var entry = Assert.Single(LauncherConfigStore.Load(config));

        Assert.Equal("demo", entry.Id);
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
    public void Load_NormalizesEntryTextFieldsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": "one",
            "name": " One ",
            "directory": " D:\\one ",
            "command": " codex ",
            "updatedAtUtc": " 2026-05-30T00:00:00Z "
          }
        ]
        """);

        var entry = Assert.Single(LauncherConfigStore.Load(config));

        Assert.Equal("One", entry.Name);
        Assert.Equal("D:\\one", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.Equal("2026-05-30T00:00:00Z", entry.UpdatedAtUtc);
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
        var dirtyEntry = new LaunchEntry
        {
            Id = " one ",
            Name = " One ",
            Directory = " D:\\one ",
            Command = " codex ",
            UpdatedAtUtc = " 2026-05-30T00:00:00Z ",
        };
        var entries = new LaunchEntry[] { null!, dirtyEntry };

        LauncherConfigStore.Save(entries, config, backup: false);

        Assert.Equal(" one ", dirtyEntry.Id);
        Assert.Equal(" One ", dirtyEntry.Name);
        Assert.Equal(" D:\\one ", dirtyEntry.Directory);
        Assert.Equal(" codex ", dirtyEntry.Command);
        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.Equal("one", entry.Id);
        Assert.Equal("One", entry.Name);
        Assert.Equal("D:\\one", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.Equal("2026-05-30T00:00:00Z", entry.UpdatedAtUtc);
        var savedJson = File.ReadAllText(config);
        Assert.DoesNotContain(" One ", savedJson);
        Assert.DoesNotContain(" D:\\\\one ", savedJson);
        Assert.DoesNotContain(" codex ", savedJson);
        Assert.DoesNotContain("\"confirm\"", savedJson);
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
                Entry(selected: true),
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
