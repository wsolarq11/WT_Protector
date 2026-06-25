using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherEntryReferences
{
    public static (LauncherPresetFile PresetFile, ImmutableList<string> CustomEntryOrder, bool Renamed) RenameEntryId(
        LauncherPresetFile presetFile,
        ImmutableList<string> customEntryOrder,
        string oldEntryId,
        string newEntryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedOldEntryId = RequireEntryId(oldEntryId, nameof(oldEntryId));
        var normalizedNewEntryId = RequireEntryId(newEntryId, nameof(newEntryId));

        // Verify the old entry ID exists before proceeding.
        var existsInCustomOrder = customEntryOrder.Any(id => LauncherEntryIds.SameNormalized(id, normalizedOldEntryId));
        var existsInAnyPreset = presetFile.Presets.Any(p => p.EntryIds.Any(id => LauncherEntryIds.SameNormalized(id, normalizedOldEntryId)));
        if (!existsInCustomOrder && !existsInAnyPreset)
            return (presetFile, customEntryOrder, false);

        // Pre-check all reference lists for target ID conflict before modifying anything.
        if (customEntryOrder.Any(id => LauncherEntryIds.Same(id, normalizedNewEntryId) && !LauncherEntryIds.Same(id, normalizedOldEntryId)))
            throw new ArgumentException(
                $"Cannot rename to '{normalizedNewEntryId}' because that ID is already present.",
                nameof(newEntryId));
        foreach (var p in presetFile.Presets)
        {
            if (p.EntryIds.Any(id => LauncherEntryIds.Same(id, normalizedNewEntryId) && !LauncherEntryIds.Same(id, normalizedOldEntryId)))
                throw new ArgumentException(
                    $"Cannot rename to '{normalizedNewEntryId}' because that ID is already present.",
                    nameof(newEntryId));
        }

        var (updatedPresetFile, updatedOrder) = TransformEntryReferences(
            presetFile, customEntryOrder,
            list => RenameReferences(list, normalizedOldEntryId, normalizedNewEntryId));

        return (updatedPresetFile, updatedOrder, true);
    }

    public static (LauncherPresetFile PresetFile, ImmutableList<string> CustomEntryOrder) RemoveEntryId(
        LauncherPresetFile presetFile,
        ImmutableList<string> customEntryOrder,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedEntryId = RequireEntryId(entryId, nameof(entryId));

        return TransformEntryReferences(
            presetFile, customEntryOrder,
            list => RemoveReferences(list, normalizedEntryId));
    }

    /// <summary>
    /// DRY helper: builds mutable copies of custom order and all preset entry lists,
    /// applies <paramref name="transform"/> to each, and reconstructs the preset file
    /// via Zip. The Zip call depends on mutablePresetLists being built from
    /// <paramref name="presetFile"/>.<c>Presets</c> in the same order.
    /// </summary>
    private static (LauncherPresetFile PresetFile, ImmutableList<string> CustomEntryOrder) TransformEntryReferences(
        LauncherPresetFile presetFile,
        ImmutableList<string> customEntryOrder,
        Func<List<string>, List<string>> transform)
    {
        var mutableOrder = customEntryOrder.ToList();
        var mutablePresetLists = new List<List<string>>(presetFile.Presets.Count);
        foreach (var p in presetFile.Presets)
            mutablePresetLists.Add([.. p.EntryIds]);

        // R22: Zip relies on mutablePresetLists corresponding 1:1 to presetFile.Presets.
        System.Diagnostics.Debug.Assert(presetFile.Presets.Count == mutablePresetLists.Count,
            "Zip ordering depends on mutablePresetLists being built from presetFile.Presets in order");

        // Transform custom order
        var updatedOrder = transform(mutableOrder);

        // Transform each preset's entry list
        for (var i = 0; i < mutablePresetLists.Count; i++)
            mutablePresetLists[i] = transform(mutablePresetLists[i]);

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

        return normalized;
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
