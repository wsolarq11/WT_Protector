using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSettingsMutationPersistence
{
    public static string? Save(
        LauncherSettings settings,
        string settingsPath,
        LauncherSettingsSnapshot snapshot,
        Action onRollback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(onRollback);

        try
        {
            LauncherSettingsStore.Save(settings, settingsPath);
            return null;
        }
        catch (Exception ex)
        {
            snapshot.Restore(settings);
            onRollback();
            return $"设置保存失败: {ex.Message}";
        }
    }
}
