using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherSettingsMutationPersistence
{
    public static string? Save(
        ref LauncherSettings settings,
        string settingsPath,
        LauncherSettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);

        try
        {
            LauncherSettingsStore.Save(settings, settingsPath);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            settings = snapshot.Restore();
            return LauncherPersistenceStatus.SettingsSaveFailed(ex);
        }
    }
}
