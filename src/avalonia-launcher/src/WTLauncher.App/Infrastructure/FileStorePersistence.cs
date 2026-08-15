using System.Diagnostics;
using System.Text.Json;
using ErrorOr;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Zone 1: File-based launcher store persistence.
/// Directly wraps <see cref="FileLauncherJsonIO"/> for JSON I/O and
/// delegates schema validation to <see cref="LauncherStoreValidator"/>.
/// No static LauncherJson facade dependencies.
/// </summary>
public sealed class FileStorePersistence : IStorePersistence, IDisposable
{
    private readonly FileLauncherJsonIO _io = new();

    // ---- Load / Save ----

    public LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null)
    {
        ThrowIfStorePathMissing(storePath);

        var configDir = Path.GetDirectoryName(Path.GetFullPath(storePath));
        if (configDir is not null)
        {
            var migrated = LegacyMigration.MigrateFromLegacyFiles(configDir);
            if (migrated is not null)
                return migrated;
        }

        LauncherStoreFile LoadAndValidate(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > 50 * 1024 * 1024)
                throw new InvalidDataException("Launcher store file exceeds 50 MB size limit.");

            var store = _io.LoadRequired<LauncherStoreFile>(path, "Launcher store");
            LauncherStoreValidator.ThrowIfInvalid(store);

            // STJ record deserialization does not run property initializers, so a
            // missing optional settings key arrives as null/false instead of the
            // declared default. Apply the declared defaults for missing/blank
            // optional settings (quickLaunchHotkey, runInTray, updateManifestUrl)
            // using the raw JSON to distinguish "key absent" from "explicit value".
            var rawJson = File.ReadAllText(path);
            return LauncherSettingsDefaults.ApplyMissingSettingsDefaults(store, rawJson);
        }

        return _io.LoadWithRecovery(storePath, LoadAndValidate, "StoreLoad.RecoveredFromBackup",
            new LauncherStoreFile(), onRecovery);
    }

    public void Save(LauncherStoreFile store, string storePath, bool backup = true)
    {
        ArgumentNullException.ThrowIfNull(store);
        ThrowIfStorePathMissing(storePath);
        _io.Save(store, storePath, backup);
    }

    // ---- ErrorOr overloads ----

    public ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath)
    {
        try { return Load(storePath); }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException or IOException)
        { return LauncherErrors.ConfigLoadFailed(ex.Message); }
        catch (ArgumentException ex)
        { return LauncherErrors.ValidationFailed(ex.Message); }
    }

    public void Dispose() => _io.Dispose();

    // ---- Path resolution ----

    public string ResolveStorePath(string? explicitPath, string? currentDirectory = null,
        string? appBaseDirectory = null)
    {
        if (explicitPath is not null)
        {
            var trimmedExplicitPath = explicitPath.Trim();
            if (trimmedExplicitPath.Length == 0)
                throw new ArgumentException("Explicit store path is required.", nameof(explicitPath));

            var fullPath = Path.GetFullPath(trimmedExplicitPath);
            if (File.Exists(fullPath))
                return fullPath;
            throw new FileNotFoundException($"Store file not found: {fullPath}", fullPath);
        }

        string?[] candidateRoots = [currentDirectory, appBaseDirectory];
        foreach (var candidateRoot in candidateRoots)
        {
            var found = FindUpward(candidateRoot);
            if (found is not null)
                return found;
        }

        foreach (var candidateRoot in candidateRoots)
        {
            if (string.IsNullOrWhiteSpace(candidateRoot))
                continue;
            var store = BuildStorePath(candidateRoot);
            if (File.Exists(store))
                return store;
        }

        return BuildStorePath(RequireSearchRoot(candidateRoots));
    }

    // ---- Path resolution helpers ----

    private static void ThrowIfStorePathMissing(string? storePath)
    {
        if (string.IsNullOrWhiteSpace(storePath))
            throw new ArgumentException("Store path is required.", nameof(storePath));
    }

    private static string? FindUpward(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
            return null;

        for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
        {
            var store = BuildStorePath(dir.FullName);
            if (File.Exists(store) && File.Exists(BuildRepositoryMarkerPath(dir.FullName)))
                return store;
        }

        return null;
    }

    private static string RequireSearchRoot(IEnumerable<string?> candidateRoots)
    {
        foreach (var candidateRoot in candidateRoots)
        {
            if (!string.IsNullOrWhiteSpace(candidateRoot))
                return candidateRoot;
        }

        throw new ArgumentException(
            "Cannot resolve store path: no explicit path was provided and both " +
            "currentDirectory and appBaseDirectory are null or empty.",
            nameof(candidateRoots));
    }

    private static string BuildStorePath(string root) =>
        Path.Combine(Path.GetFullPath(root), LauncherStore.ConfigDirectoryName, LauncherStore.StoreFileName);

    private static string BuildRepositoryMarkerPath(string root) =>
        Path.Combine(Path.GetFullPath(root), LauncherStore.RepositoryMarkerFileName);
}
