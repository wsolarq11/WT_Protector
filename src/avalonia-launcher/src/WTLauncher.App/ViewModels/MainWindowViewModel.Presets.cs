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
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out var selection))
            return;

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
        var presetName = await RequestPresetNameIfRequiredAsync(entryIds).ConfigureAwait(true);
        if (presetName is null)
            return;

        var snapshot = CapturePresetMutationSnapshot();
        var preset = LauncherPresetWorkflow.AddPreset(_presetFile, presetName, entryIds);
        if (!SavePresetMutationAndReload(snapshot, preset.Id))
            return;

        ApplyStatusText(LauncherPresetStatus.Saved(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

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

        var snapshot = CapturePresetMutationSnapshot();
        var preset = LauncherPresetWorkflow.UpdatePreset(_presetFile, selectedPreset.Id, selectedPreset.Name, entryIds);
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndReload(snapshot, preset.Id))
            return;

        ApplyStatusText(LauncherPresetStatus.Updated(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        if (!LauncherPresetWorkflow.CanDeletePreset(_presetFile, selectedPreset.Id))
        {
            StatusText = LauncherPresetStatus.DeleteBlocked(selectedPreset.Name);
            return;
        }

        if (!await ConfirmDeletePresetIfRequiredAsync(selectedPreset).ConfigureAwait(true))
            return;

        var snapshot = CapturePresetMutationSnapshot();
        var preset = LauncherPresetWorkflow.DeletePreset(_presetFile, selectedPreset.Id, out var fallbackPresetId);
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndReload(snapshot, fallbackPresetId))
            return;

        ApplyStatusText(LauncherPresetStatus.Deleted(preset.Name));
    }
}
