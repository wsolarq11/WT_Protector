using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryTextTests
{
    [Theory]
    [InlineData(" One ", "One")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void NormalizeTrimsTextAndConvertsNullToEmpty(string? value, string expected)
    {
        Assert.Equal(expected, LauncherEntryText.Normalize(value));
    }
}
