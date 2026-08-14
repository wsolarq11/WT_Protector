using System.Collections.Immutable;
using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
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
        dialogService.OnDiscoverAgents = (_, _, _) => Task.FromResult<DiscoveryDialogResult?>(null);
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Equal("已取消发现", viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Warning, viewModel.StatusTone);
    }

    [Fact]
    public async Task DiscoverAgents_WithEmptySelection_MakesNoMutation()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = (_, _, _) => Task.FromResult<DiscoveryDialogResult?>(new DiscoveredAgentsSelection([]));
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
        dialogService.OnDiscoverAgents = (_, _, _) => Task.FromResult<DiscoveryDialogResult?>(new DiscoveredAgentsSelection([
            SampleDiscoveredAgent("claude", "Claude Code", "Anthropic"),
            SampleDiscoveredAgent("aider", "Aider", "community"),
        ]));
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(4, viewModel.Entries.Count);
        Assert.Equal(4, viewModel.Settings.CustomEntryOrder.Count);
        Assert.Contains(viewModel.Entries, e => e.Name == "Claude Code");
        Assert.Contains(viewModel.Entries, e => e.Name == "Aider");
        Assert.Contains("已添加 2 个 AI Agent 启动项", viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Success, viewModel.StatusTone);

        var saved = LauncherStore.Load(config);
        Assert.Equal(4, saved.Entries.Count);
    }

    [Fact]
    public async Task DiscoverAgents_RollsBack_OnSaveFailure()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = (_, _, _) => Task.FromResult<DiscoveryDialogResult?>(new DiscoveredAgentsSelection([
            SampleDiscoveredAgent("claude", "Claude Code", "Anthropic"),
        ]));
        // CFA Step 1c: Use alphabetical sort mode so EnsureCustomOrderContainsEntries → ApplyFilter
        // doesn't call ValidateCustomOrder with stale custom order after save failure.
        var viewModel = CreateViewModel(config, settings,
            launcherSettings: new LauncherSettings
            {
                SortMode = LauncherEntryQuery.AlphabeticalSortModeId,
                EntryViewMode = LauncherEntryViewModes.CardsId,
            },
            dialogService: dialogService);

        // ponytail: simulate save failure by replacing store file path with a directory.
        Directory.CreateDirectory(config + ".blocker");
        File.Delete(config);
        Directory.CreateDirectory(config);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Contains("存储保存失败", viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Error, viewModel.StatusTone);
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task DiscoverAgents_ReportsStatus_WhenDialogThrowsRecoverableException()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnDiscoverAgents = (_, _, _) => throw new InvalidOperationException("dialog blew up");
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Contains("发现对话框打开失败", viewModel.StatusText);
    }

    [Fact]
    public async Task DiscoverAgents_ExistingDirectorySelection_AddsAndPersistsEntry()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService
        {
            OnDiscoverAgents = (_, directories, templates) =>
            {
                Assert.Equal(2, directories.Count);
                Assert.NotNull(templates);
                return Task.FromResult<DiscoveryDialogResult?>(new ExistingDirectorySelection(
                    new EntryDraft("three", "Three", directories[0].Directory, "pwsh")));
            },
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.Entries, entry => entry.Id == "three" && entry.Name == "Three");
        Assert.Contains("已新增按钮", viewModel.StatusText);
        Assert.Contains(LauncherStore.Load(config).Entries, entry => entry.Id == "three");
    }

    [Fact]
    public async Task DiscoverAgents_ExistingDirectorySelection_RollsBackOnSaveFailure()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService
        {
            OnDiscoverAgents = (_, directories, _) => Task.FromResult<DiscoveryDialogResult?>(
                new ExistingDirectorySelection(new EntryDraft("three", "Three", directories[0].Directory, "pwsh"))),
        };
        // CFA Step 1c: Use alphabetical sort mode so EnsureCustomOrderContainsEntries → ApplyFilter
        // doesn't call ValidateCustomOrder with stale custom order after save failure.
        var viewModel = CreateViewModel(config, settings,
            launcherSettings: new LauncherSettings
            {
                SortMode = LauncherEntryQuery.AlphabeticalSortModeId,
                EntryViewMode = LauncherEntryViewModes.CardsId,
            },
            dialogService: dialogService);
        File.Delete(config);
        Directory.CreateDirectory(config);

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Contains("存储保存失败", viewModel.StatusText);
    }
}
