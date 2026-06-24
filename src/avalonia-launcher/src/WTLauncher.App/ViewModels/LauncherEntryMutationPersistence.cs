using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryMutationPersistence
{
    public static string? Save(
        IEnumerable<LaunchEntry> entries,
        string configPath,
        LauncherPresetFile presetFile,
        string presetPath,
        LauncherSettings settings,
        string settingsPath,
        bool savePresets,
        LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(rollbackSnapshot);

        var configSaveFailure = LauncherConfigMutationPersistence.Save(entries, configPath);
        if (configSaveFailure is not null)
        {
            return configSaveFailure;
        }

        if (savePresets)
        {
            var presetSaveFailure = LauncherPresetMutationPersistence.Save(presetFile, presetPath);
            if (presetSaveFailure is not null)
            {
                return RestoreSnapshotOnPersistenceFailure(
                    rollbackSnapshot,
                    configPath,
                    presetPath,
                    restorePresets: true,
                    currentStatus: presetSaveFailure);
            }
        }

        try
        {
            LauncherSettingsStore.Save(settings, settingsPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return RestoreSnapshotOnPersistenceFailure(
                rollbackSnapshot,
                configPath,
                presetPath,
                restorePresets: savePresets,
                currentStatus: LauncherPersistenceStatus.SettingsSaveFailed(ex));
        }

        return null;
    }

    private static string RestoreSnapshotOnPersistenceFailure(
        LauncherEntryMutationSnapshot snapshot,
        string configPath,
        string presetPath,
        bool restorePresets,
        string currentStatus)
    {
        // WARNING: Config and preset rollback are independent — one can succeed while the
        // other fails. If config rollback succeeds but preset rollback fails, the config
        // and presets files will be inconsistent (config rolled back, presets not). This
        // is intentionally best-effort: an inconsistent state after a double failure is
        // less bad than losing data from the original save attempt.
        List<string> restoreErrors = [];
        try
        {
            LauncherConfigStore.Save(snapshot.CloneEntries(), configPath, backup: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            restoreErrors.Add($"config: {ex.Message}");
        }

        if (restorePresets)
        {
            try
            {
                LauncherPresetStore.Save(snapshot.ClonePresetFile(), presetPath, backup: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                restoreErrors.Add($"presets: {ex.Message}");
            }
        }

        return LauncherPersistenceStatus.RollbackFailed(currentStatus, restoreErrors);
    }
}
