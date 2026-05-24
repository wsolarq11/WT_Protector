using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherConfigPersistence
{
    public static bool Save(
        IEnumerable<LaunchEntry> entries,
        string configPath,
        out string? errorMessage)
    {
        try
        {
            LauncherConfigStore.Save(entries, configPath);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"配置保存失败: {ex.Message}";
            return false;
        }
    }
}
