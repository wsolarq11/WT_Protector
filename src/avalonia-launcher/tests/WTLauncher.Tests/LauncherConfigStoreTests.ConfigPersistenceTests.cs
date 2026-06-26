using System.Text.Json;
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
    public void Load_ReturnsReadOnlyEntryCollectionSnapshot()
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

        var list = Assert.IsAssignableFrom<IList<LaunchEntry>>(entries);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(new LaunchEntry()));
        Assert.Throws<NotSupportedException>(() => list[0] = new LaunchEntry());
        Assert.Equal(["demo"], entries.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void Load_SilentlySkipsObsoleteConfirmFieldFromConfig()
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

        var entries = LauncherConfigStore.Load(config);

        // Obsolete "confirm" field is silently skipped (forward-compatible).
        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Id);
        Assert.True(entry.Selected);
    }

    [Fact]
    public void Load_RejectsWrongCaseEntryIdFieldFromConfig()
    {
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "Id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex",
            "selected": true
          }
        ]
        """);

        // Wrong-case "Id" is silently skipped by UnmappedMemberHandling.Skip,
        // but "id" is a required field ($id metadata in LaunchEntry JSON).
        // System.Text.Json is case-sensitive, so "Id" does not bind to "id".
        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherConfigStore.Load(config));

        Assert.Contains("id", exception.Message);
    }

    [Fact]
    public void Load_AcceptsMissingSelectedFieldAndDefaultsToFalse()
    {
        // Since LaunchEntry.Selected is bool (non-nullable value type), a missing
        // "selected" field in the JSON defaults to false after deserialization.
        // This is standard JSON semantics — there is no reason to reject a config
        // file that omits an optional boolean field. The double-JsonDocument pass
        // (R3.10) used to reject this unnecessarily.
        using var temp = new TempDirectory();
        var config = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.ConfigFileName, """
        [
          {
            "id": "demo",
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex"
          }
        ]
        """);

        var entries = LauncherConfigStore.Load(config);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Id);
        Assert.False(entry.Selected);
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
            "command": "codex",
            "selected": true
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
            "selected": true,
            "updatedAtUtc": null
          }
        ]
        """);

        var exception = Assert.Throws<ArgumentException>(() => LauncherConfigStore.Load(config));

        Assert.Equal("Entry id is required. (Parameter 'id')", exception.Message);
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
            "command": "codex",
            "selected": true
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
            "command": "codex",
            "selected": true
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
            "command": "codex",
            "selected": true
          },
          {
            "id": " one ",
            "name": "Duplicate",
            "directory": "D:\\duplicate",
            "command": "pwsh",
            "selected": false
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
            "selected": true,
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

    [Fact]
    public void Load_RecoversFromBackupWhenConfigFileIsCorruptJson()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);

        var backupPath = config + ".bak";
        File.WriteAllText(backupPath, """
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

        // Truncated JSON (missing closing bracket) — triggers JsonException
        // regardless of UnmappedMemberHandling mode.
        File.WriteAllText(config, """
        [
          {
            "id": "x",
            "name": "x",
            "directory": "D:\\x",
            "command": "x",
            "selected": true
        """);

        var entries = LauncherConfigStore.Load(config);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Id);
    }

    [Fact]
    public void Load_ThrowsWhenConfigFileIsCorruptAndNoBackupExists()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);

        File.WriteAllText(config, "this is not JSON {{{{");

        Assert.ThrowsAny<JsonException>(() => LauncherConfigStore.Load(config));
    }

    [Fact]
    public void Load_ThrowsWhenBothMainAndBackupAreCorrupt()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);

        // Both main and .bak are invalid JSON — the recovery should fail.
        File.WriteAllText(config, "truncated [{");
        File.WriteAllText(config + ".bak", "also corrupt [{");

        // The .bak recovery attempt overwrites main, then LoadAndValidate
        // throws again. The exception must propagate (no infinite loop).
        Assert.ThrowsAny<JsonException>(() => LauncherConfigStore.Load(config));
    }

    [Fact]
    public void Load_ThrowsWhenMainMissingAndBackupIsCorrupt()
    {
        using var temp = new TempDirectory();
        var config = ConfigFilePath(temp.Path, LauncherConfigStore.ConfigFileName);

        // Main file does not exist, but .bak is corrupt.
        File.WriteAllText(config + ".bak", "garbage");

        // The recovery from .bak copies garbage over, then LoadAndValidate
        // throws. The exception must propagate.
        Assert.ThrowsAny<JsonException>(() => LauncherConfigStore.Load(config));
    }
}
