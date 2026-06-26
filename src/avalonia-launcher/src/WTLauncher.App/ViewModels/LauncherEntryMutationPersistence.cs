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

        // Journal phase: write a commit journal BEFORE the first rename so crash
        // recovery can complete the commit if the process terminates mid-commit.
        var configDir = Path.GetDirectoryName(Path.GetFullPath(configPath))
            ?? throw new InvalidOperationException("Config path must have a parent directory.");
        var journalPath = CommitJournal.ResolvePath(configDir);
        var journalEntries = new List<(string Temp, string Target)>
        {
            (configTempPath, configPath),
            (settingsTempPath, settingsPath),
        };
        if (savePresets)
            journalEntries.Insert(1, (presetTempPath, presetPath));

        var journalError = CommitJournal.Write(journalPath, journalEntries);
        if (journalError is not null)
        {
            TryDelete(configTempPath);
            TryDelete(presetTempPath);
            TryDelete(settingsTempPath);
            return LauncherPersistenceStatus.ConfigSaveFailed(
                new InvalidOperationException(journalError));
        }

        // Commit phase: rename each staged file to its target. Each rename is
        // irreversible (File.Move is atomic per-file on NTFS). The journal
        // provides crash recovery — if the process dies mid-commit, the next
        // startup replays incomplete journal entries.
        // Live rename failures leave the journal on disk with remaining entries;
        // the next Save() or startup will finish them.

        // Commit config.
        var commitError = CommitIfNeeded(journalEntries[0]);
        if (commitError is not null)
        {
            TryDelete(configTempPath);
            TryDelete(presetTempPath);
            TryDelete(settingsTempPath);
            return commitError;
        }

        // Commit presets (optional).
        if (savePresets)
        {
            commitError = CommitIfNeeded(journalEntries[1]);
            if (commitError is not null)
            {
                TryDelete(presetTempPath);
                TryDelete(settingsTempPath);
                return commitError;
            }
        }

        // Commit settings.
        var settingsIndex = savePresets ? 2 : 1;
        commitError = CommitIfNeeded(journalEntries[settingsIndex]);
        if (commitError is not null)
        {
            TryDelete(settingsTempPath);
            return commitError;
        }

        // All committed — journal no longer needed.
        CommitJournal.Discard(journalPath);
        return null;
    }

    /// <summary>Commit a single journal entry: rename temp → target.
    /// On success the temp file is consumed and the journal entry is effectively done.
    /// On failure the journal entry stays for crash/retry recovery.</summary>
    private static string? CommitIfNeeded((string Temp, string Target) entry)
    {
        if (!File.Exists(entry.Temp))
            return null; // Already committed by a prior replay.

        try
        {
            File.Move(entry.Temp, entry.Target, overwrite: true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or PathTooLongException or System.Security.SecurityException)
        {
            return LauncherPersistenceStatus.ConfigSaveFailed(ex);
        }
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

}
