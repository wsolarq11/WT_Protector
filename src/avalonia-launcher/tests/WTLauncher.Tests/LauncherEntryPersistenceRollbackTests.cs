using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryPersistenceRollbackTests
{
    [Fact]
    public void RestoreWritesConfigAndPresetsWithoutChangingStatusOnSuccess()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [Entry("one", selected: true)],
            Presets("daily", "one"),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");
        LauncherConfigStore.Save([Entry("mutated", selected: false)], configPath);
        LauncherPresetStore.Save(Presets("mutated", "mutated"), presetPath);

        var status = LauncherEntryPersistenceRollback.Restore(
            snapshot,
            configPath,
            presetPath,
            restorePresets: true,
            currentStatus: "设置保存失败: demo");

        Assert.Equal("设置保存失败: demo", status);
        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        var preset = Assert.Single(LauncherPresetStore.Load(presetPath).Presets);
        Assert.Equal("daily", preset.Id);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void RestoreCanSkipPresetFile()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [Entry("one", selected: true)],
            Presets("daily", "one"),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");
        LauncherConfigStore.Save([Entry("mutated", selected: false)], configPath);
        LauncherPresetStore.Save(Presets("mutated", "mutated"), presetPath);

        LauncherEntryPersistenceRollback.Restore(
            snapshot,
            configPath,
            presetPath,
            restorePresets: false,
            currentStatus: "启动组保存失败: demo");

        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        Assert.Equal("mutated", Assert.Single(LauncherPresetStore.Load(presetPath).Presets).Id);
    }

    [Fact]
    public void RestoreAppendsFailedWriteErrorsToCurrentStatus()
    {
        using var temp = new TempDirectory();
        var snapshot = LauncherEntryMutationSnapshot.Capture(
            [Entry("one", selected: true)],
            Presets("daily", "one"),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");

        var status = LauncherEntryPersistenceRollback.Restore(
            snapshot,
            configPath: temp.Path,
            presetPath: Path.Combine(temp.Path, "presets.json"),
            restorePresets: false,
            currentStatus: "设置保存失败: demo");

        Assert.Contains("设置保存失败: demo；回滚磁盘状态失败:", status);
        Assert.Contains("config:", status);
    }

    private static LaunchEntry Entry(string id, bool selected)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
            Selected = selected,
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
