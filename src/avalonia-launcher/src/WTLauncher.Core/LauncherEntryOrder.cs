using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherEntryOrder
{
    public static ImmutableList<string> SyncWithEntries(ImmutableList<string> customOrder, IEnumerable<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(customOrder);

        var knownIds = EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds));
        var knownSet = knownIds.ToHashSet(EntryId.Comparer);
        var orderIds = NormalizeRequiredOrder(customOrder, nameof(customOrder));

        List<string> syncedOrder = [];
        var seen = new HashSet<string>(EntryId.Comparer);
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

        return syncedOrder.ToImmutableList();
    }

    public static IReadOnlyDictionary<string, int> BuildIndex(IEnumerable<string> customOrder)
    {
        var indexById = new Dictionary<string, int>(EntryId.Comparer);
        var ids = NormalizeRequiredOrder(customOrder, nameof(customOrder));
        for (var index = 0; index < ids.Count; index++)
        {
            indexById.Add(ids[index], index);
        }

        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(indexById);
    }

    public static (bool Changed, ImmutableList<string> Result) MoveRelative(
        ImmutableList<string> customOrder,
        string draggedEntryId,
        string targetEntryId,
        bool insertAfter)
    {
        var draggedId = EntryId.RequireEntryId(draggedEntryId, nameof(draggedEntryId));
        var targetId = EntryId.RequireEntryId(targetEntryId, nameof(targetEntryId));
        if (EntryId.Same(draggedId, targetId))
            return (false, customOrder);

        var normalizedOrder = NormalizeRequiredOrder(customOrder, nameof(customOrder));
        var draggedIndex = normalizedOrder.FindIndex(id => EntryId.Same(id, draggedId));
        if (draggedIndex < 0)
            throw new ArgumentException(
                $"Dragged entry id is not in custom order: {draggedId}.",
                nameof(draggedEntryId));

        var targetIndex = normalizedOrder.FindIndex(id => EntryId.Same(id, targetId));
        if (targetIndex < 0)
            throw new ArgumentException(
                $"Target entry id is not in custom order: {targetId}.",
                nameof(targetEntryId));

        normalizedOrder.RemoveAt(draggedIndex);
        targetIndex = normalizedOrder.FindIndex(id => EntryId.Same(id, targetId));
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, normalizedOrder.Count);
        normalizedOrder.Insert(targetIndex, draggedId);
        return (true, normalizedOrder.ToImmutableList());
    }

    private static List<string> NormalizeRequiredOrder(IEnumerable<string> customOrder, string paramName)
    {
        ArgumentNullException.ThrowIfNull(customOrder);

        List<string> normalized = [];
        var seen = new HashSet<string>(EntryId.Comparer);
        var index = 0;
        foreach (var rawEntryId in customOrder)
        {
            var entryId = EntryId.Normalize(rawEntryId);
            if (!EntryId.IsUsable(entryId))
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
