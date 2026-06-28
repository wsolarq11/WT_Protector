using Avalonia;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private Size _cardsAvailableSize = new(900, 420);
    private Size _lastAppliedCardsAvailableSize;

    // Prevents infinite recursion when a settings save failure triggers
    // a property rollback that re-enters the OnXxxChanged callback.
    // NOTE: This guard pattern is intentionally duplicated in OnFilterTextChanged,
    // OnSelectedSortModeChanged, and OnSelectedDisplayModeChanged rather than
    // extracted into a shared helper — each callback has distinct validation
    // and persistence logic that would make a common abstraction brittle.
    private bool _isHandlingSettingsChange;

    // Prevents double-click double-launch. Set to true at entry, false in finally.
    private bool _isLaunching;

    // P1: Reusable collections for SyncVisibleEntries — Clear() preserves internal
    // bucket array capacity in .NET, eliminating per-call GC allocations (~2-3KB
    // per keystroke for ~200 entries).
    private readonly HashSet<LauncherEntryViewModel> _syncSortedSet = new();
    private readonly Dictionary<LauncherEntryViewModel, int> _syncPositionByEntry = new();

    // Other fields: MainWindowViewModel.cs, MainWindowViewModel.Drag.cs

    [ObservableProperty]
    private string _filterText = "";

    public IReadOnlyList<string> SearchHistoryItems => _settings.SearchHistory;

    [ObservableProperty]
    private LauncherSortModeViewModel? _selectedSortMode;

    [ObservableProperty]
    private LauncherDisplayModeViewModel? _selectedDisplayMode;

    private readonly ObservableCollection<LauncherSortModeViewModel> _sortModes =
    [
        new(LauncherEntryVisibility.CustomSortModeId, "自定义拖拽"),
        new(LauncherEntryVisibility.RecentSortModeId, "最近启动"),
        new(LauncherEntryVisibility.UpdatedSortModeId, "更新时间"),
        new(LauncherEntryVisibility.AlphabeticalSortModeId, "首字母顺序"),
    ];

    private readonly ObservableCollection<LauncherDisplayModeViewModel> _displayModes =
    [
        new(LauncherEntryViewMode.Cards, "卡片"),
        new(LauncherEntryViewMode.Details, "详细列表"),
        new(LauncherEntryViewMode.Names, "名称墙"),
    ];

    public ReadOnlyObservableCollection<LauncherSortModeViewModel> SortModes { get; }

    public ReadOnlyObservableCollection<LauncherDisplayModeViewModel> DisplayModes { get; }

    private LauncherSortModeViewModel ResolveSortMode(string? sortModeId) =>
        _sortModes.First(mode => mode.Id == LauncherEntryVisibility.NormalizeSortModeId(sortModeId));

    private LauncherDisplayModeViewModel ResolveDisplayMode(string? entryViewMode) =>
        _displayModes.First(mode => mode.Id == LauncherEntryViewModes.NormalizeId(entryViewMode));

    private LauncherEntryViewMode CurrentEntryViewMode => RequireDisplayModeSelection(SelectedDisplayMode).Mode;

    // ponytail: null-conditional avoids throwing when SelectedSortMode is null
    // (corrupted settings / deserialization failure). Returns false safely.
    public bool IsCustomSortMode => SelectedSortMode?.Id == LauncherEntryVisibility.CustomSortModeId;

    public bool IsCardsViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Cards;

    public bool IsDetailedListMode => CurrentEntryViewMode == LauncherEntryViewMode.Details;

    public bool IsNamesViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Names;

    partial void OnFilterTextChanging(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    partial void OnFilterTextChanged(string value)
    {
        if (_isHandlingSettingsChange) return;
        _isHandlingSettingsChange = true;
        try
        {
            if (TryPersistSettingsChange(s => s with { LastFilterText = value }))
            {
                ApplyFilter();
            }
            else
            {
                _filterText = _settings.LastFilterText;
                OnPropertyChanged(nameof(FilterText));
                ApplyFilter();
            }
        }
        finally
        {
            _isHandlingSettingsChange = false;
        }
    }

    partial void OnSelectedSortModeChanging(LauncherSortModeViewModel? value)
    {
        _ = RequireSortModeSelection(value);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        if (_isHandlingSettingsChange) return;
        _isHandlingSettingsChange = true;
        try
        {
            value = RequireSortModeSelection(value);
            EnsureCustomOrderContainsEntries();

            if (TryPersistSettingsChange(
                s => s with { SortMode = value.Id },
                LauncherSettingsStatus.SortModeSaved(value.Label)))
            {
                OnPropertyChanged(nameof(IsCustomSortMode));
                ApplyFilter();
                RefreshReorderCommandState();
            }
            else
            {
                _selectedSortMode = ResolveSortMode(_settings.SortMode);
                OnPropertyChanged(nameof(SelectedSortMode));
                ApplyFilter();
                RefreshReorderCommandState();
            }
        }
        finally
        {
            _isHandlingSettingsChange = false;
        }
    }

    private readonly Dictionary<LauncherEntryViewMode, double> _scrollOffsets = new();

    public void SaveScrollOffset(LauncherEntryViewMode mode, double offset) => _scrollOffsets[mode] = offset;

    public double GetScrollOffset(LauncherEntryViewMode mode) => _scrollOffsets.TryGetValue(mode, out var v) ? v : 0;

    partial void OnSelectedDisplayModeChanging(LauncherDisplayModeViewModel? value)
    {
        _ = RequireDisplayModeSelection(value);
    }

    partial void OnSelectedDisplayModeChanged(LauncherDisplayModeViewModel? value)
    {
        if (_isHandlingSettingsChange) return;
        _isHandlingSettingsChange = true;
        try
        {
            value = RequireDisplayModeSelection(value);
            NotifyDisplayModeProjectionChanged();
            LauncherEntryLayoutPresentation.Refresh(_entries);

            if (TryPersistSettingsChange(
                s => s with { EntryViewMode = value.Id },
                LauncherSettingsStatus.DisplayModeSaved(value.Label)))
            {
                // Success — status already set by helper.
            }
            else
            {
                _selectedDisplayMode = ResolveDisplayMode(_settings.EntryViewMode);
                OnPropertyChanged(nameof(SelectedDisplayMode));
                NotifyDisplayModeProjectionChanged();
                LauncherEntryLayoutPresentation.Refresh(_entries);
            }
        }
        finally
        {
            _isHandlingSettingsChange = false;
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
        string? successStatus = null)
    {
        var snapshot = CaptureSettingsMutationSnapshot();
        _settings = mutator(_settings);
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _settings = snapshot.Restore();
            ApplyStatusText(saveFailure);
            return false;
        }

        if (successStatus is not null)
            StatusText = successStatus;

        return true;
    }

    private void NotifyDisplayModeProjectionChanged()
    {
        OnPropertyChanged(nameof(IsCardsViewMode));
        OnPropertyChanged(nameof(IsDetailedListMode));
        OnPropertyChanged(nameof(IsNamesViewMode));
    }

    private void ApplyFilter()
    {
        var isDragging = _dragReorderWorkflow.HasActiveSession;

        // P1: During drag reorder, card sizes don't change — skip the calculation.
        if (!isDragging)
        {
            var sorted = LauncherEntryVisibility.FilterAndSort(
                _entries,
                FilterText,
                RequireSortModeSelection(SelectedSortMode).Id,
                _settings.CustomEntryOrder,
                ToLaunchEntry);

            SyncVisibleEntries(sorted);

            // P1: Only recalculate card sizes when available size actually changed.
            if (_cardsAvailableSize != _lastAppliedCardsAvailableSize)
            {
                LauncherEntryLayoutPresentation.ApplyCardSize(_entries, _visibleEntries, _cardsAvailableSize, _syncSortedSet);
                _lastAppliedCardsAvailableSize = _cardsAvailableSize;
            }
        }
        else
        {
            // Drag path: still need to sync VisibleCount
            VisibleCount = _visibleEntries.Count;
            return;
        }

        VisibleCount = _visibleEntries.Count;
        SelectVisibleCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Records the current FilterText into SearchHistory (dedup: moves to front;
    /// cap: 20 entries). Persists via the standard snapshot/rollback path.
    /// Only called on explicit commit (Enter key), not on every keystroke.
    /// </summary>
    public void RecordSearchTerm()
    {
        var term = FilterText;
        if (string.IsNullOrWhiteSpace(term))
            return;

        var normalized = LauncherEntryText.Normalize(term);

        // No-op when already the most recent entry.
        if (_settings.SearchHistory.Count > 0 &&
            string.Equals(_settings.SearchHistory[0], normalized, StringComparison.OrdinalIgnoreCase))
            return;

        // Remove existing match (case-insensitive) to dedup, then insert at front.
        // Insert at front: most-recent-first ordering. RequireSearchHistory truncates
        // from the tail, so the newest 20 entries survive.
        var newHistory = _settings.SearchHistory.RemoveAll(
            t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase));
        newHistory = newHistory.Insert(0, normalized);

        // Cap at MaxSearchHistoryTerms.
        if (newHistory.Count > LauncherSettingsStore.MaxSearchHistoryTerms)
            newHistory = newHistory.RemoveRange(LauncherSettingsStore.MaxSearchHistoryTerms, newHistory.Count - LauncherSettingsStore.MaxSearchHistoryTerms);

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
        for (var i = _visibleEntries.Count - 1; i >= 0; i--)
        {
            if (!_syncSortedSet.Contains(_visibleEntries[i]))
                _visibleEntries.RemoveAt(i);
        }

        // Build position map for O(1) lookups instead of O(n) IndexOf.
        // P1: Reuse instance field, Clear() preserves internal bucket capacity.
        _syncPositionByEntry.Clear();
        for (var i = 0; i < _visibleEntries.Count; i++)
            _syncPositionByEntry[_visibleEntries[i]] = i;

        // Insert new entries and move existing ones to correct positions.
        for (var targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
        {
            var entry = sorted[targetIndex];
            if (_syncPositionByEntry.TryGetValue(entry, out var currentIndex))
            {
                if (currentIndex != targetIndex)
                {
                    _visibleEntries.Move(currentIndex, targetIndex);
                    // P5: Update positions incrementally instead of rebuilding the dictionary.
                    // After Move(from, to), entries in [min, max] shift by ±1.
                    // NOTE: The rotation interval is [from, to) when moving forward
                    // and (to, from] when moving backward — these ranges are correct but
                    // fragile: changing the loop boundaries without adjusting both branches
                    // would cause a persistent off-by-one offset for all subsequent entries.
                    var from = currentIndex;
                    var to = targetIndex;
                    if (from < to)
                    {
                        for (var j = from; j < to; j++)
                            _syncPositionByEntry[_visibleEntries[j]] = j;
                    }
                    else
                    {
                        for (var j = to + 1; j <= from; j++)
                            _syncPositionByEntry[_visibleEntries[j]] = j;
                    }
                    _syncPositionByEntry[entry] = to;
                }
            }
            else
            {
                _visibleEntries.Insert(targetIndex, entry);
                // P5: Shift positions for entries after the insertion point.
                for (var j = targetIndex + 1; j < _visibleEntries.Count; j++)
                    _syncPositionByEntry[_visibleEntries[j]] = j;
                _syncPositionByEntry[entry] = targetIndex;
            }
        }
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        var normalized = LauncherEntryLayoutPresentation.NormalizeAvailableSize(availableSize);
        if (_cardsAvailableSize == normalized)
            return; // P1: Skip when size unchanged — avoids redundant PropertyChanged storm.

        _cardsAvailableSize = normalized;
        LauncherEntryLayoutPresentation.ApplyCardSize(_entries, _visibleEntries, _cardsAvailableSize);
        _lastAppliedCardsAvailableSize = _cardsAvailableSize;
    }

    public bool TrySaveWindowBounds(double width, double height, double x = 0, double y = 0, string windowState = "Normal")
    {
        DiscardDragReorderPreview();
        var storeFile = BuildStoreFile();
        if (LauncherSettingsStore.TrySaveWindowBounds(storeFile, _storePath, width, height, out var updatedSettings, x, y, windowState))
        {
            _settings = updatedSettings;
            return true;
        }

        StatusText = LauncherSettingsStatus.WindowBoundsSaveFailed;
        return false;
    }

    private void EnsureCustomOrderContainsEntries()
    {
        var currentIds = GetEntryIds();
        var synced = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, currentIds);
        if (!_settings.CustomEntryOrder.SequenceEqual(synced))
            _settings = _settings with { CustomEntryOrder = synced };
    }

    private static LauncherSortModeViewModel RequireSortModeSelection(LauncherSortModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher sort mode selection is required.");

    private static LauncherDisplayModeViewModel RequireDisplayModeSelection(LauncherDisplayModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");

    private IEnumerable<string> GetEntryIds() =>
        _entries.Select(entry => entry.Id);

    public int GetPresetReferenceCount(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return 0;

        var normalized = LauncherEntryIds.Normalize(entryId);
        return _presetFile.Presets.Count(preset =>
            preset.EntryIds.Any(id => LauncherEntryIds.Same(id, normalized)));
    }
}