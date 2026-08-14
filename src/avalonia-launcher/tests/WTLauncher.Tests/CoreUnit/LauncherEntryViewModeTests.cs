using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherEntryViewModeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NormalizeIdRejectsMissingViewModeIds(string? entryViewMode)
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryViewModes.NormalizeId(entryViewMode));

        Assert.Equal("Launcher entry view mode is required.", exception.Message);
    }

    [Theory]
    [InlineData(LauncherEntryViewMode.Cards, "cards")]
    [InlineData(LauncherEntryViewMode.Details, "details")]
    [InlineData(LauncherEntryViewMode.Names, "names")]
    public void ToIdKeepsPortableSettingsContract(LauncherEntryViewMode mode, string expected)
    {
        Assert.Equal(expected, LauncherEntryViewModes.ToId(mode));
    }

    [Fact]
    public void LauncherSettingsDefaultsToCardsViewMode()
    {
        Assert.Equal("cards", new LauncherSettings().EntryViewMode);
    }
}
