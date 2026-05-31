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
        var draggedId = LauncherEntryIds.Normalize(draggedEntryId);
        var targetId = LauncherEntryIds.Normalize(targetEntryId);
        if (!LauncherEntryIds.IsUsable(draggedId))
            return false;
        if (!LauncherEntryIds.IsUsable(targetId))
            return false;
        if (LauncherEntryIds.Same(draggedId, targetId))
            return false;
        var normalizedOrder = LauncherEntryIds.ToUniqueUsableList(customOrder);
        if (!normalizedOrder.Any(id => LauncherEntryIds.Same(id, draggedId)))
            return false;
        if (!normalizedOrder.Any(id => LauncherEntryIds.Same(id, targetId)))
            return false;

        normalizedOrder.RemoveAll(id => LauncherEntryIds.Same(id, draggedId));
        var targetIndex = normalizedOrder.FindIndex(id => LauncherEntryIds.Same(id, targetId));
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, normalizedOrder.Count);
        normalizedOrder.Insert(targetIndex, draggedId);
        customOrder.Clear();
        customOrder.AddRange(normalizedOrder);
        return true;
    }
}
