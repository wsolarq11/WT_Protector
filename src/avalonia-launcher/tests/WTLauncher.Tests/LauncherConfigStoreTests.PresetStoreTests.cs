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
}
