using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Multi-line launch receipt / dry-run preview text for the DryRun panel.
// Split from LauncherStatusText.cs: these classes format multi-line blocks
// (not single-line status bar content) for the launch receipt display area.

public static class LauncherSingleLaunchStatus
{
    public static LauncherStatusContent Preview(string entryName, bool requiresConfirmation) =>
        requiresConfirmation
            ? LauncherStatusContent.Neutral($"预演: {entryName}（需要确认）")
            : LauncherStatusContent.Neutral($"预演: {entryName}");

    public static LauncherStatusContent PreviewFailed(string entryName) =>
        LauncherStatusContent.Error($"预演失败: {entryName}");

    public static LauncherStatusContent PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"预演失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent Canceled(string entryName) =>
        LauncherStatusContent.Warning($"已取消: {entryName}");

    public static LauncherStatusContent Requested(string entryName) =>
        LauncherStatusContent.Success($"已请求启动: {entryName}");

    public static LauncherStatusContent Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"启动失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }
}

public static class LauncherSingleLaunchReceiptText
{
    public static string Requested(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已请求启动: {RequireName(entryName)}",
            plan.CommandLine,
            $"（实际执行: {plan.ExecutionCommandLine}）");
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已取消: {RequireName(entryName)}",
            plan.CommandLine,
            $"（实际执行: {plan.ExecutionCommandLine}）");
    }

    public static string PreviewFailed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {RequireName(entryName)}",
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string ConfirmationFailed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动确认失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
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
            "预演回执",
            $"预演失败: 失败 {preflight.Report.FailedCount} 项，未启动 {preflight.Items.Count} 项",
        ];

        AppendPlanLines(lines, preflight.Items, "• 未启动: ");
        AppendFailureLines(lines, preflight.Report.FailureMessages);
        return JoinBlocks(lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return JoinBlocks(
        [
            "预演回执",
            $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}",
        ]);
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        string prefix)
    {
        foreach (var item in items)
            lines.Add($"{prefix}{item.EntryName}{Environment.NewLine}{item.Plan.CommandLine}{Environment.NewLine}（实际: {item.Plan.ExecutionCommandLine}）");
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("失败:");
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
            "启动回执",
            $"已请求启动 {report.RequestedCount} 项，失败 {report.FailedCount} 项，未启动 {report.SkippedCount} 项",
        ];

        AppendPlanLines(lines, preflight.Items, report);
        AppendFailureLines(lines, report.FailureMessages);
        return string.Join(Environment.NewLine, lines);
    }

    public static string Canceled(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [
            "启动回执",
            $"已取消: 已请求启动 0 项，未启动 {items.Count} 项",
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
            "启动回执",
            $"启动确认失败: {ExceptionMessageFormatter.ForUser(exception)}",
            $"已请求启动 0 项，未启动 {items.Count} 项",
        };

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
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
            lines.Add($"• {StatusFor(index, report)}: {item.EntryName}");
            lines.Add(item.Plan.CommandLine);
        }
    }

    private static string StatusFor(int index, LauncherBatchLaunchReport report)
    {
        // R9: With continue-on-failure, failures may be interspersed with successes.
        // Use the full failure-indices set rather than a single FailureIndex.
        if (report.FailureIndices.Contains(index))
            return "启动失败";

        // All non-failure items up through (RequestedCount + FailedCount - 1) are "已请求".
        // Items past that range are "未启动" (only happens when items are skipped).
        return index < report.RequestedCount + report.FailedCount ? "已请求" : "未启动";
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("");
        lines.Add("失败:");
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
            lines.Add($"• 未启动: {item.EntryName}");
            lines.Add(item.Plan.CommandLine);
        }
    }
}
