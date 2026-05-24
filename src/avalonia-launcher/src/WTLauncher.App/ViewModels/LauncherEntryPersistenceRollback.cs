using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryPersistenceRollback
{
    public static string Restore(
        LauncherEntryMutationSnapshot? snapshot,
        string configPath,
        string presetPath,
        bool restorePresets,
        string currentStatus)
    {
        if (snapshot is null)
            return currentStatus;

        var restoreErrors = new List<string>();
        try
        {
            LauncherConfigStore.Save(snapshot.CloneEntries(), configPath, backup: false);
        }
        catch (Exception ex)
        {
            restoreErrors.Add($"config: {ex.Message}");
        }

        if (restorePresets)
        {
            try
            {
                LauncherPresetStore.Save(snapshot.ClonePresetFile(), presetPath, backup: false);
            }
            catch (Exception ex)
            {
                restoreErrors.Add($"presets: {ex.Message}");
            }
        }

        return restoreErrors.Count == 0
            ? currentStatus
            : $"{currentStatus}；回滚磁盘状态失败: {string.Join("；", restoreErrors)}";
    }
}
