using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.LauncherPersistenceTestFixtures;

namespace WTLauncher.Tests;

public sealed class LauncherEntryMutationPersistenceTests
{
    [Fact]
    public void SaveReturnsConfigFailureBeforeLaterPersistenceSteps()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var snapshot = Snapshot();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: temp.Path,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: settingsPath,
            savePresets: true,
            rollbackSnapshot: snapshot);

        Assert.Contains("配置保存失败", saveFailure);
        Assert.False(File.Exists(presetPath));
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SaveRestoresConfigOnlyWhenPresetSaveFails()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        LauncherConfigStore.Save([Entry("one")], configPath);
        var snapshot = Snapshot();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: temp.Path,
            settings: new LauncherSettings(),
            settingsPath: settingsPath,
            savePresets: true,
            rollbackSnapshot: snapshot);

        Assert.Contains("启动组保存失败", saveFailure);
        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
    }

    [Fact]
    public void SaveRestoresConfigAndPresetsWhenSettingsSaveFails()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherConfigStore.Save([Entry("one")], configPath);
        LauncherPresetStore.Save(PresetFile("daily", Preset("daily", "one")), presetPath);
        var snapshot = Snapshot();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: temp.Path,
            savePresets: true,
            rollbackSnapshot: snapshot);

        Assert.Contains("设置保存失败", saveFailure);
        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        var preset = Assert.Single(LauncherPresetStore.Load(presetPath).Presets);
        Assert.Equal("daily", preset.Id);
        Assert.Equal(["one"], preset.EntryIds);
    }

    private static LauncherEntryMutationSnapshot Snapshot()
    {
        return LauncherEntryMutationSnapshot.Capture(
            [Entry("one")],
            PresetFile("daily", Preset("daily", "one")),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");
    }
}
