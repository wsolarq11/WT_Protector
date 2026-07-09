using System.Collections.Immutable;
using System.Text.Json;

namespace WTLauncher.Core;

public static class LauncherStore
{
    public const string ConfigDirectoryName = "config";
    public const string StoreFileName = "launcher.json";
    public const string RepositoryMarkerFileName = "README.md";
    public const int ExpectedStoreSchemaVersion = 2;

    // ---- Load / Save ----

    public static LauncherStoreFile Load(string storePath)
    {
        ThrowIfStorePathMissing(storePath);

        // F06: One-time migration from legacy three-file config
        var configDir = Path.GetDirectoryName(Path.GetFullPath(storePath));
        if (configDir is not null)
        {
            var migrated = MigrateFromLegacyFiles(configDir);
            if (migrated is not null)
                return migrated;
        }

        LauncherStoreFile LoadAndValidate(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > 50 * 1024 * 1024)
                throw new InvalidDataException("Launcher store file exceeds 50 MB size limit.");

            var store = LauncherJson.LoadRequired<LauncherStoreFile>(path, "Launcher store");

            if (store.SchemaVersion > ExpectedStoreSchemaVersion)
                throw new InvalidDataException(
                    $"Launcher store schema version {store.SchemaVersion} is from a newer version of the app. Please upgrade WT Launcher.");

            return store;
        }

        return LauncherJson.LoadWithRecovery(storePath, LoadAndValidate, "StoreLoad.RecoveredFromBackup",
            new LauncherStoreFile());
    }

    public static void Save(LauncherStoreFile store, string storePath, bool backup = true)
    {
        ArgumentNullException.ThrowIfNull(store);
        ThrowIfStorePathMissing(storePath);

        LauncherJson.Save(store, storePath, backup);
    }

    // ---- Path resolution ----

    /// <summary>
    /// Resolves the path to config/launcher.json (the unified store file).
    /// </summary>
    public static string ResolveStorePath(string? explicitPath, string? currentDirectory = null,
        string? appBaseDirectory = null)
    {
        if (explicitPath is not null)
        {
            var trimmedExplicitPath = explicitPath.Trim();
            if (trimmedExplicitPath.Length == 0)
                throw new ArgumentException("Explicit store path is required.", nameof(explicitPath));

            var fullPath = Path.GetFullPath(trimmedExplicitPath);
            if (File.Exists(fullPath))
            {
                var fileName = Path.GetFileName(fullPath);
                var dirName = Path.GetFileName(Path.GetDirectoryName(fullPath));
                if (string.Equals(fileName, StoreFileName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(dirName, ConfigDirectoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return fullPath;
                }
            }
            // ponytail: explicit path missing or invalid → fall through to auto-discovery.
            // This handles cases where --config is a relative path and the working directory
            // is not the repo root (e.g. launching from src/avalonia-launcher/).
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

    // F06: Temporary type for legacy preset file deserialization (presets.json format)
    private sealed record LegacyPresetDocument
    {
        public int SchemaVersion { get; init; }
        public string? DefaultPresetId { get; init; }
        public ImmutableList<LauncherPreset>? Presets { get; init; }
    }

    // F06: One-time migration from legacy three-file format to unified store.json

    /// <summary>Detects old config.json/presets.json/settings.json and migrates to store.json.</summary>
    public static LauncherStoreFile? MigrateFromLegacyFiles(string configDirectory)
    {
        var configPath = Path.Combine(configDirectory, "config.json");
        var presetsPath = Path.Combine(configDirectory, "presets.json");
        var settingsPath = Path.Combine(configDirectory, "settings.json");
        var storePath = Path.Combine(configDirectory, StoreFileName);

        // Only migrate if unified store doesn't exist but legacy files do
        if (File.Exists(storePath))
            return null;
        if (!File.Exists(configPath))
            return null;

        // Read legacy config (flat array of LaunchEntry)
        var configItems = LauncherJson.Load<List<LaunchEntry>>(configPath)
            ?? throw new InvalidDataException("Failed to load legacy config.json");

        // Read legacy settings (optional — may not exist)
        var settings = new LauncherSettings();
        if (File.Exists(settingsPath))
        {
            try { settings = LauncherJson.LoadRequired<LauncherSettings>(settingsPath, "legacy settings"); }
            catch { /* use defaults */ }
        }

        // Read legacy presets (optional — may not exist)
        ImmutableList<LauncherPreset>? presets = null;
        var defaultPresetId = "";
        if (File.Exists(presetsPath))
        {
            try
            {
                var legacy = LauncherJson.Load<LegacyPresetDocument>(presetsPath);
                if (legacy is not null)
                {
                    presets = legacy.Presets;
                    defaultPresetId = legacy.DefaultPresetId ?? "";
                }
            }
            catch { /* use empty presets */ }
        }

        var storeFile = new LauncherStoreFile
        {
            Entries = (configItems ?? []).ToImmutableList(),
            Settings = settings,
            Presets = presets ?? ImmutableList<LauncherPreset>.Empty,
            DefaultPresetId = defaultPresetId,
        };

        // Write unified store
        LauncherJson.Save(storeFile, storePath, backup: true);

        // Rename legacy files to .bak
        TryMoveToBak(configPath);
        TryMoveToBak(presetsPath);
        TryMoveToBak(settingsPath);

        return storeFile;
    }

    private static void TryMoveToBak(string path)
    {
        try { File.Move(path, path + ".bak", overwrite: true); }
        catch { /* best-effort */ }
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
            "Cannot resolve store path: no explicit path was provided and both currentDirectory and appBaseDirectory are null or empty.",
            "currentDirectory");
    }

    private static string BuildStorePath(string root) =>
        Path.Combine(Path.GetFullPath(root), ConfigDirectoryName, StoreFileName);

    private static string BuildRepositoryMarkerPath(string root) =>
        Path.Combine(Path.GetFullPath(root), RepositoryMarkerFileName);
}
