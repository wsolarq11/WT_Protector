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
        LauncherEntryMutationSnapshot? rollbackSnapshot,
        Action beforeSaveSettings)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(beforeSaveSettings);

        try
        {
            LauncherConfigStore.Save(entries, configPath);
        }
        catch (Exception ex)
        {
            return LauncherPersistenceStatus.ConfigSaveFailed(ex);
        }

        if (savePresets)
        {
            try
            {
                LauncherPresetStore.Save(presetFile, presetPath);
            }
            catch (Exception ex)
            {
                var status = RestoreSnapshotOnPersistenceFailure(
                    rollbackSnapshot,
                    configPath,
                    presetPath,
                    restorePresets: false,
                    currentStatus: LauncherPersistenceStatus.PresetSaveFailed(ex));
                return status;
            }
        }

        beforeSaveSettings();
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
