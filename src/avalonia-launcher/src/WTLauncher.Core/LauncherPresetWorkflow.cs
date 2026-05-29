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
            EntryIds = [.. entryIds],
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
        var preset = FindPreset(presetFile, presetId);
        if (preset is null || preset.Locked)
            return null;

        preset.Name = name;
        preset.EntryIds = [.. entryIds];
        return preset;
    }

    public static LauncherPreset? DeletePreset(
        LauncherPresetFile presetFile,
        string presetId,
        out string fallbackPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = FindPreset(presetFile, presetId);
        if (preset is null || !CanDeletePreset(presetFile, preset))
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
        return CanDeletePreset(presetFile, FindPreset(presetFile, presetId));
    }

    private static LauncherPreset? FindPreset(LauncherPresetFile presetFile, string presetId) =>
        presetFile.Presets.FirstOrDefault(preset => preset.Id == presetId);

    private static bool CanDeletePreset(LauncherPresetFile presetFile, LauncherPreset? target)
    {
        if (target is null || target.Locked)
            return false;

        return presetFile.Presets
            .Any(preset => preset.Id != target.Id && preset.EntryIds.Count > 0);
    }
}
