namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static void SyncWithEntries(List<string> customOrder, IEnumerable<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(customOrder);

        var knownIds = LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds));
        var knownSet = knownIds.ToHashSet(LauncherEntryIds.Comparer);
        var orderIds = NormalizeRequiredOrder(customOrder, nameof(customOrder));

        var syncedOrder = new List<string>();
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        foreach (var id in orderIds)
        {
            if (!knownSet.Contains(id))
                continue;

            syncedOrder.Add(id);
            seen.Add(id);
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
        var ids = NormalizeRequiredOrder(customOrder, nameof(customOrder));
        for (var index = 0; index < ids.Count; index++)
        {
            indexById.Add(ids[index], index);
        }

        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(indexById);
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

        var normalizedOrder = NormalizeRequiredOrder(customOrder, nameof(customOrder));
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

    private static string RequireEntryId(string entryId, string paramName) =>
        LauncherEntryIds.RequireEntryId(entryId, paramName);

    private static List<string> NormalizeRequiredOrder(IEnumerable<string> customOrder, string paramName)
    {
        ArgumentNullException.ThrowIfNull(customOrder);

        var normalized = new List<string>();
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        var index = 0;
        foreach (var rawEntryId in customOrder)
        {
            var entryId = LauncherEntryIds.Normalize(rawEntryId);
            if (!LauncherEntryIds.IsUsable(entryId))
                throw new ArgumentException($"Custom entry order {index} is empty.", paramName);

            if (!seen.Add(entryId))
                throw new ArgumentException(
                    $"Custom entry order {index} is duplicate: {entryId}.",
                    paramName);

            normalized.Add(entryId);
            index++;
        }

        return normalized;
    }
}
