using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;

namespace WTLauncher.App.ViewModels;

// Preset command surface. Command bodies live in LauncherPresetWorkflow.

public sealed partial class MainWindowViewModel
{
    public int GetPresetReferenceCount(string entryId) =>
        _presetWorkflow.GetReferenceCount(entryId);

    internal LauncherPresetSelectionPreview BuildPresetSelectionPreview(IReadOnlyList<string> selectedEntryIds) =>
        _presetWorkflow.BuildSelectionPreview(selectedEntryIds);

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        PreviewSelectedPresetCommand.NotifyCanExecuteChanged();
        LaunchSelectedPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }

    private void LoadPresetFile(string? preferredPresetId) =>
        _presetWorkflow.LoadPresetFile(preferredPresetId);

    private bool CanApplyPreset() => _presetWorkflow.CanApplyPreset();

    private bool CanLaunchSelectedPreset() => _presetWorkflow.CanLaunchSelectedPreset();

    private bool CanUpdateSelectedPreset() => _presetWorkflow.CanUpdateSelectedPreset();

    private bool CanDeleteSelectedPreset() => _presetWorkflow.CanDeleteSelectedPreset();

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset() => _presetWorkflow.ApplyPreset();

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private async Task LaunchSelectedPresetAsync() =>
        await _presetWorkflow.LaunchSelectedPresetAsync().ConfigureAwait(true);

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private void PreviewSelectedPreset() => _presetWorkflow.PreviewSelectedPreset();

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        if (SelectedCount == 0)
        {
            StatusContent = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        await _presetWorkflow.SaveAsPresetAsync(GetSelectedEntryIds()).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset() => _presetWorkflow.UpdateSelectedPreset();

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync() =>
        await _presetWorkflow.DeleteSelectedPresetAsync().ConfigureAwait(true);
}
