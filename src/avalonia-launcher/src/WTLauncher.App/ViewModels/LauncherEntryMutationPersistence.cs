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

        if (!LauncherConfigPersistence.Save(entries, configPath, out var configError))
            return LauncherEntryMutationPersistenceResult.Failed(configError ?? "配置保存失败");

        if (savePresets && !LauncherPresetPersistence.Save(presetFile, presetPath, out var presetError))
        {
            var status = LauncherEntryPersistenceRollback.Restore(
                rollbackSnapshot,
                configPath,
                presetPath,
                restorePresets: false,
                presetError ?? "启动组保存失败");
            return LauncherEntryMutationPersistenceResult.Failed(status);
        }

        beforeSaveSettings();
        try
        {
            LauncherSettingsStore.Save(settings, settingsPath);
        }
        catch (Exception ex)
        {
            var status = LauncherEntryPersistenceRollback.Restore(
                rollbackSnapshot,
                configPath,
                presetPath,
                restorePresets: savePresets,
                currentStatus: $"设置保存失败: {ex.Message}");
            return LauncherEntryMutationPersistenceResult.Failed(status);
        }

        return LauncherEntryMutationPersistenceResult.Saved();
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
