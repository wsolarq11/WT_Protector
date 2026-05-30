using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    public Func<IReadOnlyList<LaunchEntry>, Task<bool>> ConfirmBatchLaunchAsync { get; set; } =
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
        var selected = GetSelectedEntries();
        if (selected.Count == 0)
        {
            StatusText = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        try
        {
            var selectedEntries = selected.Select(ToLaunchEntry).ToList();
            if (RequireConfirmationForLaunches)
            {
                var accepted = await ConfirmBatchLaunchAsync(selectedEntries)
                    .ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = LauncherSelectionStatus.BatchLaunchCanceled(selected.Count);
                    return;
                }
            }

            var report = _launchWorkflow.StartBatch(
                selectedEntries,
                RequireConfirmationForLaunches,
                plan => DryRunText = plan.CommandLine);
            StatusText = LauncherSelectionStatus.BatchLaunchCompleted(report);
        }
        catch (Exception ex)
        {
            StatusText = LauncherSelectionStatus.BatchLaunchFailed(ex);
        }
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        var previousSelected = item.ReplaceEntrySelectionFromOwner(selected);
        var snapshot = LauncherSelectionMutationSnapshot.Capture(item, previousSelected);
        SaveSelectionChangeOrRestore(snapshot);
    }

    private bool CanOperateOnSelection() => SelectedCount > 0;

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        var snapshot = LauncherSelectionMutationSnapshot.Capture(entries);
        foreach (var entry in entries)
        {
            entry.SetSelectionFromOwner(selected);
        }

        return SaveSelectionChangeOrRestore(snapshot);
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        var snapshot = LauncherSelectionMutationSnapshot.Capture(Entries);
        foreach (var entry in Entries)
        {
            entry.SetSelectionFromOwner(selectedIds.Contains(entry.Id));
        }

        return SaveSelectionChangeOrRestore(snapshot);
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
