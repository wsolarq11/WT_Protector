using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static LauncherPreset AddPreset(ref LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var normalizedName = NormalizeRequiredName(name);
        if (presetFile.Presets.Any(p => string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A preset with the name \"{normalizedName}\" already exists.", nameof(name));
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
        presetFile = presetFile with
        {
            Presets = presetFile.Presets.Add(preset),
            DefaultPresetId = hasExistingPresets ? defaultPresetId : preset.Id,
        };

        return preset;
    }

    public static LauncherPreset? UpdatePreset(
        ref LauncherPresetFile presetFile,
        string presetId,
        string name,
        IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = FindPreset(presetFile, presetId);
        if (preset is null || !CanUpdatePreset(preset))
            return null;

        var normalizedName = NormalizeRequiredName(name);
        if (presetFile.Presets.Any(p => !LauncherPresetIds.Same(p.Id, presetId)
            && string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A preset with the name \"{normalizedName}\" already exists.", nameof(name));
        var normalizedEntryIds = NormalizeRequiredEntryIds(entryIds);
        var updated = preset with { Name = normalizedName, EntryIds = normalizedEntryIds };
        var index = presetFile.Presets.IndexOf(preset);
        presetFile = presetFile with { Presets = presetFile.Presets.SetItem(index, updated) };
        return updated;
    }

    public static LauncherPreset? DeletePreset(
        ref LauncherPresetFile presetFile,
        string presetId,
        out string fallbackPresetId)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        var preset = FindPreset(presetFile, presetId);
        if (preset is null)
        {
            fallbackPresetId = string.Empty;
            return null;
        }

        fallbackPresetId = RequireDefaultPresetId(presetFile);
        if (!CanDeletePreset(presetFile, preset))
            return null;

        fallbackPresetId = ResolveDefaultPresetIdAfterDelete(presetFile, preset, fallbackPresetId);
        presetFile = presetFile with
        {
            Presets = presetFile.Presets.Remove(preset),
            DefaultPresetId = fallbackPresetId,
        };

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
        // E11: Don't require HasEntryReferences on the fallback — that deadlocks
        // when the only other preset references stale or zero entries. Any other
        // non-locked preset (including one with no entries) can serve as the new default.
        var fallback = presetFile.Presets.FirstOrDefault(preset =>
            !LauncherPresetIds.Same(preset.Id, target.Id) && !preset.Locked);
        if (fallback is null)
            return string.Empty;

        var fallbackPresetId = LauncherPresetIds.NormalizeReference(fallback.Id);
        if (fallbackPresetId.Length == 0)
            return string.Empty;

        return fallbackPresetId;
    }

    private static string NormalizeRequiredName(string? name)
    {
        var normalizedName = LauncherEntryText.Normalize(name);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Preset name is required.", nameof(name));

        return normalizedName;
    }

    private static ImmutableList<string> NormalizeRequiredEntryIds(IReadOnlyList<string> entryIds)
        => LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds)).ToImmutableList();

    private static bool CanUpdatePreset(LauncherPreset target) =>
        !target.Locked;

    private static bool CanDeletePreset(LauncherPresetFile presetFile, LauncherPreset? target)
    {
        if (target is null || target.Locked)
            return false;

        // E11: Any other non-locked preset can serve as a fallback — don't
        // deadlock when the only other preset references stale or zero entries.
        // Single-preset deletion is also allowed (no fallback needed).
        return presetFile.Presets.Count <= 1
            || presetFile.Presets.Any(preset => !LauncherPresetIds.Same(preset.Id, target.Id) && !preset.Locked);
    }
}
