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

        var saveFailure = LauncherSettingsMutationPersistence.Save(
            settings,
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
        settings.LastFilterText = "new";
        settings.SortMode = LauncherEntryVisibility.UpdatedSortModeId;
        settings.EntryViewMode = LauncherEntryViewModes.NamesId;
        settings.RequireConfirmationForLaunches = false;
        settings.CustomEntryOrder = ["two", "one"];

        var saveFailure = LauncherSettingsMutationPersistence.Save(
            settings: settings,
            settingsPath: temp.Path,
            snapshot: snapshot);

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
        settings.SortMode = "missing";

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsMutationPersistence.Save(
                settings,
                settingsPath,
                snapshot));

        Assert.Equal("Unknown launcher sort mode: missing.", exception.Message);
        Assert.Equal("missing", settings.SortMode);
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
