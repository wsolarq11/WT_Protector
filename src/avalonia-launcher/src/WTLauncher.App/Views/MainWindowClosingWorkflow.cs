using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowClosingWorkflow
{
    public static bool TrySaveWindowBounds(
        LauncherSettings? settings,
        string? settingsPath,
        double width,
        double height,
        Action<string>? reportStatus)
    {
        if (settings is null || settingsPath is null)
            return true;

        if (LauncherSettingsStore.TrySaveWindowBounds(settings, settingsPath, width, height))
            return true;

        reportStatus?.Invoke("窗口尺寸保存失败；下次将使用上次保存的尺寸");
        return false;
    }
}
