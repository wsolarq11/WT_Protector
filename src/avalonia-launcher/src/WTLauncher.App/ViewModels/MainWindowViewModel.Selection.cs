using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private CancellationTokenSource? _batchCts;

    public bool IsBatchLaunching => _batchCts is not null;

    private Func<LauncherBatchLaunchConfirmation, Task<bool>> _confirmBatchLaunchAsync =
        static _ => throw new InvalidOperationException("ConfirmBatchLaunchAsync is not bound by the View.");

    public Func<LauncherBatchLaunchConfirmation, Task<bool>> ConfirmBatchLaunchAsync
    {
        get => _confirmBatchLaunchAsync;
        set => _confirmBatchLaunchAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    [RelayCommand(CanExecute = nameof(CanCancelBatch))]
    private void CancelBatch()
    {
        _batchCts?.Cancel();
    }

    private bool CanCancelBatch() => IsBatchLaunching;

    [RelayCommand(CanExecute = nameof(CanSelectVisible))]
    private void SelectVisible()
    {
        DiscardDragReorderPreview();
        var targets = _visibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            ApplyStatusText(
                _visibleEntries.Count == 0
                    ? LauncherSelectionStatus.NoVisibleEntries
                    : LauncherSelectionStatus.VisibleAlreadySelected);
            return;
        }

        if (SetSelection(targets, selected: true))
            ApplyStatusText(LauncherSelectionStatus.VisibleSelected(targets.Count));
    }

    /// <summary>
    /// Clears selection on ALL entries (including hidden ones behind the current
    /// filter). This is a programmatic "deselect everything" — the View data-binds
    /// to this command for the "clear selection" toolbar button.
    ///
    /// For a user-facing "clear visible only" variant, see <see cref="ClearVisibleSelection"/>.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        DiscardDragReorderPreview();
        var targets = GetSelectedEntries();
        if (targets.Count == 0)
        {
            ApplyStatusText(LauncherSelectionStatus.NoSelectedEntriesToClear);
            return;
        }

        if (SetSelection(targets, selected: false))
            ApplyStatusText(LauncherSelectionStatus.Cleared(targets.Count));
    }

    /// <summary>
    /// Clears selection only on VISIBLE entries — the user-facing behavior
    /// that matches "what you see is what you act on."
    /// Call this when the user explicitly wants to clear only filtered results.
    /// </summary>
    public void ClearVisibleSelection()
    {
        DiscardDragReorderPreview();
        var targets = _visibleEntries.Where(entry => entry.IsSelected).ToList();
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
            "已勾选项",
            LauncherSelectionStatus.NoSelectedEntries,
            LauncherSelectionStatus.BatchLaunchCanceled,
            LauncherSelectionStatus.BatchLaunchFailed,
            LauncherSelectionStatus.BatchLaunchCompleted).ConfigureAwait(true);
    }

    private async Task LaunchBatchAsync(
        IReadOnlyList<LauncherEntryViewModel> entries,
        string confirmationTargetLabel,
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

        if (IsBatchLaunching) return;

        DiscardDragReorderPreview();
        _batchCts?.Dispose();
        _batchCts = new CancellationTokenSource();
        try
        {
            OnPropertyChanged(nameof(IsBatchLaunching));
            var selectedEntries = entries.Select(ToLaunchEntry).ToList();
            var preflight = _launchWorkflow.PreflightBatch(
                selectedEntries,
                RequireConfirmationForLaunches);
            if (!await ConfirmBatchLaunchIfRequiredAsync(
                    preflight.Items,
                    preflight.Report,
                    confirmationTargetLabel,
                    canceledStatus,
                    failedStatus).ConfigureAwait(true))
                return;

            var report = _launchWorkflow.StartBatch(preflight, _batchCts.Token);
            var timestamp = CurrentTimestampUtc();
            var startedCount = report.RequestedCount;
            var preflightItems = preflight.Items;
            var entryById = new Dictionary<string, LauncherEntryViewModel>(LauncherEntryIds.Comparer);
            foreach (var e in _entries)
            {
                if (!entryById.TryAdd(e.Id, e))
                {
                    ErrorLog.Write(
                        new InvalidDataException($"Duplicate entry ID in batch launch dictionary: {e.Id}"),
                        "LaunchBatch");
                }
            }
            var failureIndexSet = new HashSet<int>(report.FailureIndices);
            for (var i = 0; i < preflightItems.Count; i++)
            {
                if (failureIndexSet.Contains(i))
                    continue;
                var preflightItem = preflightItems[i];
                entryById.TryGetValue(preflightItem.EntryId, out var entryVm);
                entryVm?.RecordLaunchFromOwner(timestamp);
            }
            if (startedCount > 0 && !SaveConfig())
                return;
            DryRunText = LauncherBatchLaunchReceiptText.Format(preflight, report);
            StatusText = completedStatus(report);
        }
        catch (OperationCanceledException)
        {
            StatusText = "已取消批量启动";
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "BatchLaunch");
            DryRunText = LauncherBatchLaunchReceiptText.Failed(ex);
            StatusText = failedStatus(ex);
        }
        finally
        {
            _batchCts?.Dispose();
            _batchCts = null;
            OnPropertyChanged(nameof(IsBatchLaunching));
        }
    }

    private async Task<bool> ConfirmBatchLaunchIfRequiredAsync(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report,
        string confirmationTargetLabel,
        Func<int, string> canceledStatus,
        Func<Exception, string> failedStatus)
    {
        // Always confirm when preflight reports failures, regardless of RequireConfirmationForLaunches.
        // Also confirm when any individual item's plan requires confirmation (per-entry override).
        if (!RequireConfirmationForLaunches && report.FailedCount == 0 && !items.Any(i => i.Plan.RequiresConfirmation))
            return true;

        bool accepted;
        var confirmation = new LauncherBatchLaunchConfirmation(
            items,
            confirmationTargetLabel,
            report.FailedCount,
            report.FailureMessages);
        try
        {
            accepted = await ConfirmBatchLaunchAsync(confirmation).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "BatchLaunchConfirmation");
            DryRunText = LauncherBatchLaunchReceiptText.ConfirmationFailed(confirmation.Items, ex);
            StatusText = failedStatus(ex);
            return false;
        }

        if (accepted)
            return true;

        DryRunText = LauncherBatchLaunchReceiptText.Canceled(confirmation.Items);
        StatusText = canceledStatus(confirmation.Items.Count);
        return false;
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        var previousSelected = item.ReplaceEntrySelectionFromOwner(selected);
        var snapshot = CaptureSelectionMutationSnapshot(item, previousSelected);
        SaveSelectionChangeOrRestore(snapshot);
    }

    private bool CanOperateOnSelection() => SelectedCount > 0;

    private bool CanSelectVisible() => _visibleEntries.Any(entry => !entry.IsSelected);

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        var snapshot = CaptureSelectionMutationSnapshot(entries);
        try
        {
            foreach (var entry in entries)
            {
                entry.SetSelectionFromOwner(selected);
            }

            return SaveSelectionChangeOrRestore(snapshot);
        }
        catch
        {
            // If a PropertyChanged handler throws mid-loop, restore
            // the pre-mutation state so the ViewModel is not left in an
            // inconsistent half-selected condition.
            snapshot.Restore();
            throw;
        }
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        var snapshot = CaptureSelectionMutationSnapshot(_entries);
        try
        {
            foreach (var entry in _entries)
            {
                entry.SetSelectionFromOwner(selectedIds.Contains(entry.Id));
            }

            return SaveSelectionChangeOrRestore(snapshot);
        }
        catch
        {
            // If a PropertyChanged handler throws mid-loop, restore
            // the pre-mutation state so the ViewModel is not left in an
            // inconsistent half-selected condition.
            snapshot.Restore();
            throw;
        }
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
        SelectVisibleCommand.NotifyCanExecuteChanged();
        ApplyPresetCommand.NotifyCanExecuteChanged();
        CancelBatchCommand.NotifyCanExecuteChanged();
        NotifyPresetManagementCommands();
    }

    private List<LauncherEntryViewModel> GetSelectedEntries() =>
        _entries
            .Where(entry => entry.IsSelected)
            .ToList();

    private List<string> GetSelectedEntryIds()
    {
        var ids = new List<string>(_entries.Count);
        foreach (var entry in _entries)
            if (entry.IsSelected) ids.Add(entry.Id);
        return ids;
    }
}
