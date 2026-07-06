using WTLauncher.Core;
using _EntryId = WTLauncher.Core.EntryId;
using _EntryName = WTLauncher.Core.EntryName;
using _PresetId = WTLauncher.Core.PresetId;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherPresetDeleteTarget
{
    public LauncherPresetDeleteTarget(string presetId, string presetName, IReadOnlyList<string> entryIds)
    {
        PresetId = RequirePresetId(presetId);
        PresetName = _EntryName.RequireNonEmpty(presetName, "Preset name");
        var validatedEntryIds = _EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds));
        EntryCount = validatedEntryIds.Count;
    }

    public string PresetId { get; }

    public string PresetName { get; }

    public int EntryCount { get; }

    private static string RequirePresetId(string? presetId)
    {
        var normalized = _PresetId.NormalizeReference(presetId);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher preset delete target id is required.");

        return normalized;
    }

}
