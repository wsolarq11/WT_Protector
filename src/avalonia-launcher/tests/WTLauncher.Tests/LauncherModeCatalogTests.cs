using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherModeCatalogTests
{
    [Fact]
    public void CreateModesExposeExpectedOptions()
    {
        var sortModes = LauncherModeCatalog.CreateSortModes();
        var displayModes = LauncherModeCatalog.CreateDisplayModes();

        Assert.Equal(new[] { "custom", "updated", "alphabetical" }, sortModes.Select(mode => mode.Id).ToArray());
        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, displayModes.Select(mode => mode.Label).ToArray());
    }

    [Fact]
    public void ResolveSortModeFallsBackToFirstMode()
    {
        var sortModes = LauncherModeCatalog.CreateSortModes();

        Assert.Equal("updated", LauncherModeCatalog.ResolveSortMode(sortModes, "updated").Id);
        Assert.Equal("custom", LauncherModeCatalog.ResolveSortMode(sortModes, "missing").Id);
    }

    [Theory]
    [InlineData("details", LauncherEntryViewMode.Details, "details")]
    [InlineData("names", LauncherEntryViewMode.Names, "names")]
    [InlineData("missing", LauncherEntryViewMode.Cards, "cards")]
    public void ResolveDisplayModeUsesKnownValuesAndFallsBackToCards(
        string entryViewMode,
        LauncherEntryViewMode expectedMode,
        string expectedId)
    {
        var displayModes = LauncherModeCatalog.CreateDisplayModes();

        var resolved = LauncherModeCatalog.ResolveDisplayMode(displayModes, entryViewMode);

        Assert.Equal(expectedMode, resolved.Mode);
        Assert.Equal(expectedId, resolved.Id);
    }
}
