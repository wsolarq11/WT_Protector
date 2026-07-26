using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// Ultrareview: LauncherPresetValidator 已完成完整实现和测试，但至今未接入 PresetManager.Load。
/// 标记为过时。如需启用，接入 PresetManager.Load 并移除此属性。
/// See: .trellis/spec/presets-validation.md
/// </summary>
[Obsolete("LauncherPresetValidator 未接入生产代码。如需启用，接入 PresetManager.Load 后移除此属性。")]

public static class LauncherPresetValidator
{
    public static LauncherPresetFile Validate(LauncherPresetFile presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        var presetRows = presets.Presets
            ?? throw new InvalidDataException("Launcher preset list is null.");

        // Guard against maliciously large preset files (max 1000 presets).
        if (presetRows.Count > 1000)
            throw new InvalidDataException($"Launcher preset count {presetRows.Count} exceeds maximum of 1000.");

        List<LauncherPreset> normalizedPresets = [];
        var usedPresetIds = new HashSet<string>(PresetId.Comparer);
        var usedPresetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var presetRow in presetRows)
        {
            var preset = RequirePreset(presetRow, index);
            var id = NormalizePresetId(preset.Id, index);
            if (!usedPresetIds.Add(id))
                throw new InvalidDataException($"Launcher preset {index} id is duplicate: {id}.");

            var name = NormalizePresetName(preset.Name, index);
            if (!usedPresetNames.Add(name))
                throw new InvalidDataException($"Launcher preset {index} name is duplicate: {name}.");

            var entryIds = NormalizeEntryIds(preset.EntryIds, index);
            normalizedPresets.Add(preset with { Id = id, Name = name, EntryIds = entryIds.ToImmutableList() });
            index++;
        }

        return presets with
        {
            Presets = normalizedPresets.ToImmutableList(),
            DefaultPresetId = NormalizeDefaultPresetId(presets.DefaultPresetId, normalizedPresets),
        };
    }

    private static LauncherPreset RequirePreset(LauncherPreset? preset, int index) =>
        preset ?? throw new InvalidDataException($"Launcher preset {index} is null.");

    private static string NormalizePresetId(string? id, int index)
    {
        var normalized = PresetId.NormalizeReference(id);
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher preset {index} id is empty.");

        return normalized;
    }

    private static string NormalizePresetName(string? name, int index)
    {
        var normalized = EntryName.Normalize(name);
        if (normalized.Length > 256)
            throw new InvalidDataException($"Launcher preset {index} name exceeds 256 character limit.");
        if (normalized.Length == 0)
            throw new InvalidDataException($"Launcher preset {index} name is empty.");

        return normalized;
    }

    private static List<string> NormalizeEntryIds(IReadOnlyList<string>? entryIds, int index)
    {
        var source = entryIds ?? throw new InvalidDataException($"Launcher preset {index} entryIds is null.");

        // Inline uniqueness validation validates duplicates AND materializes the count.
        // While this iterates the full list, it provides defense-in-depth against
        // duplicate IDs in presets. This is called once at load time, not in a hot path.
        if (source.Count > 10000)
            throw new InvalidDataException($"Launcher preset {index} entryIds count {source.Count} exceeds maximum of 10000.");

        var normalized = new List<string>(source.Count);
        var seen = new HashSet<string>(EntryId.Comparer);
        for (var i = 0; i < source.Count; i++)
        {
            var entryId = EntryId.Normalize(source[i]);
            if (entryId.Length == 0)
                throw new InvalidDataException($"Launcher preset {index} entryIds[{i}] is empty.");

            if (!seen.Add(entryId))
                throw new InvalidDataException($"Launcher preset {index} entryIds[{i}] is duplicate: {entryId}.");
            if (EntryId.WindowsReservedNames.Contains(entryId))
                throw new InvalidDataException($"Launcher preset {index} entryIds[{i}] is a Windows reserved name: {entryId}.");
            if (entryId.Contains('\\') || entryId.Contains('/') || entryId.Contains(".."))
                throw new InvalidDataException($"Launcher preset {index} entryIds[{i}] contains path separators or '..': {entryId}.");

            normalized.Add(entryId);
        }

        return normalized;
    }

    private static string NormalizeDefaultPresetId(string? defaultPresetId, IReadOnlyList<LauncherPreset> presets)
    {
        var normalized = PresetId.NormalizeReference(defaultPresetId);
        if (presets.Count == 0)
        {
            if (normalized.Length != 0)
                throw new InvalidDataException($"Launcher preset default id is invalid without presets: {normalized}.");

            return string.Empty;
        }

        if (normalized.Length == 0)
            throw new InvalidDataException("Launcher preset default id is required when presets exist.");

        if (!presets.Any(preset => PresetId.Same(preset.Id, normalized)))
            throw new InvalidDataException($"Launcher preset default id does not match any preset: {normalized}.");

        return normalized;
    }
}
