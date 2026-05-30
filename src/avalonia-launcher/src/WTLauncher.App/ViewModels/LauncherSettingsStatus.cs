namespace WTLauncher.App.ViewModels;

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
}
