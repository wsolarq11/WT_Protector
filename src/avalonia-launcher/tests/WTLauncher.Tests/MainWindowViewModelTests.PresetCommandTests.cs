using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task LaunchGroupCommandsCreateUpdateAndDeletePortablePresets()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("blank-manual", "Manual", locked: true));

        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = _ => Task.FromResult<string?>("Project Pair"));

        Assert.True(viewModel.SaveSelectionAsPresetCommand.CanExecute(null));

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal("project-pair", viewModel.SelectedPreset?.Id);
        var presetFile = LauncherPresetStore.Load(presets);
        var created = presetFile.Presets.Single(preset => preset.Id == "project-pair");
        Assert.Equal("Project Pair", created.Name);
        Assert.Equal(["one"], created.EntryIds);
        Assert.False(created.Locked);
        Assert.False(viewModel.ApplyPresetCommand.CanExecute(null));

        viewModel.Entries.Single(entry => entry.Id == "two").IsSelected = true;

        Assert.True(viewModel.UpdateSelectedPresetCommand.CanExecute(null));

        viewModel.UpdateSelectedPresetCommand.Execute(null);

        presetFile = LauncherPresetStore.Load(presets);
        created = presetFile.Presets.Single(preset => preset.Id == "project-pair");
        Assert.Equal(["one", "two"], created.EntryIds);
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));

        viewModel.ConfirmDeletePresetAsync = preset => Task.FromResult(preset.PresetId == "project-pair");

        Assert.True(viewModel.DeleteSelectedPresetCommand.CanExecute(null));

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        presetFile = LauncherPresetStore.Load(presets);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "project-pair");
        Assert.Contains(presetFile.Presets, preset => preset.Id == "daily");
        Assert.Equal("daily", presetFile.DefaultPresetId);
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal("已删除启动组: Project Pair；当前启动组: Daily", viewModel.StatusText);

        viewModel.SelectedPreset = viewModel.Presets.Single(preset => preset.Id == "blank-manual");

        Assert.True(viewModel.SelectedPreset.IsLocked);
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));
        Assert.False(viewModel.DeleteSelectedPresetCommand.CanExecute(null));
        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
    }

    [Fact]
    public void ChangingSelectedPresetNotifiesPresetManagementCommands()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("blank-manual", "Manual", locked: true));

        var viewModel = CreateViewModel(config, settings);
        var applyNotifications = 0;
        var previewNotifications = 0;
        var launchNotifications = 0;
        var updateNotifications = 0;
        var deleteNotifications = 0;

        viewModel.ApplyPresetCommand.CanExecuteChanged += (_, _) => applyNotifications++;
        viewModel.PreviewSelectedPresetCommand.CanExecuteChanged += (_, _) => previewNotifications++;
        viewModel.LaunchSelectedPresetCommand.CanExecuteChanged += (_, _) => launchNotifications++;
        viewModel.UpdateSelectedPresetCommand.CanExecuteChanged += (_, _) => updateNotifications++;
        viewModel.DeleteSelectedPresetCommand.CanExecuteChanged += (_, _) => deleteNotifications++;

        viewModel.SelectedPreset = viewModel.Presets.Single(preset => preset.Id == "blank-manual");

        Assert.True(applyNotifications > 0);
        Assert.True(previewNotifications > 0);
        Assert.True(launchNotifications > 0);
        Assert.True(updateNotifications > 0);
        Assert.True(deleteNotifications > 0);
    }

    [Fact]
    public void LaunchGroupUpdateCommandRequiresSelectedEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var viewModel = CreateViewModel(config, settings);

        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));
    }

    [Fact]
    public async Task LaunchSelectedPresetStartsPresetEntriesWithoutPersistingSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false),
            Entry("two", directory: temp.Path, command: "opencode", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        Assert.True(viewModel.LaunchSelectedPresetCommand.CanExecute(null));

        await viewModel.LaunchSelectedPresetCommand.ExecuteAsync(null);

        Assert.Equal(["codex", "opencode"], startedPlans.Select(plan => plan.Command).ToArray());
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("• 已请求: one", viewModel.DryRunText);
        Assert.Contains("• 已请求: two", viewModel.DryRunText);
        Assert.Equal("启动组“Daily”: 已请求启动 2 个启动项", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public async Task LaunchSelectedPresetDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", "One", directory: temp.Path, command: "codex"),
            Entry("two", "Two", directory: temp.Path, command: "opencode"),
            Entry("three", "Three", directory: temp.Path));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var launcherSettings = CreateDragSettings();
        launcherSettings.RequireConfirmationForLaunches = false;
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.LaunchSelectedPresetCommand.ExecuteAsync(null);

        Assert.Equal(["codex", "opencode"], startedPlans.Select(plan => plan.Command).ToArray());
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
        Assert.All(LoadEntriesById(config), entry => Assert.False(entry.Selected));
    }

    [Fact]
    public void PreviewSelectedPresetShowsGeneratedCommandLinesWithoutStartingOrPersistingSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false),
            Entry("two", directory: temp.Path, command: "opencode", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        viewModel.PreviewSelectedPresetCommand.Execute(null);

        Assert.Empty(startedPlans);
        Assert.Contains("• one", viewModel.DryRunText);
        Assert.Contains("• two", viewModel.DryRunText);
        Assert.Contains("wt.exe", viewModel.DryRunText);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.Contains("opencode", viewModel.DryRunText);
        Assert.Equal("预演启动组: Daily（2 项）", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public void PreviewSelectedPresetDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", "One", directory: temp.Path, command: "codex"),
            Entry("two", "Two", directory: temp.Path, command: "opencode"),
            Entry("three", "Three", directory: temp.Path));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var launcherSettings = CreateDragSettings();
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.PreviewSelectedPresetCommand.Execute(null);

        Assert.Empty(startedPlans);
        Assert.Contains("wt.exe", viewModel.DryRunText);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
        Assert.All(LoadEntriesById(config), entry => Assert.False(entry.Selected));
    }

    [Fact]
    public void PreviewSelectedPresetReportsPlanFailuresWithoutStartingOrPersistingSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: Path.Combine(temp.Path, "missing"), command: "codex", selected: false),
            Entry("two", directory: temp.Path, command: "opencode", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        viewModel.PreviewSelectedPresetCommand.Execute(null);

        Assert.Empty(startedPlans);
        Assert.Contains("预演回执", viewModel.DryRunText);
        Assert.Contains("预演失败: 失败 1 项，未启动 1 项", viewModel.DryRunText);
        Assert.Contains("• 未启动: two", viewModel.DryRunText);
        Assert.Contains("opencode", viewModel.DryRunText);
        Assert.Contains("one:", viewModel.DryRunText);
        Assert.Contains("directory does not exist", viewModel.DryRunText);
        Assert.Equal("启动组“Daily”预演失败: 1 个失败", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public void PreviewSelectedPresetIgnoresMutatedEntrySnapshot()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        viewModel.DryRunText = "旧启动组预演";
        var snapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
        snapshot = snapshot with { Name = "" };

        viewModel.PreviewSelectedPresetCommand.Execute(null);

        Assert.Empty(startedPlans);
        Assert.Contains("codex", viewModel.DryRunText);
        Assert.DoesNotContain("Launch entry name is empty.", viewModel.DryRunText);
        Assert.DoesNotContain("旧启动组预演", viewModel.DryRunText);
        Assert.Equal("", snapshot.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal("预演启动组: Daily（1 项）", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public async Task LaunchSelectedPresetCancellationReceivesPreflightedPlansAndStartsNothing()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false),
            Entry("two", directory: temp.Path, command: "opencode", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var startedPlans = new List<LaunchPlan>();
        LauncherBatchLaunchConfirmation? confirmation = null;
        IReadOnlyList<LauncherBatchLaunchItem>? confirmedItems = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = candidate =>
            {
                confirmation = candidate;
                confirmedItems = candidate.Items.ToArray();
                return Task.FromResult(false);
            },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.LaunchSelectedPresetCommand.ExecuteAsync(null);

        Assert.NotNull(confirmation);
        Assert.Equal("启动组“Daily”条目", confirmation!.TargetLabel);
        Assert.NotNull(confirmedItems);
        Assert.Equal(["one", "two"], confirmedItems!.Select(item => item.EntryId).ToArray());
        Assert.Equal(["one", "two"], confirmedItems.Select(item => item.EntryName).ToArray());
        Assert.All(confirmedItems, item => Assert.Contains("wt.exe", item.Plan.CommandLine));
        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("已取消: 已请求启动 0 项，未启动 2 项", viewModel.DryRunText);
        Assert.Contains("• 未启动: one", viewModel.DryRunText);
        Assert.Contains("• 未启动: two", viewModel.DryRunText);
        Assert.Equal("已取消: 启动组 Daily（2 项）", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public async Task LaunchSelectedPresetConfirmationAcceptedStartsAllEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false),
            Entry("two", directory: temp.Path, command: "opencode", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var startedPlans = new List<LaunchPlan>();
        LauncherBatchLaunchConfirmation? confirmation = null;
        IReadOnlyList<LauncherBatchLaunchItem>? confirmedItems = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = candidate =>
            {
                confirmation = candidate;
                confirmedItems = candidate.Items.ToArray();
                return Task.FromResult(true);
            },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.LaunchSelectedPresetCommand.ExecuteAsync(null);

        Assert.NotNull(confirmation);
        Assert.Equal("启动组“Daily”条目", confirmation!.TargetLabel);
        Assert.NotNull(confirmedItems);
        Assert.Equal(["one", "two"], confirmedItems!.Select(item => item.EntryId).ToArray());
        Assert.Equal(["one", "two"], confirmedItems.Select(item => item.EntryName).ToArray());
        Assert.All(confirmedItems, item => Assert.Contains("wt.exe", item.Plan.CommandLine));
        Assert.Equal(["codex", "opencode"], startedPlans.Select(plan => plan.Command).ToArray());
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("• 已请求: one", viewModel.DryRunText);
        Assert.Contains("• 已请求: two", viewModel.DryRunText);
        Assert.Equal("启动组“Daily”: 已请求启动 2 个启动项", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public async Task LaunchSelectedPresetWithoutConfirmationDoesNotInvokeDelegate()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false),
            Entry("two", directory: temp.Path, command: "opencode", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var startedPlans = new List<LaunchPlan>();
        var confirmCalls = 0;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            vm => vm.ConfirmBatchLaunchAsync = _ =>
            {
                confirmCalls++;
                return Task.FromResult(true);
            },
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.LaunchSelectedPresetCommand.ExecuteAsync(null);

        Assert.Equal(0, confirmCalls);
        Assert.Equal(["codex", "opencode"], startedPlans.Select(plan => plan.Command).ToArray());
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("• 已请求: one", viewModel.DryRunText);
        Assert.Contains("• 已请求: two", viewModel.DryRunText);
        Assert.Equal("启动组“Daily”: 已请求启动 2 个启动项", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public async Task LaunchSelectedPresetConfirmationFailurePreservesPresetNameAndStartsNothing()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: false));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var startedPlans = new List<LaunchPlan>();
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = _ => throw new InvalidOperationException("dialog unavailable"),
            launchWorkflow: new LauncherLaunchWorkflow(startedPlans.Add));

        await viewModel.LaunchSelectedPresetCommand.ExecuteAsync(null);

        Assert.Empty(startedPlans);
        Assert.Contains("启动回执", viewModel.DryRunText);
        Assert.Contains("启动确认失败: dialog unavailable", viewModel.DryRunText);
        Assert.Contains("已请求启动 0 项，未启动 1 项", viewModel.DryRunText);
        Assert.Contains("• 未启动: one", viewModel.DryRunText);
        Assert.Contains(LaunchArgumentBuilder.TerminalExecutable, viewModel.DryRunText);
        Assert.Equal("启动组“Daily”启动失败: dialog unavailable", viewModel.StatusText);
        var entries = LoadEntriesById(config);
        Assert.All(entries, entry => Assert.False(entry.Selected));
    }

    [Fact]
    public void BrokenPresetCannotLaunch()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        SavePresetFile(presets, "broken", Preset("broken", "Broken", "one", "missing-entry"));

        var viewModel = CreateViewModel(config, settings);

        Assert.False(viewModel.PreviewSelectedPresetCommand.CanExecute(null));
        Assert.False(viewModel.LaunchSelectedPresetCommand.CanExecute(null));
    }

    [Fact]
    public async Task LaunchGroupDeleteCancellationLeavesPresetFileUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.ConfirmDeletePresetAsync = _ => Task.FromResult(false));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("已取消: 删除启动组 Daily", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupDeleteConfirmationFailureLeavesPresetFileUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.ConfirmDeletePresetAsync = _ =>
                throw new InvalidOperationException("dialog unavailable"));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("删除启动组确认失败: Daily — dialog unavailable", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupDeleteCancellationKeepsPresetSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        LauncherPresetDeleteTarget? target = null;
        viewModel.ConfirmDeletePresetAsync = candidate =>
        {
            target = candidate;
            return Task.FromResult(false);
        };

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal("daily", target!.PresetId);
        Assert.Equal("Daily", target.PresetName);
        Assert.Equal(1, target.EntryCount);
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal("Daily", viewModel.SelectedPreset?.Name);
        Assert.Equal(["one"], viewModel.SelectedPreset!.EntryIds.ToArray());
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal("已取消: 删除启动组 Daily", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupDeleteUsesConfirmationSnapshotForTargetAndStatus()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        LauncherPresetDeleteTarget? target = null;
        viewModel.ConfirmDeletePresetAsync = candidate =>
        {
            target = candidate;
            return Task.FromResult(true);
        };

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal("daily", target!.PresetId);
        Assert.Equal("Daily", target.PresetName);
        Assert.Equal(1, target.EntryCount);
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "daily");
        Assert.Equal("secondary", presetFile.DefaultPresetId);
        Assert.Equal("secondary", viewModel.SelectedPreset?.Id);
        Assert.Equal("已删除启动组: Daily；当前启动组: Secondary", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupDeleteDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.ConfirmDeletePresetAsync = _ => Task.FromResult(true));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "daily");
        Assert.Equal("secondary", viewModel.SelectedPreset?.Id);
    }
}
