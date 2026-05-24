namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static LauncherPreset AddPreset(LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Preset name is required.", nameof(name));

        var preset = new LauncherPreset
        {
            Id = LauncherPresetIds.GenerateUnique(name, presetFile.Presets.Select(item => item.Id)),
            Name = name,
            EntryIds = entryIds.ToList(),
        };
        presetFile.Presets.Add(preset);
        return preset;
    }

    public static LauncherPreset? UpdatePreset(
        LauncherPresetFile presetFile,
        string presetId,
        string name,
        IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = presetFile.Presets.FirstOrDefault(item => item.Id == presetId);
        if (preset is null || preset.Locked)
            return null;

        preset.Name = name;
        preset.EntryIds = entryIds.ToList();
        return preset;
    }

    public static LauncherPreset? DeletePreset(
        LauncherPresetFile presetFile,
        string presetId,
        out string fallbackPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = presetFile.Presets.FirstOrDefault(item => item.Id == presetId);
        if (preset is null || preset.Locked || !CanDeletePreset(presetFile, presetId))
        {
            fallbackPresetId = presetFile.DefaultPresetId;
            return null;
        }

        presetFile.Presets.Remove(preset);
        if (presetFile.DefaultPresetId == preset.Id)
            presetFile.DefaultPresetId = presetFile.Presets.FirstOrDefault()?.Id ?? string.Empty;

        fallbackPresetId = presetFile.DefaultPresetId;
        return preset;
    }

    public static bool CanDeletePreset(LauncherPresetFile presetFile, string presetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        if (presetFile.Presets.Count == 0)
            return false;

        var target = presetFile.Presets.FirstOrDefault(preset => preset.Id == presetId);
        if (target is null || target.Locked)
            return false;

        var remainingNonEmpty = presetFile.Presets
            .Count(preset => preset.Id != presetId && preset.EntryIds.Count > 0);
        return remainingNonEmpty > 0;
    }
}
