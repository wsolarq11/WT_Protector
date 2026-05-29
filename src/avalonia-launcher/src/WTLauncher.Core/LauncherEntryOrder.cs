namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static void SyncWithEntries(List<string> customOrder, IEnumerable<string> entryIds)
    {
        var knownIds = new List<string>();
        var knownSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in entryIds)
        {
            if (!IsUsableId(id))
                continue;
            if (knownSet.Add(id))
                knownIds.Add(id);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        customOrder.RemoveAll(id =>
            !IsUsableId(id)
            || !knownSet.Contains(id)
            || !seen.Add(id));

        foreach (var id in knownIds)
        {
            if (seen.Add(id))
            {
                customOrder.Add(id);
            }
        }
    }

    public static IReadOnlyDictionary<string, int> BuildIndex(IEnumerable<string> customOrder)
    {
        var indexById = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var id in customOrder)
        {
            if (!IsUsableId(id))
                continue;

            indexById.TryAdd(id, index);
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
        if (SameId(draggedEntryId, targetEntryId))
            return;

        customOrder.RemoveAll(id => SameId(id, draggedEntryId));
        var targetIndex = customOrder.FindIndex(id => SameId(id, targetEntryId));
        if (targetIndex < 0)
            targetIndex = customOrder.Count;
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, customOrder.Count);
        customOrder.Insert(targetIndex, draggedEntryId);
    }

    private static bool IsUsableId(string? id) => !string.IsNullOrWhiteSpace(id);

    private static bool SameId(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
