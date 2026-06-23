using Avalonia;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private Size _cardsAvailableSize = new(900, 420);
    private Size _lastAppliedCardsAvailableSize;

    [ObservableProperty]
    private string _filterText = "";

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

    public bool IsCustomSortMode => RequireSortModeSelection(SelectedSortMode).Id == LauncherEntryVisibility.CustomSortModeId;

    public bool IsCardsViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Cards;

    public bool IsDetailedListMode => CurrentEntryViewMode == LauncherEntryViewMode.Details;

    public bool IsNamesViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Names;

    partial void OnFilterTextChanging(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    partial void OnFilterTextChanged(string value)
    {
        var snapshot = CaptureSettingsMutationSnapshot();
        _settings = _settings with { LastFilterText = value };
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _filterText = snapshot.LastFilterText;
            OnPropertyChanged(nameof(FilterText));
            ApplyFilter();
            ApplyStatusText(saveFailure);
            return;
        }

        ApplyFilter();
    }

    partial void OnSelectedSortModeChanging(LauncherSortModeViewModel? value)
    {
        _ = RequireSortModeSelection(value);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        value = RequireSortModeSelection(value);
        OnPropertyChanged(nameof(IsCustomSortMode));

        var snapshot = CaptureSettingsMutationSnapshot();
        _settings = _settings with { SortMode = value.Id };
        EnsureCustomOrderContainsEntries();
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _selectedSortMode = ResolveSortMode(snapshot.SortMode);
            OnPropertyChanged(nameof(SelectedSortMode));
            OnPropertyChanged(nameof(IsCustomSortMode));
            ApplyFilter();
            ApplyStatusText(saveFailure);
            return;
        }

        ApplyFilter();
        StatusText = LauncherSettingsStatus.SortModeSaved(value.Label);
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
        value = RequireDisplayModeSelection(value);
        NotifyDisplayModeProjectionChanged();

        var snapshot = CaptureSettingsMutationSnapshot();
        _settings = _settings with { EntryViewMode = value.Id };
        LauncherEntryLayoutPresentation.Refresh(_entries);
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _selectedDisplayMode = ResolveDisplayMode(snapshot.EntryViewMode);
            OnPropertyChanged(nameof(SelectedDisplayMode));
            NotifyDisplayModeProjectionChanged();
            LauncherEntryLayoutPresentation.Refresh(_entries);
            ApplyStatusText(saveFailure);
            return;
        }

        StatusText = LauncherSettingsStatus.DisplayModeSaved(value.Label);
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
                LauncherEntryLayoutPresentation.ApplyCardSize(_entries, _visibleEntries, _cardsAvailableSize);
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
    }

    /// <summary>
    /// P5: O(n) incremental sync using Dictionary for position lookup.
    /// Removes disappeared entries, inserts new ones, and reorders existing
    /// ones via Move — preserving Avalonia containers.
    /// Updates position map incrementally instead of rebuilding after each operation.
    /// </summary>
    private void SyncVisibleEntries(IReadOnlyList<LauncherEntryViewModel> sorted)
    {
        // Remove entries no longer visible (iterate backwards for safe removal).
        var sortedSet = new HashSet<LauncherEntryViewModel>(sorted);
        for (var i = _visibleEntries.Count - 1; i >= 0; i--)
        {
            if (!sortedSet.Contains(_visibleEntries[i]))
                _visibleEntries.RemoveAt(i);
        }

        // Build position map for O(1) lookups instead of O(n) IndexOf.
        var positionByEntry = new Dictionary<LauncherEntryViewModel, int>(_visibleEntries.Count);
        for (var i = 0; i < _visibleEntries.Count; i++)
            positionByEntry[_visibleEntries[i]] = i;

        // Insert new entries and move existing ones to correct positions.
        for (var targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
        {
            var entry = sorted[targetIndex];
            if (positionByEntry.TryGetValue(entry, out var currentIndex))
            {
                if (currentIndex != targetIndex)
                {
                    _visibleEntries.Move(currentIndex, targetIndex);
                    // P5: Update positions incrementally instead of rebuilding the dictionary.
                    // After Move(from, to), entries in [min, max] shift by ±1.
                    var from = currentIndex;
                    var to = targetIndex;
                    if (from < to)
                    {
                        for (var j = from; j < to; j++)
                            positionByEntry[_visibleEntries[j]] = j;
                    }
                    else
                    {
                        for (var j = to + 1; j <= from; j++)
                            positionByEntry[_visibleEntries[j]] = j;
                    }
                    positionByEntry[entry] = to;
                }
            }
            else
            {
                _visibleEntries.Insert(targetIndex, entry);
                // P5: Shift positions for entries after the insertion point.
                for (var j = targetIndex + 1; j < _visibleEntries.Count; j++)
                    positionByEntry[_visibleEntries[j]] = j;
                positionByEntry[entry] = targetIndex;
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

    public bool TrySaveWindowBounds(double width, double height)
    {
        DiscardDragReorderPreview();
        if (LauncherSettingsStore.TrySaveWindowBounds(ref _settings, _settingsPath, width, height))
            return true;

        StatusText = LauncherSettingsStatus.WindowBoundsSaveFailed;
        return false;
    }

    private void EnsureCustomOrderContainsEntries() =>
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds());

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