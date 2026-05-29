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
            sortMode: "alphabetical",
            entryViewMode: "details",
            requireConfirmationForLaunches: false,
            window: new LauncherWindowSettings { Width = 1200, Height = 800 });

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.Theme = "light";
        settings.LastFilterText = "";
        settings.SortMode = "updated";
        settings.EntryViewMode = "names";
        settings.RequireConfirmationForLaunches = true;
        settings.CustomEntryOrder[0] = "changed";
        settings.Window.Width = 1;
        settings.Window.Height = 2;

        snapshot.Restore(settings);

        Assert.Equal("dark", settings.Theme);
        Assert.Equal("codex", settings.LastFilterText);
        Assert.Equal("alphabetical", settings.SortMode);
        Assert.Equal("details", settings.EntryViewMode);
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

    private static LauncherSettings Settings(
        string theme = "light",
        string lastFilterText = "",
        string sortMode = "custom",
        string entryViewMode = "cards",
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
