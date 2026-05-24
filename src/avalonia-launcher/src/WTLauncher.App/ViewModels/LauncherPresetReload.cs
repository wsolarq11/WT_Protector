using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherPresetReload
{
    public static LauncherPresetReloadResult Load(
        string presetPath,
        ObservableCollection<LauncherPresetViewModel> presets,
        string? preferredPresetId)
    {
        var error = LauncherPresetPersistence.Load(presetPath, out var presetFile);
        var selectedPreset = LauncherPresetCatalog.Apply(presetFile, presets, preferredPresetId);
        return new LauncherPresetReloadResult(
            presetFile,
            selectedPreset,
            presets.Count,
            error);
    }
}

public sealed record LauncherPresetReloadResult(
    LauncherPresetFile PresetFile,
    LauncherPresetViewModel? SelectedPreset,
    int PresetCount,
    string? ErrorMessage);
