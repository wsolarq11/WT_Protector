namespace WTLauncher.Core;

/// <summary>
/// Lightweight commit journal for multi-file atomic writes.
///
/// Pattern (inspired by SQLite WAL):
///   1. Stage all temp files.
///   2. Write commit journal (single atomic rename).
///   3. Replay journal — rename each temp file to its target.
///   4. On complete success → delete journal.
///   5. On crash → journal survives; next startup replays it.
///
/// This eliminates the partial-commit window where config.json is
/// committed but presets.json is not — the journal serves as the
/// single source of truth during the commit phase. If any rename
/// fails, the journal remains on disk for crash recovery.
/// </summary>
public static class CommitJournal
{
    private const string JournalFileName = ".commit-journal";

    /// <summary>Resolve the journal path for a config directory.</summary>
    public static string ResolvePath(string configDir)
    {
        ArgumentNullException.ThrowIfNull(configDir);
        return System.IO.Path.Combine(configDir, JournalFileName);
    }

    /// <summary>
    /// Write a commit journal listing temp→target mappings for a pending
    /// multi-file commit. The caller MUST have already written all temp
    /// files successfully before calling this.
    /// </summary>
    public static string? Write(
        string journalPath,
        IReadOnlyList<(string TempPath, string TargetPath)> entries)
    {
        ArgumentNullException.ThrowIfNull(journalPath);
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
            throw new ArgumentException("Commit journal must contain at least one entry.", nameof(entries));

        // Serialize entries as a simple JSON array of [temp, target] pairs.
        foreach (var (tempPath, targetPath) in entries)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tempPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        }
        var journalEntries = entries.Select(e => new JournalEntry(e.TempPath, e.TargetPath)).ToArray();
        var journalTempPath = journalPath + $".tmp-{System.Guid.NewGuid():N}";

        try
        {
            LauncherJson.Save(journalEntries, journalTempPath, backup: false);
            System.IO.File.Move(journalTempPath, journalPath, overwrite: true);
            return null; // success
        }
        catch (System.Exception ex) when (ex is System.IO.IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException
            or System.Text.Json.JsonException)
        {
            TryDelete(journalTempPath);
            return $"Failed to write commit journal: {ex.Message}";
        }
    }

    /// <summary>
    /// Replay a commit journal — rename each temp file to its target.
    /// Called at startup for crash recovery, and during normal Save
    /// to commit staged files.
    /// </summary>
    /// <returns>null on success, error message on failure.</returns>
    public static string? Replay(string journalPath)
    {
        if (!System.IO.File.Exists(journalPath))
            return null;

        JournalEntry[]? entries;
        try
        {
            entries = LauncherJson.Load<JournalEntry[]>(journalPath);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or System.IO.IOException or UnauthorizedAccessException)
        {
            // Corrupt journal — discard and move on.
            TryDelete(journalPath);
            return null;
        }

        if (entries is null || entries.Length == 0)
        {
            TryDelete(journalPath);
            return null;
        }

        // Resolve config directory for path traversal validation.
        var configDir = Path.GetDirectoryName(Path.GetFullPath(journalPath))!;

        foreach (var entry in entries)
        {
            // Temp file may already have been consumed by a prior partial replay.
            if (!System.IO.File.Exists(entry.Temp))
                continue;

            // Path traversal guard: both temp and target must be within config directory.
            var resolvedTemp = Path.GetFullPath(entry.Temp);
            var resolvedTarget = Path.GetFullPath(entry.Target);
            // Append separator so C:\config does not match C:\config-backup.
            var configDirWithSep = configDir.EndsWith(Path.DirectorySeparatorChar) ? configDir : configDir + Path.DirectorySeparatorChar;
            if (!resolvedTemp.StartsWith(configDirWithSep, StringComparison.OrdinalIgnoreCase)
                || !resolvedTarget.StartsWith(configDirWithSep, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(journalPath);
                return "Commit journal entry references a path outside the config directory.";
            }

            try
            {
                // Rotate existing .bak chain before overwriting the target.
                LauncherJson.RotateBackups(entry.Target);
                var bakTemp = entry.Target + $".bak-tmp-{System.Guid.NewGuid():N}";
                if (System.IO.File.Exists(entry.Target))
                {
                    System.IO.File.Copy(entry.Target, bakTemp, overwrite: true);
                    System.IO.File.Move(bakTemp, entry.Target + ".bak", overwrite: true);
                }

                System.IO.File.Move(entry.Temp, entry.Target, overwrite: true);
            }
            catch (System.Exception ex) when (ex is System.IO.IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException)
            {
                // Journal stays on disk for next startup recovery attempt.
                return $"Commit journal replay failed ({entry.Target}): {ex.Message}";
            }
        }

        // All entries committed — journal is no longer needed.
        TryDelete(journalPath);
        return null;
    }

    /// <summary>Discard a journal without replaying (all-or-nothing rollback).</summary>
    public static void Discard(string journalPath)
    {
        TryDelete(journalPath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch (System.IO.IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>JSON-serializable journal entry.</summary>
    internal sealed record JournalEntry(string Temp, string Target);
}
