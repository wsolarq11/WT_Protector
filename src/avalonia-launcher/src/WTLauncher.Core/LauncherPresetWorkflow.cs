namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static string NormalizeName(string? name) =>
        (name ?? string.Empty).Trim();

    public static LauncherPreset AddPreset(LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var normalizedName = NormalizeRequiredName(name);
        var preset = new LauncherPreset
        {
            Id = LauncherPresetIds.GenerateUnique(normalizedName, presetFile.Presets.Select(item => item.Id)),
            Name = normalizedName,
            EntryIds = NormalizeEntryIds(entryIds),
        };
        presetFile.Presets.Add(preset);
        if (!HasDefaultPreset(presetFile))
            presetFile.DefaultPresetId = preset.Id;

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

        var normalizedName = NormalizeRequiredName(name);
        preset.Name = normalizedName;
        preset.EntryIds = NormalizeEntryIds(entryIds);
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
        if (!HasDefaultPreset(presetFile))
        {
            fallbackPresetId = ResolveFallbackPresetId(presetFile);
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

    private static bool HasDefaultPreset(LauncherPresetFile presetFile) =>
        presetFile.Presets.Any(preset => LauncherPresetIds.Same(preset.Id, presetFile.DefaultPresetId));

    private static string ResolveFallbackPresetId(LauncherPresetFile presetFile) =>
        presetFile.Presets.FirstOrDefault(preset => preset.EntryIds.Count > 0)?.Id
        ?? presetFile.Presets.FirstOrDefault()?.Id
        ?? string.Empty;

    private static List<string> NormalizeEntryIds(IEnumerable<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(entryIds);

        var normalized = new List<string>();
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        foreach (var entryId in entryIds)
        {
            var trimmed = (entryId ?? string.Empty).Trim();
            if (!LauncherEntryIds.IsUsable(trimmed))
                continue;
            if (seen.Add(trimmed))
                normalized.Add(trimmed);
        }

        return normalized;
    }

    private static string NormalizeRequiredName(string? name)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Preset name is required.", nameof(name));

        return normalizedName;
    }

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
