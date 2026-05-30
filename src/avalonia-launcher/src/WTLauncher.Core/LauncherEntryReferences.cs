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

        var normalizedNewEntryId = LauncherEntryIds.Normalize(newEntryId);
        foreach (var entryIds in EntryReferenceLists(presetFile, customEntryOrder))
        {
            for (var i = 0; i < entryIds.Count; i++)
            {
                if (LauncherEntryIds.SameNormalized(entryIds[i], oldEntryId))
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

        foreach (var entryIds in EntryReferenceLists(presetFile, customEntryOrder))
        {
            entryIds.RemoveAll(id => LauncherEntryIds.SameNormalized(id, entryId));
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
}
