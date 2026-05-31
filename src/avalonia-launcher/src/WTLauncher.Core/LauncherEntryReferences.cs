namespace WTLauncher.Core;

public static class LauncherEntryReferences
{
    public static void RenameEntryId(
        LauncherPresetFile presetFile,
        List<string> customEntryOrder,
        string oldEntryId,
        string newEntryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedOldEntryId = RequireEntryId(oldEntryId, nameof(oldEntryId));
        var normalizedNewEntryId = RequireEntryId(newEntryId, nameof(newEntryId));
        foreach (var entryIds in EntryReferenceLists(presetFile, customEntryOrder))
        {
            for (var i = 0; i < entryIds.Count; i++)
            {
                if (LauncherEntryIds.SameNormalized(entryIds[i], normalizedOldEntryId))
                    entryIds[i] = normalizedNewEntryId;
            }

            NormalizeEntryIds(entryIds);
        }
    }

    public static void RemoveEntryId(
        LauncherPresetFile presetFile,
        List<string> customEntryOrder,
        string entryId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var normalizedEntryId = RequireEntryId(entryId, nameof(entryId));
        foreach (var entryIds in EntryReferenceLists(presetFile, customEntryOrder))
        {
            entryIds.RemoveAll(id => LauncherEntryIds.SameNormalized(id, normalizedEntryId));
            NormalizeEntryIds(entryIds);
        }
    }

    private static IEnumerable<List<string>> EntryReferenceLists(
        LauncherPresetFile presetFile,
        List<string> customEntryOrder)
    {
        foreach (var preset in presetFile.Presets)
        {
            yield return preset.EntryIds;
        }

        yield return customEntryOrder;
    }

    private static void NormalizeEntryIds(List<string> entryIds)
    {
        var normalized = LauncherEntryIds.ToUniqueUsableList(entryIds);
        entryIds.Clear();
        entryIds.AddRange(normalized);
    }

    private static string RequireEntryId(string? entryId, string paramName)
    {
        var normalized = LauncherEntryIds.Normalize(entryId);
        if (!LauncherEntryIds.IsUsable(normalized))
            throw new ArgumentException("Entry id is required.", paramName);

        return normalized;
    }
}
