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
            presetPath);

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
    public void PresetStore_NormalizesDirtyPresetsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var presetPath = WriteConfigDirectoryFile(temp.Path, LauncherPresetStore.PresetsFileName, """{"schemaVersion":1,"defaultPresetId":"","presets":[]}""");

        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = " Daily ",
                Presets =
                [
                    new LauncherPreset
                    {
                        Id = " Daily ",
                        Name = null!,
                        EntryIds = [" one ", null!, "one", " ", "two", " two "],
                    },
                    new LauncherPreset
                    {
                        Id = " Daily ",
                        Name = " Second ",
                        EntryIds = ["two", " three "],
                    },
                ],
            },
            presetPath);

        var savedJson = File.ReadAllText(presetPath);
        Assert.DoesNotContain(" Daily ", savedJson);
        Assert.DoesNotContain("null", savedJson);
        Assert.Contains("daily-2", savedJson);
        var loaded = LauncherPresetStore.Load(presetPath);
        Assert.Equal("daily", loaded.DefaultPresetId);
        Assert.Equal(2, loaded.Presets.Count);
        Assert.Equal("daily", loaded.Presets[0].Id);
        Assert.Equal(string.Empty, loaded.Presets[0].Name);
        Assert.Equal(["one", "two"], loaded.Presets[0].EntryIds);
        Assert.Equal("daily-2", loaded.Presets[1].Id);
        Assert.Equal("Second", loaded.Presets[1].Name);
        Assert.Equal(["two", "three"], loaded.Presets[1].EntryIds);
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
    public void PresetStore_NormalizesNullPresetDataFromConfig()
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

        var loaded = LauncherPresetStore.Load(presetPath);

        Assert.Equal("preset", loaded.DefaultPresetId);
        var preset = Assert.Single(loaded.Presets);
        Assert.Equal("preset", preset.Id);
        Assert.Equal(string.Empty, preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void PresetStore_NormalizesPresetAndEntryIdsFromConfig()
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
              "entryIds": [" one ", "one", " ", "two", " two "]
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
    public void PresetStore_NormalizesPresetDataBeforeWriting()
    {
        using var temp = new TempDirectory();
        var presetPath = ConfigFilePath(temp.Path, LauncherPresetStore.PresetsFileName);
        var dirtyPreset = new LauncherPreset
        {
            Id = " Daily ",
            Name = " Daily ",
            EntryIds = [null!, " one ", "one", " ", "two", " two "],
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
        Assert.Equal([null!, " one ", "one", " ", "two", " two "], dirtyPreset.EntryIds);
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

    private static void AssertPresetPathRequired(Action action)
    {
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("presetPath", exception.ParamName);
    }
}
