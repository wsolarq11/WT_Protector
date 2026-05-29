using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherLaunchWorkflow
{
    private readonly LauncherProcess _launcherProcess;

    public LauncherLaunchWorkflow(LauncherProcess launcherProcess)
    {
        ArgumentNullException.ThrowIfNull(launcherProcess);

        _launcherProcess = launcherProcess;
    }

    public LaunchPlan BuildPlan(LaunchEntry entry, bool requireConfirmation) =>
        LaunchArgumentBuilder.Build(
            entry,
            validateDirectory: true,
            requireConfirmation: requireConfirmation);

    public void Start(LaunchPlan plan)
    {
        _launcherProcess.Start(plan);
    }

    public LauncherBatchLaunchReport StartBatch(
        IEnumerable<LaunchEntry> entries,
        bool requireConfirmation,
        Action<LaunchPlan> onPlanBuilt)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(onPlanBuilt);

        var report = new LauncherBatchLaunchReport();
        foreach (var entry in entries)
        {
            try
            {
                var plan = BuildPlan(entry, requireConfirmation);
                onPlanBuilt(plan);
                Start(plan);
                report.RecordStarted();
            }
            catch (Exception ex)
            {
                report.RecordFailure(entry.Name, ex);
            }
        }

        return report;
    }
}
