using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsMutationPersistenceTests
{
    [Fact]
    public void SavePersistsSettingsWithoutRollbackWhenSaveSucceeds()
    {
        using var temp = new TempDirectory();
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var settings = Settings();
        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.LastFilterText = "new";
        var rollbackCalled = false;

        var result = LauncherSettingsMutationPersistence.Save(
            settings,
            settingsPath,
            snapshot,
            () => rollbackCalled = true);

        Assert.True(result.Succeeded);
        Assert.Null(result.StatusText);
        Assert.False(rollbackCalled);
        Assert.Equal("new", settings.LastFilterText);
        Assert.Equal("new", LauncherSettingsStore.Load(settingsPath).LastFilterText);
    }

    [Fact]
    public void SaveRestoresSnapshotBeforeRollbackCallbackWhenSaveFails()
    {
        using var temp = new TempDirectory();
        var settings = Settings();
        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.LastFilterText = "new";
        settings.SortMode = "updated";
        settings.EntryViewMode = "names";
        settings.RequireConfirmationForLaunches = false;
        settings.CustomEntryOrder = ["two", "one"];
        string? rollbackFilterText = null;

        var result = LauncherSettingsMutationPersistence.Save(
            settings: settings,
            settingsPath: temp.Path,
            snapshot: snapshot,
            onRollback: () => rollbackFilterText = settings.LastFilterText);

        Assert.False(result.Succeeded);
        Assert.Contains("设置保存失败", result.StatusText);
        Assert.Equal("old", rollbackFilterText);
        Assert.Equal("old", settings.LastFilterText);
        Assert.Equal("custom", settings.SortMode);
        Assert.Equal("cards", settings.EntryViewMode);
        Assert.True(settings.RequireConfirmationForLaunches);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
    }

    private static LauncherSettings Settings()
    {
        return new LauncherSettings
        {
            LastFilterText = "old",
            SortMode = "custom",
            EntryViewMode = "cards",
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["one", "two"],
        };
    }
}
