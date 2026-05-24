using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsWorkflowTests
{
    [Fact]
    public void ChangeFilterTextPersistsPortableSettings()
    {
        using var temp = new TempDirectory();
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var settings = new LauncherSettings { LastFilterText = "old" };
        var rollbackCalled = false;

        var result = LauncherSettingsWorkflow.ChangeFilterText(
            settings,
            settingsPath,
            "codex",
            _ => rollbackCalled = true);

        Assert.True(result.Succeeded);
        Assert.Null(result.StatusText);
        Assert.False(rollbackCalled);
        Assert.Equal("codex", settings.LastFilterText);
        Assert.Equal("codex", LauncherSettingsStore.Load(settingsPath).LastFilterText);
    }

    [Fact]
    public void ChangeSortModeRollsBackWhenSettingsSaveFails()
    {
        using var temp = new TempDirectory();
        var settings = new LauncherSettings
        {
            SortMode = "custom",
            CustomEntryOrder = ["one", "two"],
        };
        var syncCalled = false;
        LauncherSettingsSnapshot? rollbackSnapshot = null;

        var result = LauncherSettingsWorkflow.ChangeSortMode(
            settings,
            temp.Path,
            "alphabetical",
            () =>
            {
                syncCalled = true;
                settings.CustomEntryOrder.Add("three");
            },
            snapshot => rollbackSnapshot = snapshot);

        Assert.False(result.Succeeded);
        Assert.Contains("设置保存失败", result.StatusText);
        Assert.True(syncCalled);
        Assert.NotNull(rollbackSnapshot);
        Assert.Equal("custom", rollbackSnapshot.SortMode);
        Assert.Equal("custom", settings.SortMode);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
    }
}
