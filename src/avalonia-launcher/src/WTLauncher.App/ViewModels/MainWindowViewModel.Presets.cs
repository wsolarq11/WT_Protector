using System.Diagnostics.CodeAnalysis;
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

    private bool TryGetApplicablePresetSelection(
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset,
        [NotNullWhen(true)] out LauncherPresetSelectionPreview? selection)
    {
        selectedPreset = GetSelectedPresetOrReportMissing();
        selection = null;
        if (selectedPreset is null)
            return false;

        selection = BuildPresetSelectionPreview(GetSelectedEntryIds());
        if (selection.MissingConfigIds.Count > 0)
        {
            ApplyStatusText(LauncherPresetStatus.MissingEntryReferences(selection.MissingConfigIds));
            return false;
        }

        return true;
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

    private LauncherPresetViewModel? GetSelectedPresetOrReportMissing()
    {
        if (TryGetSelectedPreset(out var selectedPreset))
            return selectedPreset;

        StatusText = LauncherPresetStatus.NoPresetSelected;
        return null;
    }

    private bool TryGetSelectedPreset([NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset)
    {
        selectedPreset = SelectedPreset;
        return selectedPreset is not null;
    }

    private async Task<bool> ConfirmDeletePresetIfRequiredAsync(LauncherPresetViewModel selectedPreset)
    {
        var accepted = await ConfirmDeletePresetAsync(selectedPreset).ConfigureAwait(true);
        if (accepted)
            return true;

        StatusText = LauncherPresetStatus.DeleteCanceled(selectedPreset.Name);
        return false;
    }

    private async Task<string?> RequestPresetNameIfRequiredAsync(IReadOnlyList<string> entryIds)
    {
        var presetName = await RequestNewPresetAsync(entryIds).ConfigureAwait(true);
        if (presetName is null)
        {
            StatusText = LauncherPresetStatus.SaveCanceled;
            return null;
        }

        var normalizedName = LauncherPresetWorkflow.NormalizeName(presetName);
        if (normalizedName.Length == 0)
        {
            StatusText = LauncherPresetStatus.NameRequired;
            return null;
        }

        return normalizedName;
    }

    private LauncherPresetMutationSnapshot CapturePresetMutationSnapshot() =>
        LauncherPresetMutationSnapshot.Capture(_presetFile, SelectedPreset?.Id);

    private void RestorePresetSnapshotAndReportMissing(LauncherPresetMutationSnapshot snapshot)
    {
        ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();
    }

    private bool CanApplyPreset()
    {
        if (!TryGetSelectedPreset(out _) || Entries.Count == 0)
            return false;

        return BuildPresetSelectionPreview(GetSelectedEntryIds()).HasChanges;
    }

    private bool CanUpdateSelectedPreset()
    {
        if (!TryGetSelectedPreset(out var selectedPreset) || Entries.Count == 0)
            return false;

        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, selectedPreset.Id))
            return false;

        var selectedEntryIds = GetSelectedEntryIds();
        if (selectedEntryIds.Count == 0)
            return false;

        return BuildPresetSelectionPreview(selectedEntryIds).HasChanges;
    }

    private bool CanDeleteSelectedPreset() =>
        TryGetSelectedPreset(out var selectedPreset)
        && LauncherPresetWorkflow.CanDeletePreset(_presetFile, selectedPreset.Id);

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(IReadOnlyList<string> selectedEntryIds)
    {
        if (!TryGetSelectedPreset(out var selectedPreset))
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

}
