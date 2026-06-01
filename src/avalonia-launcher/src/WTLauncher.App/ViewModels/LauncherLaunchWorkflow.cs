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
        bool requireConfirmation) =>
        StartBatch(PreflightBatch(entries, requireConfirmation));

    public LauncherBatchLaunchPreflight PreflightBatch(
        IEnumerable<LaunchEntry> entries,
        bool requireConfirmation)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var report = new LauncherBatchLaunchReport();
        var items = new List<LauncherBatchLaunchItem>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            try
            {
                items.Add(new LauncherBatchLaunchItem(entry, BuildPlan(entry, requireConfirmation)));
            }
            catch (Exception ex)
            {
                RecordFailure(report, entry.Name, ex);
            }
        }

        return new LauncherBatchLaunchPreflight(items, report);
    }

    public LauncherBatchLaunchReport StartBatch(LauncherBatchLaunchPreflight preflight)
    {
        ArgumentNullException.ThrowIfNull(preflight);

        var items = preflight.Items;
        var report = preflight.Report;
        if (report.FailedCount > 0)
        {
            report.RecordSkipped(items.Count);
            return report;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            try
            {
                report.RecordPlan(item.Plan);
                Start(item.Plan);
                report.RecordRequested();
            }
            catch (Exception ex)
            {
                RecordFailure(report, item.Entry.Name, ex);
                report.RecordSkipped(items.Count - index - 1);
                break;
            }
        }

        return report;
    }

    private static void RecordFailure(
        LauncherBatchLaunchReport report,
        string? entryName,
        Exception exception)
    {
        var normalizedName = LauncherEntryText.Normalize(entryName);
        if (normalizedName.Length == 0)
        {
            report.RecordFailure(exception);
            return;
        }

        report.RecordFailure(normalizedName, exception);
    }
}
