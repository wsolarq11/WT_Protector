using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetManagementWorkflowTests
{
    [Fact]
    public void AddPresetReturnsReloadStatusAfterSave()
    {
        var presetFile = Presets();

        var result = LauncherPresetManagementWorkflow.AddPreset(
            presetFile,
            "Project Pair",
            ["one", "two"],
            preferredPresetId: "daily",
            savePresets: () => true,
            restorePresetFile: (_, _) => throw new InvalidOperationException("Restore should not run."));

        Assert.True(result.ShouldReload);
        Assert.Equal("project-pair", result.NextPresetId);
        Assert.Equal("已保存启动组: Project Pair（2 项）", result.StatusText);
        Assert.Contains(presetFile.Presets, preset => preset.Id == "project-pair");
    }

    [Fact]
    public void UpdatePresetReportsMissingPresetWithoutSaving()
    {
        var presetFile = Presets();
        var saveCalled = false;

        var result = LauncherPresetManagementWorkflow.UpdatePreset(
            presetFile,
            presetId: "missing",
            name: "Missing",
            entryIds: ["two"],
            preferredPresetId: "daily",
            savePresets: () =>
            {
                saveCalled = true;
                return true;
            },
            restorePresetFile: (_, _) => { });

        Assert.False(result.ShouldReload);
        Assert.Null(result.NextPresetId);
        Assert.Equal("当前启动组已不存在，请重新加载", result.StatusText);
        Assert.False(saveCalled);
    }

    [Fact]
    public void DeletePresetReturnsReloadStatusAfterSave()
    {
        var presetFile = Presets(
            new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
            new LauncherPreset { Id = "secondary", Name = "Secondary", EntryIds = ["two"] });

        var result = LauncherPresetManagementWorkflow.DeletePreset(
            presetFile,
            presetId: "daily",
            preferredPresetId: "daily",
            savePresets: () => true,
            restorePresetFile: (_, _) => throw new InvalidOperationException("Restore should not run."));

        Assert.True(result.ShouldReload);
        Assert.Equal("secondary", result.NextPresetId);
        Assert.Equal("已删除启动组: Daily", result.StatusText);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "daily");
    }

    private static LauncherPresetFile Presets(params LauncherPreset[] presets)
    {
        return new LauncherPresetFile
        {
            DefaultPresetId = presets.Length > 0 ? presets[0].Id : "daily",
            Presets = presets.Length > 0
                ? presets.ToList()
                :
                [
                    new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                ],
        };
    }
}
