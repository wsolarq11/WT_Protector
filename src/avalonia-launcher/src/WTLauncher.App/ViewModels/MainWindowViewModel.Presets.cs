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

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private async Task LaunchSelectedPresetAsync()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        await LaunchBatchAsync(
            GetPresetEntries(selectedPreset.EntryIds),
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
                DryRunText = string.Join(Environment.NewLine, preflight.Report.FailureMessages);
                StatusText = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, preflight.Report);
                return;
            }

            DryRunText = LauncherBatchLaunchPlanText.Format(preflight.Items);
            StatusText = LauncherPresetStatus.Previewed(selectedPreset.Name, preflight.Items.Count);
        }
        catch (Exception ex)
        {
            DryRunText = ex.Message;
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

        var entryIds = GetSelectedEntryIds();
        var presetName = await RequestPresetNameIfRequiredAsync(entryIds).ConfigureAwait(true);
        if (presetName is null)
            return;

        var snapshot = CapturePresetMutationSnapshot();
        var preset = LauncherPresetWorkflow.AddPreset(_presetFile, presetName, entryIds);
        if (!SavePresetMutationAndApply(snapshot, preset.Id))
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
        if (!SavePresetMutationAndApply(snapshot, preset.Id))
            return;

        ApplyStatusText(LauncherPresetStatus.Updated(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        DiscardDragReorderPreview();
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
        if (!SavePresetMutationAndApply(snapshot, fallbackPresetId))
            return;

        ApplyStatusText(LauncherPresetStatus.Deleted(preset.Name));
    }
}
