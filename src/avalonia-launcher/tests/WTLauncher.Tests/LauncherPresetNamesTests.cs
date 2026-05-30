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
}
