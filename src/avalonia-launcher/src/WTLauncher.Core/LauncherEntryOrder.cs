namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static void SyncWithEntries(List<string> customOrder, IEnumerable<string> entryIds)
    {
        var knownIds = new List<string>();
        var knownSet = new HashSet<string>(LauncherEntryIds.Comparer);
        foreach (var id in entryIds)
        {
            var normalized = LauncherEntryIds.Normalize(id);
            if (!LauncherEntryIds.IsUsable(normalized))
                continue;
            if (knownSet.Add(normalized))
                knownIds.Add(normalized);
        }

        var syncedOrder = new List<string>();
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        foreach (var id in customOrder)
        {
            var normalized = LauncherEntryIds.Normalize(id);
            if (!LauncherEntryIds.IsUsable(normalized)
                || !knownSet.Contains(normalized)
                || !seen.Add(normalized))
                continue;

            syncedOrder.Add(normalized);
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
        var index = 0;
        foreach (var id in customOrder)
        {
            var normalized = LauncherEntryIds.Normalize(id);
            if (!LauncherEntryIds.IsUsable(normalized))
                continue;

            indexById.TryAdd(normalized, index);
            index++;
        }

        return indexById;
    }

    public static void MoveRelative(
        List<string> customOrder,
        string draggedEntryId,
        string targetEntryId,
        bool insertAfter)
    {
        var draggedId = LauncherEntryIds.Normalize(draggedEntryId);
        var targetId = LauncherEntryIds.Normalize(targetEntryId);
        if (!LauncherEntryIds.IsUsable(draggedId))
            return;
        if (LauncherEntryIds.Same(draggedId, targetId))
            return;
        if (!customOrder.Any(id => SameNormalized(id, draggedId)))
            return;

        customOrder.RemoveAll(id => SameNormalized(id, draggedId));
        var targetIndex = customOrder.FindIndex(id => SameNormalized(id, targetId));
        if (targetIndex < 0)
            targetIndex = customOrder.Count;
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, customOrder.Count);
        customOrder.Insert(targetIndex, draggedId);
    }

    private static bool SameNormalized(string? left, string? right) =>
        LauncherEntryIds.Same(
            LauncherEntryIds.Normalize(left),
            LauncherEntryIds.Normalize(right));
}
