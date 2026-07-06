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
                return Normalize(migrated);
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

            return Normalize(store);
        }

        return LauncherJson.LoadWithRecovery(storePath, LoadAndValidate, "StoreLoad.RecoveredFromBackup",
            Normalize(new LauncherStoreFile()));
    }

    public static void Save(LauncherStoreFile store, string storePath, bool backup = true)
    {
        ArgumentNullException.ThrowIfNull(store);
        ThrowIfStorePathMissing(storePath);

        LauncherJson.Save(Normalize(store), storePath, backup);
    }

    // ---- Path resolution ----

    /// <summary>
    /// Resolves the path to config/store.json. Mirrors LauncherConfigStore.ResolveConfigPath
    /// but targets the unified store file instead of config.json.
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

    // ---- Internal normalization ----

    internal static LauncherStoreFile Normalize(LauncherStoreFile store)
    {
        return store with
        {
            SchemaVersion = ExpectedStoreSchemaVersion,  // No backward-incompatible schema changes exist yet
            Settings = NormalizeSettings(store.Settings),
            Entries = NormalizeEntries(store.Entries),
            Presets = NormalizePresets(store.Presets),
            DefaultPresetId = NormalizeDefaultPresetId(store.DefaultPresetId, store.Presets),
        };
    }

    // -- Entry normalization --

    private static ImmutableList<LaunchEntry> NormalizeEntries(ImmutableList<LaunchEntry>? entries)
    {
        if (entries is null)
            throw new InvalidDataException("Launcher store entries list is null.");

        if (entries.Count > 10000)
            throw new InvalidDataException("Launcher store exceeds 10000 entry limit.");

        List<LaunchEntry> normalized = [];
        var seenIds = new HashSet<string>(EntryId.Comparer);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i] ?? throw new InvalidDataException($"Launcher store entry {i} is null.");
            normalized.Add(NormalizeEntry(entry, i, seenIds));
        }

        return normalized.ToImmutableList();
    }

    private static LaunchEntry NormalizeEntry(LaunchEntry entry, int index, HashSet<string> seenIds)
    {
        return new LaunchEntry
        {
            Id = RequireStoreEntryId(entry.Id, index, seenIds),
            Name = RequireStoreEntryTextField(entry.Name, index, "name"),
            Directory = RequireStoreEntryTextField(entry.Directory, index, "directory"),
            Command = RequireStoreEntryTextField(entry.Command, index, "command"),
            UpdatedAtUtc = NormalizeStoreTimestamp(entry.UpdatedAtUtc, index, "updatedAtUtc"),
            LaunchedAtUtc = NormalizeStoreTimestamp(entry.LaunchedAtUtc, index, "launchedAtUtc"),
            Selected = entry.Selected,
        };
    }

    private static string RequireStoreEntryId(string? id, int index, HashSet<string> seenIds)
    {
        var normalized = EntryId.RequireEntryId(id, nameof(LaunchEntry.Id));

        if (!seenIds.Add(normalized))
            throw new InvalidDataException($"Launcher store entry {index} id is duplicate: {normalized}.");

        return normalized;
    }

    private static string RequireStoreEntryTextField(string? value, int index, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher store entry {index} {fieldName} is null.");

        var normalized = EntryName.Normalize(value);
        if (normalized.Length > 4096)
            throw new InvalidDataException(
                $"Launcher store entry {index} {fieldName} exceeds 4096 character length limit.");
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher store entry {index} {fieldName} is empty.");

        return normalized;
    }

    private static string NormalizeStoreTimestamp(string? value, int index, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher store entry {index} {fieldName} is null.");

        return EntryName.Normalize(value);
    }

    // -- Settings normalization --

    private static LauncherSettings NormalizeSettings(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings with
        {
            SchemaVersion = ExpectedStoreSchemaVersion,
            LastFilterText = NormalizeStoreFilterText(settings.LastFilterText),
            SortMode = NormalizeRequiredStoreMode(settings.SortMode, "sortMode",
                LauncherEntryVisibility.NormalizeSortModeId),
            EntryViewMode = NormalizeRequiredStoreMode(settings.EntryViewMode, "entryViewMode",
                LauncherEntryViewModes.NormalizeId),
            CommandTemplates = NormalizeStoreCommandTemplates(settings.CommandTemplates),
            CustomEntryOrder = RequireStoreCustomEntryOrder(settings.CustomEntryOrder),
            SearchHistory = RequireStoreSearchHistory(settings.SearchHistory),
            Window = NormalizeStoreWindow(RequireStoreWindow(settings.Window)),
        };
    }

    private static string NormalizeStoreFilterText(string? value)
    {
        if (value is null)
            throw new InvalidDataException("Launcher store settings lastFilterText is null.");

        var normalized = EntryName.Normalize(value);
        if (normalized.Length > 500)
            throw new InvalidDataException(
                "Launcher store settings lastFilterText exceeds 500 character limit.");

        return normalized;
    }

    private static string NormalizeRequiredStoreMode(
        string? value, string fieldName, Func<string?, string> normalize)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher store settings {fieldName} is null.");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"Launcher store settings {fieldName} is empty.");

        return normalize(value);
    }

    private static ImmutableList<string> RequireStoreCustomEntryOrder(IReadOnlyList<string>? customEntryOrder)
    {
        if (customEntryOrder is null)
            throw new InvalidDataException("Launcher store settings customEntryOrder is null.");

        if (customEntryOrder.Count > 10000)
            throw new InvalidDataException(
                "Launcher store settings customEntryOrder exceeds 10000 entry limit.");

        List<string> normalized = [];
        var seen = new HashSet<string>(EntryId.Comparer);
        for (var i = 0; i < customEntryOrder.Count; i++)
        {
            var entryId = EntryId.Normalize(customEntryOrder[i]);
            if (entryId.Length == 0)
                throw new InvalidDataException(
                    $"Launcher store settings customEntryOrder[{i}] is empty.");

            if (!seen.Add(entryId))
                throw new InvalidDataException(
                    $"Launcher store settings customEntryOrder[{i}] is duplicate: {entryId}.");

            normalized.Add(entryId);
        }

        return normalized.ToImmutableList();
    }

    private static ImmutableList<string> RequireStoreSearchHistory(ImmutableList<string>? searchHistory)
    {
        if (searchHistory is null)
            throw new InvalidDataException("Launcher store settings searchHistory is null.");

        List<string> normalized = [];
        foreach (var term in searchHistory)
        {
            var trimmed = EntryName.Normalize(term);
            if (trimmed.Length == 0 || trimmed.Length > 200)
                continue;

            if (!normalized.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                normalized.Add(trimmed);
        }

        const int maxTerms = 20;
        if (normalized.Count > maxTerms)
            normalized.RemoveRange(maxTerms, normalized.Count - maxTerms);

        return normalized.ToImmutableList();
    }

    private static ImmutableDictionary<string, string> NormalizeStoreCommandTemplates(
        ImmutableDictionary<string, string>? templates)
    {
        if (templates is null)
            return ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

        if (templates.Count > 1000)
            throw new InvalidDataException("Launcher store settings commandTemplates exceeds 1000 template limit.");

        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in templates)
        {
            var trimmedKey = EntryName.Normalize(key);
            if (trimmedKey.Length == 0)
                throw new InvalidDataException(
                    "Launcher store settings commandTemplates has an empty key.");
            if (trimmedKey.Length > 256)
                throw new InvalidDataException(
                    $"Launcher store settings commandTemplates key exceeds 256 character limit: '{trimmedKey[..Math.Min(trimmedKey.Length, 50)]}...'.");

            var trimmedValue = EntryName.Normalize(value);
            if (trimmedValue.Length == 0)
                throw new InvalidDataException(
                    $"Launcher store settings commandTemplates key '{trimmedKey}' has an empty value.");
            if (trimmedValue.Length > 8192)
                throw new InvalidDataException(
                    $"Launcher store settings commandTemplates value for key '{trimmedKey}' exceeds 8192 character limit.");

            if (!builder.TryAdd(trimmedKey, trimmedValue))
                throw new InvalidDataException(
                    $"Launcher store settings commandTemplates has duplicate key: {trimmedKey}.");
        }

        return builder.ToImmutable();
    }

    private static LauncherWindowSettings RequireStoreWindow(LauncherWindowSettings? window) =>
        window ?? throw new InvalidDataException("Launcher store settings window is null.");

    private static LauncherWindowSettings NormalizeStoreWindow(LauncherWindowSettings window)
    {
        ValidateStoreWindowSize(window.Width, "width");
        ValidateStoreWindowSize(window.Height, "height");

        if (!double.IsFinite(window.X))
            throw new InvalidDataException("Launcher store settings window x must be finite.");
        if (!double.IsFinite(window.Y))
            throw new InvalidDataException("Launcher store settings window y must be finite.");
        if (window.WindowState is not "Normal" and not "Maximized" and not "Minimized")
            throw new InvalidDataException(
                $"Launcher store settings window state must be Normal, Maximized, or Minimized. Got: '{window.WindowState}'.");

        return window;
    }

    private static void ValidateStoreWindowSize(double value, string fieldName)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new InvalidDataException($"Launcher store settings window {fieldName} must be positive.");
    }

    // -- Preset normalization (ported from LauncherPresetFiles) --

    private static ImmutableList<LauncherPreset> NormalizePresets(ImmutableList<LauncherPreset>? presets)
    {
        if (presets is null)
            throw new InvalidDataException("Launcher store preset list is null.");

        if (presets.Count > 1000)
            throw new InvalidDataException(
                $"Launcher store preset count {presets.Count} exceeds maximum of 1000.");

        List<LauncherPreset> normalizedPresets = [];
        var usedPresetIds = new HashSet<string>(PresetId.Comparer);
        var usedPresetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < presets.Count; i++)
        {
            var preset = presets[i] ?? throw new InvalidDataException($"Launcher store preset {i} is null.");

            var id = NormalizeStorePresetId(preset.Id, i);
            if (!usedPresetIds.Add(id))
                throw new InvalidDataException($"Launcher store preset {i} id is duplicate: {id}.");

            var name = NormalizeStorePresetName(preset.Name, i);
            if (!usedPresetNames.Add(name))
                throw new InvalidDataException($"Launcher store preset {i} name is duplicate: {name}.");

            var entryIds = NormalizeStorePresetEntryIds(preset.EntryIds, i);
            normalizedPresets.Add(preset with { Id = id, Name = name, EntryIds = entryIds.ToImmutableList() });
        }

        return normalizedPresets.ToImmutableList();
    }

    private static string NormalizeStorePresetId(string? id, int index)
    {
        var normalized = PresetId.NormalizeReference(id);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher store preset {index} id is empty.");

        return normalized;
    }

    private static string NormalizeStorePresetName(string? name, int index)
    {
        var normalized = EntryName.Normalize(name);
        if (normalized.Length > 256)
            throw new InvalidDataException(
                $"Launcher store preset {index} name exceeds 256 character limit.");
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher store preset {index} name is empty.");

        return normalized;
    }

    private static List<string> NormalizeStorePresetEntryIds(IReadOnlyList<string>? entryIds, int index)
    {
        if (entryIds is null)
            throw new InvalidDataException($"Launcher store preset {index} entryIds is null.");

        if (entryIds.Count > 10000)
            throw new InvalidDataException(
                $"Launcher store preset {index} entryIds count {entryIds.Count} exceeds maximum of 10000.");

        List<string> normalized = [];
        var seen = new HashSet<string>(EntryId.Comparer);
        for (var i = 0; i < entryIds.Count; i++)
        {
            var entryId = EntryId.Normalize(entryIds[i]);
            if (entryId.Length == 0)
                throw new InvalidDataException($"Launcher store preset {index} entryIds[{i}] is empty.");

            if (!seen.Add(entryId))
                throw new InvalidDataException(
                    $"Launcher store preset {index} entryIds[{i}] is duplicate: {entryId}.");
            if (EntryId.WindowsReservedNames.Contains(entryId))
                throw new InvalidDataException(
                    $"Launcher store preset {index} entryIds[{i}] is a Windows reserved name: {entryId}.");
            if (entryId.Contains('\\') || entryId.Contains('/') || entryId.Contains(".."))
                throw new InvalidDataException(
                    $"Launcher store preset {index} entryIds[{i}] contains path separators or '..': {entryId}.");

            normalized.Add(entryId);
        }

        return normalized;
    }

    private static string NormalizeDefaultPresetId(string? defaultPresetId,
        ImmutableList<LauncherPreset> presets)
    {
        var normalized = PresetId.NormalizeReference(defaultPresetId);
        if (presets.Count == 0)
        {
            if (normalized.Length != 0)
                throw new InvalidDataException(
                    $"Launcher store default preset id is invalid without presets: {normalized}.");

            return string.Empty;
        }

        if (normalized.Length == 0)
            throw new InvalidDataException(
                "Launcher store default preset id is required when presets exist.");

        if (!presets.Any(preset => PresetId.Same(preset.Id, normalized)))
            throw new InvalidDataException(
                $"Launcher store default preset id does not match any preset: {normalized}.");

        return normalized;
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
        var storePath = Path.Combine(configDirectory, "store.json");

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
