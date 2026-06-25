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
        settings = settings with { LastFilterText = "new" };

        var saveFailure = LauncherSettingsMutationPersistence.Save(
            ref settings,
            settingsPath,
            snapshot);

        Assert.Null(saveFailure);
        Assert.Equal("new", settings.LastFilterText);
        Assert.Equal("new", LauncherSettingsStore.Load(settingsPath).LastFilterText);
    }

    [Fact]
    public void SaveRestoresSnapshotWhenSaveFails()
    {
        using var temp = new TempDirectory();
        var settings = Settings();
        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings = settings with
        {
            LastFilterText = "new",
            SortMode = LauncherEntryVisibility.UpdatedSortModeId,
            EntryViewMode = LauncherEntryViewModes.NamesId,
            RequireConfirmationForLaunches = false,
            CustomEntryOrder = ["two", "one"],
        };

        var saveFailure = LauncherSettingsMutationPersistence.Save(
            ref settings,
            temp.Path,
            snapshot);

        Assert.Contains("设置保存失败", saveFailure);
        Assert.Equal("old", settings.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, settings.EntryViewMode);
        Assert.True(settings.RequireConfirmationForLaunches);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
    }

    [Fact]
    public void SaveRejectsInvalidSettingsInsteadOfReturningSaveStatus()
    {
        using var temp = new TempDirectory();
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var settings = Settings();
        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings = settings with { SortMode = "missing" };

        var saveFailure = LauncherSettingsMutationPersistence.Save(
            ref settings,
            settingsPath,
            snapshot);

        Assert.NotNull(saveFailure);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, settings.SortMode);
        Assert.False(File.Exists(settingsPath));
    }

    private static LauncherSettings Settings()
    {
        return new LauncherSettings
        {
            LastFilterText = "old",
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            EntryViewMode = LauncherEntryViewModes.CardsId,
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["one", "two"],
        };
    }
}
