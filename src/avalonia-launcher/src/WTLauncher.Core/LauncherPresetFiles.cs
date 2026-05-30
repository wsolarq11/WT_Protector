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
            Presets = (presetFile.Presets ?? [])
                .OfType<LauncherPreset>()
                .Select(preset => new LauncherPreset
                {
                    Id = preset.Id,
                    Name = preset.Name,
                    EntryIds = [.. (preset.EntryIds ?? [])],
                    Locked = preset.Locked,
                })
                .ToList(),
        };

        Normalize(clone);
        return clone;
    }

    internal static void Normalize(LauncherPresetFile presets)
    {
        var normalizedPresets = new List<LauncherPreset>();
        var usedPresetIds = new List<string>();
        foreach (var preset in (presets.Presets ?? []).OfType<LauncherPreset>())
        {
            preset.Id = LauncherPresetIds.GenerateUnique(preset.Id, usedPresetIds);
            usedPresetIds.Add(preset.Id);
            preset.Name = LauncherPresetNames.Normalize(preset.Name);
            preset.EntryIds = LauncherEntryIds.ToUniqueUsableList(preset.EntryIds ?? []);
            normalizedPresets.Add(preset);
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
}
