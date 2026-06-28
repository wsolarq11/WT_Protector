using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void PresetStore_RoundTripsHumanReadableLaunchGroups()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        LauncherPresetStore.Save(
            PresetFile("日常启动", "one", "two"),
            presetPath,
            backup: true);

        var loaded = LauncherPresetStore.Load(presetPath);

        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Equal("daily", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("日常启动", preset.Name);
        Assert.Equal(["one", "two"], preset.EntryIds);
        var savedJson = File.ReadAllText(presetPath);
        Assert.Contains("\"presets\"", savedJson);
        Assert.Contains("日常启动", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
    }

    [Fact]
    public void PresetStore_SilentlySkipsUnknownPresetFieldsFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"],
              "shortcut": "Ctrl+1"
            }
          ]
        }
        """);

        var presets = LauncherPresetStore.Load(presetPath);

        // Unknown field "shortcut" is silently skipped (forward-compatible).
        Assert.Equal("daily", presets.DefaultPresetId);
        var preset = Assert.Single(presets.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void PresetStore_RejectsMissingEntryIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily"
            }
          ]
        }
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherPresetStore.Load(presetPath));

        Assert.Contains("entryIds", exception.Message);
    }

    [Fact]
    public void PresetStore_RejectsMissingSchemaVersionFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "defaultPresetId": "daily",
          "presets": []
        }
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherPresetStore.Load(presetPath));

        Assert.Contains("schemaVersion", exception.Message);
    }

    [Fact]
    public void PresetStore_ReturnsDefaultsForMissingPresetFile()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        var presetFile = LauncherPresetStore.Load(presetPath);

        Assert.Equal(1, presetFile.SchemaVersion);
        Assert.Equal("", presetFile.DefaultPresetId);
        Assert.Empty(presetFile.Presets);
    }

    [Fact]
    public void PresetStore_RejectsInvalidPresetRowsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherPresetStore.Save(
                new LauncherPresetFile
                {
                    DefaultPresetId = "daily",
                    Presets =
                    [
                        new LauncherPreset
                        {
                            Id = "daily",
                            Name = null!,
                            EntryIds = ["one"],
                        },
                    ],
                },
                presetPath,
                backup: true));

        Assert.Equal("Launcher preset 0 name is empty.", exception.Message);
        Assert.False(File.Exists(presetPath));
    }

    [Fact]
    public void PresetStore_RejectsNullPresetRowsFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "schemaVersion": 1,
          "defaultPresetId": null,
          "presets": [
            null
          ]
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherPresetStore.Load(presetPath));

        Assert.Equal("Launcher preset 0 is null.", exception.Message);
    }

    [Fact]
    public void PresetStore_RejectsNullPresetDocument()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, "null");

        var exception = Assert.Throws<InvalidDataException>(() => LauncherPresetStore.Load(presetPath));

        Assert.Equal("Launcher presets parsed to null.", exception.Message);
    }

    [Fact]
    public void PresetStore_RejectsMissingDefaultPresetFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "missing",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"]
            }
          ]
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherPresetStore.Load(presetPath));

        Assert.Equal("Launcher preset default id does not match any preset: missing.", exception.Message);
    }

    [Fact]
    public void PresetStore_RejectsNullPresetDataFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "schemaVersion": 1,
          "defaultPresetId": null,
          "presets": [
            {
              "id": null,
              "name": null,
              "entryIds": [null, "one"]
            }
          ]
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherPresetStore.Load(presetPath));

        Assert.Equal("Launcher preset 0 id is empty.", exception.Message);
    }

    [Fact]
    public void PresetStore_TrimsPresetAndEntryIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """
        {
          "schemaVersion": 1,
          "defaultPresetId": " Daily ",
          "presets": [
            {
              "id": " Daily ",
              "name": " Daily ",
              "entryIds": [" one ", "two "]
            }
          ]
        }
        """);

        var loaded = LauncherPresetStore.Load(presetPath);

        Assert.Equal("daily", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("daily", preset.Id);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one", "two"], preset.EntryIds);
    }

    [Fact]
    public void PresetStore_RejectsNullPresetFileOnSave()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        var exception = Assert.Throws<ArgumentNullException>(
            () => LauncherPresetStore.Save(null!, presetPath, backup: false));

        Assert.Equal("presets", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void PresetStore_RejectsBlankPresetPath(string? presetPath)
    {
        AssertPresetPathRequired(() => LauncherPresetStore.Load(presetPath!));
        AssertPresetPathRequired(() => LauncherPresetStore.Save(new LauncherPresetFile(), presetPath!, backup: false));
    }

    [Fact]
    public void PresetStore_TrimsPresetDataBeforeWriting()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);
        var dirtyPreset = new LauncherPreset
        {
            Id = " Daily ",
            Name = " Daily ",
            EntryIds = [" one ", "two "],
        };
        var presets = new LauncherPresetFile
        {
            DefaultPresetId = " Daily ",
            Presets = [dirtyPreset],
        };

        LauncherPresetStore.Save(presets, presetPath, backup: false);

        Assert.Equal(" Daily ", presets.DefaultPresetId);
        Assert.Single(presets.Presets);
        Assert.Equal(" Daily ", dirtyPreset.Id);
        Assert.Equal(" Daily ", dirtyPreset.Name);
        Assert.Equal([" one ", "two "], dirtyPreset.EntryIds);
        var loaded = LauncherPresetStore.Load(presetPath);
        Assert.Equal("daily", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("daily", preset.Id);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one", "two"], preset.EntryIds);
        var savedJson = File.ReadAllText(presetPath);
        Assert.DoesNotContain("null", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
    }

    [Fact]
    public void PresetStore_RejectsNullPresetRowsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherPresetStore.Save(
                new LauncherPresetFile { Presets = [null!] },
                presetPath,
                backup: false));

        Assert.Equal("Launcher preset 0 is null.", exception.Message);
        Assert.False(File.Exists(presetPath));
    }

    [Fact]
    public void PresetStore_BackupContentMatchesOriginal()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        // Write initial file so backup:true has something to back up.
        LauncherPresetStore.Save(
            PresetFile("初始组", "zero"),
            presetPath,
            backup: false);

        var initialText = File.ReadAllText(presetPath);

        LauncherPresetStore.Save(
            PresetFile("日常启动", "one", "two"),
            presetPath,
            backup: true);

        var backupPath = presetPath + ".bak";
        Assert.True(File.Exists(backupPath), "backup file should exist after save with backup: true");

        var backupText = File.ReadAllText(backupPath);
        Assert.Equal(initialText, backupText);

        var loaded = LauncherPresetStore.Load(backupPath);
        Assert.Equal("daily", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("初始组", preset.Name);
        Assert.Equal(["zero"], preset.EntryIds);
    }

    [Fact]
    public void PresetStore_CanSkipBackupForRollbackWrites()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(
            temp.Path,
            LauncherPresetStore.PresetsFileName,
            """{"schemaVersion":1,"defaultPresetId":"","presets":[]}""");
        var configDir = Path.GetDirectoryName(presetPath)!;

        LauncherPresetStore.Save(
            PresetFile("Daily", "one"),
            presetPath,
            backup: false);

        Assert.False(File.Exists(presetPath + ".bak"));
        Assert.Empty(Directory.EnumerateFiles(configDir, $"{LauncherPresetStore.PresetsFileName}.tmp-*"));
    }

    // ponytail: ResolvePresetPath tests removed — presets now live in the unified
    // LauncherStoreFile alongside entries and settings.

    private static void AssertPresetPathRequired(Action action)
    {
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("presetPath", exception.ParamName);
    }

    [Fact]
    public void PresetStore_RecoversFromBackupWhenPresetFileIsCorruptJson()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        var backupPath = presetPath + ".bak";
        File.WriteAllText(backupPath, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"]
            }
          ]
        }
        """);

        // Valid JSON with an unknown field — triggers JsonException via
        // LauncherJson.Options.UnmappedMemberHandling.Disallow.
        File.WriteAllText(presetPath, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"],
              "unknownField": 1
            }
          ]
        }
        """);

        var loaded = LauncherPresetStore.Load(presetPath);

        Assert.Equal("daily", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void PresetStore_ThrowsWhenPresetFileIsCorruptAndNoBackupExists()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);

        File.WriteAllText(presetPath, "corrupt garbage {{{");

        Assert.ThrowsAny<JsonException>(() => LauncherPresetStore.Load(presetPath));
    }
}
