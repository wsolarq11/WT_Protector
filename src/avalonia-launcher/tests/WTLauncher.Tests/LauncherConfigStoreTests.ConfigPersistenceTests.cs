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
    public void Load_RejectsObsoleteConfirmFieldFromConfig()
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

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherConfigStore.Load(config));

        Assert.Contains("confirm", exception.Message);
    }

    [Fact]
    public void Load_RejectsWrongCaseEntryFieldsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "Id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex"
          }
        ]
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherConfigStore.Load(config));

        Assert.Contains("Id", exception.Message);
    }

    [Fact]
    public void Load_RejectsNullEntryRowsFromConfig()
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

        var exception = Assert.Throws<InvalidDataException>(() => LauncherConfigStore.Load(config));

        Assert.Equal("Launcher config entry 0 is null.", exception.Message);
    }

    [Fact]
    public void Load_RejectsNullConfigDocument()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, "null");

        var exception = Assert.Throws<InvalidDataException>(() => LauncherConfigStore.Load(config));

        Assert.Equal("Launcher config parsed to null.", exception.Message);
    }

    [Fact]
    public void Load_RejectsNullEntryFieldsFromConfig()
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

        var exception = Assert.Throws<InvalidDataException>(() => LauncherConfigStore.Load(config));

        Assert.Equal("Launcher config entry 0 id is null.", exception.Message);
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
    public void Load_RejectsBlankEntryFieldsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": "one",
            "name": " ",
            "directory": "D:\\one",
            "command": "codex"
          }
        ]
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherConfigStore.Load(config));

        Assert.Equal("Launcher config entry 0 name is empty.", exception.Message);
    }

    [Fact]
    public void Load_RejectsDuplicateEntryIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": "one",
            "name": "One",
            "directory": "D:\\one",
            "command": "codex"
          },
          {
            "id": " one ",
            "name": "Duplicate",
            "directory": "D:\\duplicate",
            "command": "pwsh"
          }
        ]
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherConfigStore.Load(config));

        Assert.Equal("Launcher config entry 1 id is duplicate: one.", exception.Message);
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
        var entries = new[] { dirtyEntry };

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
    public void Save_RejectsNullEntryRows()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);

        var exception = Assert.Throws<InvalidDataException>(
            () => LauncherConfigStore.Save([null!], config, backup: false));

        Assert.Equal("Launcher config entry 0 is null.", exception.Message);
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
            config,
            backup: true);

        var entry = Assert.Single(LauncherConfigStore.Load(config));
        Assert.True(entry.Selected);
        var savedJson = File.ReadAllText(config);
        Assert.Contains("演示", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
        Assert.True(File.Exists(config + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, $"{LauncherConfigStore.ConfigFileName}.tmp-*"));
    }

    [Fact]
    public void Save_RejectsNullEntryFieldsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);

        var exception = Assert.Throws<InvalidDataException>(
            () => LauncherConfigStore.Save(
                [
                    new LaunchEntry
                    {
                        Id = " one ",
                        Name = null!,
                        Directory = null!,
                        Command = null!,
                        UpdatedAtUtc = null!,
                    },
                ],
                config,
                backup: true));

        Assert.Equal("Launcher config entry 0 name is null.", exception.Message);
        Assert.Equal("[]", File.ReadAllText(config));
    }

    [Fact]
    public void Save_RejectsDuplicateEntryIdsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var config = WriteEmptyLauncherConfig(temp.Path);

        var exception = Assert.Throws<InvalidDataException>(
            () => LauncherConfigStore.Save(
                [
                    Entry(),
                    new LaunchEntry
                    {
                        Id = " demo ",
                        Name = "Duplicate",
                        Directory = "D:\\duplicate",
                        Command = "pwsh",
                    },
                ],
                config,
                backup: false));

        Assert.Equal("Launcher config entry 1 id is duplicate: demo.", exception.Message);
        Assert.Equal("[]", File.ReadAllText(config));
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
