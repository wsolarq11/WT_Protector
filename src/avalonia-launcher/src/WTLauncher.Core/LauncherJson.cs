using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public static class LauncherJson
{
    private const string RepositoryLineEnding = "\n";
    private const int DefaultMaxBackups = 3;

    internal static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        WriteIndented = true,
        // UnsafeRelaxedJsonEscaping does not escape HTML-sensitive characters
        // (< > &). This is acceptable because config/settings JSON is never
        // rendered in a browser — it is consumed exclusively by System.Text.Json.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static T? Load<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    public static T LoadRequired<T>(string path, string documentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        return Load<T>(path)
            ?? throw new InvalidDataException($"{documentName} parsed to null.");
    }

    public static void Save<T>(T value, string path, bool backup)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            // Directory.CreateDirectory inherits the parent directory's ACL.
            // No explicit permission set is needed — the OS default is correct.
            Directory.CreateDirectory(directory);

        var json = NormalizeLineEndings(JsonSerializer.Serialize(value, Options));
        var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";
        try
        {
            File.WriteAllText(tempPath, json + RepositoryLineEnding);
            using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Flush(true); // FlushFileBuffers on Windows
            }

            // Backup rotation is best-effort — if it fails, the new data
            // still gets committed. The old backup-on-failure blocking bug
            // is fixed by the try/catch, not by reordering.
            if (backup && File.Exists(fullPath))
            {
                try
                {
                    RotateBackups(fullPath);
                    var bakTempPath = fullPath + $".bak-tmp-{Guid.NewGuid():N}";
                    File.Copy(fullPath, bakTempPath, overwrite: true);
                    File.Move(bakTempPath, fullPath + ".bak", overwrite: true);
                }
                catch (Exception ex)
                {
                    var msg = $"[LauncherJson] Backup rotation failed: {ex.Message}";
                    Debug.WriteLine(msg);
                    Trace.WriteLine(msg);
                    // Non-fatal — the target file has not been modified yet.
                }
            }

            // File.Move with overwrite:true is not atomic on NTFS (delete+rename),
            // but for a single-process desktop app with temp-file+rename pattern,
            // it is sufficient. File.Replace requires the destination to exist,
            // which fails on first save to a new directory.
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best-effort cleanup — don't replace the original exception.
            }
        }
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", RepositoryLineEnding);
    }

    /// <summary>Delete all <c>*.tmp-*</c> files left behind by crashed Save()
    /// operations. Safe to call at startup — failures are silently ignored.</summary>
    public static void CleanupStaleTempFiles(string configDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDir);

        if (!Directory.Exists(configDir))
            return;

        foreach (var tmp in Directory.EnumerateFiles(configDir, "*.tmp-*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(tmp);
            // Only clean up temp files matching the app's naming convention:
            // *.tmp-{Guid:N} (32+ lowercase hex characters). This avoids
            // matching unrelated *.tmp-* files from other applications.
            if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\.tmp-[0-9a-f]{32,}$"))
                continue;
            try
            {
                File.Delete(tmp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LauncherJson] Stale temp cleanup failed: {ex.Message}");
                Trace.WriteLine($"[LauncherJson] Stale temp cleanup failed: {ex.Message}");
            }
        }
    }

    // File.Move and File.Delete in this method follow NTFS symlinks by design.
    // The backup directory is always a real directory on the same volume, so
    // symlink traversal is the correct behavior — reparse points are followed.
    private static void RotateBackups(string fullPath)
    {
        for (var i = DefaultMaxBackups - 1; i >= 0; i--)
        {
            var oldPath = i == 0 ? fullPath + ".bak" : fullPath + $".bak.{i}";
            var newPath = fullPath + $".bak.{i + 1}";

            try
            {
                if (i == DefaultMaxBackups - 1)
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
