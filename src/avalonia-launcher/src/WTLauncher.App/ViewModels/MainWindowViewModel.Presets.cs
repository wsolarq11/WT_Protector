using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Preset CRUD + apply/preview/launch commands + preset state management.
// Split from MainWindowViewModel.cs as part of 07-09-split-mainviewmodel-stage2.

public sealed partial class MainWindowViewModel
{
    public int GetPresetReferenceCount(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return 0;

        var normalized = EntryId.Normalize(entryId);
        return _presetFile.Presets.Count(preset =>
            preset.EntryIds.Any(id => EntryId.Same(id, normalized)));
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

        var normalizedName = EntryName.Normalize(presetName);
        if (normalizedName.Length == 0)
        {
            StatusText = LauncherPresetStatus.NameRequired;
            return null;
        }

        return normalizedName;
    }

    private (LauncherPresetFile PresetFile, string? SelectedPresetId) CapturePresetMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return (_presetFile, SelectedPreset?.Id);
    }

    private void RestorePresetMutationSnapshot((LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot) =>
        ApplyPresetFileState(snapshot.PresetFile, snapshot.SelectedPresetId);

    private void RestorePresetSnapshotAndReportMissing((LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot)
    {
        RestorePresetMutationSnapshot(snapshot);
        ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();
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
        _presetFile = new LauncherPresetFile
        {
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        ApplyPresetFileState(_presetFile, preferredPresetId);
    }

    private void ApplyPresetFileState(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        var rawTargetId = preferredPresetId ?? presetFile.DefaultPresetId;
        var targetPresetId = PresetId.NormalizeReference(rawTargetId);
        var presetViewModels = presetFile.Presets
            .Select(preset => new LauncherPresetViewModel(preset))
            .ToList();

        // F07: If no valid default preset ID but presets exist, fall back to first preset
        if (targetPresetId.Length == 0 && presetViewModels.Count > 0)
            targetPresetId = presetViewModels[0].Id;

        var selectedPreset = ResolveSelectedPreset(presetViewModels, targetPresetId);

        _presetFile = presetFile;  // F08: write after validation

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

        return presets.FirstOrDefault(preset => PresetId.Same(preset.Id, targetPresetId))
            ?? throw new InvalidDataException(
                $"Launcher selected preset id does not match any preset: {targetPresetId}.");
    }

    private bool SavePresetMutationAndApply(
        (LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot,
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
            ApplyPresetFileState(snapshot.PresetFile, snapshot.SelectedPresetId);
            return false;
        }

        if (PresetId.NormalizeReference(selectedPresetId).Length > 0)
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

    private Func<LauncherPresetSaveTarget, Task<string?>> _requestNewPresetAsync =
        static _ => Task.FromResult<string?>(null);

    private Func<LauncherPresetDeleteTarget, Task<bool>> _confirmDeletePresetAsync =
        static _ => Task.FromResult(false);

    public Func<LauncherPresetSaveTarget, Task<string?>> RequestNewPresetAsync
    {
        get => _requestNewPresetAsync;
        set => _requestNewPresetAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Func<LauncherPresetDeleteTarget, Task<bool>> ConfirmDeletePresetAsync
    {
        get => _confirmDeletePresetAsync;
        set => _confirmDeletePresetAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

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
            StatusText = LauncherPresetStatus.NoPresetEntries(selectedPreset.Name);
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
                StatusText = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, preflight.Report);
                return;
            }

            DryRunText = LauncherBatchLaunchPlanText.Format(preflight.Items);
            StatusText = LauncherPresetStatus.Previewed(selectedPreset.Name, preflight.Items.Count);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "PresetPreview");
            DryRunText = LauncherBatchLaunchPlanText.Failed(ex);
            StatusText = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        DiscardDragReorderPreview();
        if (SelectedCount == 0)
        {
            StatusText = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        var target = new LauncherPresetSaveTarget(GetSelectedEntryIds());
        var presetName = await RequestPresetNameIfRequiredAsync(target).ConfigureAwait(true);
        if (presetName is null)
            return;

        var snapshot = CapturePresetMutationSnapshot();
        LauncherPreset? preset;
        try
        {
            preset = LauncherPresetWorkflow.AddPreset(ref _presetFile, presetName, target.EntryIds);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddPreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = LauncherRefreshStatus.AddGroupFailed(ex);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset))
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

        // Defense-in-depth: skip the update if the selection already matches
        // the preset — avoids unnecessary snapshot capture and file I/O.
        // CanUpdateSelectedPreset already guards against this (via HasChanges),
        // but repeating the check here protects against future callers that
        // might bypass the CanExecute guard.
        var preview = BuildPresetSelectionPreview(selectedPreset, entryIds);
        if (!preview.HasChanges)
        {
            StatusText = LauncherRefreshStatus.GroupAlreadyUpToDate(selectedPreset.Name);
            return;
        }

        var snapshot = CapturePresetMutationSnapshot();
        LauncherPreset? preset;
        try
        {
            preset = LauncherPresetWorkflow.UpdatePreset(ref _presetFile, selectedPreset.Id, selectedPreset.Name, entryIds);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "UpdatePreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = LauncherPresetStatus.UpdateFailed(selectedPreset.Name, ex);
            return;
        }
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset))
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
        if (!LauncherPresetWorkflow.CanDeletePreset(_presetFile, target.PresetId))
        {
            StatusText = LauncherPresetStatus.DeleteBlocked(target.PresetName);
            return;
        }

        // Defer snapshot capture until after user confirmation — avoids
        // wasteful snapshot/restore cycle when the user cancels.
        if (!await ConfirmDeletePresetIfRequiredAsync(target).ConfigureAwait(true))
        {
            return;
        }

        var snapshot = CapturePresetMutationSnapshot();

        LauncherPreset? preset;
        string fallbackPresetId;
        try
        {
            preset = LauncherPresetWorkflow.DeletePreset(ref _presetFile, target.PresetId, out fallbackPresetId);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeletePreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = LauncherRefreshStatus.DeleteGroupFailed(ex);
            return;
        }
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, fallbackPresetId, out var fallbackPreset))
            return;

        ApplyStatusText(fallbackPreset is not null
            ? LauncherPresetStatus.Deleted(target.PresetName, fallbackPreset.Name)
            : LauncherPresetStatus.Deleted(target.PresetName, string.Empty));
    }
}
