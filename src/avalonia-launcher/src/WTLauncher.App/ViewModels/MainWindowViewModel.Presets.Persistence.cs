using System.Diagnostics.CodeAnalysis;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private void LoadPresetFile(string? preferredPresetId)
    {
        var presetFile = LauncherPresetStore.Load(_presetPath);
        ApplyPresetFileState(presetFile, preferredPresetId);
    }

    private void ApplyPresetFileState(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        _presetFile = presetFile;
        var targetPresetId = LauncherPresetIds.NormalizeReference(preferredPresetId ?? presetFile.DefaultPresetId);
        var presetViewModels = presetFile.Presets
            .Select(preset => new LauncherPresetViewModel(preset))
            .ToList();
        var selectedPreset = ResolveSelectedPreset(presetViewModels, targetPresetId);

        _presets.Clear();
        foreach (var preset in presetViewModels)
            _presets.Add(preset);

        SelectedPreset = selectedPreset;
        PresetCount = _presets.Count;
        NotifyPresetManagementCommands();
    }

    private static LauncherPresetViewModel? ResolveSelectedPreset(
        IReadOnlyList<LauncherPresetViewModel> presets,
        string targetPresetId)
    {
        if (presets.Count == 0)
        {
            if (targetPresetId.Length != 0)
                throw new InvalidDataException(
                    $"Launcher selected preset id is invalid without presets: {targetPresetId}.");

            return null;
        }

        if (targetPresetId.Length == 0)
            throw new InvalidDataException("Launcher selected preset id is required when presets exist.");

        return presets.FirstOrDefault(preset => LauncherPresetIds.Same(preset.Id, targetPresetId))
            ?? throw new InvalidDataException(
                $"Launcher selected preset id does not match any preset: {targetPresetId}.");
    }

    private bool SavePresetMutationAndApply(
        LauncherPresetMutationSnapshot snapshot,
        string selectedPresetId,
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset)
    {
        if (LauncherPresetIds.NormalizeReference(selectedPresetId).Length == 0)
            throw new InvalidOperationException("Launcher preset mutation selected id is required.");

        selectedPreset = null;
        var saveFailure = LauncherPresetMutationPersistence.Save(_presetFile, _presetPath);
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
            return false;
        }

        ApplyPresetFileState(_presetFile, selectedPresetId);
        selectedPreset = SelectedPreset
            ?? throw new InvalidOperationException(
                $"Launcher preset mutation selected id did not resolve: {selectedPresetId}.");
        return true;
    }
}
