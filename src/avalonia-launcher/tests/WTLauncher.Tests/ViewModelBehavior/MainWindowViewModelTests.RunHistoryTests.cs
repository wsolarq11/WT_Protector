using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task SingleLaunch_WritesRequestedHistoryAndPersists()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", directory: temp.Path));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => { })));

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        var history = LauncherStore.Load(config).RunHistory;
        var entry = Assert.Single(history);
        Assert.Equal("one", entry.EntryId);
        Assert.Equal(nameof(LauncherRunStatus.Requested), entry.Status);
        Assert.Equal(nameof(LauncherRunSource.Individual), entry.Source);
        Assert.Equal(1, viewModel.RunHistoryCount);
    }

    [Fact]
    public async Task SingleLaunchCancel_WritesCanceledHistory()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", directory: temp.Path));
        var dialog = new StubDialogService
        {
            OnConfirmLaunch = _ => Task.FromResult(false),
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            dialogService: dialog);

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        var history = LauncherStore.Load(config).RunHistory;
        var entry = Assert.Single(history);
        Assert.Equal(nameof(LauncherRunStatus.Canceled), entry.Status);
    }

    [Fact]
    public async Task BatchLaunch_WritesRequestedAndFailedHistory()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", directory: temp.Path),
            Entry("two", "Two", directory: temp.Path, command: "$missing"));
        var dialog = new StubDialogService
        {
            OnConfirmBatchLaunch = _ => Task.FromResult(true),
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => { })),
            dialogService: dialog);
        viewModel.Entries.Single(entry => entry.Id == "one").IsSelected = true;
        viewModel.Entries.Single(entry => entry.Id == "two").IsSelected = true;

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        var history = LauncherStore.Load(config).RunHistory;
        Assert.Contains(history, item => item.EntryId == "one" && item.Status == nameof(LauncherRunStatus.Requested));
        Assert.Contains(history, item => item.EntryId == "two" && item.Status == nameof(LauncherRunStatus.Failed));
        Assert.All(history, item => Assert.Equal(nameof(LauncherRunSource.Selection), item.Source));
    }

    [Fact]
    public void CorruptRunHistory_DegradesToEmptyWithoutBlockingConfigLoad()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One", directory: temp.Path));
        var store = LauncherStore.Load(config);
        LauncherStore.Save(
            store with
            {
                RunHistory =
                [
                    new LauncherRunHistoryEntry
                    {
                        Id = "bad",
                        EntryId = "one",
                        EntryName = "One",
                        TimestampUtc = "",
                        Source = "Unknown",
                        Status = "Unknown",
                    },
                ],
            },
            config,
            backup: false);

        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(0, viewModel.RunHistoryCount);
    }
}
