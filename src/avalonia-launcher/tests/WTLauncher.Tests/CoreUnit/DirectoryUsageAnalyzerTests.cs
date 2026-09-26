using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

// ponytail: inline arrays in test inputs — perf-irrelevant in tests.
#pragma warning disable CA1861

[Trait("Category", TestCategories.CoreUnit)]
public sealed class DirectoryUsageAnalyzerTests
{
    private static LaunchEntry Entry(string id, string directory, string? name = null) =>
        new()
        {
            Id = id,
            Name = name ?? id,
            Directory = (WTLauncher.Core.DirectoryPath)directory,
            Command = "pwsh",
        };

    [Fact]
    public void Analyze_EmptyEntries_ReturnsEmptyList()
    {
        var rows = DirectoryUsageAnalyzer.Analyze(Array.Empty<LaunchEntry>());
        Assert.Empty(rows);
    }

    [Fact]
    public void Analyze_SingleEntry_ReturnsOneRowWithCountOne()
    {
        var rows = DirectoryUsageAnalyzer.Analyze(new[] { Entry("a", @"D:\x") });
        var row = Assert.Single(rows);
        Assert.Equal(@"D:\x", row.Directory);
        Assert.Equal(1, row.ReferenceCount);
    }

    [Fact]
    public void Analyze_MultipleEntriesSameDirectory_GroupsAndCounts()
    {
        // ponytail: 当前 launcher.json 现状 — 3 对重复 directory 各 2 个 entry
        var entries = new[]
        {
            Entry("a", @"D:\proj1", "codex"),
            Entry("b", @"D:\proj1", "opencode"),
            Entry("c", @"D:\proj2", "codex"),
            Entry("d", @"D:\proj2", "claude"),
        };

        var rows = DirectoryUsageAnalyzer.Analyze(entries);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].ReferenceCount);
        Assert.Equal(2, rows[1].ReferenceCount);
    }

    [Fact]
    public void Analyze_WindowsPathsCaseInsensitive()
    {
        // Windows path 大小写不敏感 — D:\Foo ≡ d:\foo
        var entries = new[]
        {
            Entry("a", @"D:\Foo"),
            Entry("b", @"d:\foo"),
        };

        var rows = DirectoryUsageAnalyzer.Analyze(entries);
        var row = Assert.Single(rows);
        Assert.Equal(2, row.ReferenceCount);
    }

    [Fact]
    public void CanDeleteDirectory_TrueWhenNoReferences()
    {
        var entries = new[] { Entry("a", @"D:\x") };
        Assert.True(DirectoryUsageAnalyzer.CanDeleteDirectory(@"D:\y", entries));
        Assert.False(DirectoryUsageAnalyzer.CanDeleteDirectory(@"D:\x", entries));
    }

    [Fact]
    public void CanDeleteDirectory_CaseInsensitive()
    {
        var entries = new[] { Entry("a", @"D:\Foo") };
        Assert.False(DirectoryUsageAnalyzer.CanDeleteDirectory(@"d:\foo", entries));
    }
}

