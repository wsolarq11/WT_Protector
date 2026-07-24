using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features.Presets;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 预设状态管理 — _presetFile, _presets, _selectedPreset。
/// 失败半径: Large — 状态不一致会导致预设操作异常。
/// </summary>
public sealed partial class PresetManager : ObservableObject
{
    private LauncherPresetFile _presetFile = new();
    private readonly ObservableCollection<LauncherPresetViewModel> _presets = [];

    [ObservableProperty]
    private LauncherPresetViewModel? _selectedPreset;

    public ReadOnlyObservableCollection<LauncherPresetViewModel> Presets { get; }
    public int PresetCount => _presets.Count;
    public LauncherPresetFile File => _presetFile;

    public event Action? FileChanged;

    public PresetManager()
    {
        Presets = new ReadOnlyObservableCollection<LauncherPresetViewModel>(_presets);
    }

    public void Load(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        // Transactional: restore _presetFile if ApplyPresetFileState throws.
        // Prevents ghost state where _presetFile is updated but _presets/SelectedPreset
        // reflect a different file. ApplyPresetFileState still clears+repopulates
        // _presets before ResolveSelectedPreset (rollback callers depend on this).
        var previousFile = _presetFile;
        _presetFile = presetFile;
        try
        {
            ApplyPresetFileState(preferredPresetId);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            _presetFile = previousFile;
            throw;
        }
    }

    public (LauncherPresetFile File, string? SelectedPresetId) CaptureSnapshot()
    {
        return (_presetFile, SelectedPreset?.Id);
    }

    public void ApplyPresetMutation(LauncherPresetFile updatedFile, string? targetPresetId)
    {
        _presetFile = updatedFile;
        ApplyPresetFileState(targetPresetId);
    }

    public int GetReferenceCount(string entryId, IPresetService service) =>
        service.CountEntryReferences(_presetFile, entryId);

    public bool CanUpdatePreset(string presetId, IPresetService service) =>
        service.CanUpdatePreset(_presetFile, presetId);

    public bool CanDeletePreset(string presetId, IPresetService service) =>
        service.CanDeletePreset(_presetFile, presetId);

    public static LauncherPresetSelectionPreview BuildSelectionPreview(
        LauncherPresetViewModel preset, IReadOnlyList<string> selectedEntryIds,
        IEnumerable<string> allEntryIds, IPresetService service) =>
        service.BuildSelectionPreview(preset.Name, selectedEntryIds, allEntryIds, preset.EntryIds);

    private void ApplyPresetFileState(string? preferredPresetId)
    {
        var rawTargetId = preferredPresetId ?? _presetFile.DefaultPresetId;
        var targetPresetId = PresetId.NormalizeReference(rawTargetId);
        var presetViewModels = _presetFile.Presets
            .Select(preset => new LauncherPresetViewModel(preset))
            .ToList();

        // F07: If no valid default preset ID but presets exist, fall back to first preset
        if (targetPresetId.Length == 0 && presetViewModels.Count > 0)
            targetPresetId = presetViewModels[0].Id;

        _presets.Clear();
        foreach (var preset in presetViewModels)
            _presets.Add(preset);

        var selectedPreset = ResolveSelectedPreset(presetViewModels, targetPresetId);
        SelectedPreset = selectedPreset;
        FileChanged?.Invoke();
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

        return presets.FirstOrDefault(preset => PresetId.Same(preset.Id, targetPresetId))
            ?? throw new InvalidDataException(
                $"Launcher selected preset id does not match any preset: {targetPresetId}.");
    }
}
