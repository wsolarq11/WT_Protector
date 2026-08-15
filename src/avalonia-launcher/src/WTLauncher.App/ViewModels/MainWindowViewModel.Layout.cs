using Avalonia;
using WTLauncher.App.Features;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Layout command surface + projection internals. Persistence and layout
// command bodies live in LauncherLayoutWorkflow.

public sealed partial class MainWindowViewModel
{
    public IReadOnlyList<string> SearchHistoryItems => CurrentSettings.SearchHistory;

    public void RecordSearchTerm() => _layoutWorkflow.RecordSearchTerm();

    public void UpdateCardsAvailableSize(Size availableSize) =>
        _layoutWorkflow.UpdateCardsAvailableSize(availableSize);

    public bool TrySaveWindowBounds(double width, double height, double x = 0, double y = 0, string windowState = "Normal") =>
        _layoutWorkflow.TrySaveWindowBounds(width, height, x, y, windowState);

    private void EnsureCustomOrderContainsEntries() =>
        _layoutWorkflow.EnsureCustomOrderContainsEntries();

    private bool TryPersistSettingsChange(
        Func<LauncherSettings, LauncherSettings> mutator,
        LauncherStatusContent? successStatus = null) =>
        _layoutWorkflow.PersistSettings(mutator, successStatus);

    private void RebuildVisibleProjection()
    {
        var isDragging = _dragReorderWorkflow.HasActiveSession;

        if (!isDragging)
        {
            var sorted = LauncherEntryQuery.FilterAndSort(
                _list.MutableEntries,
                _layoutManager.FilterText,
                _layoutManager.SelectedSortMode?.Id ?? LauncherEntryQuery.CustomSortModeId,
                CurrentSettings.CustomEntryOrder,
                ToLaunchEntry);

            SyncVisibleEntries(sorted);

            _layoutManager.CardLayout.Recalculate(
                _layoutManager.CardsAvailableSize, _list.MutableVisibleEntries.Count);
        }
        else
        {
            DiscardDragReorderPreview();
            VisibleCount = _list.MutableVisibleEntries.Count;
            return;
        }

        VisibleCount = _list.MutableVisibleEntries.Count;
        SelectVisibleCommand.NotifyCanExecuteChanged();
        RebuildVisibleGroups();
    }

    private void SyncVisibleEntries(IReadOnlyList<LauncherEntryViewModel> sorted)
    {
        _list.MutableVisibleEntries.Clear();
        foreach (var item in sorted)
            _list.MutableVisibleEntries.Add(item);
    }

    public void SaveScrollOffset(LauncherEntryViewMode mode, double offset) =>
        _layoutManager.SaveScrollOffset(mode, offset);

    public double GetScrollOffset(LauncherEntryViewMode mode) =>
        _layoutManager.GetScrollOffset(mode);
}
