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
            Presets = ClonePresets(presetFile.Presets ?? []),
        };

        Normalize(clone);
        return clone;
    }

    internal static void Normalize(LauncherPresetFile presets)
    {
        var normalizedPresets = new List<LauncherPreset>();
        var usedPresetIds = new List<string>();
        var index = 0;
        foreach (var presetRow in presets.Presets ?? [])
        {
            var preset = RequirePreset(presetRow, index);
            preset.Id = LauncherPresetIds.GenerateUnique(preset.Id, usedPresetIds);
            usedPresetIds.Add(preset.Id);
            preset.Name = LauncherPresetNames.Normalize(preset.Name);
            preset.EntryIds = LauncherEntryIds.ToUniqueUsableList(preset.EntryIds ?? []);
            normalizedPresets.Add(preset);
            index++;
        }

        presets.Presets = normalizedPresets;
        presets.DefaultPresetId = LauncherPresetIds.NormalizeReference(presets.DefaultPresetId);
        if (normalizedPresets.Count == 0)
        {
            presets.DefaultPresetId = string.Empty;
            return;
        }

        if (!normalizedPresets.Any(preset => LauncherPresetIds.Same(preset.Id, presets.DefaultPresetId)))
            presets.DefaultPresetId = normalizedPresets[0].Id;
    }

    private static List<LauncherPreset> ClonePresets(IEnumerable<LauncherPreset> presets)
    {
        var clone = new List<LauncherPreset>();
        var index = 0;
        foreach (var presetRow in presets)
        {
            var preset = RequirePreset(presetRow, index);
            clone.Add(new LauncherPreset
            {
                Id = preset.Id,
                Name = preset.Name,
                EntryIds = [.. (preset.EntryIds ?? [])],
                Locked = preset.Locked,
            });
            index++;
        }

        return clone;
    }

    private static LauncherPreset RequirePreset(LauncherPreset? preset, int index) =>
        preset ?? throw new InvalidDataException($"Launcher preset {index} is null.");
}
