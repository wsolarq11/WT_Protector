namespace WTLauncher.Core;

public static class LauncherEntryReferences
{
    public static void RenameEntryId(
        LauncherPresetFile presetFile,
        List<string> customEntryOrder,
        string oldEntryId,
        string newEntryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedOldEntryId = RequireEntryId(oldEntryId, nameof(oldEntryId));
        var normalizedNewEntryId = RequireEntryId(newEntryId, nameof(newEntryId));
        var updates = EntryReferenceLists(presetFile, customEntryOrder)
            .Select(entryIds => (Target: entryIds, EntryIds: RenameReferences(entryIds, normalizedOldEntryId, normalizedNewEntryId)))
            .ToList();

        ApplyUpdates(updates);
    }

    public static void RemoveEntryId(
        LauncherPresetFile presetFile,
        List<string> customEntryOrder,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedEntryId = RequireEntryId(entryId, nameof(entryId));
        var updates = EntryReferenceLists(presetFile, customEntryOrder)
            .Select(entryIds => (Target: entryIds, EntryIds: RemoveReferences(entryIds, normalizedEntryId)))
            .ToList();

        ApplyUpdates(updates);
    }

    private static List<string> RenameReferences(
        IEnumerable<string> entryIds,
        string oldEntryId,
        string newEntryId)
    {
        var normalized = LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds));
        for (var i = 0; i < normalized.Count; i++)
        {
            if (LauncherEntryIds.Same(normalized[i], oldEntryId))
                normalized[i] = newEntryId;
        }

        return LauncherEntryIds.ToRequiredUniqueList(normalized, nameof(entryIds));
    }

    private static List<string> RemoveReferences(IEnumerable<string> entryIds, string entryId)
    {
        return LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds))
            .Where(id => !LauncherEntryIds.Same(id, entryId))
            .ToList();
    }

    private static IEnumerable<List<string>> EntryReferenceLists(
        LauncherPresetFile presetFile,
        List<string> customEntryOrder)
    {
        foreach (var preset in presetFile.Presets)
        {
            yield return preset.EntryIds;
        }

        yield return customEntryOrder;
    }

    private static void ApplyUpdates(IEnumerable<(List<string> Target, List<string> EntryIds)> updates)
    {
        foreach (var update in updates)
        {
            update.Target.Clear();
            update.Target.AddRange(update.EntryIds);
        }
    }

    private static string RequireEntryId(string? entryId, string paramName) =>
        LauncherEntryIds.RequireEntryId(entryId, paramName);
}
