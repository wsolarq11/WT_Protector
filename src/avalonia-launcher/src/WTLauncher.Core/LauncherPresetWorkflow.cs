namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static LauncherPreset AddPreset(LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var normalizedName = NormalizeRequiredName(name);
        var hasExistingPresets = presetFile.Presets.Count > 0;
        var defaultPresetId = hasExistingPresets
            ? RequireDefaultPresetId(presetFile)
            : string.Empty;
        var normalizedEntryIds = NormalizeRequiredEntryIds(entryIds);
        var preset = new LauncherPreset
        {
            Id = LauncherPresetIds.GenerateUnique(normalizedName, presetFile.Presets.Select(item => item.Id)),
            Name = normalizedName,
            EntryIds = normalizedEntryIds,
        };
        presetFile.Presets.Add(preset);
        presetFile.DefaultPresetId = hasExistingPresets ? defaultPresetId : preset.Id;

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
        var normalizedEntryIds = NormalizeRequiredEntryIds(entryIds);
        preset.Name = normalizedName;
        preset.EntryIds = normalizedEntryIds;
        return preset;
    }

    public static LauncherPreset? DeletePreset(
        LauncherPresetFile presetFile,
        string presetId,
        out string fallbackPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = FindPreset(presetFile, presetId);
        fallbackPresetId = RequireDefaultPresetId(presetFile);
        if (preset is null || !CanDeletePreset(presetFile, preset))
            return null;

        fallbackPresetId = ResolveDefaultPresetIdAfterDelete(presetFile, preset, fallbackPresetId);
        presetFile.Presets.Remove(preset);
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

    private static string RequireDefaultPresetId(LauncherPresetFile presetFile)
    {
        if (TryGetDefaultPresetId(presetFile, out var defaultPresetId))
            return defaultPresetId;

        var normalizedDefaultPresetId = LauncherPresetIds.NormalizeReference(presetFile.DefaultPresetId);
        throw new InvalidDataException(
            normalizedDefaultPresetId.Length == 0
                ? "Launcher preset default id is required when presets exist."
                : $"Launcher preset default id does not match any preset: {normalizedDefaultPresetId}.");
    }

    private static bool TryGetDefaultPresetId(LauncherPresetFile presetFile, out string defaultPresetId)
    {
        var candidate = LauncherPresetIds.NormalizeReference(presetFile.DefaultPresetId);
        defaultPresetId = candidate;
        return candidate.Length > 0
            && presetFile.Presets.Any(preset => LauncherPresetIds.Same(preset.Id, candidate));
    }

    private static string ResolveDefaultPresetIdAfterDelete(
        LauncherPresetFile presetFile,
        LauncherPreset target,
        string currentDefaultPresetId) =>
        LauncherPresetIds.Same(currentDefaultPresetId, target.Id)
            ? RequireFallbackPresetIdAfterDelete(presetFile, target)
            : currentDefaultPresetId;

    private static string RequireFallbackPresetIdAfterDelete(LauncherPresetFile presetFile, LauncherPreset target)
    {
        var fallback = presetFile.Presets.FirstOrDefault(preset =>
            !LauncherPresetIds.Same(preset.Id, target.Id) && HasEntryReferences(preset));
        if (fallback is null)
            throw new InvalidDataException("Launcher preset fallback id is required after delete.");

        var fallbackPresetId = LauncherPresetIds.NormalizeReference(fallback.Id);
        if (fallbackPresetId.Length == 0)
            throw new InvalidDataException("Launcher preset fallback id is required after delete.");

        return fallbackPresetId;
    }

    private static bool HasEntryReferences(LauncherPreset preset) =>
        LauncherEntryIds.ToRequiredUniqueList(preset.EntryIds, nameof(preset.EntryIds)).Count > 0;

    private static string NormalizeRequiredName(string? name)
    {
        var normalizedName = LauncherEntryText.Normalize(name);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Preset name is required.", nameof(name));

        return normalizedName;
    }

    private static List<string> NormalizeRequiredEntryIds(IReadOnlyList<string> entryIds)
        => LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds));

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
