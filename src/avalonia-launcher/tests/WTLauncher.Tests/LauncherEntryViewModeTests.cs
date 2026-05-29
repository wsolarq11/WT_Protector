using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryViewModeTests
{
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
