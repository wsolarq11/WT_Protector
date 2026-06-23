using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
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
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out var selection))
            return;

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
        var preset = LauncherPresetWorkflow.AddPreset(ref _presetFile, presetName, target.EntryIds);
        if (!SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset))
            return;

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

        var snapshot = CapturePresetMutationSnapshot();
        var preset = LauncherPresetWorkflow.UpdatePreset(ref _presetFile, selectedPreset.Id, selectedPreset.Name, entryIds);
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset))
            return;

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

        var snapshot = CapturePresetMutationSnapshot();
        if (!await ConfirmDeletePresetIfRequiredAsync(target).ConfigureAwait(true))
        {
            RestorePresetMutationSnapshot(snapshot);
            return;
        }

        RestorePresetMutationSnapshot(snapshot);
        var preset = LauncherPresetWorkflow.DeletePreset(ref _presetFile, target.PresetId, out var fallbackPresetId);
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, fallbackPresetId, out var fallbackPreset))
            return;

        ApplyStatusText(LauncherPresetStatus.Deleted(
            target.PresetName,
            fallbackPreset.Name));
    }
}
