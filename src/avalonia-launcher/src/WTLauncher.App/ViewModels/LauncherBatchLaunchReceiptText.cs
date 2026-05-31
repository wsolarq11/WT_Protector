namespace WTLauncher.App.ViewModels;

public static class LauncherBatchLaunchReceiptText
{
    public static string Format(
        LauncherBatchLaunchPreflight preflight,
        LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(report);

        var lines = new List<string>
        {
            "启动回执",
            $"已请求启动 {report.RequestedCount} 项，失败 {report.FailedCount} 项，未启动 {report.SkippedCount} 项",
        };

        AppendPlanLines(lines, preflight.Items, report);
        AppendFailureLines(lines, report.FailureMessages);
        return string.Join(Environment.NewLine, lines);
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
            lines.Add($"• {StatusFor(index, report)}: {item.Entry.Name}");
            lines.Add(item.Plan.CommandLine);
        }
    }

    private static string StatusFor(int index, LauncherBatchLaunchReport report)
    {
        if (report.FailedCount > 0 && report.LastCommandLine is not null && index == report.RequestedCount)
            return "启动失败";

        return index < report.RequestedCount ? "已请求" : "未启动";
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("");
        lines.Add("失败:");
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }
}
