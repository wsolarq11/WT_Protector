namespace WTLauncher.Core;

public static class LauncherEntryVisibility
{
    public const string CustomSortModeId = "custom";
    public const string UpdatedSortModeId = "updated";
    public const string AlphabeticalSortModeId = "alphabetical";

    public static string NormalizeSortModeId(string? sortModeId)
    {
        var normalized = (sortModeId ?? string.Empty).Trim();
        return normalized switch
        {
            "" => CustomSortModeId,
            CustomSortModeId => CustomSortModeId,
            UpdatedSortModeId => UpdatedSortModeId,
            AlphabeticalSortModeId => AlphabeticalSortModeId,
            _ => throw new InvalidDataException($"Unknown launcher sort mode: {normalized}."),
        };
    }

    public static IReadOnlyList<TEntry> FilterAndSort<TEntry>(
        IEnumerable<TEntry> entries,
        string? filterText,
        string? sortModeId,
        IEnumerable<string> customEntryOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(customEntryOrder);
        ArgumentNullException.ThrowIfNull(selectEntry);

        var source = entries.ToList();
        var normalizedFilter = (filterText ?? string.Empty).Trim();
        var filtered = normalizedFilter.Length == 0
            ? source
            : source.Where(entry => MatchesFilter(selectEntry(entry), normalizedFilter));

        var sorted = NormalizeSortModeId(sortModeId) switch
        {
            UpdatedSortModeId => SortByUpdated(filtered, selectEntry),
            AlphabeticalSortModeId => SortAlphabetically(filtered, selectEntry),
            _ => SortByCustomOrder(filtered, customEntryOrder, selectEntry),
        };

        return sorted.ToList();
    }

    private static bool MatchesFilter(LaunchEntry entry, string filterText)
    {
        var name = RequireText(entry.Name, "name");
        var directory = RequireText(entry.Directory, "directory");
        var command = RequireText(entry.Command, "command");

        return name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
            || directory.Contains(filterText, StringComparison.OrdinalIgnoreCase)
            || command.Contains(filterText, StringComparison.OrdinalIgnoreCase);
    }

    private static IOrderedEnumerable<TEntry> SortByUpdated<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        ThenByNameThenId(
            entries.OrderByDescending(entry => ParseUpdatedAt(selectEntry(entry).UpdatedAtUtc)),
            selectEntry);

    private static IOrderedEnumerable<TEntry> SortAlphabetically<TEntry>(
        IEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        entries.OrderBy(entry => RequireText(selectEntry(entry).Name, "name"), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => RequireEntryId(selectEntry(entry)), LauncherEntryIds.Comparer);

    private static IOrderedEnumerable<TEntry> SortByCustomOrder<TEntry>(
        IEnumerable<TEntry> entries,
        IEnumerable<string> customEntryOrder,
        Func<TEntry, LaunchEntry> selectEntry)
    {
        var order = LauncherEntryOrder.BuildIndex(customEntryOrder);
        return ThenByNameThenId(
            entries.OrderBy(entry => order.TryGetValue(RequireEntryId(selectEntry(entry)), out var index) ? index : int.MaxValue),
            selectEntry);
    }

    private static IOrderedEnumerable<TEntry> ThenByNameThenId<TEntry>(
        IOrderedEnumerable<TEntry> entries,
        Func<TEntry, LaunchEntry> selectEntry) =>
        entries
            .ThenBy(entry => RequireText(selectEntry(entry).Name, "name"), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => RequireEntryId(selectEntry(entry)), LauncherEntryIds.Comparer);

    private static string RequireEntryId(LaunchEntry entry)
    {
        var normalized = LauncherEntryIds.Normalize(entry.Id);
        if (!LauncherEntryIds.IsUsable(normalized))
            throw new InvalidDataException("Launcher entry id is required for projection.");

        return normalized;
    }

    private static string RequireText(string? value, string fieldName)
    {
        var normalized = LauncherEntryText.Normalize(value);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher entry {fieldName} is required for projection.");

        return normalized;
    }

    private static DateTimeOffset ParseUpdatedAt(string? value)
    {
        if (value is null)
            throw new InvalidDataException("Launcher entry updatedAtUtc is required for projection.");

        var normalized = LauncherEntryText.Normalize(value);
        if (normalized.Length == 0)
            return DateTimeOffset.MinValue;

        if (DateTimeOffset.TryParse(normalized, out var parsed))
            return parsed;

        throw new InvalidDataException($"Invalid launcher entry updatedAtUtc: {normalized}.");
    }
}
