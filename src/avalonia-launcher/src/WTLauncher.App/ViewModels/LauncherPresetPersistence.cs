using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetPersistence
{
    public static string? Load(string presetPath, out LauncherPresetFile presetFile)
    {
        try
        {
            presetFile = LauncherPresetStore.Load(presetPath);
            return null;
        }
        catch (Exception ex)
        {
            presetFile = new LauncherPresetFile();
            return $"启动组加载失败: {ex.Message}";
        }
    }

    public static bool Save(LauncherPresetFile presetFile, string presetPath, out string? errorMessage)
    {
        try
        {
            LauncherPresetStore.Save(presetFile, presetPath);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"启动组保存失败: {ex.Message}";
            return false;
        }
    }
}
