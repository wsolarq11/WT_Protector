using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public static class LauncherJson
{
    private const string RepositoryLineEnding = "\n";
    private const int DefaultMaxBackups = 3;

    /// <summary>
    /// Optional hook for logging recovery-from-backup events. Set by the App layer
    /// at startup to avoid coupling Core to app-layer infrastructure.
    /// </summary>
    public static Action<Exception, string>? OnRecoveryFromBackup;

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

    /// <summary>
    /// Shared file-load-with-backup-recovery used by all Store.Load methods.
    /// Tries <paramref name="loadAndValidate"/>, falls back to <c>.bak</c> on
    /// corruption, and returns <paramref name="defaultValue"/> when no file exists.
    /// </summary>
    internal static T LoadWithRecovery<T>(
        string path,
        Func<string, T> loadAndValidate,
        string recoveryContext,
        T defaultValue)
    {
        if (File.Exists(path))
        {
            Exception? loadException = null;
            try
            {
                return loadAndValidate(path);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException)
            {
                loadException = ex;
            }

            if (loadException is not null)
            {
                var backupPath = path + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        var recoveryTempPath = path + $".recovery-{Guid.NewGuid():N}";
                        File.Copy(backupPath, recoveryTempPath, overwrite: true);
                        File.Move(recoveryTempPath, path, overwrite: true);
                        OnRecoveryFromBackup?.Invoke(loadException, recoveryContext);
                        return loadAndValidate(path);
                    }
                    catch (Exception recoveryEx) when (recoveryEx is IOException or UnauthorizedAccessException)
                    {
                        // Recovery failed — surface the original load exception, not the recovery failure.
                    }
                }
                throw loadException;
            }
        }

        // File missing — try backup or return default.
        var backupPathForMissing = path + ".bak";
        if (File.Exists(backupPathForMissing))
        {
            try
            {
                var recoveryTempPath = path + $".recovery-{Guid.NewGuid():N}";
                File.Copy(backupPathForMissing, recoveryTempPath, overwrite: true);
                File.Move(recoveryTempPath, path, overwrite: true);
                return loadAndValidate(path);
            }
            catch (Exception recoveryEx) when (recoveryEx is IOException or UnauthorizedAccessException)
            {
                // Recovery failed — fall through to return default.
            }
        }

        return defaultValue;
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
            .Replace("\r\n", RepositoryLineEnding)
            .Replace("\r", RepositoryLineEnding);
    }

    /// <summary>Delete all <c>*.tmp-*</c>, <c>*.stage-*</c>, <c>*.bak-tmp-*</c>, and
    /// <c>*.recovery-*</c> files left behind by crashed Save() or recovery
    /// operations. Safe to call at startup — failures are silently ignored.</summary>
    public static void CleanupStaleTempFiles(string configDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDir);

        if (!Directory.Exists(configDir))
            return;

        foreach (var pattern in new[] { "*.tmp-*", "*.stage-*", "*.bak-tmp-*", "*.recovery-*" })
        {
            foreach (var tmp in Directory.EnumerateFiles(configDir, pattern, SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(tmp);
                // Only clean up temp files matching the app's naming convention:
                // *.{tmp|stage|bak-tmp|recovery}-{Guid:N} (32+ lowercase hex characters).
                // This avoids matching unrelated files from other applications.
                if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\.(tmp|stage|bak-tmp|recovery)-[0-9a-f]{32,}$"))
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
    }

    // File.Move and File.Delete in this method follow NTFS symlinks by design.
    // The backup directory is always a real directory on the same volume, so
    // symlink traversal is the correct behavior — reparse points are followed.
    public static void RotateBackups(string fullPath, int maxBackups = DefaultMaxBackups)
    {
        for (var i = maxBackups - 1; i >= 0; i--)
        {
            var oldPath = i == 0 ? fullPath + ".bak" : fullPath + $".bak.{i}";
            var newPath = fullPath + $".bak.{i + 1}";

            try
            {
                if (i == maxBackups - 1)
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
