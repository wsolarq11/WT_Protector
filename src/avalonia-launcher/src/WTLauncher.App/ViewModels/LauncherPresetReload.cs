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
        LauncherPresetFile presetFile;
        string? error;
        try
        {
            presetFile = LauncherPresetStore.Load(presetPath);
            error = null;
        }
        catch (Exception ex)
        {
            presetFile = new LauncherPresetFile();
            error = $"启动组加载失败: {ex.Message}";
        }

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
