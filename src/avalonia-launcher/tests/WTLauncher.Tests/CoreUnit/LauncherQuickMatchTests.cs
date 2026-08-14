using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherQuickMatchTests
{
    [Fact]
    public void EmptyQuery_MatchesEverything_WithZeroScore()
    {
        var entry = Entry("alphabet");

        Assert.True(LauncherQuickMatch.IsMatch("", entry));
        Assert.True(LauncherQuickMatch.IsMatch(null, entry));
        Assert.Equal(0, LauncherQuickMatch.Score("", entry));
    }

    [Fact]
    public void Prefix_RanksAboveSubstringMatch()
    {
        var prefix = Entry("alpha-crawler");
        var substring = Entry("project-alphabet");

        Assert.True(LauncherQuickMatch.Score("alpha", prefix) > LauncherQuickMatch.Score("alpha", substring));
    }

    [Fact]
    public void DirectoryMatch_RanksAboveCommandMatch()
    {
        var directory = Entry("crawler", directory: @"D:\data\alpha");
        var command = Entry("crawler", command: "alpha-crawler.exe");

        Assert.True(LauncherQuickMatch.Score("alpha", directory) > LauncherQuickMatch.Score("alpha", command));
    }

    [Fact]
    public void Subsequence_AddsBonusWhenNameDoesNotContainExactQuery()
    {
        var entry = Entry("phoenix-crawler");

        Assert.True(LauncherQuickMatch.Score("pc", entry) > 0);
    }

    private static LaunchEntry Entry(string name, string? directory = null, string command = "pwsh") =>
        new()
        {
            Id = name,
            Name = name,
            Directory = directory is null ? DirectoryPath.Empty : (DirectoryPath)directory,
            Command = command,
        };
}
