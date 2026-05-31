using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherLaunchWorkflow
{
    private readonly Action<LaunchPlan> _start;

    public LauncherLaunchWorkflow(LauncherProcess launcherProcess)
    {
        ArgumentNullException.ThrowIfNull(launcherProcess);

        _start = plan => launcherProcess.Start(plan);
    }

    public LauncherLaunchWorkflow(Action<LaunchPlan> start)
    {
        _start = start ?? throw new ArgumentNullException(nameof(start));
    }

    public LaunchPlan BuildPlan(LaunchEntry entry, bool requireConfirmation) =>
        LaunchArgumentBuilder.Build(
            entry,
            validateDirectory: true,
            requireConfirmation: requireConfirmation);

    public void Start(LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _start(plan);
    }

    public LauncherBatchLaunchReport StartBatch(
        IEnumerable<LaunchEntry> entries,
        bool requireConfirmation)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var report = new LauncherBatchLaunchReport();
        var plans = new List<(string EntryName, LaunchPlan Plan)>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            try
            {
                plans.Add((entry.Name, BuildPlan(entry, requireConfirmation)));
            }
            catch (Exception ex)
            {
                report.RecordFailure(entry.Name, ex);
            }
        }

        if (report.FailedCount > 0)
        {
            report.RecordSkipped(plans.Count);
            return report;
        }

        for (var index = 0; index < plans.Count; index++)
        {
            var (entryName, plan) = plans[index];
            try
            {
                report.RecordPlan(plan);
                Start(plan);
                report.RecordStarted();
            }
            catch (Exception ex)
            {
                report.RecordFailure(entryName, ex);
                report.RecordSkipped(plans.Count - index - 1);
                break;
            }
        }

        return report;
    }
}
