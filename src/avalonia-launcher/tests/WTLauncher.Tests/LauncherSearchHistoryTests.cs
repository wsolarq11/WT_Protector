using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSearchHistoryTests
{
    [Fact]
    public void Record_NormalizesAndAddsNewestTermFirst()
    {
        var result = LauncherSearchHistory.Record([], "  demo project  ");

        Assert.Equal(["demo project"], result);
    }

    [Fact]
    public void Record_DeduplicatesCaseInsensitively()
    {
        ImmutableList<string> history = ["other", "Demo", "old"];

        var result = LauncherSearchHistory.Record(history, "demo");

        Assert.Equal(["demo", "other", "old"], result);
    }

    [Fact]
    public void Record_WhenAlreadyNewest_ReturnsSameInstance()
    {
        ImmutableList<string> history = ["Demo", "other"];

        var result = LauncherSearchHistory.Record(history, "demo");

        Assert.Same(history, result);
    }

    [Fact]
    public void Record_KeepsOnlyNewestMaximumTerms()
    {
        var history = Enumerable.Range(1, LauncherSearchHistory.MaximumTerms)
            .Select(index => $"term-{index}")
            .ToImmutableList();

        var result = LauncherSearchHistory.Record(history, "newest");

        Assert.Equal(LauncherSearchHistory.MaximumTerms, result.Count);
        Assert.Equal("newest", result[0]);
        Assert.DoesNotContain("term-20", result);
    }
}
