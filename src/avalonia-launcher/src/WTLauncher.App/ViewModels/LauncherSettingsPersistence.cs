using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSettingsPersistence
{
    public static bool Save(LauncherSettings settings, string settingsPath, out string? errorMessage)
    {
        try
        {
            LauncherSettingsStore.Save(settings, settingsPath);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"设置保存失败: {ex.Message}";
            return false;
        }
    }
}
