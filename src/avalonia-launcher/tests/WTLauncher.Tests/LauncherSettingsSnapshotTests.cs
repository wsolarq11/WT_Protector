using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsSnapshotTests
{
    [Fact]
    public void CaptureDeepCopiesPortableSettingsAndRestoresAllFields()
    {
        var settings = new LauncherSettings
        {
            Theme = "dark",
            LastFilterText = "codex",
            SortMode = "alphabetical",
            EntryViewMode = "details",
            RequireConfirmationForLaunches = false,
            CustomEntryOrder = ["one", "two"],
            Window = new LauncherWindowSettings { Width = 1200, Height = 800 },
        };

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
        var settings = new LauncherSettings
        {
            Theme = "light",
            LastFilterText = "old",
            SortMode = "custom",
            EntryViewMode = "cards",
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["one", "two"],
        };
        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        settings.LastFilterText = "new";
        settings.CustomEntryOrder = ["two", "one"];

        snapshot.RestoreCustomEntryOrder(settings);

        Assert.Equal("new", settings.LastFilterText);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
    }
}
