using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetStateWorkflow
{
    public static string? Reload(
        string presetPath,
        ObservableCollection<LauncherPresetViewModel> presets,
        string? preferredPresetId,
        Action<LauncherPresetState> applyState,
        Action notifyPresetManagementCommands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presetPath);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(applyState);
        ArgumentNullException.ThrowIfNull(notifyPresetManagementCommands);

        var result = LauncherPresetReload.Load(presetPath, presets, preferredPresetId);
        ApplyState(
            new LauncherPresetState(result.PresetFile, result.SelectedPreset, result.PresetCount),
            applyState,
            notifyPresetManagementCommands);
        return result.ErrorMessage;
    }

    public static void ApplySnapshot(
        LauncherPresetFile presetFile,
        ObservableCollection<LauncherPresetViewModel> presets,
        string? preferredPresetId,
        Action<LauncherPresetState> applyState,
        Action notifyPresetManagementCommands)
    {
        ArgumentNullException.ThrowIfNull(presetFile);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(applyState);
        ArgumentNullException.ThrowIfNull(notifyPresetManagementCommands);

        var selectedPreset = LauncherPresetCatalog.Apply(presetFile, presets, preferredPresetId);
        ApplyState(
            new LauncherPresetState(presetFile, selectedPreset, presets.Count),
            applyState,
            notifyPresetManagementCommands);
    }

    private static void ApplyState(
        LauncherPresetState state,
        Action<LauncherPresetState> applyState,
        Action notifyPresetManagementCommands)
    {
        applyState(state);
        notifyPresetManagementCommands();
    }
}

public sealed record LauncherPresetState(
    LauncherPresetFile PresetFile,
    LauncherPresetViewModel? SelectedPreset,
    int PresetCount);
