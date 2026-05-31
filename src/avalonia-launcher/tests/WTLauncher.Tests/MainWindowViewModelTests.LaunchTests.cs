using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void GlobalConfirmationSwitchPersistsAndAppliesUniformlyToAllEntries()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            Entry(
                "one",
                directory: temp.Path,
                command: "codex --dangerously-bypass-approvals-and-sandbox"),
            Entry("two", directory: temp.Path),
        };
        var (config, settings, _) = CreatePortableFixture(temp, entries);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        Assert.False(viewModel.RequireConfirmationForLaunches);
        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);
        Assert.Equal("预演: one", viewModel.StatusText);

        viewModel.RequireConfirmationForLaunches = true;

        Assert.True(LauncherSettingsStore.Load(settings).RequireConfirmationForLaunches);
        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);
        Assert.Equal("预演: one（需要确认）", viewModel.StatusText);
        viewModel.Entries.Single(entry => entry.Id == "two").DryRunCommand.Execute(null);
        Assert.Equal("预演: two（需要确认）", viewModel.StatusText);

        viewModel.FilterText = "two";

        Assert.Equal(1, viewModel.VisibleCount);
    }

    [Fact]
    public async Task SingleLaunchCancellationStopsAfterConfirmationAndKeepsDryRunPlan()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex --version"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true });
        var confirmCalls = 0;
        viewModel.ConfirmLaunchAsync = (_, _) =>
        {
            confirmCalls++;
            return Task.FromResult(false);
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Equal(1, confirmCalls);
        Assert.StartsWith(
            $"{LaunchArgumentBuilder.TerminalExecutable} {LaunchArgumentBuilder.WorkingDirectoryOption} ",
            viewModel.DryRunText,
            StringComparison.Ordinal);
        Assert.Equal("已取消: one", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchStartsAfterConfirmationAcceptsPlan()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex --version"));
        LaunchPlan? confirmedPlan = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmLaunchAsync = (_, plan) =>
            {
                confirmedPlan = plan;
                return Task.FromResult(true);
            });

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.NotNull(confirmedPlan);
        Assert.Equal(confirmedPlan!.CommandLine, viewModel.DryRunText);
        Assert.Equal("已启动: one", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"),
            Entry("two", directory: temp.Path, command: "opencode"),
            Entry("three", directory: temp.Path, command: "pwsh"));
        var launcherSettings = CreateDragSettings();
        launcherSettings.RequireConfirmationForLaunches = false;
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("已启动: one", viewModel.StatusText);
    }

    [Fact]
    public void DryRunDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"),
            Entry("two", directory: temp.Path, command: "opencode"),
            Entry("three", directory: temp.Path, command: "pwsh"));
        var launcherSettings = CreateDragSettings();
        launcherSettings.RequireConfirmationForLaunches = false;
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("预演: one", viewModel.StatusText);
    }

    [Fact]
    public async Task BatchLaunchReportsPerEntryPlanFailuresWithoutStartingProcesses()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: Path.Combine(temp.Path, "missing-one"), command: "codex", selected: true),
            Entry("two", directory: Path.Combine(temp.Path, "missing-two"), command: "opencode", selected: true));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal("选择一个启动项查看预演命令。", viewModel.DryRunText);
        Assert.Contains("已启动 0 个勾选项，2 个失败", viewModel.StatusText);
        Assert.Contains("one:", viewModel.StatusText);
        Assert.Contains("two:", viewModel.StatusText);
    }

    [Fact]
    public async Task BatchLaunchPlanFailureSkipsConfirmation()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: Path.Combine(temp.Path, "missing-one"), command: "codex", selected: true));
        var confirmCalls = 0;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = _ =>
            {
                confirmCalls++;
                return Task.FromResult(true);
            });

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal(0, confirmCalls);
        Assert.Equal("选择一个启动项查看预演命令。", viewModel.DryRunText);
        Assert.Contains("已启动 0 个勾选项，1 个失败", viewModel.StatusText);
    }

    [Fact]
    public async Task BatchLaunchDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true),
            Entry("two", directory: temp.Path, command: "opencode"),
            Entry("three", directory: temp.Path, command: "pwsh"));
        var launcherSettings = CreateDragSettings();
        launcherSettings.RequireConfirmationForLaunches = false;
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("已启动 1 个勾选项", viewModel.StatusText);
    }

    [Fact]
    public async Task BatchLaunchCancellationReceivesPreflightedPlansAndStopsBeforeStart()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true),
            Entry("two", directory: temp.Path, command: "opencode", selected: true));
        IReadOnlyList<LauncherBatchLaunchItem>? confirmedItems = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = items =>
            {
                confirmedItems = items.ToArray();
                return Task.FromResult(false);
            });

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.NotNull(confirmedItems);
        Assert.Equal(["one", "two"], confirmedItems!.Select(item => item.Entry.Id).ToArray());
        Assert.All(confirmedItems, item => Assert.Contains("wt.exe", item.Plan.CommandLine));
        Assert.Equal("选择一个启动项查看预演命令。", viewModel.DryRunText);
        Assert.Equal("已取消: 批量启动 2 项", viewModel.StatusText);
    }

    [Fact]
    public void LaunchActionsUseOneTypedWorkflowField()
    {
        Assert.Null(typeof(MainWindowViewModel).GetField("_launcherProcess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.NotNull(typeof(MainWindowViewModel).GetField("_launchWorkflow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.Null(typeof(MainWindowViewModel).GetMethod("BuildLaunchPlan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetConstructors().SelectMany(constructor => constructor.GetParameters()),
            parameter => parameter.ParameterType == typeof(LauncherProcess));
    }
}
