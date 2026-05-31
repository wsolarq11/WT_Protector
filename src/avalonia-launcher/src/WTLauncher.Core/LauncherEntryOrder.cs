namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static void SyncWithEntries(List<string> customOrder, IEnumerable<string> entryIds)
    {
        var knownIds = LauncherEntryIds.ToUniqueUsableList(entryIds);
        var knownSet = knownIds.ToHashSet(LauncherEntryIds.Comparer);

        var syncedOrder = new List<string>();
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        foreach (var id in LauncherEntryIds.ToUniqueUsableList(customOrder))
        {
            if (!knownSet.Contains(id) || !seen.Add(id))
                continue;

            syncedOrder.Add(id);
        }

        foreach (var id in knownIds)
        {
            if (seen.Add(id))
            {
                syncedOrder.Add(id);
            }
        }

        customOrder.Clear();
        customOrder.AddRange(syncedOrder);
    }

    public static IReadOnlyDictionary<string, int> BuildIndex(IEnumerable<string> customOrder)
    {
        var indexById = new Dictionary<string, int>(LauncherEntryIds.Comparer);
        var ids = LauncherEntryIds.ToUniqueUsableList(customOrder);
        for (var index = 0; index < ids.Count; index++)
        {
            indexById.Add(ids[index], index);
        }

        return indexById;
    }

    public static bool MoveRelative(
        List<string> customOrder,
        string draggedEntryId,
        string targetEntryId,
        bool insertAfter)
    {
        var draggedId = RequireEntryId(draggedEntryId, nameof(draggedEntryId));
        var targetId = RequireEntryId(targetEntryId, nameof(targetEntryId));
        if (LauncherEntryIds.Same(draggedId, targetId))
            return false;

        var normalizedOrder = NormalizeRequiredOrder(customOrder);
        var draggedIndex = normalizedOrder.FindIndex(id => LauncherEntryIds.Same(id, draggedId));
        if (draggedIndex < 0)
            throw new ArgumentException(
                $"Dragged entry id is not in custom order: {draggedId}.",
                nameof(draggedEntryId));

        var targetIndex = normalizedOrder.FindIndex(id => LauncherEntryIds.Same(id, targetId));
        if (targetIndex < 0)
            throw new ArgumentException(
                $"Target entry id is not in custom order: {targetId}.",
                nameof(targetEntryId));

        normalizedOrder.RemoveAt(draggedIndex);
        targetIndex = normalizedOrder.FindIndex(id => LauncherEntryIds.Same(id, targetId));
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, normalizedOrder.Count);
        normalizedOrder.Insert(targetIndex, draggedId);
        customOrder.Clear();
        customOrder.AddRange(normalizedOrder);
        return true;
    }

    private static string RequireEntryId(string entryId, string paramName)
    {
        var normalized = LauncherEntryIds.Normalize(entryId);
        if (!LauncherEntryIds.IsUsable(normalized))
            throw new ArgumentException("Entry id is required.", paramName);

        return normalized;
    }

    private static List<string> NormalizeRequiredOrder(List<string> customOrder)
    {
        ArgumentNullException.ThrowIfNull(customOrder);

        var normalized = new List<string>(customOrder.Count);
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        for (var i = 0; i < customOrder.Count; i++)
        {
            var entryId = LauncherEntryIds.Normalize(customOrder[i]);
            if (!LauncherEntryIds.IsUsable(entryId))
                throw new ArgumentException($"Custom entry order {i} is empty.", nameof(customOrder));

            if (!seen.Add(entryId))
                throw new ArgumentException(
                    $"Custom entry order {i} is duplicate: {entryId}.",
                    nameof(customOrder));

            normalized.Add(entryId);
        }

        return normalized;
    }
}
