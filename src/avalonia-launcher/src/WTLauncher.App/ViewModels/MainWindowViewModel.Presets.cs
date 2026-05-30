using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public Func<IReadOnlyList<string>, Task<string?>> RequestNewPresetAsync { get; set; } =
        static _ => Task.FromResult<string?>(null);

    public Func<LauncherPresetViewModel, Task<bool>> ConfirmDeletePresetAsync { get; set; } =
        static _ => Task.FromResult(false);

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null)
        {
            ApplyStatusText(LauncherPresetStatus.NoPresetSelected);
            return;
        }

        var selection = BuildPresetSelectionPreview(GetSelectedEntryIds());
        if (selection.MissingConfigIds.Count > 0)
        {
            ApplyStatusText(LauncherPresetStatus.MissingEntryReferences(selection.MissingConfigIds));
            return;
        }

        if (ReplaceSelection(selection.PresetIds))
            ApplyStatusText(LauncherPresetStatus.Applied(selectedPreset.Name, selection.PresetIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        if (SelectedCount == 0)
        {
            StatusText = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        var entryIds = GetSelectedEntryIds();
        var presetName = await RequestNewPresetAsync(entryIds).ConfigureAwait(true);
        if (presetName is null)
        {
            StatusText = LauncherPresetStatus.SaveCanceled;
            return;
        }

        var name = presetName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = LauncherPresetStatus.NameRequired;
            return;
        }

        var preferredPresetId = SelectedPreset?.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.AddPreset(_presetFile, name, entryIds);
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(preset.Id);
        ApplyStatusText(LauncherPresetStatus.Saved(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null)
        {
            StatusText = LauncherPresetStatus.NoPresetSelected;
            return;
        }
        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, selectedPreset.Id))
        {
            StatusText = LauncherPresetStatus.UpdateBlocked(selectedPreset.Name);
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            StatusText = LauncherSelectionStatus.SelectionRequired;
            return;
        }

        var preferredPresetId = selectedPreset.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.UpdatePreset(_presetFile, selectedPreset.Id, selectedPreset.Name, entryIds);
        if (preset is null)
        {
            ApplyPresetFileState(snapshot, preferredPresetId);
            ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
            return;
        }
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(preset.Id);
        ApplyStatusText(LauncherPresetStatus.Updated(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null)
        {
            StatusText = LauncherPresetStatus.NoPresetSelected;
            return;
        }
        if (!LauncherPresetWorkflow.CanDeletePreset(_presetFile, selectedPreset.Id))
        {
            StatusText = LauncherPresetStatus.DeleteBlocked(selectedPreset.Name);
            return;
        }

        var accepted = await ConfirmDeletePresetAsync(selectedPreset).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = LauncherPresetStatus.DeleteCanceled(selectedPreset.Name);
            return;
        }

        var preferredPresetId = selectedPreset.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.DeletePreset(_presetFile, selectedPreset.Id, out var fallbackPresetId);
        if (preset is null)
        {
            ApplyPresetFileState(snapshot, preferredPresetId);
            ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
            return;
        }
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(fallbackPresetId);
        ApplyStatusText(LauncherPresetStatus.Deleted(preset.Name));
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();
    }

    private bool CanApplyPreset()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null || Entries.Count == 0)
            return false;

        return BuildPresetSelectionPreview(GetSelectedEntryIds()).HasChanges;
    }

    private bool CanUpdateSelectedPreset()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null || Entries.Count == 0)
            return false;

        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, selectedPreset.Id))
            return false;

        var selectedEntryIds = GetSelectedEntryIds();
        if (selectedEntryIds.Count == 0)
            return false;

        return BuildPresetSelectionPreview(selectedEntryIds).HasChanges;
    }

    private bool CanDeleteSelectedPreset() =>
        SelectedPreset is { } selectedPreset
        && LauncherPresetWorkflow.CanDeletePreset(_presetFile, selectedPreset.Id);

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(IReadOnlyList<string> selectedEntryIds)
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null)
            return LauncherPresetSelectionPreview.NoPreset(PresetCount);

        return LauncherPresetSelectionPreview.ForPreset(
            selectedPreset.Name,
            selectedEntryIds,
            GetEntryIds(),
            selectedPreset.EntryIds);
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }

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

    private bool SavePresetMutationOrRestore(LauncherPresetFile snapshot, string? preferredPresetId)
    {
        if (SavePresets())
            return true;

        ApplyPresetFileState(snapshot, preferredPresetId);
        return false;
    }

    private bool SavePresets()
    {
        try
        {
            LauncherPresetStore.Save(_presetFile, _presetPath);
            return true;
        }
        catch (Exception ex)
        {
            ApplyStatusText(LauncherPersistenceStatus.PresetSaveFailed(ex));
            return false;
        }
    }
}
