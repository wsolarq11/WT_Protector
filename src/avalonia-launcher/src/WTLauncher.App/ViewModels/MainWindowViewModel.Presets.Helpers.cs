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

    private async Task<bool> ConfirmDeletePresetIfRequiredAsync(LauncherPresetDeleteTarget target)
    {
        bool accepted;
        try
        {
            accepted = await ConfirmDeletePresetAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            StatusText = LauncherPresetStatus.DeleteConfirmationFailed(target.PresetName, ex);
            return false;
        }

        if (accepted)
            return true;

        StatusText = LauncherPresetStatus.DeleteCanceled(target.PresetName);
        return false;
    }

    private async Task<string?> RequestPresetNameIfRequiredAsync(LauncherPresetSaveTarget target)
    {
        string? presetName;
        try
        {
            presetName = await RequestNewPresetAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            StatusText = LauncherPresetStatus.SaveDialogFailed(ex);
            return null;
        }

        if (presetName is null)
        {
            StatusText = LauncherPresetStatus.SaveCanceled;
            return null;
        }

        var normalizedName = LauncherEntryText.Normalize(presetName);
        if (normalizedName.Length == 0)
        {
            StatusText = LauncherPresetStatus.NameRequired;
            return null;
        }

        return normalizedName;
    }

    private LauncherPresetMutationSnapshot CapturePresetMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return LauncherPresetMutationSnapshot.Capture(_presetFile, SelectedPreset?.Id);
    }

    private void RestorePresetMutationSnapshot(LauncherPresetMutationSnapshot snapshot) =>
        ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);

    private void RestorePresetSnapshotAndReportMissing(LauncherPresetMutationSnapshot snapshot)
    {
        RestorePresetMutationSnapshot(snapshot);
        ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();

        // L33: Persist last-used preset ID so it is restored on next launch.
        // Skip persistence during bulk-load/restore operations (handled by
        // _isLoadingPresets guard in MainWindowViewModel.Presets.Persistence.cs).
        if (_isLoadingPresets || value is null)
            return;

        // Use the shared helper — no success status to avoid overwriting the
        // status bar when the user simply clicks a different preset.
        TryPersistSettingsChange(s => s with { LastUsedPresetId = value.Id });
    }

    /// <summary>
    /// L34: Always allow apply when a preset with entries is selected, even if the
    /// current selection exactly matches — re-applying a preset also forces a config
    /// save, which is useful after external modifications.
    /// </summary>
    private bool CanApplyPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.MissingConfigIds.Count == 0
        && selection.PresetIds.Count > 0;

    private bool CanLaunchSelectedPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.MissingConfigIds.Count == 0
        && selection.PresetIds.Count > 0;

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
        if (!TryGetSelectedPreset(out selectedPreset) || _entries.Count == 0)
            return false;

        selection = BuildPresetSelectionPreview(selectedPreset, GetSelectedEntryIds());
        return true;
    }

    private List<LauncherEntryViewModel> GetPresetEntries(IEnumerable<string> entryIds)
    {
        var entriesById = new Dictionary<string, LauncherEntryViewModel>(LauncherEntryIds.Comparer);
        foreach (var entry in _entries)
        {
            if (!entriesById.ContainsKey(entry.Id))
                entriesById[entry.Id] = entry;
        }

        var uniqueIds = LauncherEntryIds.ToRequiredUniqueList(entryIds, nameof(entryIds));
        var result = new List<LauncherEntryViewModel>(uniqueIds.Count);
        foreach (var id in uniqueIds)
        {
            // M11: Use TryGetValue instead of indexer to give a clear error when
            // a preset references an entry ID that does not exist in _entries.
            if (!entriesById.TryGetValue(id, out var entry))
                throw new InvalidDataException($"Preset entry ID '{id}' does not match any loaded entry.");

            result.Add(entry);
        }

        return result;
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        PreviewSelectedPresetCommand.NotifyCanExecuteChanged();
        LaunchSelectedPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }
}
