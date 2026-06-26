using System.Globalization;

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
        if (normalized.Length == 0)
            throw new InvalidDataException("Launcher sort mode is required.");
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
        where TEntry : notnull
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(filterText);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(selectEntry);

        var normalizedFilter = filterText.Trim();
        List<TEntry>? source = null;
        List<TEntry> filtered;
        if (normalizedFilter.Length == 0)
        {
            filtered = entries as List<TEntry> ?? entries.ToList();
        }
        else
        {
            source = entries.ToList();
            var tokens = normalizedFilter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filtered = new List<TEntry>(source.Count);
            foreach (var entry in source)
            {
                if (MatchesFilter(selectEntry(entry), tokens))
                    filtered.Add(entry);
            }
        }

        var sortMode = NormalizeSortModeId(sortModeId);
        IReadOnlyDictionary<string, int>? customOrder = null;
        Dictionary<TEntry, string>? nameCache = null;
        Comparison<TEntry> comparison;

        switch (sortMode)
        {
            case RecentSortModeId:
            {
                var cache = new Dictionary<TEntry, DateTimeOffset>(filtered.Count);
                foreach (var entry in filtered)
                {
                    var launchEntry = selectEntry(entry);
                    RequireEntryId(launchEntry);
                    RequireText(launchEntry.Name, "name");
                    cache[entry] = ParseLaunchedAt(launchEntry.LaunchedAtUtc);
                }
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Recent, timeCache: cache);
                break;
            }
            case UpdatedSortModeId:
            {
                var cache = new Dictionary<TEntry, DateTimeOffset>(filtered.Count);
                foreach (var entry in filtered)
                {
                    var launchEntry = selectEntry(entry);
                    RequireEntryId(launchEntry);
                    RequireText(launchEntry.Name, "name");
                    cache[entry] = ParseUpdatedAt(launchEntry.UpdatedAtUtc);
                }
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Updated, timeCache: cache);
                break;
            }
            case AlphabeticalSortModeId:
                nameCache = new Dictionary<TEntry, string>(filtered.Count);
                foreach (var entry in filtered)
                    nameCache[entry] = RequireText(selectEntry(entry).Name, "name");
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Alphabetical, nameCache: nameCache);
                break;
            default:
                customOrder = LauncherEntryOrder.BuildIndex(customEntryOrder);
                ValidateCustomOrder(source ?? filtered, customOrder, selectEntry);
                comparison = (a, b) => CompareEntries(a, b, selectEntry, LauncherSortMode.Custom, customOrder);
                break;
        }

        if (sortMode is not RecentSortModeId and not UpdatedSortModeId)
            ThrowIfInvalidProjection(filtered, selectEntry);
        filtered.Sort(comparison);
        return filtered;
    }

    /// <summary>
    /// Validates that every entry has non-empty Id and Name.
    /// Called only for sort modes that do not require timestamp parsing
    /// (Custom and Alphabetical) — timestamp validation is handled in
    /// <see cref="FilterAndSort"/> before calling this method.
    /// </summary>
    private static void ThrowIfInvalidProjection<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry)
        where TEntry : notnull
    {
        foreach (var item in entries)
        {
            var entry = selectEntry(item);
            RequireEntryId(entry);
            RequireText(entry.Name, "name");
        }
    }

    private static int CompareEntries<TEntry>(
        TEntry a, TEntry b,
        Func<TEntry, LaunchEntry> selectEntry,
        LauncherSortMode sortKind,
        IReadOnlyDictionary<string, int>? customOrder = null,
        IReadOnlyDictionary<TEntry, string>? nameCache = null,
        IReadOnlyDictionary<TEntry, DateTimeOffset>? timeCache = null)
        where TEntry : notnull
    {
        var entryA = selectEntry(a);
        var entryB = selectEntry(b);

        int cmp = sortKind switch
        {
            LauncherSortMode.Recent => timeCache![b].CompareTo(timeCache![a]),
            LauncherSortMode.Updated => timeCache![b].CompareTo(timeCache![a]),
            LauncherSortMode.Alphabetical => StringComparer.OrdinalIgnoreCase.Compare(
                ResolveName(entryA, a, nameCache), ResolveName(entryB, b, nameCache)),
            LauncherSortMode.Custom => customOrder![RequireEntryId(entryA)].CompareTo(customOrder[RequireEntryId(entryB)]),
            _ => throw new ArgumentOutOfRangeException(nameof(sortKind), sortKind, null),
        };

        if (cmp != 0) return cmp;

        cmp = StringComparer.OrdinalIgnoreCase.Compare(
            ResolveName(entryA, a, nameCache), ResolveName(entryB, b, nameCache));
        if (cmp != 0) return cmp;

        return LauncherEntryIds.Comparer.Compare(RequireEntryId(entryA), RequireEntryId(entryB));
    }

    private static string ResolveName<TEntry>(LaunchEntry entry, TEntry key, IReadOnlyDictionary<TEntry, string>? nameCache)
        where TEntry : notnull
    {
        if (nameCache is not null && nameCache.TryGetValue(key, out var cached))
            return cached;

        return RequireText(entry.Name, "name");
    }

    private static bool MatchesFilter(LaunchEntry entry, string[] tokens)
    {
        if (tokens.Length == 0)
            return true;

        var name = RequireText(entry.Name, "name");
        var directory = RequireText(entry.Directory, "directory");
        var command = RequireText(entry.Command, "command");

        if (tokens.Length == 1)
        {
            var token = tokens[0];
            return name.Contains(token, StringComparison.OrdinalIgnoreCase)
                || directory.Contains(token, StringComparison.OrdinalIgnoreCase)
                || command.Contains(token, StringComparison.OrdinalIgnoreCase);
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
        where TEntry : notnull
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

        // ISO 8601 round-trip "O" format with fractional seconds and offset: yyyy-MM-ddTHH:mm:ss.fffffffzzz
        if (DateTimeOffset.TryParseExact(normalized, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;
        // Without fractional seconds, with timezone (e.g., 2026-05-28T12:00:00Z).
        if (DateTimeOffset.TryParseExact(normalized, "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            return parsed;
        // Without timezone offset (legacy entries written before R4.1).
        if (DateTimeOffset.TryParseExact(normalized, "yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            return parsed;
        // Without fractional seconds and without timezone (legacy).
        if (DateTimeOffset.TryParseExact(normalized, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            return parsed;

        throw new InvalidDataException($"Invalid launcher entry {fieldName}: {normalized}.");
    }
}
