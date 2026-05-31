using WTLauncher.Core;

namespace WTLauncher.Tests;

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

        Assert.Equal("Unknown launcher entry view mode: .", exception.Message);
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
