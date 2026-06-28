using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherStore
{
    public const string ConfigDirectoryName = "config";
    public const string StoreFileName = "store.json";
    public const string RepositoryMarkerFileName = "README.md";
    public const int ExpectedStoreSchemaVersion = 2;

    // ---- Load / Save ----

    public static LauncherStoreFile Load(string storePath)
    {
        ThrowIfStorePathMissing(storePath);

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
            new LauncherStoreFile());
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
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Store file not found: {fullPath}", fullPath);

            var fileName = Path.GetFileName(fullPath);
            var dirName = Path.GetFileName(Path.GetDirectoryName(fullPath));
            if (!string.Equals(fileName, StoreFileName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(dirName, ConfigDirectoryName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Explicit store path must end with {ConfigDirectoryName}/{StoreFileName}, got: {fullPath}",
                    nameof(explicitPath));

            return fullPath;
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

    private static LauncherStoreFile Normalize(LauncherStoreFile store)
    {
        return store with
        {
            SchemaVersion = ExpectedStoreSchemaVersion,
            Settings = NormalizeSettings(store.Settings),
            Entries = NormalizeEntries(store.Entries),
            Presets = NormalizePresets(store.Presets),
            DefaultPresetId = NormalizeDefaultPresetId(store.DefaultPresetId, store.Presets),
        };
    }

    // -- Entry normalization (ported from LauncherConfigStore) --

    private static ImmutableList<LaunchEntry> NormalizeEntries(ImmutableList<LaunchEntry>? entries)
    {
        if (entries is null)
            throw new InvalidDataException("Launcher store entries list is null.");

        if (entries.Count > 10000)
            throw new InvalidDataException("Launcher store exceeds 10000 entry limit.");

        List<LaunchEntry> normalized = [];
        var seenIds = new HashSet<string>(LauncherEntryIds.Comparer);
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
        var normalized = LauncherEntryIds.RequireEntryId(id, nameof(LaunchEntry.Id));

        if (!seenIds.Add(normalized))
            throw new InvalidDataException($"Launcher store entry {index} id is duplicate: {normalized}.");

        return normalized;
    }

    private static string RequireStoreEntryTextField(string? value, int index, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher store entry {index} {fieldName} is null.");

        var normalized = LauncherEntryText.Normalize(value);
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

        return LauncherEntryText.Normalize(value);
    }

    // -- Settings normalization (ported from LauncherSettingsStore) --

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

        var normalized = LauncherEntryText.Normalize(value);
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
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        for (var i = 0; i < customEntryOrder.Count; i++)
        {
            var entryId = LauncherEntryIds.Normalize(customEntryOrder[i]);
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
            var trimmed = LauncherEntryText.Normalize(term);
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
            var trimmedKey = LauncherEntryText.Normalize(key);
            if (trimmedKey.Length == 0)
                throw new InvalidDataException(
                    "Launcher store settings commandTemplates has an empty key.");
            if (trimmedKey.Length > 256)
                throw new InvalidDataException(
                    $"Launcher store settings commandTemplates key exceeds 256 character limit: '{trimmedKey[..Math.Min(trimmedKey.Length, 50)]}...'.");

            var trimmedValue = LauncherEntryText.Normalize(value);
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
        var usedPresetIds = new HashSet<string>(LauncherPresetIds.Comparer);
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
        var normalized = LauncherPresetIds.NormalizeReference(id);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher store preset {index} id is empty.");

        return normalized;
    }

    private static string NormalizeStorePresetName(string? name, int index)
    {
        var normalized = LauncherEntryText.Normalize(name);
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
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        for (var i = 0; i < entryIds.Count; i++)
        {
            var entryId = LauncherEntryIds.Normalize(entryIds[i]);
            if (entryId.Length == 0)
                throw new InvalidDataException($"Launcher store preset {index} entryIds[{i}] is empty.");

            if (!seen.Add(entryId))
                throw new InvalidDataException(
                    $"Launcher store preset {index} entryIds[{i}] is duplicate: {entryId}.");

            normalized.Add(entryId);
        }

        return normalized;
    }

    private static string NormalizeDefaultPresetId(string? defaultPresetId,
        ImmutableList<LauncherPreset> presets)
    {
        var normalized = LauncherPresetIds.NormalizeReference(defaultPresetId);
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

        if (!presets.Any(preset => LauncherPresetIds.Same(preset.Id, normalized)))
            throw new InvalidDataException(
                $"Launcher store default preset id does not match any preset: {normalized}.");

        return normalized;
    }

    // ---- Path resolution helpers (adapted from LauncherConfigStore) ----

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
