using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetNamesTests
{
    [Theory]
    [InlineData(" Daily ", "Daily")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void NormalizeTrimsPresetNameAndConvertsNullToEmpty(string? name, string expected)
    {
        Assert.Equal(expected, LauncherPresetNames.Normalize(name));
    }

    [Theory]
    [InlineData(" Daily ", true, "Daily")]
    [InlineData("", false, "")]
    [InlineData("  ", false, "")]
    [InlineData(null, false, "")]
    public void TryNormalizeRequiredReturnsNormalizedNameAndRequiredState(
        string? name,
        bool expectedAccepted,
        string expectedName)
    {
        var accepted = LauncherPresetNames.TryNormalizeRequired(name, out var normalizedName);

        Assert.Equal(expectedAccepted, accepted);
        Assert.Equal(expectedName, normalizedName);
    }
}
