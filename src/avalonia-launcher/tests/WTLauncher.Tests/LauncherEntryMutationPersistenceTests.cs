using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryMutationPersistenceTests
{
    [Fact]
    public void SaveReturnsConfigFailureBeforeLaterPersistenceSteps()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var beforeSettingsSaveCalled = false;

        var result = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: temp.Path,
            presetFile: Presets("mutated", "mutated"),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: settingsPath,
            savePresets: true,
            rollbackSnapshot: null,
            beforeSaveSettings: () => beforeSettingsSaveCalled = true);

        Assert.False(result.Succeeded);
        Assert.Contains("配置保存失败", result.StatusText);
        Assert.False(beforeSettingsSaveCalled);
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
        var beforeSettingsSaveCalled = false;

        var result = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: Presets("mutated", "mutated"),
            presetPath: temp.Path,
            settings: new LauncherSettings(),
            settingsPath: settingsPath,
            savePresets: true,
            rollbackSnapshot: snapshot,
            beforeSaveSettings: () => beforeSettingsSaveCalled = true);

        Assert.False(result.Succeeded);
        Assert.Contains("启动组保存失败", result.StatusText);
        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        Assert.False(beforeSettingsSaveCalled);
    }

    [Fact]
    public void SaveRestoresConfigAndPresetsWhenSettingsSaveFails()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherConfigStore.Save([Entry("one")], configPath);
        LauncherPresetStore.Save(Presets("daily", "one"), presetPath);
        var snapshot = Snapshot();
        var beforeSettingsSaveCalled = false;

        var result = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: Presets("mutated", "mutated"),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: temp.Path,
            savePresets: true,
            rollbackSnapshot: snapshot,
            beforeSaveSettings: () => beforeSettingsSaveCalled = true);

        Assert.False(result.Succeeded);
        Assert.Contains("设置保存失败", result.StatusText);
        Assert.True(beforeSettingsSaveCalled);
        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        var preset = Assert.Single(LauncherPresetStore.Load(presetPath).Presets);
        Assert.Equal("daily", preset.Id);
        Assert.Equal(["one"], preset.EntryIds);
    }

    private static LauncherEntryMutationSnapshot Snapshot()
    {
        return LauncherEntryMutationSnapshot.Capture(
            [Entry("one")],
            Presets("daily", "one"),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");
    }

    private static LaunchEntry Entry(string id)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
        };
    }

    private static LauncherPresetFile Presets(string presetId, string entryId)
    {
        return new LauncherPresetFile
        {
            DefaultPresetId = presetId,
            Presets =
            [
                new LauncherPreset
                {
                    Id = presetId,
                    Name = presetId,
                    EntryIds = [entryId],
                },
            ],
        };
    }
}
