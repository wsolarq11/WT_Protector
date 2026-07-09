using System.Diagnostics;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherLaunchWorkflow
{
    private readonly Action<LaunchPlan> _start;

    public IReadOnlyDictionary<string, string>? CommandTemplates { get; internal set; }

    public LauncherLaunchWorkflow(Action<LaunchPlan> start)
    {
        _start = start ?? throw new ArgumentNullException(nameof(start));
    }

    public static LauncherLaunchWorkflow Create() =>
        new(plan =>
        {
            using var process = Process.Start(new ProcessStartInfo(plan.Executable)
            {
                Arguments = plan.Arguments,
                WorkingDirectory = plan.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
            }) ?? throw new InvalidOperationException($"Failed to start process: {plan.Executable}");
        });

    public LaunchPlan BuildPlan(LaunchEntry entry, bool requireConfirmation) =>
        LaunchArgumentBuilder.Build(
            entry,
            validateDirectory: true,
            requireConfirmation: requireConfirmation,
            commandTemplates: CommandTemplates);

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

        var report = new LauncherBatchLaunchReportBuilder();
        List<LauncherBatchLaunchItem> items = [];
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            try
            {
                items.Add(new LauncherBatchLaunchItem(entry, BuildPlan(entry, requireConfirmation)));
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "Preflight");
                RecordFailure(report, entry.Name, ex);
            }
        }

        return new LauncherBatchLaunchPreflight(items, report.ToReport());
    }

    public LauncherBatchLaunchReport StartBatch(LauncherBatchLaunchPreflight preflight, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preflight);

        var items = preflight.Items;
        var report = new LauncherBatchLaunchReportBuilder(preflight.Report);

        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[index];
            try
            {
                report.RecordPlan(item.Plan);
                Start(item.Plan);
                report.RecordRequested();
            }
            catch (OperationCanceledException)
            {
                report.RecordSkipped(items.Count - index);
                break;
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "StartBatch");
                RecordFailure(report, item.EntryName, index, ex);
                continue;
            }
        }

        return report.ToReport();
    }

    private static void RecordFailure(
        LauncherBatchLaunchReportBuilder report,
        string? entryName,
        Exception exception)
    {
        RecordFailure(report, entryName, failureIndex: null, exception);
    }

    private static void RecordFailure(
        LauncherBatchLaunchReportBuilder report,
        string? entryName,
        int? failureIndex,
        Exception exception)
    {
        var normalizedName = EntryName.Normalize(entryName);
        if (normalizedName.Length == 0)
        {
            report.RecordFailure(exception);
            return;
        }

        if (failureIndex is not null)
            report.RecordFailure(normalizedName, failureIndex.Value, exception);
        else
            report.RecordFailure(normalizedName, exception);
    }
}
