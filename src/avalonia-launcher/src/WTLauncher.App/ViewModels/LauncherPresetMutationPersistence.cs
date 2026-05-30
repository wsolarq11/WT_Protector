using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetMutationPersistence
{
    public static string? Save(LauncherPresetFile presetFile, string presetPath)
    {
        ArgumentNullException.ThrowIfNull(presetFile);

        try
        {
            LauncherPresetStore.Save(presetFile, presetPath);
            return null;
        }
        catch (Exception ex)
        {
            return LauncherPersistenceStatus.PresetSaveFailed(ex);
        }
    }
}
