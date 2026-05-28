using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSettingsMutationPersistence
{
    public static LauncherSettingsMutationResult Save(
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
            return LauncherSettingsMutationResult.Saved();
        }
        catch (Exception ex)
        {
            snapshot.Restore(settings);
            onRollback();
            return LauncherSettingsMutationResult.Failed($"设置保存失败: {ex.Message}");
        }
    }
}

public readonly record struct LauncherSettingsMutationResult(bool Succeeded, string? StatusText)
{
    public static LauncherSettingsMutationResult Saved()
    {
        return new LauncherSettingsMutationResult(true, null);
    }

    public static LauncherSettingsMutationResult Failed(string statusText)
    {
        return new LauncherSettingsMutationResult(false, statusText);
    }
}
