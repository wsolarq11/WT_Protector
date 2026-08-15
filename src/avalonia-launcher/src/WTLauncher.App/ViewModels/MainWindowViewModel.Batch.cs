using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;

namespace WTLauncher.App.ViewModels;

// Selection command surface + selection state projection. Mutation and batch
// launch command bodies live in LauncherSelectionCommandWorkflow.

// _batchCts lives in LaunchOrchestrator (which suppresses CA1001 internally);
// MainWindowViewModel does not own any disposable field and needs no suppression.
public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanSelectVisible))]
    private void SelectVisible() => _selectionWorkflow.SelectVisible();

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection() => _selectionWorkflow.ClearSelection();

    public void ClearVisibleSelection() => _selectionWorkflow.ClearVisibleSelection();

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task LaunchSelectedAsync() =>
        await _selectionWorkflow.LaunchSelectedAsync().ConfigureAwait(true);

    [RelayCommand(CanExecute = nameof(CanCancelBatch))]
    private void CancelBatch() => _launchOrchestrator.CancelBatch();

    private bool CanCancelBatch() => _launchOrchestrator.IsBatchLaunching;

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected) =>
        _selectionWorkflow.UpdateSelection(item, selected);

    private bool CanOperateOnSelection() => _selectionWorkflow.CanOperateOnSelection();

    private bool CanSelectVisible() => _selectionWorkflow.CanSelectVisible();

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

    private IReadOnlyList<string> GetSelectedEntryIds() =>
        _selectionWorkflow.GetSelectedEntryIds();
}
