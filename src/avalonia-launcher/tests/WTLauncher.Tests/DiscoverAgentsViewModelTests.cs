using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class DiscoverAgentsViewModelTests
{
    private static DiscoverAgentsViewModel CreateViewModel(Func<AgentDiscoveryResult> scanFunc) =>
        new(scanFunc, alreadyAddedExecNames: Array.Empty<string>());

    private static readonly AgentCatalogEntry ClaudeEntry = new()
    {
        ExecName = "claude",
        DisplayName = "Claude Code",
        Vendor = "Anthropic",
        VersionArgs = "--version",
        Category = AgentCategory.TerminalNative,
        BrandColorHex = "#CC7833",
    };

    private static readonly AgentCatalogEntry CodexEntry = new()
    {
        ExecName = "codex",
        DisplayName = "Codex CLI",
        Vendor = "OpenAI",
        VersionArgs = "--version",
        Category = AgentCategory.TerminalNative,
        BrandColorHex = "#75AFFF",
    };

    [Fact]
    public async Task ScanAsync_WhenScanFuncThrowsRecoverable_SurfacesErrorAndClearsScanning()
    {
        // InvalidOperationException is one of the scanner's own per-agent catch types,
        // so it is representative of an orchestration-level escape here.
        Func<AgentDiscoveryResult> throws = () => throw new InvalidOperationException("boom");
        var viewModel = CreateViewModel(throws);

        await viewModel.ScanAsync();

        Assert.False(viewModel.IsScanning);
        Assert.Equal("扫描过程中发生错误", viewModel.StatusText);
        Assert.Empty(viewModel.Rows);
    }

    [Fact]
    public async Task ScanAsync_WhenScanSucceeds_PopulatesRowsAndUpdatesStatus()
    {
        var agents = ImmutableArray.Create(
            new DiscoveredAgent
            {
                Catalog = ClaudeEntry,
                ResolvedPath = @"C:\tools\claude.exe",
                Version = "1.0.0",
            },
            new DiscoveredAgent
            {
                Catalog = CodexEntry,
                ResolvedPath = @"C:\tools\codex.exe",
                Version = "2.0.0",
            });
        Func<AgentDiscoveryResult> success = () => new AgentDiscoveryResult
        {
            Found = agents,
            Failures = ImmutableArray<AgentDiscoveryFailure>.Empty,
        };
        var viewModel = CreateViewModel(success);

        await viewModel.ScanAsync();

        Assert.False(viewModel.IsScanning);
        Assert.Equal(2, viewModel.Rows.Count);
        Assert.Contains("发现", viewModel.StatusText);
        Assert.Equal("Claude Code", viewModel.Rows[0].DisplayName);
        Assert.Equal("Codex CLI", viewModel.Rows[1].DisplayName);
        Assert.False(viewModel.Rows[0].IsAlreadyAdded);
    }

    [Fact]
    public async Task ScanAsync_WhenScanReturnsEmpty_ShowsNoAgentMessage()
    {
        Func<AgentDiscoveryResult> empty = () => AgentDiscoveryResult.Empty;
        var viewModel = CreateViewModel(empty);

        await viewModel.ScanAsync();

        Assert.False(viewModel.IsScanning);
        Assert.Equal("未发现任何已安装的 AI Agent", viewModel.StatusText);
        Assert.Empty(viewModel.Rows);
    }
}
