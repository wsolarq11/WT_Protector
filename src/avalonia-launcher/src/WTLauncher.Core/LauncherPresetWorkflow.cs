namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static string NormalizeName(string? name) =>
        (name ?? string.Empty).Trim();

    public static LauncherPreset AddPreset(LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Preset name is required.", nameof(name));

        var preset = new LauncherPreset
        {
            Id = LauncherPresetIds.GenerateUnique(normalizedName, presetFile.Presets.Select(item => item.Id)),
            Name = normalizedName,
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
        if (preset is null || !CanUpdatePreset(preset))
            return null;

        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Preset name is required.", nameof(name));

        preset.Name = normalizedName;
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
        fallbackPresetId = presetFile.DefaultPresetId;
        if (preset is null || !CanDeletePreset(presetFile, preset))
            return null;

        presetFile.Presets.Remove(preset);
        if (LauncherPresetIds.Same(presetFile.DefaultPresetId, preset.Id))
        {
            fallbackPresetId = presetFile.Presets.FirstOrDefault()?.Id ?? string.Empty;
            presetFile.DefaultPresetId = fallbackPresetId;
        }

        return preset;
    }

    public static bool CanDeletePreset(LauncherPresetFile presetFile, string presetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        return CanDeletePreset(presetFile, FindPreset(presetFile, presetId));
    }

    public static bool CanUpdatePreset(LauncherPresetFile presetFile, string presetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = FindPreset(presetFile, presetId);
        return preset is not null && CanUpdatePreset(preset);
    }

    private static LauncherPreset? FindPreset(LauncherPresetFile presetFile, string presetId) =>
        presetFile.Presets.FirstOrDefault(preset => LauncherPresetIds.Same(preset.Id, presetId));

    private static bool CanUpdatePreset(LauncherPreset target) =>
        !target.Locked;

    private static bool CanDeletePreset(LauncherPresetFile presetFile, LauncherPreset? target)
    {
        if (target is null || target.Locked)
            return false;

        return presetFile.Presets
            .Any(preset => !LauncherPresetIds.Same(preset.Id, target.Id) && preset.EntryIds.Count > 0);
    }
}
