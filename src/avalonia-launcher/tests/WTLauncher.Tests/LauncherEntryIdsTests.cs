using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryIdsTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("one", true)]
    public void IsUsableRejectsBlankIds(string? id, bool expected)
    {
        Assert.Equal(expected, LauncherEntryIds.IsUsable(id));
    }

    [Fact]
    public void SameUsesOrdinalEntryIdMatch()
    {
        Assert.True(LauncherEntryIds.Same("one", "one"));
        Assert.True(LauncherEntryIds.Same(null, null));
        Assert.False(LauncherEntryIds.Same("One", "one"));
        Assert.False(LauncherEntryIds.Same(null, "one"));
    }
}
