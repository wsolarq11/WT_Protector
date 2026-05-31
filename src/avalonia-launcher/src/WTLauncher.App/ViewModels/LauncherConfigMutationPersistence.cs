using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherConfigMutationPersistence
{
    public static string? Save(IEnumerable<LaunchEntry> entries, string configPath)
    {
        ArgumentNullException.ThrowIfNull(entries);

        try
        {
            LauncherConfigStore.Save(entries, configPath, backup: true);
            return null;
        }
        catch (Exception ex)
        {
            return LauncherPersistenceStatus.ConfigSaveFailed(ex);
        }
    }
}
