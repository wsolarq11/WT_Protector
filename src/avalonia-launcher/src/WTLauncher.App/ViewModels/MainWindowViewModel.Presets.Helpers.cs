using System.Diagnostics.CodeAnalysis;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
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

    private bool CanApplyPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.HasChanges;

    private bool CanUpdateSelectedPreset()
    {
        if (!TryBuildPresetSelectionPreviewForCommand(out var selectedPreset, out var selection))
            return false;

        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, selectedPreset.Id))
            return false;

        return selection.SelectedIds.Count > 0 && selection.HasChanges;
    }

    private bool CanDeleteSelectedPreset() =>
        TryGetSelectedPreset(out var selectedPreset)
        && LauncherPresetWorkflow.CanDeletePreset(_presetFile, selectedPreset.Id);

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(IReadOnlyList<string> selectedEntryIds)
    {
        if (!TryGetSelectedPreset(out var selectedPreset))
            return LauncherPresetSelectionPreview.NoPreset(PresetCount);

        return BuildPresetSelectionPreview(selectedPreset, selectedEntryIds);
    }

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(
        LauncherPresetViewModel selectedPreset,
        IReadOnlyList<string> selectedEntryIds)
    {
        return LauncherPresetSelectionPreview.ForPreset(
            selectedPreset.Name,
            selectedEntryIds,
            GetEntryIds(),
            selectedPreset.EntryIds);
    }

    private bool TryBuildPresetSelectionPreviewForCommand(
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset,
        [NotNullWhen(true)] out LauncherPresetSelectionPreview? selection)
    {
        selectedPreset = null;
        selection = null;
        if (!TryGetSelectedPreset(out selectedPreset) || Entries.Count == 0)
            return false;

        selection = BuildPresetSelectionPreview(selectedPreset, GetSelectedEntryIds());
        return true;
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }
}
