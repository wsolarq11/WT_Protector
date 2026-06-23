using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsSnapshotTests
{
    [Fact]
    public void CaptureDeepCopiesPortableSettingsAndRestoresAllFields()
    {
        var settings = Settings(
            lastFilterText: "codex",
            sortMode: LauncherEntryVisibility.AlphabeticalSortModeId,
            entryViewMode: LauncherEntryViewModes.DetailsId,
            requireConfirmationForLaunches: false,
            window: new LauncherWindowSettings { Width = 1200, Height = 800 });

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings = settings with
        {
            LastFilterText = "",
            SortMode = LauncherEntryVisibility.UpdatedSortModeId,
            EntryViewMode = LauncherEntryViewModes.NamesId,
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["changed", "two"],
            Window = settings.Window with { Width = 1, Height = 2 },
        };

        settings = snapshot.Restore(settings);

        Assert.Equal("codex", settings.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.AlphabeticalSortModeId, settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.DetailsId, settings.EntryViewMode);
        Assert.False(settings.RequireConfirmationForLaunches);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
        Assert.Equal(1200d, settings.Window.Width);
        Assert.Equal(800d, settings.Window.Height);
    }

    [Fact]
    public void RestoreCustomEntryOrderLeavesOtherSettingsUntouched()
    {
        var settings = Settings(lastFilterText: "old");
        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings = settings with
        {
            LastFilterText = "new",
            CustomEntryOrder = ["two", "one"],
        };

        settings = snapshot.RestoreCustomEntryOrder(settings);

        Assert.Equal("new", settings.LastFilterText);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
    }

    [Fact]
    public void CaptureRestoresNullOrderAndWindowWithoutInventingDefaults()
    {
        var settings = Settings() with
        {
            CustomEntryOrder = null!,
            Window = null!,
        };

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings = settings with
        {
            CustomEntryOrder = ["one"],
            Window = new LauncherWindowSettings { Width = 1, Height = 2 },
        };

        settings = snapshot.Restore(settings);

        Assert.Null(settings.CustomEntryOrder);
        Assert.Null(settings.Window);
    }

    private static LauncherSettings Settings(
        string lastFilterText = "",
        string sortMode = LauncherEntryVisibility.CustomSortModeId,
        string entryViewMode = LauncherEntryViewModes.CardsId,
        bool requireConfirmationForLaunches = true,
        LauncherWindowSettings? window = null) =>
        new()
        {
            LastFilterText = lastFilterText,
            SortMode = sortMode,
            EntryViewMode = entryViewMode,
            RequireConfirmationForLaunches = requireConfirmationForLaunches,
            CustomEntryOrder = ["one", "two"],
            Window = window ?? new LauncherWindowSettings(),
        };
}
