namespace WTLauncher.Core;

public static class LauncherEntryReferences
{
    public static void Rename(
        string oldEntryId,
        string newEntryId,
        IEnumerable<LauncherPreset> presets,
        List<string> customEntryOrder)
    {
        if (string.Equals(oldEntryId, newEntryId, StringComparison.Ordinal))
            return;

        foreach (var preset in presets)
        {
            for (var i = 0; i < preset.EntryIds.Count; i++)
            {
                if (string.Equals(preset.EntryIds[i], oldEntryId, StringComparison.Ordinal))
                    preset.EntryIds[i] = newEntryId;
            }
        }

        for (var i = 0; i < customEntryOrder.Count; i++)
        {
            if (string.Equals(customEntryOrder[i], oldEntryId, StringComparison.Ordinal))
                customEntryOrder[i] = newEntryId;
        }
    }

    public static void Remove(
        string entryId,
        IEnumerable<LauncherPreset> presets,
        List<string> customEntryOrder)
    {
        foreach (var preset in presets)
        {
            preset.EntryIds.RemoveAll(id => string.Equals(id, entryId, StringComparison.Ordinal));
        }

        customEntryOrder.RemoveAll(id => string.Equals(id, entryId, StringComparison.Ordinal));
    }
}
