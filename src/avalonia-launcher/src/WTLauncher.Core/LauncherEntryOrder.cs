namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static void SyncWithEntries(List<string> customOrder, IEnumerable<string> entryIds)
    {
        var knownIds = new List<string>();
        var knownSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in entryIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (knownSet.Add(id))
                knownIds.Add(id);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        customOrder.RemoveAll(id =>
            string.IsNullOrWhiteSpace(id)
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
            if (string.IsNullOrWhiteSpace(id))
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
        if (string.Equals(draggedEntryId, targetEntryId, StringComparison.Ordinal))
            return;

        customOrder.RemoveAll(id => string.Equals(id, draggedEntryId, StringComparison.Ordinal));
        var targetIndex = customOrder.FindIndex(id => string.Equals(id, targetEntryId, StringComparison.Ordinal));
        if (targetIndex < 0)
            targetIndex = customOrder.Count;
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, customOrder.Count);
        customOrder.Insert(targetIndex, draggedEntryId);
    }
}
