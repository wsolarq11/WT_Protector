namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static LauncherPreset AddPreset(LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var normalizedName = NormalizeRequiredName(name);
        var hasDefaultPreset = TryGetDefaultPresetId(presetFile, out var defaultPresetId);
        var preset = new LauncherPreset
        {
            Id = LauncherPresetIds.GenerateUnique(normalizedName, presetFile.Presets.Select(item => item.Id)),
            Name = normalizedName,
            EntryIds = LauncherEntryIds.ToUniqueUsableList(entryIds),
        };
        presetFile.Presets.Add(preset);
        presetFile.DefaultPresetId = hasDefaultPreset ? defaultPresetId : preset.Id;

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
        preset.EntryIds = LauncherEntryIds.ToUniqueUsableList(entryIds);
        return preset;
    }

    public static LauncherPreset? DeletePreset(
        LauncherPresetFile presetFile,
        string presetId,
        out string fallbackPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = FindPreset(presetFile, presetId);
        fallbackPresetId = ResolveDefaultPresetId(presetFile);
        if (preset is null || !CanDeletePreset(presetFile, preset))
            return null;

        presetFile.Presets.Remove(preset);
        fallbackPresetId = ResolveDefaultPresetId(presetFile);
        presetFile.DefaultPresetId = fallbackPresetId;

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

    private static LauncherPreset? FindPreset(LauncherPresetFile presetFile, string presetId)
    {
        var normalizedPresetId = LauncherPresetIds.NormalizeReference(presetId);
        if (normalizedPresetId.Length == 0)
            return null;

        return presetFile.Presets.FirstOrDefault(preset => LauncherPresetIds.Same(preset.Id, normalizedPresetId));
    }

    private static string ResolveDefaultPresetId(LauncherPresetFile presetFile) =>
        TryGetDefaultPresetId(presetFile, out var defaultPresetId)
            ? defaultPresetId
            : ResolveFallbackPresetId(presetFile);

    private static bool TryGetDefaultPresetId(LauncherPresetFile presetFile, out string defaultPresetId)
    {
        var candidate = LauncherPresetIds.NormalizeReference(presetFile.DefaultPresetId);
        defaultPresetId = candidate;
        return candidate.Length > 0
            && presetFile.Presets.Any(preset => LauncherPresetIds.Same(preset.Id, candidate));
    }

    private static string ResolveFallbackPresetId(LauncherPresetFile presetFile) =>
        presetFile.Presets.FirstOrDefault(HasEntryReferences)?.Id
        ?? presetFile.Presets.FirstOrDefault()?.Id
        ?? string.Empty;

    private static bool HasEntryReferences(LauncherPreset preset) =>
        preset.EntryIds.Any(LauncherEntryIds.IsUsable);

    private static string NormalizeRequiredName(string? name)
    {
        var normalizedName = LauncherPresetNames.Normalize(name);
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
            .Any(preset => !LauncherPresetIds.Same(preset.Id, target.Id) && HasEntryReferences(preset));
    }
}
