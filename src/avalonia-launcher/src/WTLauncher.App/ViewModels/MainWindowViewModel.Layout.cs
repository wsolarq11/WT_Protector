using Avalonia;
using WTLauncher.App.Features;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

// Filter / sort / view-mode / scroll / cards-available-size orchestration.
// Layout state lives in LayoutManager; this file owns persistence + apply.
// Split from MainWindowViewModel.cs as part of 07-09-split-mainviewmodel-stage2.

public sealed partial class MainWindowViewModel
{
    // Prevents infinite recursion when a settings save failure triggers
    // a property rollback that re-enters the event handler.
    private bool _isHandlingSettingsChange;

    /// <summary>
    /// Attempts to begin a settings change scope. Returns false if already
    /// inside a settings change (re-entrant call), in which case the caller
    /// should return immediately without proceeding.
    /// </summary>
    private bool TryBeginSettingsChange()
    {
        if (_isHandlingSettingsChange) return false;
        _isHandlingSettingsChange = true;
        return true;
    }

    /// <summary>
    /// Ends the settings change scope, allowing re-entrant calls to proceed.
    /// </summary>
    private void EndSettingsChange() => _isHandlingSettingsChange = false;

    // P1: Reusable collections for SyncVisibleEntries — Clear() preserves internal
    // bucket array capacity in .NET, eliminating per-call GC allocations (~2-3KB
    // per keystroke for ~200 entries).
    private readonly HashSet<LauncherEntryViewModel> _syncSortedSet = new();
    private readonly Dictionary<LauncherEntryViewModel, int> _syncPositionByEntry = new();

    public IReadOnlyList<string> SearchHistoryItems => _settings.SearchHistory;

    // Layout event handlers — persist settings, then apply filter / refresh.

    private void OnLayoutFilterChanged()
    {
        if (!TryBeginSettingsChange()) return;
        try
        {
            if (TryPersistSettingsChange(s => s with { LastFilterText = _layoutManager.FilterText }))
            {
                ApplyFilter();
            }
            else
            {
                // Rollback — restore FilterText from _settings.
                _layoutManager.FilterText = _settings.LastFilterText;
                ApplyFilter();
            }
        }
        finally
        {
            EndSettingsChange();
        }
    }

    private void OnLayoutSortChanged()
    {
        if (!TryBeginSettingsChange()) return;
        try
        {
            var sortMode = _layoutManager.SelectedSortMode;
            sortMode = sortMode ?? throw new InvalidOperationException("Launcher sort mode selection is required.");
            EnsureCustomOrderContainsEntries();

            if (TryPersistSettingsChange(
                s => s with { SortMode = sortMode.Id },
                LauncherSettingsStatus.SortModeSaved(sortMode.Label)))
            {
                OnPropertyChanged(nameof(IsCustomSortMode));
                ApplyFilter();
                RefreshReorderCommandState();
            }
            else
            {
                _layoutManager.SelectedSortMode = _layoutManager.ResolveSortMode(_settings.SortMode);
                ApplyFilter();
                RefreshReorderCommandState();
            }
        }
        finally
        {
            EndSettingsChange();
        }
    }

    private void OnLayoutViewModeChanged()
    {
        if (!TryBeginSettingsChange()) return;
        try
        {
            var displayMode = _layoutManager.SelectedDisplayMode;
            displayMode = displayMode ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");
            _layoutManager.NotifyDisplayModeProjectionChanged();
            LauncherEntryLayoutPresentation.Refresh(_list.MutableEntries);

            if (TryPersistSettingsChange(
                s => s with { EntryViewMode = displayMode.Id },
                LauncherSettingsStatus.DisplayModeSaved(displayMode.Label)))
            {
                // Success — status already set by helper.
            }
            else
            {
                _layoutManager.SelectedDisplayMode = _layoutManager.ResolveDisplayMode(_settings.EntryViewMode);
                _layoutManager.NotifyDisplayModeProjectionChanged();
                LauncherEntryLayoutPresentation.Refresh(_list.MutableEntries);
            }
        }
        finally
        {
            EndSettingsChange();
        }
    }

    private void OnLayoutGroupKeyChanged()
    {
        if (!TryBeginSettingsChange()) return;
        try
        {
            _settings = _settings with { CardsGroupKey = _layoutManager.CardsGroupKey };
            var saveFailure = SaveStoreFile(BuildStoreFile());
            if (saveFailure is not null)
            {
                var oldCardsGroupKey = _appStore.Select(s => s.Settings.CardsGroupKey);
                // Restore LayoutManager BEFORE RestoreVmStateFromStore so that
                // ApplyFilter → RebuildVisibleGroups reads the correct old key.
                _layoutManager.SetGroupKeyDirect(oldCardsGroupKey);
                RestoreVmStateFromStore();
                StatusContent = LauncherStatusContent.Error(saveFailure);
                return;
            }
            RebuildVisibleGroups();
        }
        finally
        {
            EndSettingsChange();
        }
    }

    /// <summary>
    /// Encapsulates the CaptureSettingsMutationSnapshot → mutate _settings →
    /// SaveSettingsChange → rollback-on-failure boilerplate repeated across settings
    /// mutation callbacks.
    /// On success, sets <see cref="StatusText"/> to <paramref name="successStatus"/>
    /// (when non-null) and returns true.
    /// On failure, restores _settings from the snapshot, applies the save failure
    /// status, and returns false — callers must then re-sync their own UI properties.
    /// </summary>
    private bool TryPersistSettingsChange(
        Func<LauncherSettings, LauncherSettings> mutator,
        LauncherStatusContent? successStatus = null)
    {
        var snapshot = CaptureSettingsMutationSnapshot();
        _settings = mutator(_settings);
        var saveFailure = SaveSettingsChange(snapshot);
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
                _settings.CustomEntryOrder,
                ToLaunchEntry);

            SyncVisibleEntries(sorted);

            _layoutManager.TryRefreshCardSize(_list.MutableEntries, _list.MutableVisibleEntries);
        }
        else
        {
            // Drag path: still need to sync VisibleCount
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
        var newHistory = LauncherSearchHistory.Record(_settings.SearchHistory, _layoutManager.FilterText);
        if (ReferenceEquals(newHistory, _settings.SearchHistory))
            return;

        TryPersistSettingsChange(s => s with { SearchHistory = newHistory });
        OnPropertyChanged(nameof(SearchHistoryItems));
    }

    /// <summary>
    /// P5: O(n) incremental sync using Dictionary for position lookup.
    /// Removes disappeared entries, inserts new ones, and reorders existing
    /// ones via Move — preserving Avalonia containers.
    /// Updates position map incrementally instead of rebuilding after each operation.
    /// Each RemoveAt / Move / Insert call triggers a CollectionChanged event on the
    /// ObservableCollection. For current data sizes (typically &lt; 200 entries),
    /// Avalonia's ItemContainerGenerator virtualization absorbs the notification
    /// volume efficiently — batching is unnecessary overhead at this scale.
    /// </summary>
    private void SyncVisibleEntries(IReadOnlyList<LauncherEntryViewModel> sorted)
    {
        // Remove entries no longer visible (iterate backwards for safe removal).
        // P1: Reuse instance field, Clear() preserves internal bucket capacity.
        _syncSortedSet.Clear();
        foreach (var item in sorted)
            _syncSortedSet.Add(item);
        for (var i = _list.MutableVisibleEntries.Count - 1; i >= 0; i--)
        {
            if (!_syncSortedSet.Contains(_list.MutableVisibleEntries[i]))
                _list.MutableVisibleEntries.RemoveAt(i);
        }

        // Build position map for O(1) lookups instead of O(n) IndexOf.
        // P1: Reuse instance field, Clear() preserves internal bucket capacity.
        _syncPositionByEntry.Clear();
        for (var i = 0; i < _list.MutableVisibleEntries.Count; i++)
            _syncPositionByEntry[_list.MutableVisibleEntries[i]] = i;

        // Insert new entries and move existing ones to correct positions.
        for (var targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
        {
            var entry = sorted[targetIndex];
            if (_syncPositionByEntry.TryGetValue(entry, out var currentIndex))
            {
                if (currentIndex != targetIndex)
                {
                    _list.MutableVisibleEntries.Move(currentIndex, targetIndex);
                    // P5: Update positions incrementally instead of rebuilding the dictionary.
                    // After Move(from, to), entries in [min, max] shift by �1.
                    // NOTE: The rotation interval is [from, to) when moving forward
                    // and (to, from] when moving backward — these ranges are correct but
                    // fragile: changing the loop boundaries without adjusting both branches
                    // would cause a persistent off-by-one offset for all subsequent entries.
                    var from = currentIndex;
                    var to = targetIndex;
                    if (from < to)
                    {
                        for (var j = from; j < to; j++)
                            _syncPositionByEntry[_list.MutableVisibleEntries[j]] = j;
                    }
                    else
                    {
                        for (var j = to + 1; j <= from; j++)
                            _syncPositionByEntry[_list.MutableVisibleEntries[j]] = j;
                    }
                    _syncPositionByEntry[entry] = to;
                }
            }
            else
            {
                _list.MutableVisibleEntries.Insert(targetIndex, entry);
                // P5: Shift positions for entries after the insertion point.
                for (var j = targetIndex + 1; j < _list.MutableVisibleEntries.Count; j++)
                    _syncPositionByEntry[_list.MutableVisibleEntries[j]] = j;
                _syncPositionByEntry[entry] = targetIndex;
            }
        }
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _layoutManager.UpdateCardsAvailableSize(availableSize);
        _layoutManager.TryRefreshCardSize(_list.MutableEntries, _list.MutableVisibleEntries);
        // Track for filter reapply.
    }

    public bool TrySaveWindowBounds(double width, double height, double x = 0, double y = 0, string windowState = "Normal")
    {
        DiscardDragReorderPreview();
        var storeFile = BuildStoreFile();
        var nextSettings = storeFile.Settings with
        {
            Window = storeFile.Settings.Window with
            {
                Width = width, Height = height, X = x, Y = y, WindowState = windowState
            }
        };
        var nextStore = storeFile with { Settings = nextSettings };

        try
        {
            LauncherStore.Save(nextStore, _storePath);
            _settings = nextSettings;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or PathTooLongException or DirectoryNotFoundException or System.Security.SecurityException)
        {
            StatusContent = LauncherSettingsStatus.WindowBoundsSaveFailed;
            return false;
        }
    }

    private void EnsureCustomOrderContainsEntries()
    {
        var currentIds = GetEntryIds();
        var synced = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, currentIds);
        if (!_settings.CustomEntryOrder.SequenceEqual(synced))
            _settings = _settings with { CustomEntryOrder = synced };
    }

    public void SaveScrollOffset(LauncherEntryViewMode mode, double offset) =>
        _layoutManager.SaveScrollOffset(mode, offset);

    public double GetScrollOffset(LauncherEntryViewMode mode) =>
        _layoutManager.GetScrollOffset(mode);
}
