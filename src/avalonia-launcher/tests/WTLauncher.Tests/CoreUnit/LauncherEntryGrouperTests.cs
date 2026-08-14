using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

// ponytail: inline arrays in test inputs — perf-irrelevant in tests.
#pragma warning disable CA1861

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherEntryGrouperTests
{
    private static LaunchEntry Entry(string id, string command = "pwsh", string? directory = null) =>
        new()
        {
            Id = id,
            Name = id,
            Directory = (WTLauncher.Core.DirectoryPath)(directory ?? $@"D:\{id}"),
            Command = command,
        };

    [Fact]
    public void Group_EmptyEntries_NoneKey_ReturnsSingleEmptyGroup()
    {
        var groups = LauncherEntryGrouper.Group(Array.Empty<LaunchEntry>(), LauncherCardsGroupKeys.None);
        var group = Assert.Single(groups);
        Assert.Equal(string.Empty, group.Key);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void Group_NoneKey_ReturnsSingleAllGroup()
    {
        var entries = new[]
        {
            Entry("a", command: "$codex"),
            Entry("b", command: "$claude"),
        };

        var groups = LauncherEntryGrouper.Group(entries, LauncherCardsGroupKeys.None);
        var group = Assert.Single(groups);
        Assert.Equal(2, group.Count);
        Assert.Equal(string.Empty, group.Key);
    }

    [Fact]
    public void Group_CommandKey_GroupsByCommandFieldCaseInsensitive()
    {
        // ponytail: 当前 launcher.json 现状 — 14 个 $codex entry
        var codexEntries = Enumerable.Range(1, 14)
            .Select(i => Entry($"c{i}", command: "$codex"))
            .ToArray();
        var others = new[]
        {
            Entry("a", command: "$claude"),
            Entry("b", command: "$claude"),
            Entry("d", command: "codex"), // 裸命令,与 $codex 不同
        };

        var groups = LauncherEntryGrouper.Group(codexEntries.Concat(others), LauncherCardsGroupKeys.Command);

        Assert.Equal(3, groups.Count);
        // count desc: $codex (14), $claude (2), codex (1)
        Assert.Equal("$codex", groups[0].Key);
        Assert.Equal(14, groups[0].Count);
        Assert.Equal("$claude", groups[1].Key);
        Assert.Equal(2, groups[1].Count);
        Assert.Equal("codex", groups[2].Key);
        Assert.Equal(1, groups[2].Count);
    }

    [Fact]
    public void Group_DirectoryKey_GroupsByDirectoryCaseInsensitive()
    {
        var entries = new[]
        {
            Entry("a", directory: @"D:\proj1"),
            Entry("b", directory: @"d:\proj1"),
            Entry("c", directory: @"D:\proj2"),
        };

        var groups = LauncherEntryGrouper.Group(entries, LauncherCardsGroupKeys.Directory);
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count); // proj1
        Assert.Equal(1, groups[1].Count); // proj2
    }

    [Fact]
    public void Group_UnknownKey_FallsBackToNone()
    {
        var entries = new[] { Entry("a"), Entry("b") };
        var groups = LauncherEntryGrouper.Group(entries, "tag");
        var group = Assert.Single(groups);
        Assert.Equal(string.Empty, group.Key);
    }

    [Fact]
    public void Group_NullKey_FallsBackToNone()
    {
        var entries = new[] { Entry("a") };
        var groups = LauncherEntryGrouper.Group(entries, null!);
        var group = Assert.Single(groups);
        Assert.Equal(string.Empty, group.Key);
    }

    [Fact]
    public void Normalize_LowerCasesKnownValues()
    {
        Assert.Equal("command", LauncherCardsGroupKeys.Normalize("COMMAND"));
        Assert.Equal("directory", LauncherCardsGroupKeys.Normalize("Directory"));
        Assert.Equal("none", LauncherCardsGroupKeys.Normalize("NONE"));
    }

    [Fact]
    public void Normalize_UnknownReturnsNone()
    {
        Assert.Equal("none", LauncherCardsGroupKeys.Normalize("tag"));
        Assert.Equal("none", LauncherCardsGroupKeys.Normalize(null));
        Assert.Equal("none", LauncherCardsGroupKeys.Normalize(""));
    }
}

