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
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = temp.Path,
                Command = "codex --dangerously-bypass-approvals-and-sandbox",
                Confirm = true,
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "two",
                Directory = temp.Path,
                Command = "pwsh",
                Confirm = false,
            },
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
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = temp.Path,
                Command = "codex --version",
                Confirm = true,
            });
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
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = Path.Combine(temp.Path, "missing-one"),
                Command = "codex",
                Confirm = false,
                Selected = true,
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "two",
                Directory = Path.Combine(temp.Path, "missing-two"),
                Command = "opencode",
                Confirm = false,
                Selected = true,
            });
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
