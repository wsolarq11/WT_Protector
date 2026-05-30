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
                command: "codex --dangerously-bypass-approvals-and-sandbox",
                confirm: true),
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
            Entry("one", directory: temp.Path, command: "codex --version", confirm: true));
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
        Assert.StartsWith("wt.exe -d ", viewModel.DryRunText, StringComparison.Ordinal);
        Assert.Equal("已取消: one", viewModel.StatusText);
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
    public async Task BatchLaunchCancellationStopsBeforeBuildingPlans()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", directory: temp.Path, command: "codex", selected: true),
            Entry("two", directory: temp.Path, command: "opencode", selected: true));
        IReadOnlyList<string>? confirmedEntryIds = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = true },
            vm => vm.ConfirmBatchLaunchAsync = entries =>
            {
                confirmedEntryIds = entries.Select(entry => entry.Id).ToArray();
                return Task.FromResult(false);
            });

        await viewModel.LaunchSelectedCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], confirmedEntryIds);
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
