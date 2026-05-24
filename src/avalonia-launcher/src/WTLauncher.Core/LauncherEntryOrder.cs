namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static void SyncWithEntries(List<string> customOrder, IEnumerable<string> entryIds)
    {
        var knownIds = entryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var knownSet = knownIds.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        customOrder.RemoveAll(id =>
            string.IsNullOrWhiteSpace(id)
            || !knownSet.Contains(id)
            || !seen.Add(id));

        foreach (var id in knownIds)
        {
            if (!seen.Contains(id))
            {
                customOrder.Add(id);
                seen.Add(id);
            }
        }
    }

    public static IReadOnlyDictionary<string, int> BuildIndex(IEnumerable<string> customOrder)
    {
        return customOrder
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select((id, index) => new { id, index })
            .GroupBy(item => item.id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.Ordinal);
    }

    public static void MoveBefore(List<string> customOrder, string draggedEntryId, string targetEntryId)
    {
        MoveRelative(customOrder, draggedEntryId, targetEntryId, insertAfter: false);
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
