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
        LauncherEntryMutationSnapshot? rollbackSnapshot)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(settings);

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
                var status = RestoreSnapshotOnPersistenceFailure(
                    rollbackSnapshot,
                    configPath,
                    presetPath,
                    restorePresets: false,
                    currentStatus: presetSaveFailure);
                return status;
            }
        }

        try
        {
            LauncherSettingsStore.Save(settings, settingsPath);
        }
        catch (Exception ex)
        {
            var status = RestoreSnapshotOnPersistenceFailure(
                rollbackSnapshot,
                configPath,
                presetPath,
                restorePresets: savePresets,
                currentStatus: LauncherPersistenceStatus.SettingsSaveFailed(ex));
            return status;
        }

        return null;
    }

    private static string RestoreSnapshotOnPersistenceFailure(
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

        return LauncherPersistenceStatus.RollbackFailed(currentStatus, restoreErrors);
    }
}
