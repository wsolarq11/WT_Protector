using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryMutationPersistence
{
    public static LauncherEntryMutationPersistenceResult Save(
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
            return LauncherEntryMutationPersistenceResult.Failed($"配置保存失败: {ex.Message}");
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
                    currentStatus: $"启动组保存失败: {ex.Message}");
                return LauncherEntryMutationPersistenceResult.Failed(status);
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
                currentStatus: $"设置保存失败: {ex.Message}");
            return LauncherEntryMutationPersistenceResult.Failed(status);
        }

        return LauncherEntryMutationPersistenceResult.Saved();
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

        return restoreErrors.Count == 0
            ? currentStatus
            : $"{currentStatus}；回滚磁盘状态失败: {string.Join("；", restoreErrors)}";
    }
}

public readonly record struct LauncherEntryMutationPersistenceResult(bool Succeeded, string? StatusText)
{
    public static LauncherEntryMutationPersistenceResult Saved()
    {
        return new LauncherEntryMutationPersistenceResult(true, null);
    }

    public static LauncherEntryMutationPersistenceResult Failed(string statusText)
    {
        return new LauncherEntryMutationPersistenceResult(false, statusText);
    }
}
