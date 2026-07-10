using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class AgentRecommendationFactoryTests
{
    private static DiscoveredAgent SampleAgent(string execName = "claude", string recommendedArgs = "") =>
        new()
        {
            Catalog = new AgentCatalogEntry
            {
                ExecName = execName,
                DisplayName = "Claude Code",
                Vendor = "Anthropic",
                VersionArgs = "--version",
                RecommendedArgs = recommendedArgs,
                Category = AgentCategory.TerminalNative,
            },
            ResolvedPath = @"C:\tools\claude.exe",
            Version = "1.0.14",
        };

    [Fact]
    public void ToDraft_MapsDisplayNameAndExecCommand()
    {
        var draft = AgentRecommendationFactory.ToDraft(SampleAgent(), @"D:\work");
        Assert.Equal("Claude Code", draft.Name);
        Assert.Equal("claude", draft.Command);
        Assert.Equal(@"D:\work", draft.Directory);
        Assert.False(draft.Selected);
    }

    [Fact]
    public void ToDraft_StableId_DerivedFromExecName()
    {
        // ponytail: id derived from exec name with hyphens → underscores.
        // Survives restarts, makes "already added" dedup robust.
        var draft = AgentRecommendationFactory.ToDraft(SampleAgent(execName: "qwen-code"), @"D:\work");
        Assert.Equal("qwen_code", draft.Id);
    }

    [Fact]
    public void ToDraft_AppendsRecommendedArgs_WhenNonEmpty()
    {
        var draft = AgentRecommendationFactory.ToDraft(
            SampleAgent(recommendedArgs: "--dangerously-bypass"),
            @"D:\work");
        Assert.Equal("claude --dangerously-bypass", draft.Command);
    }

    [Fact]
    public void ToDraft_FallsBackToExecName_WhenDirectoryBlank()
    {
        // ponytail: caller contract — blank directory → command becomes the
        // directory placeholder, which will fail validation. Caller surfaces
        // the error; factory does not paper over it.
        var draft = AgentRecommendationFactory.ToDraft(SampleAgent(), "");
        Assert.Equal("claude", draft.Directory);
    }
}
