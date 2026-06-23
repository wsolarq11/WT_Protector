namespace WTLauncher.Core;

public static class LauncherPresetFiles
{
    public static LauncherPresetFile Clone(LauncherPresetFile presetFile)
    {
        ArgumentNullException.ThrowIfNull(presetFile);

        var clone = new LauncherPresetFile
        {
            SchemaVersion = presetFile.SchemaVersion,
            DefaultPresetId = presetFile.DefaultPresetId,
            Presets = ClonePresets(presetFile.Presets),
        };

        Validate(clone);
        return clone;
    }

    internal static void Validate(LauncherPresetFile presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        var presetRows = presets.Presets
            ?? throw new InvalidDataException("Launcher preset list is null.");
        List<LauncherPreset> normalizedPresets = [];
        var usedPresetIds = new HashSet<string>(LauncherPresetIds.Comparer);
        var index = 0;
        foreach (var presetRow in presetRows)
        {
            var preset = RequirePreset(presetRow, index);
            var id = NormalizePresetId(preset.Id, index);
            if (!usedPresetIds.Add(id))
                throw new InvalidDataException($"Launcher preset {index} id is duplicate: {id}.");

            var name = NormalizePresetName(preset.Name, index);
            var entryIds = NormalizeEntryIds(preset.EntryIds, index);
            normalizedPresets.Add(new LauncherPreset
            {
                Id = id,
                Name = name,
                EntryIds = entryIds,
                Locked = preset.Locked,
            });
            index++;
        }

        presets.Presets = normalizedPresets;
        presets.DefaultPresetId = NormalizeDefaultPresetId(presets.DefaultPresetId, normalizedPresets);
    }

    private static List<LauncherPreset> ClonePresets(IEnumerable<LauncherPreset>? presets)
    {
        if (presets is null)
            throw new InvalidDataException("Launcher preset list is null.");

        List<LauncherPreset> clone = [];
        var index = 0;
        foreach (var presetRow in presets)
        {
            var preset = RequirePreset(presetRow, index);
            clone.Add(new LauncherPreset
            {
                Id = preset.Id,
                Name = preset.Name,
                EntryIds = [.. preset.EntryIds],
                Locked = preset.Locked,
            });
            index++;
        }

        return clone;
    }

    private static LauncherPreset RequirePreset(LauncherPreset? preset, int index) =>
        preset ?? throw new InvalidDataException($"Launcher preset {index} is null.");

    private static string NormalizePresetId(string? id, int index)
    {
        var normalized = LauncherPresetIds.NormalizeReference(id);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher preset {index} id is empty.");

        return normalized;
    }

    private static string NormalizePresetName(string? name, int index)
    {
        var normalized = LauncherEntryText.Normalize(name);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher preset {index} name is empty.");

        return normalized;
    }

    private static List<string> NormalizeEntryIds(IReadOnlyList<string>? entryIds, int index)
    {
        var source = entryIds ?? throw new InvalidDataException($"Launcher preset {index} entryIds is null.");
        var normalized = new List<string>(source.Count);
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        for (var i = 0; i < source.Count; i++)
        {
            var entryId = LauncherEntryIds.Normalize(source[i]);
            if (entryId.Length == 0)
                throw new InvalidDataException($"Launcher preset {index} entryIds[{i}] is empty.");

            if (!seen.Add(entryId))
                throw new InvalidDataException($"Launcher preset {index} entryIds[{i}] is duplicate: {entryId}.");

            normalized.Add(entryId);
        }

        return normalized;
    }

    private static string NormalizeDefaultPresetId(string? defaultPresetId, IReadOnlyList<LauncherPreset> presets)
    {
        var normalized = LauncherPresetIds.NormalizeReference(defaultPresetId);
        if (presets.Count == 0)
        {
            if (normalized.Length != 0)
                throw new InvalidDataException($"Launcher preset default id is invalid without presets: {normalized}.");

            return string.Empty;
        }

        if (normalized.Length == 0)
            throw new InvalidDataException("Launcher preset default id is required when presets exist.");

        if (!presets.Any(preset => LauncherPresetIds.Same(preset.Id, normalized)))
            throw new InvalidDataException($"Launcher preset default id does not match any preset: {normalized}.");

        return normalized;
    }
}
