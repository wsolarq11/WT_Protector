using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryRootSettings_RoundTripPortableStateFields()
    {
        var settingsPath = TestRepositoryPaths.Config("settings.json");

        var settings = LauncherSettingsStore.Load(settingsPath);

        Assert.Equal("", settings.LastFilterText);
        Assert.Equal("custom", settings.SortMode);
        Assert.Contains(settings.EntryViewMode, new[] { LauncherEntryViewModes.CardsId, LauncherEntryViewModes.DetailsId, LauncherEntryViewModes.NamesId });
        Assert.False(settings.RequireConfirmationForLaunches);
        Assert.NotEmpty(settings.CustomEntryOrder!);
        Assert.Equal(LauncherWindowSettings.DefaultWidth, settings.Window.Width);
        Assert.Equal(LauncherWindowSettings.DefaultHeight, settings.Window.Height);
    }
}
