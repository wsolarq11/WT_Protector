using WTLauncher.App.Features;
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
    public static LauncherStatusContent Ready => LauncherStatusContent.Neutral("就绪");

    public static string DryRunPlaceholder => "选择一个启动项查看预演命令。";

    public static string PresetSelectionPlaceholder => "选择启动组后可查看与当前勾选的差异。";

    public static string StorePathDisplay(string storePath) =>
        $"存储: {storePath}";

    public static LauncherStatusContent Loaded(int entryCount, int presetCount) =>
        entryCount == 0
            ? LauncherStatusContent.Neutral("欢迎使用 WT Launcher — 点击上方「管理 → 地址管理」添加项目目录，或使用「发现工具」自动扫描")
            : LauncherStatusContent.Neutral($"已加载 {entryCount} 个启动项、{presetCount} 个启动组；勾选自动保存到 config\\launcher.json");

    public static LauncherStatusContent ConfigLoadFailed(string detail) =>
        LauncherStatusContent.Error($"配置加载失败: {detail}");

    public static LauncherStatusContent ConfigValidationFailed(string detail) =>
        LauncherStatusContent.Error($"配置验证失败: {detail}");

    public static LauncherStatusContent UndefinedCommandTemplateKeys(string keys) =>
        LauncherStatusContent.Warning($"警告：以下命令模板键未定义: {keys}");
}

public static class LauncherRefreshStatus
{
    public static LauncherStatusContent DialogOpenFailed(string dialogName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"{dialogName}对话框打开失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent DialogCanceled(string action) =>
        LauncherStatusContent.Warning($"已取消{action}");

    public static LauncherStatusContent SaveGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"保存启动组失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent AddGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"新增启动组失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent DeleteGroupFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"删除启动组失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent GroupAlreadyUpToDate(string groupName) =>
        LauncherStatusContent.Neutral($"启动组已是最新状态: {groupName}");

    public static LauncherStatusContent SaveFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"保存失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent DiscoveryScanning => LauncherStatusContent.Neutral("扫描中…");

    public static LauncherStatusContent DiscoveryFailed => LauncherStatusContent.Error("扫描过程中发生错误");

    public static LauncherStatusContent GroupNoEntries => LauncherStatusContent.Warning("组内无 entry。");

    public static LauncherStatusContent GroupNoEntriesCannotSave => LauncherStatusContent.Warning("组内无 entry,无法保存为启动组。");

    public static LauncherStatusContent CanceledBatch => LauncherStatusContent.Warning("已取消批量启动");
}

public static class LauncherDiscoveryStatus
{
    public static LauncherStatusContent AgentsAdded(int addedCount, IReadOnlyList<string> skipped)
    {
        ArgumentNullException.ThrowIfNull(skipped);
        return skipped.Count == 0
            ? LauncherStatusContent.Success($"已添加 {addedCount} 个 AI Agent 启动项")
            : LauncherStatusContent.Warning(
                $"已添加 {addedCount} 个 AI Agent 启动项（{skipped.Count} 个跳过: {string.Join(", ", skipped)}）");
    }
}

public static class LauncherGroupStatus
{
    public static LauncherStatusContent BatchCompleted(LauncherBatchLaunchReport report) =>
        report.FailedCount == 0
            ? LauncherStatusContent.Success($"已启动组内 {report.RequestedCount} 个 entry。")
            : LauncherStatusContent.Warning($"已启动组内 {report.RequestedCount} 个 entry，{report.FailedCount} 个失败。");
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
    public static LauncherStatusContent StoreSaveFailed(Exception exception) =>
        Failed("存储保存失败", exception);

    public static LauncherStatusContent ConfigSaveFailed(Exception exception) =>
        Failed("配置保存失败", exception);

    public static LauncherStatusContent PresetSaveFailed(Exception exception) =>
        Failed("启动组保存失败", exception);

    public static LauncherStatusContent SettingsSaveFailed(Exception exception) =>
        Failed("设置保存失败", exception);

    private static LauncherStatusContent Failed(string prefix, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"{prefix}: {ExceptionMessageFormatter.ForUser(exception)}");
    }
}

public static class LauncherEntryStatus
{
    public static LauncherStatusContent AddCanceled => LauncherStatusContent.Warning("已取消: 新增按钮");

    public static LauncherStatusContent AddDialogFailed(Exception exception) =>
        LauncherStatusContent.Error($"新增按钮对话框失败: {ExceptionMessageFormatter.ForUser(exception)}");

    public static LauncherStatusContent EditCanceled(string entryName) =>
        LauncherStatusContent.Warning($"已取消: 编辑 {entryName}");

    public static LauncherStatusContent EditDialogFailed(string entryName, Exception exception) =>
        LauncherStatusContent.Error($"编辑 {entryName} 对话框失败: {ExceptionMessageFormatter.ForUser(exception)}");

    public static LauncherStatusContent DeleteCanceled(string entryName) =>
        LauncherStatusContent.Warning($"已取消: 删除 {entryName}");

    public static LauncherStatusContent DeleteConfirmationFailed(string entryName, Exception exception) =>
        LauncherStatusContent.Error($"删除确认失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}");

    public static LauncherStatusContent Added(string entryName) =>
        LauncherStatusContent.Success($"已新增按钮: {entryName}（已保存）");

    public static LauncherStatusContent Updated(string entryName) =>
        LauncherStatusContent.Success($"已更新按钮: {entryName}（已保存）");

    public static LauncherStatusContent Deleted(string entryName) =>
        LauncherStatusContent.Success($"已删除按钮: {entryName}（已保存）");
}

public static class LauncherPresetStatus
{
    public static LauncherStatusContent NoPresetSelected => LauncherStatusContent.Warning("未选择启动组");

    public static LauncherStatusContent SaveCanceled => LauncherStatusContent.Warning("已取消: 保存启动组");

    public static LauncherStatusContent SaveDialogFailed(Exception exception) =>
        LauncherStatusContent.Error($"保存启动组对话框失败: {ExceptionMessageFormatter.ForUser(exception)}");

    public static LauncherStatusContent NameRequired => LauncherStatusContent.Error("启动组名称不能为空");

    public static LauncherStatusContent CurrentPresetMissing => LauncherStatusContent.Error("当前启动组已不存在，请重新加载");

    public static LauncherStatusContent AddFailedInvalidEntryIds(IReadOnlyList<string> invalidIds) =>
        LauncherStatusContent.Error($"启动项 ID 不存在: {string.Join(", ", invalidIds)}");

    public static LauncherStatusContent UpdateFailedInvalidEntryIds(IReadOnlyList<string> invalidIds) =>
        LauncherStatusContent.Error($"启动项 ID 不存在: {string.Join(", ", invalidIds)}");

    public static LauncherStatusContent MissingEntryReferences(IEnumerable<string> entryIds) =>
        LauncherStatusContent.Error($"启动组引用了不存在的启动项: {string.Join(", ", entryIds)}");

    public static LauncherStatusContent NoPresetEntries(string presetName) =>
        LauncherStatusContent.Warning($"启动组\u201c{presetName}\u201d没有启动项");

    public static LauncherStatusContent Applied(string presetName, int entryCount) =>
        LauncherStatusContent.Success($"已应用启动组: {presetName}（{entryCount} 项，已保存）");

    public static LauncherStatusContent Previewed(string presetName, int entryCount) =>
        LauncherStatusContent.Neutral($"预演启动组: {presetName}（{entryCount} 项）");

    public static LauncherStatusContent PreviewFailed(string presetName, LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return LauncherStatusContent.Error($"启动组\u201c{presetName}\u201d预演失败: {report.FailedCount} 个失败");
    }

    public static LauncherStatusContent PreviewFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"启动组\u201c{presetName}\u201d预演失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent Launched(string presetName, LauncherBatchLaunchReport report)
    {
        var batch = LauncherSelectionStatus.BatchLaunchCompleted(report, "启动项");
        return new LauncherStatusContent($"启动组\u201c{presetName}\u201d: {batch.Text}", batch.Tone);
    }

    public static LauncherStatusContent LaunchCanceled(string presetName, int entryCount) =>
        LauncherStatusContent.Warning($"已取消: 启动组 {presetName}（{entryCount} 项）");

    public static LauncherStatusContent LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"启动组\u201c{presetName}\u201d启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static LauncherStatusContent Saved(string presetName, int entryCount) =>
        LauncherStatusContent.Success($"已保存启动组: {presetName}（{entryCount} 项）");

    public static LauncherStatusContent UpdateFailed(string presetName, Exception exception) =>
        LauncherStatusContent.Error($"更新启动组失败: {presetName} — {ExceptionMessageFormatter.ForUser(exception)}");

    public static LauncherStatusContent UpdateBlocked(string presetName) =>
        LauncherStatusContent.Warning($"系统启动组不能更新: {presetName}");

    public static LauncherStatusContent Updated(string presetName, int entryCount) =>
        LauncherStatusContent.Success($"已更新启动组: {presetName}（{entryCount} 项）");

    public static LauncherStatusContent DeleteBlocked(string presetName) =>
        LauncherStatusContent.Warning($"启动组不能删除: {presetName}");

    public static LauncherStatusContent DeleteCanceled(string presetName) =>
        LauncherStatusContent.Warning($"已取消: 删除启动组 {presetName}");

    public static LauncherStatusContent DeleteConfirmationFailed(string presetName, Exception exception) =>
        LauncherStatusContent.Error($"删除启动组确认失败: {presetName} \u2014 {ExceptionMessageFormatter.ForUser(exception)}");

    public static LauncherStatusContent Deleted(string presetName, string selectedPresetName) =>
        LauncherStatusContent.Success($"已删除启动组: {presetName}；当前启动组: {selectedPresetName}");
}

public static class LauncherSelectionStatus
{
    public static LauncherStatusContent NoVisibleEntries => LauncherStatusContent.Warning("当前没有可勾选的可见项");

    public static LauncherStatusContent VisibleAlreadySelected => LauncherStatusContent.Neutral("可见项已全部勾选");

    public static LauncherStatusContent NoSelectedEntries => LauncherStatusContent.Warning("未选择任何启动项");

    public static LauncherStatusContent NoSelectedEntriesToClear => LauncherStatusContent.Neutral("当前没有已勾选项");

    public static LauncherStatusContent SelectionRequired => LauncherStatusContent.Warning("请先勾选至少一个启动项");

    public static LauncherStatusContent VisibleSelected(int entryCount) =>
        LauncherStatusContent.Success($"已勾选可见 {entryCount} 项（已保存）");

    public static LauncherStatusContent Cleared(int entryCount) =>
        LauncherStatusContent.Success($"已清空 {entryCount} 个勾选项（已保存）");

    public static LauncherStatusContent BatchLaunchCanceled(int entryCount) =>
        LauncherStatusContent.Warning($"已取消: 批量启动 {entryCount} 项");

    public static LauncherStatusContent BatchLaunchCompleted(LauncherBatchLaunchReport report)
    {
        return BatchLaunchCompleted(report, "勾选项");
    }

    public static LauncherStatusContent BatchLaunchCompleted(LauncherBatchLaunchReport report, string itemLabel)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);
        return report.FailedCount == 0
            ? LauncherStatusContent.Success($"已请求启动 {report.RequestedCount} 个{itemLabel}")
            : LauncherStatusContent.Warning($"已请求启动 {report.RequestedCount} 个{itemLabel}，{report.FailedCount} 个失败{FormatSkipped(report)}：{FormatBatchFailures(report.FailureMessages)}");
    }

    private static string FormatSkipped(LauncherBatchLaunchReport report) =>
        report.SkippedCount == 0 ? "" : $"，{report.SkippedCount} 个未启动";

    public static LauncherStatusContent BatchLaunchFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return LauncherStatusContent.Error($"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
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
    public static LauncherStatusContent SortModeSaved(string modeLabel) =>
        LauncherStatusContent.Success($"排序模式: {modeLabel}（已保存）");

    public static LauncherStatusContent DisplayModeSaved(string modeLabel) =>
        LauncherStatusContent.Success($"视图模式: {modeLabel}（已保存）");

    public static LauncherStatusContent LaunchConfirmationSaved(bool requireConfirmation) =>
        requireConfirmation
            ? LauncherStatusContent.Success("启动确认: 所有启动项启动前统一确认")
            : LauncherStatusContent.Success("启动确认: 所有启动项直接启动");

    public static LauncherStatusContent WindowBoundsSaveFailed =>
        LauncherStatusContent.Error("窗口尺寸保存失败；下次将使用上次保存的尺寸");
}

// Launch receipt text classes (LauncherSingleLaunchStatus, LauncherSingleLaunchReceiptText,
// LauncherBatchLaunchPlanText, LauncherBatchLaunchReceiptText) moved to
// LauncherLaunchReceiptText.cs as part of status-text domain decomposition.

public static class LauncherDragReorderStatus
{
    public static LauncherStatusContent Saved => LauncherStatusContent.Success("已保存自定义拖拽排序");

    public static LauncherStatusContent Canceled => LauncherStatusContent.Warning("已取消: 拖拽排序");

    public static LauncherStatusContent Moved(string entryName) =>
        LauncherStatusContent.Success($"已保存自定义排序: {entryName}");

    public static LauncherStatusContent MoveUnavailable => LauncherStatusContent.Warning("当前条目不能继续移动");
}
