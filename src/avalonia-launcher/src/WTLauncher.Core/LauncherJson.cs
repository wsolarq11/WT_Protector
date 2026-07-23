using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ErrorOr;

[assembly: InternalsVisibleTo("WTLauncher.App")]
[assembly: InternalsVisibleTo("WTLauncher.Tests")]

namespace WTLauncher.Core;

public static class LauncherJson
{
    private const string RepositoryLineEnding = "\n";
    private const int DefaultMaxBackups = 3;

    /// <summary>Source-generated JSON serializer options used internally by Load/Save.
    /// Exposed for test round-trip verification.</summary>
    internal static JsonSerializerOptions Options => LauncherJsonContext.Default.Options;

    /// <summary>Defense-in-depth: serializes concurrent calls to Save.
    /// AppStore already serializes per-instance; this protects against cross-instance
    /// or direct LauncherStore.Save calls from multiple threads.</summary>
    private static readonly SemaphoreSlim SaveLock = new(1, 1);
    public static T? Load<T>(string path) where T : notnull
    {
        using var stream = File.OpenRead(path);
        var typeInfo = (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!;
        return JsonSerializer.Deserialize(stream, typeInfo);
    }

    public static T LoadRequired<T>(string path, string documentName) where T : notnull
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
        T defaultValue,
        Action<Exception, string>? onRecovery = null)
    {
        if (File.Exists(path))
        {
            Exception? loadException = null;
            try
            {
                return loadAndValidate(path);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException or IOException)
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
                        // Atomic replace — path exists (we just failed to load it).
                        File.Replace(recoveryTempPath, path, null);
                        var recovered = loadAndValidate(path);
                        onRecovery?.Invoke(loadException, recoveryContext);
                        return recovered;
                    }
                    catch (Exception recoveryEx) when (recoveryEx is IOException or UnauthorizedAccessException)
                    {
                        // Recovery failed — surface the original load exception, not the recovery failure.
                    }
                }
                // Primary file exists but is corrupt, no backup available.
                // Keep the corrupt file on disk and throw so the caller knows.
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
                // Primary doesn't exist — simple rename. Atomicity isn't critical
                // here since the destination is empty.
                File.Move(recoveryTempPath, path);
                var recovered = loadAndValidate(path);
                onRecovery?.Invoke(new InvalidDataException("Config file was missing and recovered from backup."), recoveryContext);
                return recovered;
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
        SaveLock.Wait();
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                // Directory.CreateDirectory inherits the parent directory's ACL.
                // No explicit permission set is needed — the OS default is correct.
                Directory.CreateDirectory(directory);

            var typeInfo = (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!;
            var json = NormalizeLineEndings(JsonSerializer.Serialize(value, typeInfo));
            var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";

            File.WriteAllText(tempPath, json + RepositoryLineEnding);

            // Backup rotation is best-effort — if it fails, the new data
            // still gets committed. The old backup-on-failure blocking bug
            // is fixed by the try/catch, not by reordering.
            if (backup && File.Exists(fullPath))
            {
                var bakTempPath = fullPath + $".bak-tmp-{Guid.NewGuid():N}";
                try
                {
                    // ponytail: copy current to temp BEFORE rotating old backups.
                    // If crash occurs after rotation, bakTempPath already holds
                    // the safe copy — no window where .bak is missing.
                    File.Copy(fullPath, bakTempPath, overwrite: true);
                    RotateBackups(fullPath);
                    File.Move(bakTempPath, fullPath + ".bak", overwrite: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    try { File.Delete(bakTempPath); }
                    catch (Exception deleteEx) when (deleteEx is IOException or UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"[LauncherJson] Stale bak-tmp cleanup failed: {deleteEx.Message}");
                    }
                    var msg = $"[LauncherJson] Backup rotation failed: {ex.Message}";
                    Debug.WriteLine(msg);
                    Trace.WriteLine(msg);
                    // Non-fatal — the target file has not been modified yet.
                }
            }

            // Commit: writes are idempotent (full overwrite). If this throws,
            // tempPath remains for recovery — CleanupStaleTempFiles handles it.
            if (File.Exists(fullPath))
            {
                // Atomic on NTFS: MoveFileEx with MOVEFILE_REPLACE_EXISTING.
                // The replacement file is moved onto the target atomically —
                // no window where the target is deleted without replacement.
                File.Replace(tempPath, fullPath, null);
            }
            else
            {
                File.Move(tempPath, fullPath);
            }
        }
        finally
        {
            SaveLock.Release();
        }
    }

    // ---- ErrorOr overloads ----

    /// <summary>Load and deserialize JSON, returning ErrorOr for recoverable failures.</summary>
    public static ErrorOr<T> LoadErrorOr<T>(string path) where T : notnull
    {
        try
        {
            var value = Load<T>(path);
            return value is not null ? value : Error.Failure("Load.Null", "Parsed to null");
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException or IOException)
        { return LauncherErrors.ConfigLoadFailed(ex.Message); }
    }

    /// <summary>Serialize and save JSON, returning ErrorOr for recoverable failures.</summary>
    public static ErrorOr<Success> SaveErrorOr<T>(T value, string path, bool backup)
    {
        try { Save(value, path, backup); return Result.Success; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return LauncherErrors.ConfigSaveFailed(ex.Message); }
    }

    // ponytail: ReplaceLineEndings replaces the hand-rolled \r\n → \n normalizer.
    private static string NormalizeLineEndings(string text) =>
        text.ReplaceLineEndings(RepositoryLineEnding);

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
                if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\.(tmp|stage|bak-tmp|recovery)-[0-9a-f]{32}$"))
                    continue;
                try
                {
                    File.Delete(tmp);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
