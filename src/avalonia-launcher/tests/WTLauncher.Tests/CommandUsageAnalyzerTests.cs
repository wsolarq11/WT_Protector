using System.Collections.Immutable;
using WTLauncher.Core;

// ponytail: inline arrays in test assertions/inputs — perf-irrelevant in tests.
#pragma warning disable CA1861

namespace WTLauncher.Tests;

public sealed class CommandUsageAnalyzerTests
{
    private static LaunchEntry Entry(string id, string command, string? name = null) =>
        new()
        {
            Id = id,
            Name = name ?? id,
            Directory = $@"D:\{id}",
            Command = command,
        };

    [Fact]
    public void Analyze_EmptyEntries_ReturnsEmptyList()
    {
        var rows = CommandUsageAnalyzer.Analyze(Array.Empty<LaunchEntry>());
        Assert.Empty(rows);
    }

    [Fact]
    public void Analyze_SingleEntry_ReturnsOneRowWithCountOne()
    {
        var rows = CommandUsageAnalyzer.Analyze(new[] { Entry("a", "codex") });
        var row = Assert.Single(rows);
        Assert.Equal("codex", row.Command);
        Assert.Equal(1, row.ReferenceCount);
        Assert.Equal(new[] { "a" }, row.ReferencingEntryNames);
    }

    [Fact]
    public void Analyze_MultipleEntriesSameCommand_GroupsAndCounts()
    {
        // ponytail: 当前 launcher.json 现状 — 14 个 $codex entry
        var entries = Enumerable.Range(1, 14)
            .Select(i => Entry($"entry-{i}", "$codex", $"Entry {i}"))
            .ToArray();

        var rows = CommandUsageAnalyzer.Analyze(entries);
        var row = Assert.Single(rows);
        Assert.Equal("$codex", row.Command);
        Assert.Equal(14, row.ReferenceCount);
        Assert.Equal(14, row.ReferencingEntryNames.Count);
    }

    [Fact]
    public void Analyze_DifferentCommands_GroupsSeparately()
    {
        var entries = new[]
        {
            Entry("a", "$codex"),
            Entry("b", "$codex"),
            Entry("c", "$claude"),
            Entry("d", "codex"), // 裸命令
        };

        var rows = CommandUsageAnalyzer.Analyze(entries);
        Assert.Equal(3, rows.Count);
        // 按 ref count 倒序: $codex (2), $claude (1), codex (1); 相同 count 按 command ordinal
        Assert.Equal("$codex", rows[0].Command);
        Assert.Equal(2, rows[0].ReferenceCount);
        // $claude (1) 和 codex (1) 都 count=1, 按 command ordinal: $ 在 c 之前
        Assert.Equal("$claude", rows[1].Command);
        Assert.Equal("codex", rows[2].Command);
    }

    [Fact]
    public void Analyze_OrdinalIgnoreCase_GroupsDifferentCaseTogether()
    {
        // ponytail: Windows command line 是 case-insensitive, $CODEX 和 $codex 视为同 command。
        var entries = new[]
        {
            Entry("a", "$codex"),
            Entry("b", "$CODEX"),
        };

        var rows = CommandUsageAnalyzer.Analyze(entries);
        var row = Assert.Single(rows);
        Assert.Equal(2, row.ReferenceCount);
    }

    [Fact]
    public void AnalyzeTemplates_EmptyTemplates_ReturnsEmptyList()
    {
        var rows = CommandUsageAnalyzer.AnalyzeTemplates(
            ImmutableDictionary<string, string>.Empty,
            Array.Empty<LaunchEntry>());
        Assert.Empty(rows);
    }

    [Fact]
    public void AnalyzeTemplates_CountsReferencesForEachKey()
    {
        // 模拟当前 launcher.json
        var templates = new Dictionary<string, string>
        {
            ["codex"] = "codex --enable goals --dangerously-bypass-approvals-and-sandbox",
            ["claude"] = "claude --dangerously-skip-permissions",
            ["opencode"] = "opencode",
        }.ToImmutableDictionary();

        var entries = Enumerable.Range(1, 14)
            .Select(i => Entry($"c{i}", "$codex"))
            .Concat(new[] { Entry("c15", "$claude"), Entry("c16", "$claude") })
            .Concat(new[] { Entry("c17", "$opencode") })
            .Concat(new[] { Entry("c18", "codex") }) // 裸命令, 不算 $codex 引用
            .ToArray();

        var rows = CommandUsageAnalyzer.AnalyzeTemplates(templates, entries);
        Assert.Equal(3, rows.Count);

        Assert.Equal("codex", rows[0].Key);
        Assert.Equal(14, rows[0].ReferenceCount);
        Assert.Contains("--enable goals", rows[0].ExpandedValue);

        Assert.Equal("claude", rows[1].Key);
        Assert.Equal(2, rows[1].ReferenceCount);

        Assert.Equal("opencode", rows[2].Key);
        Assert.Equal(1, rows[2].ReferenceCount);
    }

    [Fact]
    public void CanDeleteCommand_TrueWhenNoReferences()
    {
        var entries = new[] { Entry("a", "$codex") };
        Assert.True(CommandUsageAnalyzer.CanDeleteCommand("$claude", entries));
        Assert.False(CommandUsageAnalyzer.CanDeleteCommand("$codex", entries));
    }

    [Fact]
    public void CanDeleteCommand_CaseInsensitive()
    {
        var entries = new[] { Entry("a", "$codex") };
        // $CODEX 和 $codex 视为同 command, 有引用, 不能删
        Assert.False(CommandUsageAnalyzer.CanDeleteCommand("$CODEX", entries));
    }

    [Fact]
    public void CanDeleteTemplate_TrueWhenNoEntriesReferenceDollarKey()
    {
        var entries = new[] { Entry("a", "$codex") };
        Assert.True(CommandUsageAnalyzer.CanDeleteTemplate("claude", entries));
        Assert.False(CommandUsageAnalyzer.CanDeleteTemplate("codex", entries));
        // case-insensitive
        Assert.False(CommandUsageAnalyzer.CanDeleteTemplate("CODEX", entries));
    }
}
