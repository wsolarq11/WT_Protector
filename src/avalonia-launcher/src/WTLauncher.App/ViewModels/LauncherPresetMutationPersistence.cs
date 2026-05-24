using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetMutationPersistence
{
    public static LauncherPresetMutationResult AddPreset(
        LauncherPresetFile presetFile,
        string name,
        IReadOnlyList<string> entryIds,
        string? preferredPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        var snapshot = LauncherPresetFiles.Clone(presetFile);
        var preset = LauncherPresetWorkflow.AddPreset(presetFile, name, entryIds);
        return SaveOrRestore(snapshot, preferredPresetId, preset, preset.Id, savePresets, restorePresetFile);
    }

    public static LauncherPresetMutationResult UpdatePreset(
        LauncherPresetFile presetFile,
        string presetId,
        string name,
        IReadOnlyList<string> entryIds,
        string? preferredPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        var snapshot = LauncherPresetFiles.Clone(presetFile);
        var preset = LauncherPresetWorkflow.UpdatePreset(presetFile, presetId, name, entryIds);
        if (preset is null)
        {
            restorePresetFile(snapshot, preferredPresetId);
            return LauncherPresetMutationResult.Missing(preferredPresetId);
        }

        return SaveOrRestore(snapshot, preferredPresetId, preset, preset.Id, savePresets, restorePresetFile);
    }

    public static LauncherPresetMutationResult DeletePreset(
        LauncherPresetFile presetFile,
        string presetId,
        string? preferredPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        var snapshot = LauncherPresetFiles.Clone(presetFile);
        var preset = LauncherPresetWorkflow.DeletePreset(presetFile, presetId, out var fallbackPresetId);
        if (preset is null)
        {
            restorePresetFile(snapshot, preferredPresetId);
            return LauncherPresetMutationResult.Missing(preferredPresetId);
        }

        return SaveOrRestore(snapshot, preferredPresetId, preset, fallbackPresetId, savePresets, restorePresetFile);
    }

    private static LauncherPresetMutationResult SaveOrRestore(
        LauncherPresetFile snapshot,
        string? preferredPresetId,
        LauncherPreset preset,
        string? nextPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        if (savePresets())
            return LauncherPresetMutationResult.Saved(preset, nextPresetId);

        restorePresetFile(snapshot, preferredPresetId);
        return LauncherPresetMutationResult.SaveFailed(preset, preferredPresetId);
    }
}

public enum LauncherPresetMutationStatus
{
    Saved,
    MissingPreset,
    SaveFailed,
}

public readonly record struct LauncherPresetMutationResult(
    LauncherPresetMutationStatus Status,
    LauncherPreset? Preset,
    string? NextPresetId)
{
    public bool IsSaved => Status == LauncherPresetMutationStatus.Saved;

    public static LauncherPresetMutationResult Saved(LauncherPreset preset, string? nextPresetId)
    {
        return new LauncherPresetMutationResult(LauncherPresetMutationStatus.Saved, preset, nextPresetId);
    }

    public static LauncherPresetMutationResult Missing(string? nextPresetId)
    {
        return new LauncherPresetMutationResult(LauncherPresetMutationStatus.MissingPreset, null, nextPresetId);
    }

    public static LauncherPresetMutationResult SaveFailed(LauncherPreset preset, string? nextPresetId)
    {
        return new LauncherPresetMutationResult(LauncherPresetMutationStatus.SaveFailed, preset, nextPresetId);
    }
}
