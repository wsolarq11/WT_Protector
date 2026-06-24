using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherEntryReferences
{
    public static (LauncherPresetFile PresetFile, ImmutableList<string> CustomEntryOrder) RenameEntryId(
        LauncherPresetFile presetFile,
        ImmutableList<string> customEntryOrder,
        string oldEntryId,
        string newEntryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedOldEntryId = RequireEntryId(oldEntryId, nameof(oldEntryId));
        var normalizedNewEntryId = RequireEntryId(newEntryId, nameof(newEntryId));

        // H4: Pre-check all reference lists for target ID conflict before modifying anything.
        // Using mutable List copies for the rename operation; reconstruct preset file at the end.
        var mutableOrder = customEntryOrder.ToList();
        var mutablePresetLists = new List<List<string>>(presetFile.Presets.Count);
        foreach (var p in presetFile.Presets)
            mutablePresetLists.Add([.. p.EntryIds]);

        foreach (var list in mutablePresetLists)
        {
            if (list.Any(id => LauncherEntryIds.Same(id, normalizedNewEntryId) && !LauncherEntryIds.Same(id, normalizedOldEntryId)))
                throw new ArgumentException(
                    $"Cannot rename to '{normalizedNewEntryId}' because that ID is already present.",
                    nameof(newEntryId));
        }
        if (mutableOrder.Any(id => LauncherEntryIds.Same(id, normalizedNewEntryId) && !LauncherEntryIds.Same(id, normalizedOldEntryId)))
            throw new ArgumentException(
                $"Cannot rename to '{normalizedNewEntryId}' because that ID is already present.",
                nameof(newEntryId));

        // Rename in custom order
        var updatedOrder = RenameReferences(mutableOrder, normalizedOldEntryId, normalizedNewEntryId);

        // Rename in each preset's entry list
        for (var i = 0; i < mutablePresetLists.Count; i++)
            mutablePresetLists[i] = RenameReferences(mutablePresetLists[i], normalizedOldEntryId, normalizedNewEntryId);

        // Reconstruct preset file with updated entry IDs
        var updatedPresets = presetFile.Presets
            .Zip(mutablePresetLists, (preset, ids) => preset with { EntryIds = ids.ToImmutableList() })
            .ToImmutableList();
        var updatedPresetFile = presetFile with { Presets = updatedPresets };

        return (updatedPresetFile, updatedOrder.ToImmutableList());
    }

    public static (LauncherPresetFile PresetFile, ImmutableList<string> CustomEntryOrder) RemoveEntryId(
        LauncherPresetFile presetFile,
        ImmutableList<string> customEntryOrder,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedEntryId = RequireEntryId(entryId, nameof(entryId));

        var mutableOrder = customEntryOrder.ToList();
        var mutablePresetLists = new List<List<string>>(presetFile.Presets.Count);
        foreach (var p in presetFile.Presets)
            mutablePresetLists.Add([.. p.EntryIds]);

        // Remove from custom order
        var updatedOrder = RemoveReferences(mutableOrder, normalizedEntryId);

        // Remove from each preset's entry list
        for (var i = 0; i < mutablePresetLists.Count; i++)
            mutablePresetLists[i] = RemoveReferences(mutablePresetLists[i], normalizedEntryId);

        // Reconstruct preset file with updated entry IDs
        var updatedPresets = presetFile.Presets
            .Zip(mutablePresetLists, (preset, ids) => preset with { EntryIds = ids.ToImmutableList() })
            .ToImmutableList();
        var updatedPresetFile = presetFile with { Presets = updatedPresets };

        return (updatedPresetFile, updatedOrder.ToImmutableList());
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

    private static string RequireEntryId(string? entryId, string paramName) =>
        LauncherEntryIds.RequireEntryId(entryId, paramName);
}
