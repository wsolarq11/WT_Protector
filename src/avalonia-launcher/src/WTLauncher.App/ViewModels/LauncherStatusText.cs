using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// All user-facing status / receipt text strings, split from MainWindowViewModel.cs
// as part of 07-09-split-mainviewmodel-stage2. Pure static containers — no state.

public sealed record LauncherSortModeViewModel(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record LauncherDisplayModeViewModel(LauncherEntryViewMode Mode, string Label)
{
    public string Id => LauncherEntryViewModes.ToId(Mode);

    public override string ToString() => Label;
}

public static class LauncherMainWindowText
{
    public static string Ready => "就绪";

    public static string DryRunPlaceholder => "选择一个启动项查看预演命令。";

    public static string PresetSelectionPlaceholder => "选择启动组后可查看与当前勾选的差异。";

    public static string StorePathDisplay(string storePath) =>
        $"存储: {storePath}";

    public static string Loaded(int entryCount, int presetCount) =>
        $"已加载 {entryCount} 个启动项、{presetCount} 个启动组；勾选自动保存到 config\\launcher.json";

    public static string ConfigLoadFailed(string detail) =>
        $"配置加载失败: {detail}";

    public static string ConfigValidationFailed(string detail) =>
        $"配置验证失败: {detail}";

    public static string UndefinedCommandTemplateKeys(string keys) =>
        $"警告：以下命令模板键未定义: {keys}";
}

public static class LauncherRefreshStatus
{
    public static string DialogOpenFailed(string dialogName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{dialogName}对话框打开失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string DialogCanceled(string action) =>
        $"已取消{action}";

    public static string SaveGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"保存启动组失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string AddGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"新增启动组失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string DeleteGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"删除启动组失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string GroupAlreadyUpToDate(string groupName) =>
        $"启动组已是最新状态: {groupName}";

    public static string SaveFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"保存失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string DiscoveryScanning => "扫描中…";

    public static string DiscoveryFailed => "扫描过程中发生错误";

    public static string ProcessLaunchFailed(string execName, string detail) =>
        $"进程启动失败: {detail}";

    public static string GroupNoEntries => "组内无 entry。";

    public static string GroupNoEntriesCannotSave => "组内无 entry,无法保存为启动组。";

    public static string CanceledBatch => "已取消批量启动";
}

public static class ExceptionMessageFormatter
{
    /// <summary>
    /// Sanitize exception messages for user-facing display.
    /// File-system exceptions leak paths in their Message property;
    /// this replaces them with stable, user-safe strings.
    /// </summary>
    public static string ForUser(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            // File-system exceptions leak paths in their Message — replace with safe text.
            FileNotFoundException => "找不到文件",
            DirectoryNotFoundException => "找不到目录",
            UnauthorizedAccessException => "没有访问权限",
            IOException => "文件操作失败",
            // Application-layer exceptions carry intentionally user-safe messages crafted
            // by the throwing code — pass them through.
            InvalidDataException or ArgumentException or InvalidOperationException => exception.Message,
            // Unknown exception types: use generic text to avoid leaking internal details.
            _ => "操作失败，请查看错误日志了解详情",
        };
    }
}

public static class LauncherPersistenceStatus
{
    public static string StoreSaveFailed(Exception exception) =>
        Failed("存储保存失败", exception);

    public static string ConfigSaveFailed(Exception exception) =>
        Failed("配置保存失败", exception);

    public static string PresetSaveFailed(Exception exception) =>
        Failed("启动组保存失败", exception);

    public static string SettingsSaveFailed(Exception exception) =>
        Failed("设置保存失败", exception);

    private static string Failed(string prefix, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{prefix}: {ExceptionMessageFormatter.ForUser(exception)}";
    }
}

public static class LauncherEntryStatus
{
    public static string AddCanceled => "已取消: 新增按钮";

    public static string AddDialogFailed(Exception exception) =>
        $"新增按钮对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string EditCanceled(string entryName) =>
        $"已取消: 编辑 {entryName}";

    public static string EditDialogFailed(string entryName, Exception exception) =>
        $"编辑 {entryName} 对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string DeleteCanceled(string entryName) =>
        $"已取消: 删除 {entryName}";

    public static string DeleteConfirmationFailed(string entryName, Exception exception) =>
        $"删除确认失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}";

    public static string Added(string entryName) =>
        $"已新增按钮: {entryName}（已保存）";

    public static string Updated(string entryName) =>
        $"已更新按钮: {entryName}（已保存）";

    public static string Deleted(string entryName) =>
        $"已删除按钮: {entryName}（已保存）";
}

public static class LauncherPresetStatus
{
    public static string NoPresetSelected => "未选择启动组";

    public static string SaveCanceled => "已取消: 保存启动组";

    public static string SaveDialogFailed(Exception exception) =>
        $"保存启动组对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string NameRequired => "启动组名称不能为空";

    public static string CurrentPresetMissing => "当前启动组已不存在，请重新加载";

    public static string MissingEntryReferences(IEnumerable<string> entryIds) =>
        $"启动组引用了不存在的启动项: {string.Join(", ", entryIds)}";

    public static string NoPresetEntries(string presetName) =>
        $"启动组\u201c{presetName}\u201d没有启动项";

    public static string Applied(string presetName, int entryCount) =>
        $"已应用启动组: {presetName}（{entryCount} 项，已保存）";

    public static string Previewed(string presetName, int entryCount) =>
        $"预演启动组: {presetName}（{entryCount} 项）";

    public static string PreviewFailed(string presetName, LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return $"启动组\u201c{presetName}\u201d预演失败: {report.FailedCount} 个失败";
    }

    public static string PreviewFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d预演失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Launched(string presetName, LauncherBatchLaunchReport report) =>
        $"启动组\u201c{presetName}\u201d: {LauncherSelectionStatus.BatchLaunchCompleted(report, "启动项")}";

    public static string LaunchCanceled(string presetName, int entryCount) =>
        $"已取消: 启动组 {presetName}（{entryCount} 项）";

    public static string LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Saved(string presetName, int entryCount) =>
        $"已保存启动组: {presetName}（{entryCount} 项）";

    public static string UpdateFailed(string presetName, Exception exception) =>
        $"更新启动组失败: {presetName} — {ExceptionMessageFormatter.ForUser(exception)}";

    public static string UpdateBlocked(string presetName) =>
        $"系统启动组不能更新: {presetName}";

    public static string Updated(string presetName, int entryCount) =>
        $"已更新启动组: {presetName}（{entryCount} 项）";

    public static string DeleteBlocked(string presetName) =>
        $"启动组不能删除: {presetName}";

    public static string DeleteCanceled(string presetName) =>
        $"已取消: 删除启动组 {presetName}";

    public static string DeleteConfirmationFailed(string presetName, Exception exception) =>
        $"删除启动组确认失败: {presetName} \u2014 {ExceptionMessageFormatter.ForUser(exception)}";

    public static string Deleted(string presetName, string selectedPresetName) =>
        $"已删除启动组: {presetName}；当前启动组: {selectedPresetName}";
}

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
        return BatchLaunchCompleted(report, "勾选项");
    }

    public static string BatchLaunchCompleted(LauncherBatchLaunchReport report, string itemLabel)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);
        return report.FailedCount == 0
            ? $"已请求启动 {report.RequestedCount} 个{itemLabel}"
            : $"已请求启动 {report.RequestedCount} 个{itemLabel}，{report.FailedCount} 个失败{FormatSkipped(report)}：{FormatBatchFailures(report.FailureMessages)}";
    }

    private static string FormatSkipped(LauncherBatchLaunchReport report) =>
        report.SkippedCount == 0 ? "" : $"，{report.SkippedCount} 个未启动";

    public static string BatchLaunchFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    private static string FormatBatchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += $"；…另有 {failures.Count - 3} 项";

        return preview;
    }
}

public static class LauncherSettingsStatus
{
    public static string SortModeSaved(string modeLabel) =>
        $"排序模式: {modeLabel}（已保存）";

    public static string DisplayModeSaved(string modeLabel) =>
        $"视图模式: {modeLabel}（已保存）";

    public static string LaunchConfirmationSaved(bool requireConfirmation) =>
        requireConfirmation
            ? "启动确认: 所有启动项启动前统一确认"
            : "启动确认: 所有启动项直接启动";

    public static string WindowBoundsSaveFailed =>
        "窗口尺寸保存失败；下次将使用上次保存的尺寸";
}

public static class LauncherSingleLaunchStatus
{
    public static string Preview(string entryName, bool requiresConfirmation) =>
        requiresConfirmation
            ? $"预演: {entryName}（需要确认）"
            : $"预演: {entryName}";

    public static string PreviewFailed(string entryName) =>
        $"预演失败: {entryName}";

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Canceled(string entryName) =>
        $"已取消: {entryName}";

    public static string Requested(string entryName) =>
        $"已请求启动: {entryName}";

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
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

public static class LauncherDragReorderStatus
{
    public static string Saved => "已保存自定义拖拽排序";

    public static string Canceled => "已取消: 拖拽排序";

    public static string Moved(string entryName) => $"已保存自定义排序: {entryName}";

    public static string MoveUnavailable => "当前条目不能继续移动";
}
