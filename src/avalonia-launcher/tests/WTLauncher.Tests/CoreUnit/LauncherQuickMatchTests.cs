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

    [Fact]
    public void Typo_WithinEditDistance_GetsBonusBelowExactMatch()
    {
        var entry = Entry("codex");

        var exact = LauncherQuickMatch.Score("codex", entry);
        var typo = LauncherQuickMatch.Score("codix", entry);

        Assert.True(typo > 0);
        Assert.True(exact > typo);
    }

    [Fact]
    public void Pinyin_FullPinyin_MatchesChineseName()
    {
        var entry = Entry("上海项目", id: "shanghai-project");

        Assert.True(LauncherQuickMatch.Score("shanghai", entry) > 0);
        Assert.True(LauncherQuickMatch.Score("shanghaixiangmu", entry) > 0);
    }

    [Fact]
    public void Pinyin_Initials_MatchesChineseName()
    {
        var entry = Entry("上海项目", id: "shanghai-project");

        Assert.True(LauncherQuickMatch.Score("shxm", entry) > 0);
    }

    [Fact]
    public void Pinyin_Directory_MatchesChineseDirectory()
    {
        var entry = Entry("alpha", directory: @"D:\项目\上海", id: "alpha-sh");

        Assert.True(LauncherQuickMatch.Score("shanghai", entry) > 0);
    }

    private static LaunchEntry Entry(string name, string? directory = null, string command = "pwsh", string? id = null) =>
        new()
        {
            Id = id ?? name,
            Name = name,
            Directory = directory is null ? DirectoryPath.Empty : (DirectoryPath)directory,
            Command = command,
        };
}
