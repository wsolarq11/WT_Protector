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
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已取消: one", viewModel.DryRunText);
        Assert.Contains(LaunchArgumentBuilder.TerminalExecutable, viewModel.DryRunText);
        Assert.Equal("已取消: one", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchConfirmationFailureShowsCommandReceiptAndStartsNothing()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex --version"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmLaunchAsync = (_, _) =>
                throw new InvalidOperationException("dialog unavailable"),
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动确认失败: one", viewModel.DryRunText);
        Assert.Contains(LaunchArgumentBuilder.TerminalExecutable, viewModel.DryRunText);
        Assert.Contains("codex --version", viewModel.DryRunText);
        Assert.Contains("错误: dialog unavailable", viewModel.DryRunText);
        Assert.Equal("启动失败: one — dialog unavailable", viewModel.StatusText);
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
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动: one", viewModel.DryRunText);
        Assert.Contains(confirmedPlan!.CommandLine, viewModel.DryRunText);
        Assert.Equal("已请求启动: one", viewModel.StatusText);
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
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动: one", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Equal("已请求启动: one", viewModel.StatusText);
    }


    [Fact]
    public async Task SingleLaunchPlanFailureShowsReceiptWithoutStartingProcess()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: Path.Combine(temp.Path, "missing"), command: "codex"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动失败: one", viewModel.DryRunText);
        Assert.Contains("directory does not exist", viewModel.DryRunText);
        Assert.DoesNotContain(LaunchArgumentBuilder.TerminalExecutable, viewModel.DryRunText);
        Assert.Contains("启动失败: one", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchStartFailureShowsCommandReceipt()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            launchWorkflow: new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("blocked")));

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动失败: one", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Contains("错误: blocked", viewModel.DryRunText);
        Assert.Equal("启动失败: one — blocked", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchUnexpectedFailureReplacesStaleReceiptWithoutInventingName()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        viewModel.DryRunText = "旧单项回执";
        viewModel.Entries.Single(entry => entry.Id == "one").Entry.Name = "";

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动失败: Launch entry name is empty.", viewModel.DryRunText);
        Assert.DoesNotContain("旧单项回执", viewModel.DryRunText);
        Assert.DoesNotContain("(unnamed)", viewModel.DryRunText);
        Assert.Equal("启动失败: Launch entry name is empty.", viewModel.StatusText);
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
    public void DryRunUnexpectedFailureReplacesStaleReceiptWithoutInventingName()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        viewModel.DryRunText = "旧预演";
        viewModel.Entries.Single(entry => entry.Id == "one").Entry.Name = "";

        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);

        Assert.Contains("预演回执", viewModel.DryRunText);
        Assert.Contains("预演失败: Launch entry name is empty.", viewModel.DryRunText);
        Assert.DoesNotContain("旧预演", viewModel.DryRunText);
        Assert.DoesNotContain("(unnamed)", viewModel.DryRunText);
        Assert.Equal("预演失败: Launch entry name is empty.", viewModel.StatusText);
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

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 项，失败 2 项，未启动 0 项", viewModel.DryRunText);
        Assert.Contains("one:", viewModel.DryRunText);
        Assert.Contains("two:", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 个勾选项，2 个失败", viewModel.StatusText);
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
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 项，失败 1 项，未启动 0 项", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 个勾选项，1 个失败", viewModel.StatusText);
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
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("• 已请求: one", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Equal("已请求启动 1 个勾选项", viewModel.StatusText);
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
        Assert.Equal(["one", "two"], confirmedItems!.Select(item => item.EntryId).ToArray());
        Assert.Equal(["one", "two"], confirmedItems.Select(item => item.EntryName).ToArray());
        Assert.All(confirmedItems, item => Assert.Contains("wt.exe", item.Plan.CommandLine));
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已取消: 已请求启动 0 项，未启动 2 项", viewModel.DryRunText);
        Assert.Contains("• 未启动: one", viewModel.DryRunText);
        Assert.Contains("• 未启动: two", viewModel.DryRunText);
        Assert.Equal("已取消: 批量启动 2 项", viewModel.StatusText);
    }

    [Fact]
    public async Task BatchLaunchConfirmationFailureShowsPreflightReceiptAndStartsNothing()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true),
            Entry("two", directory: temp.Path, command: "opencode", selected: true));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = _ =>
                throw new InvalidOperationException("dialog unavailable"),
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动确认失败: dialog unavailable", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 项，未启动 2 项", viewModel.DryRunText);
        Assert.Contains("• 未启动: one", viewModel.DryRunText);
        Assert.Contains("• 未启动: two", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Contains("opencode", viewModel.DryRunText);
        Assert.Equal("批量启动失败: dialog unavailable", viewModel.StatusText);
    }

    [Fact]
    public async Task BatchLaunchPreflightFailureReplacesStaleReceipt()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        viewModel.DryRunText = "旧回执";
        viewModel.Entries.Single(entry => entry.Id == "one").Entry.Name = "";

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 项，失败 1 项，未启动 0 项", viewModel.DryRunText);
        Assert.Contains("Launch entry name is empty.", viewModel.DryRunText);
        Assert.DoesNotContain("旧回执", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 个勾选项，1 个失败", viewModel.StatusText);
        Assert.Contains("Launch entry name is empty.", viewModel.StatusText);
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
