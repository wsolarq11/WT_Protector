using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsSnapshotTests
{
    [Fact]
    public void CaptureDeepCopiesPortableSettingsAndRestoresAllFields()
    {
        var settings = Settings(
            theme: "dark",
            lastFilterText: "codex",
            sortMode: LauncherEntryVisibility.AlphabeticalSortModeId,
            entryViewMode: LauncherEntryViewModes.DetailsId,
            requireConfirmationForLaunches: false,
            window: new LauncherWindowSettings { Width = 1200, Height = 800 });

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.Theme = "light";
        settings.LastFilterText = "";
        settings.SortMode = LauncherEntryVisibility.UpdatedSortModeId;
        settings.EntryViewMode = LauncherEntryViewModes.NamesId;
        settings.RequireConfirmationForLaunches = true;
        settings.CustomEntryOrder[0] = "changed";
        settings.Window.Width = 1;
        settings.Window.Height = 2;

        snapshot.Restore(settings);

        Assert.Equal("dark", settings.Theme);
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
        settings.LastFilterText = "new";
        settings.CustomEntryOrder = ["two", "one"];

        snapshot.RestoreCustomEntryOrder(settings);

        Assert.Equal("new", settings.LastFilterText);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
    }

    [Fact]
    public void CaptureRestoresNullOrderAndWindowWithoutInventingDefaults()
    {
        var settings = Settings();
        settings.CustomEntryOrder = null!;
        settings.Window = null!;

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.CustomEntryOrder = ["one"];
        settings.Window = new LauncherWindowSettings { Width = 1, Height = 2 };

        snapshot.Restore(settings);

        Assert.Null(settings.CustomEntryOrder);
        Assert.Null(settings.Window);
    }

    private static LauncherSettings Settings(
        string theme = "light",
        string lastFilterText = "",
        string sortMode = LauncherEntryVisibility.CustomSortModeId,
        string entryViewMode = LauncherEntryViewModes.CardsId,
        bool requireConfirmationForLaunches = true,
        LauncherWindowSettings? window = null) =>
        new()
        {
            Theme = theme,
            LastFilterText = lastFilterText,
            SortMode = sortMode,
            EntryViewMode = entryViewMode,
            RequireConfirmationForLaunches = requireConfirmationForLaunches,
            CustomEntryOrder = ["one", "two"],
            Window = window ?? new LauncherWindowSettings(),
        };
}
