using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class LauncherLaunchWorkflowTests
{
    [Fact]
    public void BuildPlanUsesGlobalConfirmationSetting()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => { }));
        var entry = Entry("one", temp.Path);

        var directPlan = workflow.BuildPlan(entry, requireConfirmation: false);
        var confirmedPlan = workflow.BuildPlan(entry, requireConfirmation: true);

        Assert.False(directPlan.RequiresConfirmation);
        Assert.True(confirmedPlan.RequiresConfirmation);
    }

    [Fact]
    public void StartBatchReportsPlanFailuresWithoutStartingProcesses()
    {
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => { }));

        var report = workflow.StartBatch(
            [
                Entry("one", "D:\\missing-one"),
                Entry("two", "D:\\missing-two"),
            ],
            requireConfirmation: false);

        Assert.Null(report.LastCommandLine);
        Assert.Equal(0, report.RequestedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("one:", StringComparison.Ordinal));
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("two:", StringComparison.Ordinal));
    }

    [Fact]
    public void PreflightBatchBuildsPlansWithoutStartingProcesses()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("should not start")));

        var preflight = workflow.PreflightBatch(
            [
                Entry("one", temp.Path),
            ],
            requireConfirmation: true);

        var item = Assert.Single(preflight.Items);
        Assert.Equal("one", item.EntryId);
        Assert.Equal("one", item.EntryName);
        Assert.True(item.Plan.RequiresConfirmation);
        Assert.Equal(0, preflight.Report.FailedCount);
        Assert.Equal(0, preflight.Report.RequestedCount);
    }

    [Fact]
    public void PreflightItemsDoNotExposeMutableStorage()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("should not start")));

        var preflight = workflow.PreflightBatch(
            [
                Entry("one", temp.Path),
            ],
            requireConfirmation: false);
        var items = Assert.IsAssignableFrom<IList<LauncherBatchLaunchItem>>(preflight.Items);

        Assert.True(items.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => items.Add(Assert.Single(preflight.Items)));
        Assert.Single(preflight.Items);
    }

    [Fact]
    public void StartBatchDoesNotMutatePreflightReport()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("should not start")));

        var preflight = workflow.PreflightBatch(
            [
                Entry("valid", temp.Path),
                Entry("missing", Path.Combine(temp.Path, "missing")),
            ],
            requireConfirmation: false);

        Assert.Single(preflight.Items);
        Assert.Equal(1, preflight.Report.FailedCount);
        Assert.Equal(0, preflight.Report.RequestedCount);
        Assert.Equal(0, preflight.Report.SkippedCount);

        var report = workflow.StartBatch(preflight, TestContext.Current.CancellationToken);

        Assert.NotSame(preflight.Report, report);
        Assert.Equal(1, preflight.Report.FailedCount);
        Assert.Equal(0, preflight.Report.RequestedCount);
        Assert.Equal(0, preflight.Report.SkippedCount);
        // E8: StartBatch now attempts to launch valid items even when preflight has failures.
        // The start delegate throws, recording an additional failure but no skipped count.
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(0, report.RequestedCount);
        Assert.Equal(0, report.SkippedCount);
    }

    [Fact]
    public void StartBatchPreflightsAllPlansBeforeStartingAnyProcess()
    {
        using var temp = new TempDirectory();
        var startedPlans = new List<LaunchPlan>();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(plan => startedPlans.Add(plan)));

        var report = workflow.StartBatch(
            [
                Entry("valid", temp.Path),
                Entry("missing", Path.Combine(temp.Path, "missing")),
            ],
            requireConfirmation: false);

        // E8: Valid items launch even when preflight reports failures; only valid item starts.
        Assert.Single(startedPlans);
        Assert.NotNull(report.LastCommandLine);
        Assert.Equal(1, report.RequestedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Contains(report.FailureMessages, failure => failure.StartsWith("missing:", StringComparison.Ordinal));
    }

    [Fact]
    public void StartBatchRejectsNullEntryRows()
    {
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => { }));

        var exception = Assert.Throws<ArgumentNullException>(() =>
            workflow.StartBatch([null!], requireConfirmation: false));

        Assert.Equal("entry", exception.ParamName);
    }

    [Fact]
    public void PreflightBatchReportsBlankEntryNameWithoutInventingFallbackName()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("should not start")));

        var preflight = workflow.PreflightBatch(
            [
                new LaunchEntry { Id = "one", Name = EntryName.Empty, Directory = (WTLauncher.Core.DirectoryPath)temp.Path, Command = "pwsh" },
            ],
            requireConfirmation: false);

        Assert.Empty(preflight.Items);
        Assert.Equal(1, preflight.Report.FailedCount);
        Assert.Equal(["Launch entry name is empty."], preflight.Report.FailureMessages);
    }

    [Fact]
    public void PreflightBatchReportsBlankEntryIdWithoutInventingFallbackId()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("should not start")));

        var preflight = workflow.PreflightBatch(
            [
                new LaunchEntry { Id = EntryId.Empty, Name = "one", Directory = (WTLauncher.Core.DirectoryPath)temp.Path, Command = "pwsh" },
            ],
            requireConfirmation: false);

        Assert.Empty(preflight.Items);
        Assert.Equal(1, preflight.Report.FailedCount);
        var message = Assert.Single(preflight.Report.FailureMessages);
        Assert.Contains("不能为空", message);
    }

    [Fact]
    public void StartBatchKeepsLastCommandLineWhenStartFails()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("blocked")));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
            ],
            requireConfirmation: false);

        Assert.Equal(0, report.RequestedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Contains("one: blocked", Assert.Single(report.FailureMessages), StringComparison.Ordinal);
        Assert.Contains("pwsh", report.LastCommandLine);
    }

    [Fact]
    public void StartBatchUsesPreflightNameSnapshotWhenEntryNameMutates()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(_ => throw new InvalidOperationException("blocked")));
        var entry = Entry("one", temp.Path);
        var preflight = workflow.PreflightBatch([entry], requireConfirmation: false);
        entry = entry with { Name = EntryName.Empty };

        var report = workflow.StartBatch(preflight, TestContext.Current.CancellationToken);

        var item = Assert.Single(preflight.Items);
        Assert.Equal("one", item.EntryId);
        Assert.Equal("one", item.EntryName);
        Assert.Equal(0, report.RequestedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(["one: blocked"], report.FailureMessages);
        Assert.Contains("pwsh", report.LastCommandLine);
    }

    [Fact]
    public void StartBatchContinuesAfterFirstStartFailure()
    {
        using var temp = new TempDirectory();
        var attempted = new List<string>();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(plan =>
        {
            attempted.Add(plan.WorkingDirectory);
            if (attempted.Count == 2)
                throw new InvalidOperationException("blocked");
        }));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
                Entry("three", temp.Path),
            ],
            requireConfirmation: false);

        // With continue: all three items are attempted, only two fails.
        Assert.Equal(3, attempted.Count);
        Assert.Equal(2, report.RequestedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Contains("two: blocked", Assert.Single(report.FailureMessages), StringComparison.Ordinal);
    }

    [Fact]
    public void StartBatch_ContinuesAfterSingleFailure()
    {
        using var temp = new TempDirectory();
        var attempted = new List<string>();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(plan =>
        {
            attempted.Add(plan.WorkingDirectory);
            if (attempted.Count == 1)
                throw new InvalidOperationException("blocked");
        }));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
                Entry("three", temp.Path),
            ],
            requireConfirmation: false);

        // First item fails but remaining items are still attempted.
        Assert.Equal(3, attempted.Count);
        Assert.Equal(2, report.RequestedCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Contains("one: blocked", Assert.Single(report.FailureMessages), StringComparison.Ordinal);
    }

    [Fact]
    public void StartBatch_FailureIndexRecordsFirstFailure()
    {
        using var temp = new TempDirectory();
        var attempted = new List<string>();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(plan =>
        {
            attempted.Add(plan.WorkingDirectory);
            if (attempted.Count is 1 or 3)
                throw new InvalidOperationException("blocked");
        }));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
                Entry("three", temp.Path),
            ],
            requireConfirmation: false);

        // Items at index 0 and 2 fail, item at index 1 succeeds.
        // FailureIndex captures the first failure (index 0), not the last.
        Assert.Equal(3, attempted.Count);
        Assert.Equal(1, report.RequestedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(0, report.FailureIndex);
    }

    [Fact]
    public void StartBatchStartsEveryValidPlanAndReportsSuccess()
    {
        using var temp = new TempDirectory();
        var startedPlans = new List<LaunchPlan>();
        var workflow = new LauncherLaunchWorkflow(new FakeLaunchProcessStarter(plan => startedPlans.Add(plan)));

        var report = workflow.StartBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
            ],
            requireConfirmation: true);

        Assert.Equal(2, report.RequestedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
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
            Directory = (WTLauncher.Core.DirectoryPath)directory,
            Command = "pwsh",
        };
    }
}

