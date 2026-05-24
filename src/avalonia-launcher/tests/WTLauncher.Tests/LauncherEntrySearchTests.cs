using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntrySearchTests
{
    [Fact]
    public void MatchesTreatsBlankFilterAsMatchAll()
    {
        var entry = new LaunchEntry
        {
            Name = "One",
            Directory = "D:\\one",
            Command = "pwsh",
        };

        Assert.True(LauncherEntrySearch.Matches(entry, null));
        Assert.True(LauncherEntrySearch.Matches(entry, "   "));
    }

    [Theory]
    [InlineData("codex", true)]
    [InlineData("WORK", true)]
    [InlineData("pwsh", true)]
    [InlineData("missing", false)]
    public void MatchesSearchesNameDirectoryAndCommandCaseInsensitively(string filter, bool expected)
    {
        var entry = new LaunchEntry
        {
            Name = "Workstation",
            Directory = "D:\\Projects\\Work",
            Command = "pwsh -NoProfile -Command codex",
        };

        Assert.Equal(expected, LauncherEntrySearch.Matches(entry, filter));
    }
}
