using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetMutationPersistence
{
    public static string? Save(LauncherPresetFile presetFile, string presetPath)
    {
        ArgumentNullException.ThrowIfNull(presetFile);

        try
        {
            LauncherPresetStore.Save(presetFile, presetPath, backup: true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return LauncherPersistenceStatus.PresetSaveFailed(ex);
        }
    }
}
