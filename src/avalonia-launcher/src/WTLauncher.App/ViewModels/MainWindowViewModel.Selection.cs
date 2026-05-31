using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public Func<IReadOnlyList<LauncherBatchLaunchItem>, Task<bool>> ConfirmBatchLaunchAsync { get; set; } =
        static _ => Task.FromResult(false);

    [RelayCommand]
    private void SelectVisible()
    {
        var targets = VisibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            ApplyStatusText(
                VisibleEntries.Count == 0
                    ? LauncherSelectionStatus.NoVisibleEntries
                    : LauncherSelectionStatus.VisibleAlreadySelected);
            return;
        }

        if (SetSelection(targets, selected: true))
            ApplyStatusText(LauncherSelectionStatus.VisibleSelected(targets.Count));
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        var targets = GetSelectedEntries();
        if (targets.Count == 0)
        {
            ApplyStatusText(LauncherSelectionStatus.NoSelectedEntriesToClear);
            return;
        }

        if (SetSelection(targets, selected: false))
            ApplyStatusText(LauncherSelectionStatus.Cleared(targets.Count));
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task LaunchSelectedAsync()
    {
        await LaunchBatchAsync(
            GetSelectedEntries(),
            LauncherSelectionStatus.NoSelectedEntries,
            LauncherSelectionStatus.BatchLaunchCanceled,
            LauncherSelectionStatus.BatchLaunchFailed,
            LauncherSelectionStatus.BatchLaunchCompleted).ConfigureAwait(true);
    }

    private async Task LaunchBatchAsync(
        IReadOnlyList<LauncherEntryViewModel> entries,
        string emptyStatus,
        Func<int, string> canceledStatus,
        Func<Exception, string> failedStatus,
        Func<LauncherBatchLaunchReport, string> completedStatus)
    {
        if (entries.Count == 0)
        {
            StatusText = emptyStatus;
            return;
        }

        DiscardDragReorderPreview();
        try
        {
            var selectedEntries = entries.Select(ToLaunchEntry).ToList();
            var preflight = _launchWorkflow.PreflightBatch(
                selectedEntries,
                RequireConfirmationForLaunches);
            if (preflight.Report.FailedCount == 0 &&
                !await ConfirmBatchLaunchIfRequiredAsync(preflight.Items, canceledStatus).ConfigureAwait(true))
                return;

            var report = _launchWorkflow.StartBatch(preflight);
            DryRunText = LauncherBatchLaunchReceiptText.Format(preflight, report);
            StatusText = completedStatus(report);
        }
        catch (Exception ex)
        {
            StatusText = failedStatus(ex);
        }
    }

    private async Task<bool> ConfirmBatchLaunchIfRequiredAsync(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        Func<int, string> canceledStatus)
    {
        if (!RequireConfirmationForLaunches)
            return true;

        var accepted = await ConfirmBatchLaunchAsync(items).ConfigureAwait(true);
        if (accepted)
            return true;

        StatusText = canceledStatus(items.Count);
        return false;
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        var previousSelected = item.ReplaceEntrySelectionFromOwner(selected);
        var snapshot = CaptureSelectionMutationSnapshot(item, previousSelected);
        SaveSelectionChangeOrRestore(snapshot);
    }

    private bool CanOperateOnSelection() => SelectedCount > 0;

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        var snapshot = CaptureSelectionMutationSnapshot(entries);
        foreach (var entry in entries)
        {
            entry.SetSelectionFromOwner(selected);
        }

        return SaveSelectionChangeOrRestore(snapshot);
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        var snapshot = CaptureSelectionMutationSnapshot(Entries);
        foreach (var entry in Entries)
        {
            entry.SetSelectionFromOwner(selectedIds.Contains(entry.Id));
        }

        return SaveSelectionChangeOrRestore(snapshot);
    }

    private LauncherSelectionMutationSnapshot CaptureSelectionMutationSnapshot(
        LauncherEntryViewModel item,
        bool previousSelected)
    {
        DiscardDragReorderPreview();
        return LauncherSelectionMutationSnapshot.Capture(item, previousSelected);
    }

    private LauncherSelectionMutationSnapshot CaptureSelectionMutationSnapshot(
        IEnumerable<LauncherEntryViewModel> entries)
    {
        DiscardDragReorderPreview();
        return LauncherSelectionMutationSnapshot.Capture(entries);
    }

    private bool SaveSelectionChangeOrRestore(
        LauncherSelectionMutationSnapshot snapshot)
    {
        RefreshSelectionState();
        if (SaveConfig())
            return true;

        snapshot.Restore();
        RefreshSelectionState();
        return false;
    }

    private void RefreshSelectionState()
    {
        var selectedEntryIds = GetSelectedEntryIds();
        var preview = BuildPresetSelectionPreview(selectedEntryIds);
        SelectedCount = selectedEntryIds.Count;
        ApplyPresetButtonText = preview.ApplyButtonText;
        PresetSelectionStatusText = preview.StatusText;
        NotifySelectionCommands();
    }

    private void NotifySelectionCommands()
    {
        LaunchSelectedCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        ApplyPresetCommand.NotifyCanExecuteChanged();
        NotifyPresetManagementCommands();
    }

    private List<LauncherEntryViewModel> GetSelectedEntries() =>
        Entries
            .Where(entry => entry.IsSelected)
            .ToList();

    private List<string> GetSelectedEntryIds() =>
        GetSelectedEntries()
            .Select(entry => entry.Id)
            .ToList();
}
