namespace WTLauncher.App.ViewModels;

public static class LauncherPersistenceStatus
{
    public static string ConfigSaveFailed(Exception exception) =>
        Failed("配置保存失败", exception);

    public static string PresetSaveFailed(Exception exception) =>
        Failed("启动组保存失败", exception);

    public static string SettingsSaveFailed(Exception exception) =>
        Failed("设置保存失败", exception);

    public static string RollbackFailed(string currentStatus, IReadOnlyList<string> restoreErrors) =>
        restoreErrors.Count == 0
            ? currentStatus
            : $"{currentStatus}；回滚磁盘状态失败: {string.Join("；", restoreErrors)}";

    private static string Failed(string prefix, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{prefix}: {exception.Message}";
    }
}
