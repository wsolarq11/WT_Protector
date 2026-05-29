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

        foreach (var entryIds in EntryReferenceLists(presetFile, customEntryOrder))
        {
            for (var i = 0; i < entryIds.Count; i++)
            {
                if (string.Equals(entryIds[i], oldEntryId, StringComparison.Ordinal))
                    entryIds[i] = newEntryId;
            }
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
            entryIds.RemoveAll(id => string.Equals(id, entryId, StringComparison.Ordinal));
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
}
