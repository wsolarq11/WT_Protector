using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Preset CRUD + apply/preview/launch commands + preset state management.
// Split from MainWindowViewModel.cs as part of 07-09-split-mainviewmodel-stage2.

public sealed partial class MainWindowViewModel
{
    public int GetPresetReferenceCount(string entryId) =>
        _presetManager.GetReferenceCount(entryId, _presetService);

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

        StatusContent = LauncherPresetStatus.NoPresetSelected;
        return null;
    }

    private bool TryGetSelectedPreset([NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset)
    {
        selectedPreset = SelectedPreset;
        return selectedPreset is not null;
    }

    private async Task<bool> ConfirmDeletePresetIfRequiredAsync(LauncherPresetDeleteTarget target)
    {
        var result = await _presetService.ConfirmDeletePresetAsync(target).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LauncherPresetDeleteTarget>:
                return true;
            case ServiceCanceled<LauncherPresetDeleteTarget> canceled:
                StatusContent = canceled.Status;
                return false;
            case ServiceFailure<LauncherPresetDeleteTarget> failure:
                StatusContent = failure.Status;
                return false;
            default:
                throw new UnreachableException();
        }
    }

    private async Task<string?> RequestPresetNameIfRequiredAsync(LauncherPresetSaveTarget target)
    {
        var result = await _presetService.RequestNewPresetAsync(
            target, _presetManager.Presets.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<string> success:
                return success.Value;
            case ServiceCanceled<string> canceled:
                StatusContent = canceled.Status;
                return null;
            case ServiceFailure<string> failure:
                StatusContent = failure.Status;
                return null;
            default:
                throw new UnreachableException();
        }
    }

    private (LauncherPresetFile PresetFile, string? SelectedPresetId) CapturePresetMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return _presetManager.CaptureSnapshot();
    }

    private void RestorePresetMutationSnapshot((LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot) =>
        _presetManager.Load(snapshot.PresetFile, snapshot.SelectedPresetId);

    private void RestorePresetSnapshotAndReportMissing((LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot)
    {
        _presetManager.Load(snapshot.PresetFile, snapshot.SelectedPresetId);
    }

    /// <summary>
    /// Always allow apply when a preset with entries is selected, even if the
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

        if (!_presetManager.CanUpdatePreset(selectedPreset.Id, _presetService))
            return false;

        return selection.SelectedIds.Count > 0 && selection.HasChanges;
    }

    private bool CanDeleteSelectedPreset() =>
        TryGetSelectedPreset(out var selectedPreset)
        && _presetManager.CanDeletePreset(selectedPreset.Id, _presetService);

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
        return _presetService.BuildSelectionPreview(
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
        var entriesById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
        foreach (var entry in _entries)
        {
            if (!entriesById.ContainsKey(entry.Id))
                entriesById[entry.Id] = entry;
        }

        var uniqueIds = EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds));
        var result = new List<LauncherEntryViewModel>(uniqueIds.Count);
        foreach (var id in uniqueIds)
        {
            // Use TryGetValue instead of indexer to give a clear error when
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

    private void LoadPresetFile(string? preferredPresetId)
    {
        LauncherStoreFile storeFile;
        try { storeFile = LauncherStore.Load(_storePath); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "LoadPresetFile");
            storeFile = new LauncherStoreFile();
        }
        var presetFile = new LauncherPresetFile
        {
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        _presetManager.Load(presetFile, preferredPresetId);
    }

    private bool SavePresetMutationAndApply(
        (LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot,
        LauncherPresetFile newPresetFile,
        string selectedPresetId,
        out LauncherPresetViewModel? selectedPreset)
    {
        selectedPreset = null;
        var saveFailure = LauncherMutationTransaction.Commit(
            snapshot,
            () =>
            {
                _presetManager.ApplyPresetMutation(newPresetFile, selectedPresetId);
                return SaveStoreFile(BuildStoreFile());
            },
            rollbackSnapshot => _presetManager.Load(
                rollbackSnapshot.PresetFile,
                rollbackSnapshot.SelectedPresetId));
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return false;
        }

        selectedPreset = SelectedPreset;
        return true;
    }

    // RequestNewPresetAsync and ConfirmDeletePresetAsync are delegated through IDialogService.

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        DiscardDragReorderPreview();

        if (!TryGetApplicablePresetSelection(out var selectedPreset, out var selection))
        {
            // Update button text to reflect the failure reason (e.g. "检查启动组" for missing entries).
            RefreshSelectionState();
            return;
        }

        if (ReplaceSelection(selection.PresetIds))
            ApplyStatusText(LauncherPresetStatus.Applied(selectedPreset.Name, selection.PresetIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private async Task LaunchSelectedPresetAsync()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        await LaunchBatchAsync(
            GetPresetEntries(selectedPreset.EntryIds),
            $"启动组“{selectedPreset.Name}”条目",
            LauncherPresetStatus.NoPresetEntries(selectedPreset.Name),
            count => LauncherPresetStatus.LaunchCanceled(selectedPreset.Name, count),
            ex => LauncherPresetStatus.LaunchFailed(selectedPreset.Name, ex),
            report => LauncherPresetStatus.Launched(selectedPreset.Name, report)).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private void PreviewSelectedPreset()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        var entries = GetPresetEntries(selectedPreset.EntryIds);
        if (entries.Count == 0)
        {
            StatusContent = LauncherPresetStatus.NoPresetEntries(selectedPreset.Name);
            return;
        }

        DiscardDragReorderPreview();
        try
        {
            var preflight = _launchWorkflow.PreflightBatch(
                entries.Select(ToLaunchEntry),
                RequireConfirmationForLaunches);
            if (preflight.Report.FailedCount > 0)
            {
                DryRunText = LauncherBatchLaunchPlanText.FailedPreflight(preflight);
                StatusContent = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, preflight.Report);
                return;
            }

            DryRunText = LauncherBatchLaunchPlanText.Format(preflight.Items);
            StatusContent = LauncherPresetStatus.Previewed(selectedPreset.Name, preflight.Items.Count);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "PresetPreview");
            DryRunText = LauncherBatchLaunchPlanText.Failed(ex);
            StatusContent = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        DiscardDragReorderPreview();
        if (SelectedCount == 0)
        {
            StatusContent = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        var target = new LauncherPresetSaveTarget(GetSelectedEntryIds());
        var presetName = await RequestPresetNameIfRequiredAsync(target).ConfigureAwait(true);
        if (presetName is null)
            return;

        var snapshot = _presetManager.CaptureSnapshot();
        var addResult = _presetService.AddPreset(_presetManager.File, presetName, target.EntryIds);
        if (addResult is not ServiceSuccess<PresetMutation> addSuccess)
        {
            _presetManager.Load(snapshot.File, snapshot.SelectedPresetId);
            StatusContent = addResult switch
            {
                ServiceCanceled<PresetMutation> canceled => canceled.Status,
                ServiceFailure<PresetMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            };
            return;
        }
        var preset = addSuccess.Value.Preset;
        if (!SavePresetMutationAndApply(snapshot, addSuccess.Value.PresetFile, preset.Id, out var savedPreset))
            return;

        if (savedPreset is null) return;
        ApplyStatusText(LauncherPresetStatus.Saved(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        if (!_presetManager.CanUpdatePreset(selectedPreset.Id, _presetService))
        {
            StatusContent = LauncherPresetStatus.UpdateBlocked(selectedPreset.Name);
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            StatusContent = LauncherSelectionStatus.SelectionRequired;
            return;
        }

        // Defense-in-depth: skip the update if the selection already matches
        // the preset — avoids unnecessary snapshot capture and file I/O.
        // CanUpdateSelectedPreset already guards against this (via HasChanges),
        // but repeating the check here protects against future callers that
        // might bypass the CanExecute guard.
        var preview = BuildPresetSelectionPreview(selectedPreset, entryIds);
        if (!preview.HasChanges)
        {
            StatusContent = LauncherRefreshStatus.GroupAlreadyUpToDate(selectedPreset.Name);
            return;
        }

        var snapshot = _presetManager.CaptureSnapshot();
        var updateResult = _presetService.UpdatePreset(_presetManager.File, selectedPreset.Id, selectedPreset.Name, entryIds);
        if (updateResult is not ServiceSuccess<PresetMutation> updateSuccess)
        {
            _presetManager.Load(snapshot.File, snapshot.SelectedPresetId);
            StatusContent = updateResult switch
            {
                ServiceCanceled<PresetMutation> canceled => canceled.Status,
                ServiceFailure<PresetMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            };
            return;
        }
        var preset = updateSuccess.Value.Preset;
        if (!SavePresetMutationAndApply(snapshot, updateSuccess.Value.PresetFile, preset.Id, out var savedPreset))
            return;

        if (savedPreset is null) return;
        ApplyStatusText(LauncherPresetStatus.Updated(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        DiscardDragReorderPreview();
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        var target = new LauncherPresetDeleteTarget(
            selectedPreset.Id,
            selectedPreset.Name,
            selectedPreset.EntryIds);
        if (!_presetManager.CanDeletePreset(target.PresetId, _presetService))
        {
            StatusContent = LauncherPresetStatus.DeleteBlocked(target.PresetName);
            return;
        }

        // Defer snapshot capture until after user confirmation — avoids
        // wasteful snapshot/restore cycle when the user cancels.
        if (!await ConfirmDeletePresetIfRequiredAsync(target).ConfigureAwait(true))
        {
            return;
        }

        var snapshot = _presetManager.CaptureSnapshot();

        var deleteResult = _presetService.DeletePreset(_presetManager.File, target.PresetId);
        if (deleteResult is not ServiceSuccess<PresetDeleteMutation> deleteSuccess)
        {
            _presetManager.Load(snapshot.File, snapshot.SelectedPresetId);
            StatusContent = deleteResult switch
            {
                ServiceCanceled<PresetDeleteMutation> canceled => canceled.Status,
                ServiceFailure<PresetDeleteMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            };
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, deleteSuccess.Value.PresetFile, deleteSuccess.Value.FallbackPresetId, out var fallbackPreset))
            return;

        ApplyStatusText(fallbackPreset is not null
            ? LauncherPresetStatus.Deleted(target.PresetName, fallbackPreset.Name)
            : LauncherPresetStatus.Deleted(target.PresetName, string.Empty));
    }
}
