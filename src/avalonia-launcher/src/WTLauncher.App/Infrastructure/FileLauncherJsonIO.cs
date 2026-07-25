using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ErrorOr;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Zone 1: File-based JSON persistence with atomic writes,
/// backup rotation, and corrupt-file recovery.
/// </summary>
public sealed class FileLauncherJsonIO : ILauncherJsonIO, IDisposable
{
    private const string RepositoryLineEnding = "\n";
    private const int DefaultMaxBackups = 3;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public T? Load<T>(string path) where T : notnull
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream,
            (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!);
    }

    public T LoadRequired<T>(string path, string documentName) where T : notnull
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
    public T LoadWithRecovery<T>(
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
                        File.Replace(recoveryTempPath, path, null);
                        var recovered = loadAndValidate(path);
                        onRecovery?.Invoke(loadException, recoveryContext);
                        return recovered;
                    }
                    catch (Exception recoveryEx) when (recoveryEx is IOException or UnauthorizedAccessException)
                    {
                    }
                }
                throw loadException;
            }
        }

        for (var bakIndex = 0; bakIndex < DefaultMaxBackups; bakIndex++)
        {
            var backupPathForMissing = bakIndex == 0 ? path + ".bak" : path + $".bak.{bakIndex}";
            if (!File.Exists(backupPathForMissing))
                continue;

            try
            {
                var recoveryTempPath = path + $".recovery-{Guid.NewGuid():N}";
                File.Copy(backupPathForMissing, recoveryTempPath, overwrite: true);
                File.Move(recoveryTempPath, path);
                var recovered = loadAndValidate(path);
                onRecovery?.Invoke(new InvalidDataException($"Config file was missing and recovered from backup chain ({backupPathForMissing})."), recoveryContext);
                return recovered;
            }
            catch (Exception recoveryEx) when (recoveryEx is IOException or UnauthorizedAccessException)
            {
                continue;
            }
        }

        return defaultValue;
    }

    public void Save<T>(T value, string path, bool backup) where T : notnull
    {
        _saveLock.Wait();
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var json = LauncherJsonSerializer.Serialize(value);
            var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";

            File.WriteAllText(tempPath, json + RepositoryLineEnding);

            if (backup && File.Exists(fullPath))
            {
                var bakTempPath = fullPath + $".bak-tmp-{Guid.NewGuid():N}";
                try
                {
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
                }
            }

            if (File.Exists(fullPath))
                File.Replace(tempPath, fullPath, null);
            else
                File.Move(tempPath, fullPath);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public ErrorOr<T> LoadErrorOr<T>(string path) where T : notnull
    {
        try
        {
            var value = Load<T>(path);
            return value is not null ? value : Error.Failure("Load.Null", "Parsed to null");
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException or IOException)
        { return LauncherErrors.ConfigLoadFailed(ex.Message); }
    }

    public ErrorOr<Success> SaveErrorOr<T>(T value, string path, bool backup) where T : notnull
    {
        try { Save(value, path, backup); return Result.Success; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return LauncherErrors.ConfigSaveFailed(ex.Message); }
    }

    /// <inheritdoc/>
    public void CleanupStaleTempFiles(string configDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDir);
        if (!Directory.Exists(configDir))
            return;

        _saveLock.Wait();
        try
        {
            foreach (var pattern in new[] { "*.tmp-*", "*.stage-*", "*.bak-tmp-*", "*.recovery-*" })
            {
                foreach (var tmp in Directory.EnumerateFiles(configDir, pattern, SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileName(tmp);
                    if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, @"\.(tmp|stage|bak-tmp|recovery)-[0-9a-f]{32}$"))
                        continue;
                    try { File.Delete(tmp); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"[LauncherJson] Stale temp cleanup failed: {ex.Message}");
                        Trace.WriteLine($"[LauncherJson] Stale temp cleanup failed: {ex.Message}");
                    }
                }
            }
        }
        finally { _saveLock.Release(); }
    }

    public void RotateBackups(string fullPath, int maxBackups = DefaultMaxBackups)
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
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    public void Dispose() => _saveLock.Dispose();
}
