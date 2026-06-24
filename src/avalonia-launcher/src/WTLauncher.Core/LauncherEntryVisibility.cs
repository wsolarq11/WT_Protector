namespace WTLauncher.Core;

public enum LauncherSortMode { Recent, Updated, Alphabetical, Custom }

public static class LauncherEntryVisibility
{
    public const string CustomSortModeId = "custom";
    public const string RecentSortModeId = "recent";
    public const string UpdatedSortModeId = "updated";
    public const string AlphabeticalSortModeId = "alphabetical";

    public static string NormalizeSortModeId(string? sortModeId)
    {
        var normalized = (sortModeId ?? string.Empty).Trim();
        return normalized switch
        {
            CustomSortModeId => CustomSortModeId,
            RecentSortModeId => RecentSortModeId,
            UpdatedSortModeId => UpdatedSortModeId,
            AlphabeticalSortModeId => AlphabeticalSortModeId,
            _ => throw new InvalidDataException($"Unknown launcher sort mode: {normalized}."),
        };
    }

    public static IReadOnlyList<TEntry> FilterAndSort<TEntry>(
        IEnumerable<TEntry> entries,
        string filterText,
        string? sortModeId,
        IEnumerable<string> customEntryOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(filterText);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(selectEntry);

        var source = entries.ToList();
        var normalizedFilter = filterText.Trim();
        List<TEntry> filtered;
        if (normalizedFilter.Length == 0)
        {
            filtered = source;
        }
        else
        {
            filtered = new List<TEntry>(source.Count);
            foreach (var entry in source)
            {
                if (MatchesFilter(selectEntry(entry), normalizedFilter))
                    filtered.Add(entry);
            }
        }

        var sortMode = NormalizeSortModeId(sortModeId);
        IReadOnlyDictionary<string, int>? customOrder = null;
        Comparison<TEntry> comparison;

        switch (sortMode)
        {
            case RecentSortModeId:
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Recent);
                break;
            case UpdatedSortModeId:
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Updated);
                break;
            case AlphabeticalSortModeId:
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Alphabetical);
                break;
            default:
                customOrder = LauncherEntryOrder.BuildIndex(customEntryOrder);
                ValidateCustomOrder(source, customOrder, selectEntry);
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Custom, customOrder);
                break;
        }

        ThrowIfInvalidProjection(filtered, sortMode, selectEntry);
        filtered.Sort(comparison);
        return filtered.AsReadOnly();
    }

    private static void ThrowIfInvalidProjection<TEntry>(
        IEnumerable<TEntry> entries,
        string sortMode,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        foreach (var item in entries)
        {
            var entry = selectEntry(item);
            RequireEntryId(entry);
            RequireText(entry.Name, "name");

            if (sortMode == RecentSortModeId)
                ParseLaunchedAt(entry.LaunchedAtUtc);

            if (sortMode == UpdatedSortModeId)
                ParseUpdatedAt(entry.UpdatedAtUtc);
        }
    }

    private static int CompareEntries<TEntry>(
        TEntry a, TEntry b,
        Func<TEntry, LaunchEntry> selectEntry,
        LauncherSortMode sortKind,
        IReadOnlyDictionary<string, int>? customOrder = null)
    {
        var entryA = selectEntry(a);
        var entryB = selectEntry(b);

        int cmp = sortKind switch
        {
            LauncherSortMode.Recent => ParseLaunchedAt(entryB.LaunchedAtUtc).CompareTo(ParseLaunchedAt(entryA.LaunchedAtUtc)),
            LauncherSortMode.Updated => ParseUpdatedAt(entryB.UpdatedAtUtc).CompareTo(ParseUpdatedAt(entryA.UpdatedAtUtc)),
            LauncherSortMode.Alphabetical => StringComparer.OrdinalIgnoreCase.Compare(
                RequireText(entryA.Name, "name"), RequireText(entryB.Name, "name")),
            LauncherSortMode.Custom => customOrder![RequireEntryId(entryA)].CompareTo(customOrder[RequireEntryId(entryB)]),
            _ => throw new ArgumentOutOfRangeException(nameof(sortKind), sortKind, null),
        };

        if (cmp != 0) return cmp;

        cmp = StringComparer.OrdinalIgnoreCase.Compare(
            RequireText(entryA.Name, "name"), RequireText(entryB.Name, "name"));
        if (cmp != 0) return cmp;

        return LauncherEntryIds.Comparer.Compare(RequireEntryId(entryA), RequireEntryId(entryB));
    }

    private static bool MatchesFilter(LaunchEntry entry, string filterText)
    {
        var name = RequireText(entry.Name, "name");
        var directory = RequireText(entry.Directory, "directory");
        var command = RequireText(entry.Command, "command");

        var tokens = filterText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length <= 1)
        {
            return name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || directory.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || command.Contains(filterText, StringComparison.OrdinalIgnoreCase);
        }

        return tokens.All(token =>
            name.Contains(token, StringComparison.OrdinalIgnoreCase)
            || directory.Contains(token, StringComparison.OrdinalIgnoreCase)
            || command.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateCustomOrder<TEntry>(
        IReadOnlyList<TEntry> entries,
        IReadOnlyDictionary<string, int> customOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        var entryIds = entries
            .Select(entry => RequireEntryId(selectEntry(entry)))
            .ToHashSet(LauncherEntryIds.Comparer);

        foreach (var entryId in entryIds)
        {
            if (!customOrder.ContainsKey(entryId))
                throw new InvalidDataException(
                    $"Launcher custom order is missing entry id for projection: {entryId}.");
        }

        foreach (var entryId in customOrder.Keys)
        {
            if (!entryIds.Contains(entryId))
                throw new InvalidDataException(
                    $"Launcher custom order references missing entry id for projection: {entryId}.");
        }
    }

    private static string RequireEntryId(LaunchEntry entry) =>
        LauncherEntryIds.RequireEntryId(entry.Id, nameof(LaunchEntry.Id));

    private static string RequireText(string? value, string fieldName)
    {
        var normalized = LauncherEntryText.Normalize(value);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher entry {fieldName} is required for projection.");

        return normalized;
    }

    private static DateTimeOffset ParseLaunchedAt(string? value) =>
        ParseTimestamp(value, "launchedAtUtc");

    private static DateTimeOffset ParseUpdatedAt(string? value) =>
        ParseTimestamp(value, "updatedAtUtc");

    private static DateTimeOffset ParseTimestamp(string? value, string fieldName)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher entry {fieldName} is required for projection.");

        var normalized = LauncherEntryText.Normalize(value);
        if (normalized.Length == 0)
            return DateTimeOffset.MinValue;

        if (DateTimeOffset.TryParse(normalized, out var parsed))
            return parsed;

        throw new InvalidDataException($"Invalid launcher entry {fieldName}: {normalized}.");
    }
}
