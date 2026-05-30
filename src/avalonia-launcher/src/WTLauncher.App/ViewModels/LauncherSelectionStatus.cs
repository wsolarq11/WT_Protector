namespace WTLauncher.App.ViewModels;

public static class LauncherSelectionStatus
{
    public static string NoVisibleEntries => "当前没有可勾选的可见项";

    public static string VisibleAlreadySelected => "可见项已全部勾选";

    public static string NoSelectedEntries => "未选择任何启动项";

    public static string NoSelectedEntriesToClear => "当前没有已勾选项";

    public static string SelectionRequired => "请先勾选至少一个启动项";

    public static string VisibleSelected(int entryCount) =>
        $"已勾选可见 {entryCount} 项（已保存）";

    public static string Cleared(int entryCount) =>
        $"已清空 {entryCount} 个勾选项（已保存）";

    public static string BatchLaunchCanceled(int entryCount) =>
        $"已取消: 批量启动 {entryCount} 项";

    public static string BatchLaunchCompleted(LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.FailedCount == 0
            ? $"已启动 {report.StartedCount} 个勾选项"
            : $"已启动 {report.StartedCount} 个勾选项，{report.FailedCount} 个失败：{FormatBatchFailures(report.FailureMessages)}";
    }

    public static string BatchLaunchFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"批量启动失败: {exception.Message}";
    }

    private static string FormatBatchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += $"；…另有 {failures.Count - 3} 项";

        return preview;
    }
}
