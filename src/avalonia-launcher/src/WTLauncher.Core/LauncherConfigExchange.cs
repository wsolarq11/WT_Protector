using System.Collections.Immutable;

namespace WTLauncher.Core;

public enum LauncherImportMode
{
    AddNew,
    Replace,
}

/// <summary>
/// User-decided import plan: add/replace mode, per-entry directory overrides,
/// and entry ids to skip. Core stays free of UI and file pickers.
/// </summary>
public sealed record LauncherImportPlan
{
    public LauncherImportMode Mode { get; init; } = LauncherImportMode.AddNew;

    public IReadOnlyDictionary<string, string> DirectoryOverrides { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> SkippedEntryIds { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record LauncherImportPreview
{
    public ImmutableList<LauncherImportRow> Rows { get; init; } = ImmutableList<LauncherImportRow>.Empty;

    public ImmutableList<string> InvalidPresetReferences { get; init; } = ImmutableList<string>.Empty;

    public ImmutableList<string> DuplicatePresetIds { get; init; } = ImmutableList<string>.Empty;

    public bool HasInvalidPresetReferences => InvalidPresetReferences.Count > 0;
}

public sealed record LauncherImportRow
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Directory { get; init; } = "";

    public bool IsDuplicate { get; init; }

    public bool IsMissing { get; init; }
}

public static class LauncherConfigExchange
{
    public static LauncherStoreFile BuildExportFile(
        LauncherStoreFile source,
        IReadOnlyCollection<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(entryIds);

        var wanted = entryIds.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = source.Entries
            .Where(entry => wanted.Contains(entry.Id.Value))
            .Select(entry => entry with { Selected = false })
            .ToImmutableList();
        var finalIds = entries.Select(entry => entry.Id.Value).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var presets = source.Presets
            .Where(preset => preset.EntryIds.All(finalIds.Contains))
            .ToImmutableList();
        var settings = source.Settings with
        {
            SearchHistory = ImmutableList<string>.Empty,
            CustomEntryOrder = ImmutableList<string>.Empty,
            Window = new LauncherWindowSettings(),
        };

        return new LauncherStoreFile
        {
            SchemaVersion = source.SchemaVersion,
            Entries = entries,
            Presets = presets,
            Settings = settings,
            DefaultPresetId = presets.Any(preset => string.Equals(preset.Id, source.DefaultPresetId, StringComparison.OrdinalIgnoreCase))
                ? source.DefaultPresetId
                : "",
        };
    }

    public static LauncherImportPreview AnalyzeImport(
        LauncherStoreFile current,
        LauncherStoreFile imported)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(imported);

        var currentEntryIds = current.Entries
            .Select(entry => entry.Id.Value)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var currentPresetIds = current.Presets
            .Select(preset => preset.Id)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var importedEntryIds = imported.Entries
            .Select(entry => entry.Id.Value)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = imported.Entries
            .Select(entry => new LauncherImportRow
            {
                Id = entry.Id.Value,
                Name = entry.Name.Value,
                Directory = entry.Directory.Value,
                IsDuplicate = currentEntryIds.Contains(entry.Id.Value),
                IsMissing = !Directory.Exists(entry.Directory.Value),
            })
            .ToImmutableList();

        var invalidPresetReferences = imported.Presets
            .SelectMany(preset => preset.EntryIds)
            .Where(id => !importedEntryIds.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();
        var duplicatePresetIds = imported.Presets
            .Select(preset => preset.Id)
            .Where(id => currentPresetIds.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();

        return new LauncherImportPreview
        {
            Rows = rows,
            InvalidPresetReferences = invalidPresetReferences,
            DuplicatePresetIds = duplicatePresetIds,
        };
    }

    public static LauncherStoreFile ApplyImport(
        LauncherStoreFile current,
        LauncherStoreFile imported,
        LauncherImportPlan plan)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentNullException.ThrowIfNull(plan);

        var skipped = plan.SkippedEntryIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var overrides = plan.DirectoryOverrides ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var entries = plan.Mode == LauncherImportMode.Replace
            ? imported.Entries
                .Where(entry => !skipped.Contains(entry.Id.Value))
                .Select(entry => MapEntry(entry, overrides))
                .ToImmutableList()
            : MergeAddEntries(current.Entries, imported.Entries, skipped, overrides);

        var finalEntryIds = entries
            .Select(entry => entry.Id.Value)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var currentPresetIds = current.Presets
            .Select(preset => preset.Id)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var presets = plan.Mode == LauncherImportMode.Replace
            ? imported.Presets
                .Where(preset => preset.EntryIds.All(finalEntryIds.Contains))
                .ToImmutableList()
            : current.Presets
                .Concat(imported.Presets.Where(preset =>
                    !currentPresetIds.Contains(preset.Id) && preset.EntryIds.All(finalEntryIds.Contains)))
                .ToImmutableList();

        var settings = plan.Mode == LauncherImportMode.Replace
            ? imported.Settings with
            {
                Window = current.Settings.Window,
                SearchHistory = current.Settings.SearchHistory,
                CustomEntryOrder = current.Settings.CustomEntryOrder
                    .Where(finalEntryIds.Contains)
                    .ToImmutableList(),
            }
            : current.Settings with
            {
                CommandTemplates = MergeTemplates(current.Settings.CommandTemplates, imported.Settings.CommandTemplates),
                QuickLaunchHotkey = imported.Settings.QuickLaunchHotkey,
                RunInTray = imported.Settings.RunInTray,
                CardsGroupKey = imported.Settings.CardsGroupKey,
                BackgroundMode = imported.Settings.BackgroundMode,
                CustomEntryOrder = current.Settings.CustomEntryOrder
                    .Where(finalEntryIds.Contains)
                    .ToImmutableList(),
            };

        var defaultPresetId = ResolveDefaultPresetId(
            current.DefaultPresetId,
            imported.DefaultPresetId,
            presets,
            plan.Mode);

        return new LauncherStoreFile
        {
            SchemaVersion = imported.SchemaVersion,
            Entries = entries,
            Presets = presets,
            Settings = settings,
            DefaultPresetId = defaultPresetId,
        };
    }

    private static ImmutableList<LaunchEntry> MergeAddEntries(
        ImmutableList<LaunchEntry> current,
        ImmutableList<LaunchEntry> imported,
        IReadOnlySet<string> skipped,
        IReadOnlyDictionary<string, string> overrides)
    {
        var result = current.ToList();
        var currentIds = current
            .Select(entry => entry.Id.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in imported)
        {
            if (skipped.Contains(entry.Id.Value) || currentIds.Contains(entry.Id.Value))
                continue;

            result.Add(MapEntry(entry, overrides));
            currentIds.Add(entry.Id.Value);
        }

        return result.ToImmutableList();
    }

    private static LaunchEntry MapEntry(
        LaunchEntry entry,
        IReadOnlyDictionary<string, string> overrides)
    {
        var directory = overrides.TryGetValue(entry.Id.Value, out var mapped)
            && !string.IsNullOrWhiteSpace(mapped)
            ? (DirectoryPath)mapped
            : entry.Directory;
        return entry with
        {
            Directory = directory,
            Selected = false,
        };
    }

    private static ImmutableDictionary<string, string> MergeTemplates(
        ImmutableDictionary<string, string> current,
        ImmutableDictionary<string, string> imported)
    {
        var builder = current.ToBuilder();
        foreach (var pair in imported)
        {
            if (!builder.ContainsKey(pair.Key))
                builder[pair.Key] = pair.Value;
        }

        return builder.ToImmutable();
    }

    private static string ResolveDefaultPresetId(
        string currentDefault,
        string importedDefault,
        ImmutableList<LauncherPreset> presets,
        LauncherImportMode mode)
    {
        var ids = presets.Select(preset => preset.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (mode == LauncherImportMode.Replace)
            return ids.Contains(importedDefault) ? importedDefault : "";
        if (ids.Contains(currentDefault))
            return currentDefault;
        return ids.Contains(importedDefault) ? importedDefault : "";
    }
}
