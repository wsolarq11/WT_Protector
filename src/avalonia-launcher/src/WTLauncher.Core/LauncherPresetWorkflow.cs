using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherPresetWorkflow
{
    public static LauncherPreset AddPreset(ref LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        if (presetFile.Presets.Count > 1000)
            throw new InvalidOperationException("Maximum of 1000 presets reached.");
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
            Id = PresetId.GenerateUnique(normalizedName, presetFile.Presets.Select(item => item.Id)),
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
        if (presetFile.Presets.Any(p => !PresetId.Same(p.Id, preset.Id)
            && string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A preset with the name \"{normalizedName}\" already exists.", nameof(name));
        var normalizedEntryIds = NormalizeRequiredEntryIds(entryIds);
        var updated = preset with { Name = normalizedName, EntryIds = normalizedEntryIds };
        var index = presetFile.Presets.FindIndex(p => PresetId.Same(p.Id, preset.Id));
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

        fallbackPresetId = PresetId.NormalizeReference(presetFile.DefaultPresetId);
        if (!CanDeletePreset(presetFile, preset))
            return null;

        // Validate DefaultPresetId AFTER CanDeletePreset — avoids crashing when
        // DefaultPresetId is invalid but the target was already findable.
        if (presetFile.Presets.Count > 0 && !TryGetDefaultPresetId(presetFile, out _))
        {
            var validDefault = presetFile.Presets.FirstOrDefault()?.Id ?? string.Empty;
            fallbackPresetId = PresetId.NormalizeReference(validDefault);
        }

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
        var normalizedPresetId = PresetId.NormalizeReference(presetId);
        if (normalizedPresetId.Length == 0)
            return null;

        return presetFile.Presets.FirstOrDefault(preset => PresetId.Same(preset.Id, normalizedPresetId));
    }

    private static string RequireDefaultPresetId(LauncherPresetFile presetFile)
    {
        if (TryGetDefaultPresetId(presetFile, out var defaultPresetId))
            return defaultPresetId;

        var normalizedDefaultPresetId = PresetId.NormalizeReference(presetFile.DefaultPresetId);
        throw new InvalidDataException(
            normalizedDefaultPresetId.Length == 0
                ? "Launcher preset default id is required when presets exist."
                : $"Launcher preset default id does not match any preset: {normalizedDefaultPresetId}.");
    }

    private static bool TryGetDefaultPresetId(LauncherPresetFile presetFile, out string defaultPresetId)
    {
        var candidate = PresetId.NormalizeReference(presetFile.DefaultPresetId);
        defaultPresetId = candidate;
        return candidate.Length > 0
            && presetFile.Presets.Any(preset => PresetId.Same(preset.Id, candidate));
    }

    private static string ResolveDefaultPresetIdAfterDelete(
        LauncherPresetFile presetFile,
        LauncherPreset target,
        string currentDefaultPresetId) =>
        PresetId.Same(currentDefaultPresetId, target.Id)
            ? RequireFallbackPresetIdAfterDelete(presetFile, target)
            : currentDefaultPresetId;

    private static string RequireFallbackPresetIdAfterDelete(LauncherPresetFile presetFile, LauncherPreset target)
    {
        // Prefer a non-locked fallback, but accept a locked one as last resort.
        var fallback = presetFile.Presets.FirstOrDefault(preset =>
            !PresetId.Same(preset.Id, target.Id) && !preset.Locked)
            ?? presetFile.Presets.FirstOrDefault(preset =>
            !PresetId.Same(preset.Id, target.Id));
        if (fallback is null)
            return string.Empty;

        var fallbackPresetId = PresetId.NormalizeReference(fallback.Id);
        if (fallbackPresetId.Length == 0)
            return string.Empty;

        return fallbackPresetId;
    }

    private static string NormalizeRequiredName(string? name)
    {
        var normalizedName = EntryName.Normalize(name);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Preset name is required.", nameof(name));

        return normalizedName;
    }

    private static ImmutableList<string> NormalizeRequiredEntryIds(IReadOnlyList<string> entryIds)
        => EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds)).ToImmutableList();

    private static bool CanUpdatePreset(LauncherPreset target) =>
        !target.Locked;

    private static bool CanDeletePreset(LauncherPresetFile presetFile, LauncherPreset? target)
    {
        if (target is null || target.Locked)
            return false;

        // Any other preset (including locked) can serve as a fallback — don't
        // deadlock when the only other preset is locked. Locked presets can be
        // applied as the default; they just can't be edited or deleted.
        // Single-preset deletion is also allowed (no fallback needed).
        return presetFile.Presets.Count <= 1
            || presetFile.Presets.Any(preset => !PresetId.Same(preset.Id, target.Id) && !preset.Locked)
            || presetFile.Presets.Any(preset => !PresetId.Same(preset.Id, target.Id));
    }
}
