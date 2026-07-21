using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WTLauncher.Core.Messaging;

namespace WTLauncher.App.ViewModels;

// Selection + batch launch delegation. Split from MainWindowViewModel.cs as part of
// 07-09-split-mainviewmodel-stage2. Batch launch is delegated to LaunchOrchestrator.

// _batchCts lives in LaunchOrchestrator (which suppresses CA1001 internally);
// MainWindowViewModel does not own any disposable field and needs no suppression.
public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanSelectVisible))]
    private void SelectVisible()
    {
        DiscardDragReorderPreview();
        var targets = _list.MutableVisibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            ApplyStatusText(
                _list.MutableVisibleEntries.Count == 0
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
        var targets = _list.MutableVisibleEntries.Where(entry => entry.IsSelected).ToList();
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
        await _launchOrchestrator.LaunchBatchAsync(
            GetSelectedEntries(),
            "已勾选项",
            LauncherSelectionStatus.NoSelectedEntries,
            LauncherSelectionStatus.BatchLaunchCanceled,
            LauncherSelectionStatus.BatchLaunchFailed,
            LauncherSelectionStatus.BatchLaunchCompleted).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanCancelBatch))]
    private void CancelBatch() => _launchOrchestrator.CancelBatch();

    private bool CanCancelBatch() => _launchOrchestrator.IsBatchLaunching;

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        var previousSelected = item.ReplaceEntrySelectionFromOwner(selected);
        var snapshot = CaptureSelectionMutationSnapshot(item, previousSelected);
        SaveSelectionChangeOrRestore(snapshot);
    }

    private bool CanOperateOnSelection() => SelectedCount > 0;

    private bool CanSelectVisible() => _list.MutableVisibleEntries.Any(entry => !entry.IsSelected);

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
        var snapshot = CaptureSelectionMutationSnapshot(_list.MutableEntries);
        try
        {
            foreach (var entry in _list.MutableEntries)
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
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is null)
            return true;

        snapshot.Restore();
        RefreshSelectionState();
        ApplyStatusText(saveFailure);
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
        WeakReferenceMessenger.Default.Send(
            new SelectionChangedMessage(selectedEntryIds.ToImmutableList()));
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
        _list.MutableEntries
            .Where(entry => entry.IsSelected)
            .ToList();

    private List<string> GetSelectedEntryIds()
    {
        var ids = new List<string>(_list.MutableEntries.Count);
        foreach (var entry in _list.MutableEntries)
            if (entry.IsSelected) ids.Add(entry.Id);
        return ids;
    }
}
