namespace WTLauncher.App.ViewModels;

public static class LauncherPresetStatus
{
    public static string NoPresetSelected => "未选择启动组";

    public static string SaveCanceled => "已取消: 保存启动组";

    public static string SaveDialogFailed(Exception exception) =>
        $"保存启动组对话框失败: {exception.Message}";

    public static string NameRequired => "启动组名称不能为空";

    public static string CurrentPresetMissing => "当前启动组已不存在，请重新加载";

    public static string MissingEntryReferences(IEnumerable<string> entryIds) =>
        $"启动组引用了不存在的启动项: {string.Join(", ", entryIds)}";

    public static string NoPresetEntries(string presetName) =>
        $"启动组“{presetName}”没有启动项";

    public static string Applied(string presetName, int entryCount) =>
        $"已应用启动组: {presetName}（{entryCount} 项，已保存）";

    public static string Previewed(string presetName, int entryCount) =>
        $"预演启动组: {presetName}（{entryCount} 项）";

    public static string PreviewFailed(string presetName, LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return $"启动组“{presetName}”预演失败: {report.FailedCount} 个失败";
    }

    public static string PreviewFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组“{presetName}”预演失败: {exception.Message}";
    }

    public static string Launched(string presetName, LauncherBatchLaunchReport report) =>
        $"启动组“{presetName}”: {LauncherSelectionStatus.BatchLaunchCompleted(report, "启动项")}";

    public static string LaunchCanceled(string presetName, int entryCount) =>
        $"已取消: 启动组 {presetName}（{entryCount} 项）";

    public static string LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组“{presetName}”启动失败: {exception.Message}";
    }

    public static string Saved(string presetName, int entryCount) =>
        $"已保存启动组: {presetName}（{entryCount} 项）";

    public static string UpdateBlocked(string presetName) =>
        $"系统启动组不能更新: {presetName}";

    public static string Updated(string presetName, int entryCount) =>
        $"已更新启动组: {presetName}（{entryCount} 项）";

    public static string DeleteBlocked(string presetName) =>
        $"启动组不能删除: {presetName}";

    public static string DeleteCanceled(string presetName) =>
        $"已取消: 删除启动组 {presetName}";

    public static string DeleteConfirmationFailed(string presetName, Exception exception) =>
        $"删除启动组确认失败: {presetName} — {exception.Message}";

    public static string Deleted(string presetName, string selectedPresetName) =>
        $"已删除启动组: {presetName}；当前启动组: {selectedPresetName}";
}
