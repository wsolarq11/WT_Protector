using WTLauncher.App.Features;
using WTLauncher.App.Localization;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Multi-line launch receipt / dry-run preview text for the DryRun panel.
// User-facing strings resolve through LauncherLocalization; only block layout
// and the plan/report formatting logic live here.

public static class LauncherSingleLaunchStatus
{
    public static LauncherStatusContent Preview(string entryName, bool requiresConfirmation) =>
        requiresConfirmation
            ? LauncherStatusContent.Neutral(LauncherLocalization.Format("single.previewConfirm", entryName))
            : LauncherStatusContent.Neutral(LauncherLocalization.Format("single.preview", entryName));

    public static LauncherStatusContent PreviewFailed(string entryName) =>
        LauncherStatusContent.Error(LauncherLocalization.Format("single.previewFailedName", entryName));

    public static LauncherStatusContent PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("single.previewFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent Canceled(string entryName) =>
        LauncherStatusContent.Warning(LauncherLocalization.Format("single.canceled", entryName));

    public static LauncherStatusContent Requested(string entryName) =>
        LauncherStatusContent.Success(LauncherLocalization.Format("single.requested", entryName));

    public static LauncherStatusContent Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("single.failedName", entryName, ExceptionMessageFormatter.ForUser(exception)));
    }

    public static LauncherStatusContent Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error(
            LauncherLocalization.Format("single.failed", ExceptionMessageFormatter.ForUser(exception)));
    }
}

public static class LauncherSingleLaunchReceiptText
{
    public static string Requested(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("single.requested", RequireName(entryName)),
            plan.CommandLine,
            LauncherLocalization.Format("single.actualExecution", plan.ExecutionCommandLine));
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("single.canceled", RequireName(entryName)),
            plan.CommandLine,
            LauncherLocalization.Format("single.actualExecution", plan.ExecutionCommandLine));
    }

    public static string PreviewFailed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.previewTitle"),
            LauncherLocalization.Format("single.previewFailedName", RequireName(entryName)),
            LauncherLocalization.Format("single.error", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.previewTitle"),
            LauncherLocalization.Format("single.previewFailed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static string ConfirmationFailed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("single.confirmationFailed", RequireName(entryName)),
            plan.CommandLine,
            LauncherLocalization.Format("single.error", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("single.failedName", RequireName(entryName), ExceptionMessageFormatter.ForUser(exception)),
            LauncherLocalization.Format("single.error", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("single.failed", ExceptionMessageFormatter.ForUser(exception)));
    }

    public static string Failed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("single.failedName", RequireName(entryName), ExceptionMessageFormatter.ForUser(exception)),
            plan.CommandLine,
            LauncherLocalization.Format("single.error", ExceptionMessageFormatter.ForUser(exception)));
    }

    private static string RequireName(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        var normalizedName = EntryName.Normalize(entryName);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Entry name is required for single launch receipt.", nameof(entryName));

        return normalizedName;
    }
}

public static class LauncherBatchLaunchPlanText
{
    public static string Format(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [];
        AppendPlanLines(lines, items, "• ");
        return JoinBlocks(lines);
    }

    public static string FailedPreflight(LauncherBatchLaunchPreflight preflight)
    {
        ArgumentNullException.ThrowIfNull(preflight);

        List<string> lines = [
            LauncherLocalization.Format("receipt.previewTitle"),
            LauncherLocalization.Format("batch.preflightFailed", preflight.Report.FailedCount, preflight.Items.Count),
        ];

        AppendPlanLines(lines, preflight.Items, LauncherLocalization.Format("batch.unstartedMarker"));
        AppendFailureLines(lines, preflight.Report.FailureMessages);
        return JoinBlocks(lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return JoinBlocks(
        [
            LauncherLocalization.Format("receipt.previewTitle"),
            LauncherLocalization.Format("single.previewFailed", ExceptionMessageFormatter.ForUser(exception)),
        ]);
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        string prefix)
    {
        foreach (var item in items)
            lines.Add($"{prefix}{item.EntryName}{Environment.NewLine}{item.Plan.CommandLine}{Environment.NewLine}{LauncherLocalization.Format("batch.actual", item.Plan.ExecutionCommandLine)}");
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add(LauncherLocalization.Format("batch.failure"));
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }

    private static string JoinBlocks(IReadOnlyList<string> lines) =>
        string.Join(Environment.NewLine + Environment.NewLine, lines);
}

public static class LauncherBatchLaunchReceiptText
{
    public static string Format(
        LauncherBatchLaunchPreflight preflight,
        LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(report);

        List<string> lines = [
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("batch.requestedSummary", report.RequestedCount, report.FailedCount, report.SkippedCount),
        ];

        AppendPlanLines(lines, preflight.Items, report);
        AppendFailureLines(lines, report.FailureMessages);
        return string.Join(Environment.NewLine, lines);
    }

    public static string Canceled(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("batch.canceledSummary", items.Count),
        ];

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string ConfirmationFailed(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(exception);

        var lines = new List<string>
        {
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("batch.confirmationFailedSummary", ExceptionMessageFormatter.ForUser(exception)),
            LauncherLocalization.Format("batch.canceledSummary", items.Count),
        };

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            LauncherLocalization.Format("receipt.launchTitle"),
            LauncherLocalization.Format("batch.failedLaunch", ExceptionMessageFormatter.ForUser(exception)));
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report)
    {
        if (items.Count == 0)
            return;

        lines.Add("");
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lines.Add(LauncherLocalization.Format("batch.prefix", StatusFor(index, report), item.EntryName));
            lines.Add(item.Plan.CommandLine);
        }
    }

    private static string StatusFor(int index, LauncherBatchLaunchReport report)
    {
        // R9: With continue-on-failure, failures may be interspersed with successes.
        if (report.FailureIndices.Contains(index))
            return LauncherLocalization.Format("batch.statusFailed");

        return index < report.RequestedCount + report.FailureIndices.Count
            ? LauncherLocalization.Format("batch.statusRequested")
            : LauncherLocalization.Format("batch.statusSkipped");
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("");
        lines.Add(LauncherLocalization.Format("batch.failure"));
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }

    private static void AppendUnstartedPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        if (items.Count == 0)
            return;

        lines.Add("");
        foreach (var item in items)
        {
            lines.Add(LauncherLocalization.Format("batch.unstartedPrefix", item.EntryName));
            lines.Add(item.Plan.CommandLine);
        }
    }
}
