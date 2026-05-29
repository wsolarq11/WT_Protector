using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherLaunchWorkflowTests
{
    [Fact]
    public void BuildPlanUsesGlobalConfirmationSetting()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new LauncherProcess());
        var entry = Entry("one", temp.Path);

        var directPlan = workflow.BuildPlan(entry, requireConfirmation: false);
        var confirmedPlan = workflow.BuildPlan(entry, requireConfirmation: true);

        Assert.False(directPlan.RequiresConfirmation);
        Assert.True(confirmedPlan.RequiresConfirmation);
    }

    [Fact]
    public void StartBatchReportsPlanFailuresWithoutStartingProcesses()
    {
        var workflow = new LauncherLaunchWorkflow(new LauncherProcess());
        var plannedCommands = new List<string>();

        var report = workflow.StartBatch(
            [
                Entry("one", "D:\\missing-one"),
                Entry("two", "D:\\missing-two"),
            ],
            requireConfirmation: false,
            plan => plannedCommands.Add(plan.CommandLine));

        Assert.Empty(plannedCommands);
        Assert.Contains("已启动 0 个勾选项，2 个失败", report.ToStatusText());
        Assert.Contains("one:", report.ToStatusText());
        Assert.Contains("two:", report.ToStatusText());
    }

    private static LaunchEntry Entry(string id, string directory)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = directory,
            Command = "pwsh",
            Confirm = false,
        };
    }
}
