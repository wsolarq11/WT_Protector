using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryMutationPersistence
{
    private const int MaxBackups = 3;

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

        // Stage phase: write all files to temp paths.
        // If any write fails, originals are untouched — we discard the temp files
        // and return the error. No file-level rollback is needed because originals
        // were never modified.
        var configTempPath = configPath + $".stage-{Guid.NewGuid():N}";
        var presetTempPath = presetPath + $".stage-{Guid.NewGuid():N}";
        var settingsTempPath = settingsPath + $".stage-{Guid.NewGuid():N}";

        // Stage config.
        try
        {
            LauncherConfigStore.Save(entries, configTempPath, backup: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or PathTooLongException or System.Security.SecurityException)
        {
            TryDelete(configTempPath);
            return LauncherPersistenceStatus.ConfigSaveFailed(ex);
        }

        // Stage presets (optional).
        if (savePresets)
        {
            try
            {
                LauncherPresetStore.Save(presetFile, presetTempPath, backup: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or PathTooLongException or System.Security.SecurityException)
            {
                TryDelete(configTempPath);
                TryDelete(presetTempPath);
                return LauncherPersistenceStatus.PresetSaveFailed(ex);
            }
        }

        // Stage settings. InvalidDataException intentionally propagates here —
        // invalid settings data is a programming error that should surface,
        // not be silently converted to a status message.
        try
        {
            LauncherSettingsStore.Save(settings, settingsTempPath, backup: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException or System.Security.SecurityException)
        {
            TryDelete(configTempPath);
            TryDelete(presetTempPath);
            TryDelete(settingsTempPath);
            return LauncherPersistenceStatus.SettingsSaveFailed(ex);
        }

        // Commit phase: backup rotation on originals (best-effort), then
        // atomically rename each staged file to its final path.
        // File.Move is atomic on the same volume (NTFS). Each rename has its
        // own error handler so we can report which specific file failed.
        // Once a rename succeeds, that file is committed and cannot be undone.
        // This is acceptable because commit failures are extremely rare in
        // production — all paths are validated at app startup.

        // Commit config.
        try
        {
            TryBackupOriginal(configPath);
            File.Move(configTempPath, configPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException or System.Security.SecurityException)
        {
            TryDelete(configTempPath);
            TryDelete(presetTempPath);
            TryDelete(settingsTempPath);
            return LauncherPersistenceStatus.ConfigSaveFailed(ex);
        }

        // Commit presets (optional).
        if (savePresets)
        {
            try
            {
                TryBackupOriginal(presetPath);
                File.Move(presetTempPath, presetPath, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException or System.Security.SecurityException)
            {
                TryDelete(presetTempPath);
                TryDelete(settingsTempPath);
                return LauncherPersistenceStatus.PresetSaveFailed(ex);
            }
        }

        // Commit settings.
        try
        {
            TryBackupOriginal(settingsPath);
            File.Move(settingsTempPath, settingsPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException or System.Security.SecurityException)
        {
            TryDelete(settingsTempPath);
            return LauncherPersistenceStatus.SettingsSaveFailed(ex);
        }

        return null;
    }

    /// <summary>Best-effort file deletion. Intentionally silent — cleanup
    /// should never mask the original error.</summary>
    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    /// <summary>Best-effort backup rotation and .bak copy of the original file.
    /// Failures are silently ignored — backup is a convenience, not a contract.</summary>
    private static void TryBackupOriginal(string fullPath)
    {
        if (!File.Exists(fullPath))
            return;

        try
        {
            RotateBackupChain(fullPath);
            var bakTempPath = fullPath + $".bak-tmp-{Guid.NewGuid():N}";
            File.Copy(fullPath, bakTempPath, overwrite: true);
            File.Move(bakTempPath, fullPath + ".bak", overwrite: true);
        }
        catch
        {
            // Best-effort — backup rotation failures are non-fatal.
        }
    }

    /// <summary>Rotate the .bak, .bak.1, .bak.2 chain for the given file.
    /// Mirrors LauncherJson.RotateBackups for use outside the save pipeline.</summary>
    private static void RotateBackupChain(string fullPath)
    {
        for (var i = MaxBackups - 1; i >= 0; i--)
        {
            var oldPath = i == 0 ? fullPath + ".bak" : fullPath + $".bak.{i}";
            var newPath = fullPath + $".bak.{i + 1}";

            try
            {
                if (i == MaxBackups - 1)
                    File.Delete(oldPath);
                else
                    File.Move(oldPath, newPath, overwrite: true);
            }
            catch (FileNotFoundException)
            {
                // Best-effort: the backup file doesn't exist — nothing to rotate.
            }
            catch (DirectoryNotFoundException)
            {
                // Best-effort: the backup directory doesn't exist — nothing to rotate.
            }
        }
    }
}
