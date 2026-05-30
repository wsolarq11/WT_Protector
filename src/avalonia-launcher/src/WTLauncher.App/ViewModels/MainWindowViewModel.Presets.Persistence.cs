using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private string? LoadPresetFile(string? preferredPresetId)
    {
        LauncherPresetFile presetFile;
        string? error;
        try
        {
            presetFile = LauncherPresetStore.Load(_presetPath);
            error = null;
        }
        catch (Exception ex)
        {
            presetFile = new LauncherPresetFile();
            error = LauncherPersistenceStatus.PresetLoadFailed(ex);
        }

        ApplyPresetFileState(presetFile, preferredPresetId);
        return error;
    }

    private void ApplyPresetFileState(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        Presets.Clear();
        foreach (var preset in presetFile.Presets)
        {
            Presets.Add(new LauncherPresetViewModel(preset));
        }

        var targetPresetId = preferredPresetId ?? presetFile.DefaultPresetId;
        _presetFile = presetFile;
        var selectedPreset = Presets.FirstOrDefault(preset => LauncherPresetIds.Same(preset.Id, targetPresetId))
            ?? Presets.FirstOrDefault();
        SelectedPreset = selectedPreset;
        PresetCount = Presets.Count;
        NotifyPresetManagementCommands();
    }

    private bool SavePresetMutationAndReload(
        LauncherPresetFile snapshot,
        string? preferredPresetId,
        string? loadedPresetId)
    {
        if (SavePresets())
        {
            LoadPresetFile(loadedPresetId);
            return true;
        }

        ApplyPresetFileState(snapshot, preferredPresetId);
        return false;
    }

    private bool SavePresets()
    {
        var saveFailure = LauncherPresetMutationPersistence.Save(_presetFile, _presetPath);
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return false;
        }

        return true;
    }
}
