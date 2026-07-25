using Avalonia;
using WTLauncher.App.Features;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Filter / sort / view-mode / scroll / cards-available-size orchestration.
// Layout state lives in LayoutManager; this file owns persistence + apply.
// Split from MainWindowViewModel.cs as part of 07-09-split-mainviewmodel-stage2.

public sealed partial class MainWindowViewModel
{
    public IReadOnlyList<string> SearchHistoryItems => CurrentSettings.SearchHistory;

    // Layout event handlers — persist settings, then apply filter / refresh.

    private void OnLayoutFilterChanged()
    {
        // ponytail: Uses DispatchErrorOr for persistence — AppStore handles
        // snapshot+rollback internally. No re-entrancy guard needed because
        // DispatchErrorOr is synchronous and rollback doesn't re-trigger this handler.
        var result = _storeGrain.DispatchErrorOr(
            s => s with { Settings = s.Settings with { LastFilterText = _layoutManager.FilterText } });
        if (result.IsError)
        {
            _layoutManager.FilterText = _storeGrain.Select(s => s.Settings.LastFilterText);
            StatusContent = LauncherStatusContent.Error("存储保存失败: " + result.Errors[0].Description);
        }
        EnsureCustomOrderContainsEntries();
        ApplyFilter();
    }

    private void OnLayoutSortChanged()
    {
        var sortMode = _layoutManager.SelectedSortMode;
        sortMode = sortMode ?? throw new InvalidOperationException("Launcher sort mode selection is required.");
        EnsureCustomOrderContainsEntries();

        var result = _storeGrain.DispatchErrorOr(
            s => s with { Settings = s.Settings with { SortMode = sortMode.Id } });
        if (result.IsError)
        {
            _layoutManager.SelectedSortMode = _layoutManager.ResolveSortMode(
                _storeGrain.Select(s => s.Settings.SortMode));
            StatusContent = LauncherStatusContent.Error("存储保存失败: " + result.Errors[0].Description);
        }
        else
        {
            StatusContent = LauncherSettingsStatus.SortModeSaved(sortMode.Label);
            OnPropertyChanged(nameof(IsCustomSortMode));
        }
        ApplyFilter();
        RefreshReorderCommandState();
    }

    private void OnLayoutViewModeChanged()
    {
        var displayMode = _layoutManager.SelectedDisplayMode;
        displayMode = displayMode ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");
        _layoutManager.NotifyDisplayModeProjectionChanged();

        var result = _storeGrain.DispatchErrorOr(
            s => s with { Settings = s.Settings with { EntryViewMode = displayMode.Id } });
        if (result.IsError)
        {
            _layoutManager.SelectedDisplayMode = _layoutManager.ResolveDisplayMode(
                _storeGrain.Select(s => s.Settings.EntryViewMode));
            _layoutManager.NotifyDisplayModeProjectionChanged();
            StatusContent = LauncherStatusContent.Error("存储保存失败: " + result.Errors[0].Description);
        }
        else
        {
            StatusContent = LauncherSettingsStatus.DisplayModeSaved(displayMode.Label);
        }
    }

    private void OnLayoutGroupKeyChanged()
    {
        var result = _storeGrain.DispatchErrorOr(
            s => s with { Settings = s.Settings with { CardsGroupKey = _layoutManager.CardsGroupKey } });
        if (result.IsError)
        {
            var oldCardsGroupKey = _storeGrain.Select(s => s.Settings.CardsGroupKey);
            // Restore LayoutManager BEFORE RestoreVmStateFromStore so that
            // ApplyFilter → RebuildVisibleGroups reads the correct old key.
            _layoutManager.SetGroupKeyDirect(oldCardsGroupKey);
            RestoreVmStateFromStore();
            StatusContent = LauncherStatusContent.Error(result.Errors[0].Description);
            return;
        }
        RebuildVisibleGroups();
    }

    /// <summary>
    /// Mutates settings via CurrentSettings (AppStore.Select), builds a new store
    /// file with the result, and saves atomically. No local _settings field needed
    /// — failure is reported but there is no stale local state to restore.
    /// </summary>
    private bool TryPersistSettingsChange(
        Func<LauncherSettings, LauncherSettings> mutator,
        LauncherStatusContent? successStatus = null)
    {
        var newSettings = mutator(CurrentSettings);
        var storeFile = BuildStoreFile() with { Settings = newSettings };
        var saveFailure = SaveStoreFile(storeFile);
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return false;
        }

        if (successStatus is not null)
            StatusContent = successStatus;

        return true;
    }

    private void ApplyFilter()
    {
        var isDragging = _dragReorderWorkflow.HasActiveSession;

        // P1: During drag reorder, card sizes don't change — skip the calculation.
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
            // Drag path: discard preview and sync VisibleCount
            DiscardDragReorderPreview();
            VisibleCount = _list.MutableVisibleEntries.Count;
            return;
        }

        VisibleCount = _list.MutableVisibleEntries.Count;
        SelectVisibleCommand.NotifyCanExecuteChanged();
        RebuildVisibleGroups();
    }

    /// <summary>
    /// Records the current FilterText into SearchHistory (dedup: moves to front;
    /// cap: 20 entries). Persists via the standard snapshot/rollback path.
    /// Only called on explicit commit (Enter key), not on every keystroke.
    /// </summary>
    public void RecordSearchTerm()
    {
        var newHistory = LauncherSearchHistory.Record(CurrentSettings.SearchHistory, _layoutManager.FilterText);
        if (ReferenceEquals(newHistory, CurrentSettings.SearchHistory))
            return;

        TryPersistSettingsChange(s => s with { SearchHistory = newHistory });
        OnPropertyChanged(nameof(SearchHistoryItems));
    }

    private void SyncVisibleEntries(IReadOnlyList<LauncherEntryViewModel> sorted)
    {
        _list.MutableVisibleEntries.Clear();
        foreach (var item in sorted)
            _list.MutableVisibleEntries.Add(item);
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _layoutManager.UpdateCardsAvailableSize(availableSize);
        _layoutManager.CardLayout.Recalculate(
            _layoutManager.CardsAvailableSize, _list.MutableVisibleEntries.Count);
        // Track for filter reapply.
    }

    public bool TrySaveWindowBounds(double width, double height, double x = 0, double y = 0, string windowState = "Normal")
    {
        DiscardDragReorderPreview();
        try
        {
            _storeGrain.Dispatch(state =>
            {
                var nextWindow = state.Settings.Window with
                {
                    Width = width,
                    Height = height,
                    X = x,
                    Y = y,
                    WindowState = windowState,
                };
                return state with { Settings = state.Settings with { Window = nextWindow } };
            });
            return true;
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            StatusContent = LauncherSettingsStatus.WindowBoundsSaveFailed;
            ErrorLog.Write(ex, "TrySaveWindowBounds");
            return false;
        }
    }

    private void EnsureCustomOrderContainsEntries()
    {
        var currentIds = GetEntryIds();
        var synced = LauncherEntryOrder.SyncWithEntries(CurrentSettings.CustomEntryOrder, currentIds);
        if (!CurrentSettings.CustomEntryOrder.SequenceEqual(synced))
        {
            var store = BuildStoreFile();
            store = store with { Settings = store.Settings with { CustomEntryOrder = synced } };
            SaveStoreFile(store);
            ApplyFilter();
        }
    }

    public void SaveScrollOffset(LauncherEntryViewMode mode, double offset) =>
        _layoutManager.SaveScrollOffset(mode, offset);

    public double GetScrollOffset(LauncherEntryViewMode mode) =>
        _layoutManager.GetScrollOffset(mode);
}