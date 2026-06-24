using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherPresetDeleteTarget
{
    public LauncherPresetDeleteTarget(string presetId, string presetName, IReadOnlyList<string> entryIds)
    {
        PresetId = RequirePresetId(presetId);
        PresetName = RequirePresetName(presetName);
        EntryCount = LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds)).Count;
    }

    public string PresetId { get; }

    public string PresetName { get; }

    public int EntryCount { get; }

    private static string RequirePresetId(string? presetId)
    {
        var normalized = LauncherPresetIds.NormalizeReference(presetId);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher preset delete target id is required.");

        return normalized;
    }

    private static string RequirePresetName(string? presetName)
    {
        var normalized = LauncherEntryText.Normalize(presetName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher preset delete target name is required.");

        return normalized;
    }
}
