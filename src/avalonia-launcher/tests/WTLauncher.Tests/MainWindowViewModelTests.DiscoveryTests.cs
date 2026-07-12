using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    private static DiscoveredAgent SampleDiscoveredAgent(string execName, string displayName, string vendor = "Test") =>
        new()
        {
            Catalog = new AgentCatalogEntry
            {
                ExecName = execName,
                DisplayName = displayName,
                Vendor = vendor,
                VersionArgs = "--version",
                Category = AgentCategory.TerminalNative,
                BrandColorHex = "#D97757",
            },
            ResolvedPath = $@"C:\tools\{execName}.exe",
            Version = "1.0.0",
        };

    [Fact]
    public async Task DiscoverAgents_WithCanceledDialog_MakesNoMutation()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = _ => Task.FromResult<IReadOnlyList<DiscoveredAgent>?>(null);
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Equal("已取消发现", viewModel.StatusText);
    }

    [Fact]
    public async Task DiscoverAgents_WithEmptySelection_MakesNoMutation()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = _ => Task.FromResult<IReadOnlyList<DiscoveredAgent>?>([]);
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Equal("已取消发现", viewModel.StatusText);
    }

    [Fact]
    public async Task DiscoverAgents_AddsSelectedAgents_Atomically()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = _ => Task.FromResult<IReadOnlyList<DiscoveredAgent>?>([
            SampleDiscoveredAgent("claude", "Claude Code", "Anthropic"),
            SampleDiscoveredAgent("aider", "Aider", "community"),
        ]);
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(4, viewModel.Entries.Count);
        Assert.Equal(4, viewModel.Settings.CustomEntryOrder.Count);
        Assert.Contains(viewModel.Entries, e => e.Name == "Claude Code");
        Assert.Contains(viewModel.Entries, e => e.Name == "Aider");
        Assert.Contains("已添加 2 个 AI Agent 启动项", viewModel.StatusText);

        var saved = LauncherStore.Load(config);
        Assert.Equal(4, saved.Entries.Count);
    }

    [Fact]
    public async Task DiscoverAgents_RollsBack_OnSaveFailure()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = _ => Task.FromResult<IReadOnlyList<DiscoveredAgent>?>([
            SampleDiscoveredAgent("claude", "Claude Code", "Anthropic"),
        ]);
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        // ponytail: simulate save failure by replacing store file path with a directory.
        Directory.CreateDirectory(config + ".blocker");
        File.Delete(config);
        Directory.CreateDirectory(config);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Contains("存储保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task DiscoverAgents_ReportsStatus_WhenDialogThrowsRecoverableException()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = _ => throw new InvalidOperationException("dialog blew up");
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Contains("发现对话框打开失败", viewModel.StatusText);
    }
}
