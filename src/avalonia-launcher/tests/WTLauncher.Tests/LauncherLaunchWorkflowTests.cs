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
        Assert.Equal(0, report.StartedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("one:", StringComparison.Ordinal));
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("two:", StringComparison.Ordinal));
    }

    [Fact]
    public void StartBatchStartsEveryValidPlanAndReportsSuccess()
    {
        using var temp = new TempDirectory();
        var startedPlans = new List<LaunchPlan>();
        var previewPlans = new List<LaunchPlan>();
        var workflow = new LauncherLaunchWorkflow(plan => startedPlans.Add(plan));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
            ],
            requireConfirmation: true,
            previewPlans.Add);

        Assert.Equal(2, report.StartedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(["pwsh", "pwsh"], startedPlans.Select(plan => plan.Command).ToArray());
        Assert.All(startedPlans, plan => Assert.True(plan.RequiresConfirmation));
        Assert.Equal(startedPlans.Select(plan => plan.CommandLine), previewPlans.Select(plan => plan.CommandLine));
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
