using System.Diagnostics.CodeAnalysis;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private void LoadPresetFile(string? preferredPresetId)
    {
        var loadResult = _store.Load(_storePath);
        var storeFile = loadResult.Match(
            ok: f => f,
            err: _ => new LauncherStoreFile());
        _presetFile = new LauncherPresetFile
        {
            SchemaVersion = 1,
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        ApplyPresetFileState(_presetFile, preferredPresetId);
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
        out LauncherPresetViewModel? selectedPreset)
    {
        // When no preset remains (e.g., deleting the last preset), selectedPresetId
        // is empty and SelectedPreset resolves to null — the dropdown shows nothing.
        selectedPreset = null;
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
            return false;
        }

        if (LauncherPresetIds.NormalizeReference(selectedPresetId).Length > 0)
        {
            ApplyPresetFileState(_presetFile, selectedPresetId);
            selectedPreset = SelectedPreset;
        }
        else
        {
            ApplyPresetFileState(_presetFile, null);
        }
        return true;
    }
}
