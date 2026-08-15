using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
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

        Assert.True(LauncherStore.Load(settings).Settings.RequireConfirmationForLaunches);
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
        var dialogService = new StubDialogService();
        var confirmCalls = 0;
        dialogService.OnConfirmLaunch = _ =>
        {
            confirmCalls++;
            return Task.FromResult(false);
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            dialogService: dialogService);

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
        var dialogService = new StubDialogService();
        dialogService.OnConfirmLaunch = _ =>
            throw new InvalidOperationException("dialog unavailable");
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(startedPlans.Add)),
            dialogService: dialogService);

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
    public async Task SingleLaunchConfirmationFailureUsesPlanTimeEntrySnapshot()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex --version"));
        var startedPlans = new List<LaunchPlan>();
        LaunchEntry? externalSnapshot = null;
        LauncherSingleLaunchConfirmation? confirmation = null;
        var dialogService = new StubDialogService();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(startedPlans.Add)),
            dialogService: dialogService);
        dialogService.OnConfirmLaunch = candidate =>
        {
            confirmation = candidate;
            externalSnapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
            externalSnapshot = externalSnapshot! with { Name = EntryName.Empty };
            throw new InvalidOperationException("dialog unavailable");
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.NotNull(confirmation);
        Assert.Equal("one", confirmation!.EntryId);
        Assert.Equal("one", confirmation.EntryName);
        Assert.Equal("", externalSnapshot!.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动确认失败: one", viewModel.DryRunText);
        Assert.Contains("错误: dialog unavailable", viewModel.DryRunText);
        Assert.DoesNotContain("Entry name is required", viewModel.DryRunText);
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
        var dialogService = new StubDialogService();
        dialogService.OnConfirmLaunch = confirmation =>
        {
            confirmedPlan = confirmation.Plan;
            return Task.FromResult(true);
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            dialogService: dialogService);

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.NotNull(confirmedPlan);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动: one", viewModel.DryRunText);
        Assert.Contains(confirmedPlan!.CommandLine, viewModel.DryRunText);
        Assert.Equal("已请求启动: one", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchRequestedReceiptUsesPlanTimeEntrySnapshot()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex --version"));
        var startedPlans = new List<LaunchPlan>();
        LaunchEntry? externalSnapshot = null;
        LauncherSingleLaunchConfirmation? confirmation = null;
        var dialogService = new StubDialogService();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(startedPlans.Add)),
            dialogService: dialogService);
        dialogService.OnConfirmLaunch = candidate =>
        {
            confirmation = candidate;
            externalSnapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
            externalSnapshot = externalSnapshot! with { Name = EntryName.Empty };
            return Task.FromResult(true);
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.NotNull(confirmation);
        Assert.Equal("one", confirmation!.EntryId);
        Assert.Equal("one", confirmation.EntryName);
        Assert.Equal("", externalSnapshot!.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Single(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动: one", viewModel.DryRunText);
        Assert.DoesNotContain("Entry name is required", viewModel.DryRunText);
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
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
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
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(startedPlans.Add)));

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动失败: one", viewModel.DryRunText);
        Assert.Contains("找不到目录", viewModel.DryRunText);
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
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("blocked"))));

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动失败: one", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Contains("错误: blocked", viewModel.DryRunText);
        Assert.Equal("启动失败: one — blocked", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchIgnoresMutatedEntrySnapshot()
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
        var snapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
        snapshot = snapshot with { Name = EntryName.Empty };

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动: one", viewModel.DryRunText);
        Assert.DoesNotContain("旧单项回执", viewModel.DryRunText);
        Assert.DoesNotContain("Launch entry name is empty.", viewModel.DryRunText);
        Assert.Equal("", snapshot.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal("已请求启动: one", viewModel.StatusText);
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
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("预演: one", viewModel.StatusText);
    }

    [Fact]
    public void DryRunIgnoresMutatedEntrySnapshot()
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
        var snapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
        snapshot = snapshot with { Name = EntryName.Empty };

        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);

        Assert.Contains("wt", viewModel.DryRunText);
        Assert.DoesNotContain("旧预演", viewModel.DryRunText);
        Assert.DoesNotContain("Launch entry name is empty.", viewModel.DryRunText);
        Assert.Equal("", snapshot.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Name);
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
        var dialogService = new StubDialogService();
        dialogService.OnConfirmBatchLaunch = _ => Task.FromResult(true);
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            dialogService: dialogService);

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
    public async Task BatchLaunchPlanFailureShowsConfirmation()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: Path.Combine(temp.Path, "missing-one"), command: "codex", selected: true));
        var confirmCalls = 0;
        LauncherBatchLaunchConfirmation? confirmation = null;
        var dialogService = new StubDialogService();
        dialogService.OnConfirmBatchLaunch = candidate =>
        {
            confirmCalls++;
            confirmation = candidate;
            return Task.FromResult(false);
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            dialogService: dialogService);

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal(1, confirmCalls);
        Assert.NotNull(confirmation);
        Assert.Equal(1, confirmation!.FailedCount);
        Assert.Single(confirmation.FailureMessages);
        Assert.Contains("one", confirmation.FailureMessages[0]);
        Assert.Equal("已勾选项", confirmation.TargetLabel);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已取消: 已请求启动 0 项，未启动 0 项", viewModel.DryRunText);
        Assert.Equal("已取消: 批量启动 0 项", viewModel.StatusText);
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
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("• 已请求: one", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Equal("已请求启动 1 个勾选项", viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Success, viewModel.StatusTone);
    }

    [Fact]
    public async Task BatchLaunchCancellationReceivesPreflightedPlansAndStopsBeforeStart()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true),
            Entry("two", directory: temp.Path, command: "opencode", selected: true));
        LauncherBatchLaunchConfirmation? confirmation = null;
        IReadOnlyList<LauncherBatchLaunchItem>? confirmedItems = null;
        var dialogService = new StubDialogService();
        dialogService.OnConfirmBatchLaunch = candidate =>
        {
            confirmation = candidate;
            confirmedItems = candidate.Items.ToArray();
            return Task.FromResult(false);
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            dialogService: dialogService);

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.NotNull(confirmation);
        Assert.Equal("已勾选项", confirmation!.TargetLabel);
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
        var dialogService = new StubDialogService();
        dialogService.OnConfirmBatchLaunch = _ =>
            throw new InvalidOperationException("dialog unavailable");
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            launchWorkflow: new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(startedPlans.Add)),
            dialogService: dialogService);

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
    public async Task BatchLaunchIgnoresMutatedEntrySnapshot()
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
        var snapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
        snapshot = snapshot with { Name = EntryName.Empty };

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动 1 项，失败 0 项", viewModel.DryRunText);
        Assert.DoesNotContain("Launch entry name is empty.", viewModel.DryRunText);
        Assert.DoesNotContain("旧回执", viewModel.DryRunText);
        Assert.Equal("", snapshot.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal("已请求启动 1 个勾选项", viewModel.StatusText);
    }

    [Fact]
    public async Task SingleLaunchSaveConfigFailurePreservesSaveErrorStatus()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex"));
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false });

        // Replace the config directory with a file so Directory.CreateDirectory
        // fails when SaveConfig() tries to write — the only reliable way to
        // trigger a config-save failure in a temp-directory test.
        var configDir = Path.GetDirectoryName(config)!;
        Directory.Delete(configDir, recursive: true);
        File.WriteAllText(configDir, "blocked");

        await viewModel.Entries.Single(entry => entry.Id == "one").LaunchCommand.ExecuteAsync(null);

        // SaveConfig failed — StatusText MUST preserve the save-error message
        // set by ApplyStatusText, NOT be overwritten by "已请求启动".
        Assert.Contains("存储保存失败", viewModel.StatusText);
        Assert.DoesNotContain("已请求启动", viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Error, viewModel.StatusTone);

        // DryRunText still shows the launch receipt — the process was started.
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已请求启动: one", viewModel.DryRunText);

        // LaunchedAtUtc is recorded before save (new order: timestamp-then-save).
        // When save fails, the timestamp is in-memory only and would be lost on next refresh.
        var snapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
        Assert.NotEmpty(snapshot.LaunchedAtUtc);
    }

    [Fact]
    public void LaunchActionsUseOneTypedWorkflowField()
    {
        Assert.Null(typeof(MainWindowViewModel).GetField("_launcherProcess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.NotNull(typeof(MainWindowViewModel).GetField("_launchOrchestrator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.Null(typeof(MainWindowViewModel).GetMethod("BuildLaunchPlan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        // ponytail: LauncherProcess deleted — ViewModel takes only Action<LaunchPlan>.
    }
}
