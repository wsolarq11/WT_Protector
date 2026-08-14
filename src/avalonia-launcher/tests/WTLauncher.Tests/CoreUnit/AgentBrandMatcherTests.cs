using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class AgentBrandMatcherTests
{
    [Theory]
    [InlineData("codex", "codex")]
    [InlineData("CODEX", "codex")]
    [InlineData("codex.exe", "codex")]
    [InlineData("codex.cmd", "codex")]
    [InlineData("codex.bat", "codex")]
    [InlineData("C:\\path\\codex.cmd", "codex")]
    [InlineData("/usr/bin/claude", "claude")]
    [InlineData("codex --enable goals --dangerously-bypass-approvals-and-sandbox", "codex")]
    [InlineData("  claude  ", "claude")]
    [InlineData("aider", "aider")]
    [InlineData("qwen-code", "qwen-code")]
    public void TryMatch_MatchesKnownAgents(string command, string expectedExecName)
    {
        var matched = AgentBrandMatcher.TryMatch(command, out var entry);
        Assert.True(matched, $"Expected match for: {command}");
        Assert.Equal(expectedExecName, entry.ExecName);
        Assert.False(string.IsNullOrWhiteSpace(entry.BrandColorHex));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknownbin")]
    [InlineData("cod")]
    [InlineData("codex-extra")]
    [InlineData("./relative/unknown")]
    public void TryMatch_RejectsUnknownOrMalformed(string command)
    {
        var matched = AgentBrandMatcher.TryMatch(command, out var entry);
        Assert.False(matched);
        Assert.Null(entry);  // out parameter is default! when no match
    }

    [Fact]
    public void TryMatch_HandlesNull()
    {
        var matched = AgentBrandMatcher.TryMatch(null, out var entry);
        Assert.False(matched);
        Assert.Null(entry);
    }

    [Fact]
    public void TryMatch_ReturnedEntryHasValidBrandColor()
    {
        // Every matched catalog entry must carry a badge-ready hex.
        var matched = AgentBrandMatcher.TryMatch("codex", out var entry);
        Assert.True(matched);
        Assert.Matches("^#[0-9A-Fa-f]{6}$", entry.BrandColorHex);
    }
}
