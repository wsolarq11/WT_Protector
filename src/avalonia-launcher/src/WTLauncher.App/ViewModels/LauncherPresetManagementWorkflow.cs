using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetManagementWorkflow
{
    public static LauncherPresetManagementResult AddPreset(
        LauncherPresetFile presetFile,
        string name,
        IReadOnlyList<string> entryIds,
        string? preferredPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(savePresets);
        ArgumentNullException.ThrowIfNull(restorePresetFile);

        var result = LauncherPresetMutationPersistence.AddPreset(
            presetFile,
            name,
            entryIds,
            preferredPresetId,
            savePresets,
            restorePresetFile);
        if (!result.IsSaved)
            return LauncherPresetManagementResult.NoChange();

        var preset = result.Preset!;
        return LauncherPresetManagementResult.Reload(
            result.NextPresetId,
            $"已保存启动组: {preset.Name}（{preset.EntryIds.Count} 项）");
    }

    public static LauncherPresetManagementResult UpdatePreset(
        LauncherPresetFile presetFile,
        string presetId,
        string name,
        IReadOnlyList<string> entryIds,
        string? preferredPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(presetId);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(savePresets);
        ArgumentNullException.ThrowIfNull(restorePresetFile);

        var result = LauncherPresetMutationPersistence.UpdatePreset(
            presetFile,
            presetId,
            name,
            entryIds,
            preferredPresetId,
            savePresets,
            restorePresetFile);
        if (result.Status == LauncherPresetMutationStatus.MissingPreset)
            return LauncherPresetManagementResult.Status("当前启动组已不存在，请重新加载");
        if (!result.IsSaved)
            return LauncherPresetManagementResult.NoChange();

        var preset = result.Preset!;
        return LauncherPresetManagementResult.Reload(
            result.NextPresetId,
            $"已更新启动组: {preset.Name}（{preset.EntryIds.Count} 项）");
    }

    public static LauncherPresetManagementResult DeletePreset(
        LauncherPresetFile presetFile,
        string presetId,
        string? preferredPresetId,
        Func<bool> savePresets,
        Action<LauncherPresetFile, string?> restorePresetFile)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(presetId);
        ArgumentNullException.ThrowIfNull(savePresets);
        ArgumentNullException.ThrowIfNull(restorePresetFile);

        var result = LauncherPresetMutationPersistence.DeletePreset(
            presetFile,
            presetId,
            preferredPresetId,
            savePresets,
            restorePresetFile);
        if (result.Status == LauncherPresetMutationStatus.MissingPreset)
            return LauncherPresetManagementResult.Status("当前启动组已不存在，请重新加载");
        if (!result.IsSaved)
            return LauncherPresetManagementResult.NoChange();

        var preset = result.Preset!;
        return LauncherPresetManagementResult.Reload(
            result.NextPresetId,
            $"已删除启动组: {preset.Name}");
    }
}

public readonly record struct LauncherPresetManagementResult(
    bool ShouldReload,
    string? NextPresetId,
    string? StatusText)
{
    public static LauncherPresetManagementResult NoChange()
    {
        return new LauncherPresetManagementResult(false, null, null);
    }

    public static LauncherPresetManagementResult Status(string statusText)
    {
        return new LauncherPresetManagementResult(false, null, statusText);
    }

    public static LauncherPresetManagementResult Reload(string? nextPresetId, string statusText)
    {
        return new LauncherPresetManagementResult(true, nextPresetId, statusText);
    }
}
