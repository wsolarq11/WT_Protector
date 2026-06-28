using System.Text.Json.Serialization;

namespace WTLauncher.Core;

// ponytail: shrunk from 169→~90 lines. Write/Discard are dead since the
// multi-file save path was replaced by LauncherStore.Save. Keep Replay
// for startup crash recovery (Program.cs calls it).
public static class CommitJournal
{
    private const string JournalFileName = ".commit-journal";

    public static string ResolvePath(string configDir)
    {
        ArgumentNullException.ThrowIfNull(configDir);
        return System.IO.Path.Combine(configDir, JournalFileName);
    }

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
            TryDelete(journalPath);
            return null;
        }

        if (entries is null || entries.Length == 0)
        {
            TryDelete(journalPath);
            return null;
        }

        var configDir = Path.GetDirectoryName(Path.GetFullPath(journalPath))!;

        foreach (var entry in entries)
        {
            if (!System.IO.File.Exists(entry.Temp))
                continue;

            var resolvedTemp = Path.GetFullPath(entry.Temp);
            var resolvedTarget = Path.GetFullPath(entry.Target);
            var configDirWithSep = configDir.EndsWith(Path.DirectorySeparatorChar) ? configDir : configDir + Path.DirectorySeparatorChar;
            if (!resolvedTemp.StartsWith(configDirWithSep, StringComparison.OrdinalIgnoreCase)
                || !resolvedTarget.StartsWith(configDirWithSep, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(journalPath);
                return "Commit journal entry references a path outside the config directory.";
            }

            try
            {
                var bakTemp = entry.Target + $".bak-tmp-{System.Guid.NewGuid():N}";
                if (System.IO.File.Exists(entry.Target))
                {
                    System.IO.File.Copy(entry.Target, bakTemp, overwrite: true);
                    LauncherJson.RotateBackups(entry.Target);
                    System.IO.File.Move(bakTemp, entry.Target + ".bak", overwrite: true);
                }

                System.IO.File.Replace(entry.Temp, entry.Target, null);
            }
            catch (System.Exception ex) when (ex is System.IO.IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException)
            {
                return $"Commit journal replay failed ({entry.Target}): {ex.Message}";
            }
        }

        TryDelete(journalPath);
        return null;
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

    internal sealed record JournalEntry(
        [property: JsonPropertyName("temp")] string Temp,
        [property: JsonPropertyName("target")] string Target);
}
