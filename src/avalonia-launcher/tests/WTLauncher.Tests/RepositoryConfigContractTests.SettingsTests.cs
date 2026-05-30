using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryRootSettings_RoundTripPortableStateFields()
    {
        var settingsPath = TestRepositoryPaths.Config("settings.json");

        var settings = LauncherSettingsStore.Load(settingsPath);

        Assert.Equal("light", settings.Theme);
        Assert.Equal("", settings.LastFilterText);
        Assert.Equal("custom", settings.SortMode);
        Assert.Equal("cards", settings.EntryViewMode);
        Assert.True(settings.RequireConfirmationForLaunches);
        Assert.NotEmpty(settings.CustomEntryOrder);
        Assert.Equal(1180d, settings.Window.Width);
        Assert.Equal(760d, settings.Window.Height);
    }
}
