using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

// task 07-11-entry-launch-count-frecency — launchCount 自增 + 持久化 + 回滚行为。
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task SingleLaunch_IncrementsLaunchCount()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        Assert.Equal(0, viewModel.Entries.Single(e => e.Id == "one").SnapshotEntry().LaunchCount);

        await viewModel.Entries.Single(e => e.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Entries.Single(e => e.Id == "one").SnapshotEntry().LaunchCount);
    }

    [Fact]
    public async Task SingleLaunch_PersistsIncrementedLaunchCountAcrossReload()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        await viewModel.Entries.Single(e => e.Id == "one").LaunchCommand.ExecuteAsync(null);
        await viewModel.Entries.Single(e => e.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Single(e => e.Id == "one").SnapshotEntry().LaunchCount);

        var reloaded = LauncherStore.Load(config);
        Assert.Equal(2, reloaded.Entries.Single(e => e.Id == "one").LaunchCount);
    }

    [Fact]
    public async Task BatchLaunch_IncrementsLaunchCountForEachSelected()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            Entry("one", directory: temp.Path, command: "codex", selected: true),
            Entry("two", directory: temp.Path, command: "opencode", selected: true),
            Entry("three", directory: temp.Path, command: "pwsh", selected: false),
        };
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var dialogService = new StubDialogService { OnConfirmBatchLaunch = _ => Task.FromResult(true) };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            dialogService: dialogService);

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Entries.Single(e => e.Id == "one").SnapshotEntry().LaunchCount);
        Assert.Equal(1, viewModel.Entries.Single(e => e.Id == "two").SnapshotEntry().LaunchCount);
        Assert.Equal(0, viewModel.Entries.Single(e => e.Id == "three").SnapshotEntry().LaunchCount);
    }

    [Fact]
    public async Task LaunchCount_InMemoryIncrement_WhenSaveFails()
    {
        // ponytail: launch 路径与 LaunchedAtUtc 同模式 — RecordLaunchFromOwner 在 save 之前跑,
        // save 失败时 in-memory 保留新增的 count,磁盘保留旧值(无 launch-level rollback,by design)。
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        var configDir = Path.GetDirectoryName(config)!;
        Directory.Delete(configDir, recursive: true);
        File.WriteAllText(configDir, "blocked");

        await viewModel.Entries.Single(e => e.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Entries.Single(e => e.Id == "one").SnapshotEntry().LaunchCount);
        Assert.Contains("存储保存失败", viewModel.StatusText);
    }

    [Fact]
    public async Task FrequentSortMode_PersistsAcrossSaveLoad()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings
            {
                RequireConfirmationForLaunches = false,
                SortMode = LauncherEntryQuery.FrequentSortModeId,
            });

        Assert.Equal(LauncherEntryQuery.FrequentSortModeId, viewModel.SelectedSortMode!.Id);

        // 触发任意 save 路径(选中切换会 save)。
        viewModel.Entries.Single(e => e.Id == "one").IsSelected = true;

        var reloaded = LauncherStore.Load(config);
        Assert.Equal(LauncherEntryQuery.FrequentSortModeId, reloaded.Settings.SortMode);
    }

    [Fact]
    public async Task Launch_LegacyEntriesWithoutLaunchCount_LoadsAsZero()
    {
        // 前向兼容:旧 store 无 launchCount 字段时,反序列化为 0。
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        // 手动抹掉 launchCount(模拟旧 store)。
        var json = File.ReadAllText(config);
        // launchCount 不在 JSON 中(Entry helper 没写),保留原样加载。
        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(0, viewModel.Entries.Single(e => e.Id == "one").SnapshotEntry().LaunchCount);
    }
}
