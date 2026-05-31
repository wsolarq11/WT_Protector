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

        var report = workflow.StartBatch(
            [
                Entry("one", "D:\\missing-one"),
                Entry("two", "D:\\missing-two"),
            ],
            requireConfirmation: false);

        Assert.Null(report.LastCommandLine);
        Assert.Equal(0, report.StartedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("one:", StringComparison.Ordinal));
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("two:", StringComparison.Ordinal));
    }

    [Fact]
    public void StartBatchRejectsNullEntryRows()
    {
        var workflow = new LauncherLaunchWorkflow(new LauncherProcess());

        var exception = Assert.Throws<ArgumentNullException>(() =>
            workflow.StartBatch([null!], requireConfirmation: false));

        Assert.Equal("entry", exception.ParamName);
    }

    [Fact]
    public void StartBatchKeepsLastCommandLineWhenStartFails()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("blocked"));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
            ],
            requireConfirmation: false);

        Assert.Equal(0, report.StartedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Contains("one: blocked", Assert.Single(report.FailureMessages), StringComparison.Ordinal);
        Assert.Contains("pwsh", report.LastCommandLine);
    }

    [Fact]
    public void StartBatchStartsEveryValidPlanAndReportsSuccess()
    {
        using var temp = new TempDirectory();
        var startedPlans = new List<LaunchPlan>();
        var workflow = new LauncherLaunchWorkflow(plan => startedPlans.Add(plan));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
            ],
            requireConfirmation: true);

        Assert.Equal(2, report.StartedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(["pwsh", "pwsh"], startedPlans.Select(plan => plan.Command).ToArray());
        Assert.All(startedPlans, plan => Assert.True(plan.RequiresConfirmation));
        Assert.Equal(startedPlans.Last().CommandLine, report.LastCommandLine);
    }

    private static LaunchEntry Entry(string id, string directory)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = directory,
            Command = "pwsh",
        };
    }
}
