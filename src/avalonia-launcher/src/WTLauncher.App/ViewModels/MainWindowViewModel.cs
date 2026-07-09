using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly string _storePath;
    private LauncherSettings _settings;
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private readonly Func<DateTimeOffset> _utcNow;
    private LauncherPresetFile _presetFile = new();
    private readonly ObservableCollection<LauncherEntryViewModel> _entries = [];
    private readonly ObservableCollection<LauncherEntryViewModel> _visibleEntries = [];
    private readonly ObservableCollection<LauncherPresetViewModel> _presets = [];

    [ObservableProperty]
    private string _statusText = LauncherMainWindowText.Ready;

    [ObservableProperty]
    private string _dryRunText = LauncherMainWindowText.DryRunPlaceholder;

    [ObservableProperty]
    private string _presetSelectionStatusText = LauncherMainWindowText.PresetSelectionPlaceholder;

    [ObservableProperty]
    private string _applyPresetButtonText = LauncherPresetSelectionPreview.DefaultApplyButtonText;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    public bool IsVisibleEmpty => VisibleCount == 0;

    partial void OnVisibleCountChanged(int value)
    {
        OnPropertyChanged(nameof(IsVisibleEmpty));
    }

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    [ObservableProperty]
    private LauncherPresetViewModel? _selectedPreset;

    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    private string? _keyboardFocusedEntryId;

    public string? KeyboardFocusedEntryId
    {
        get => _keyboardFocusedEntryId;
        set => SetKeyboardFocusedEntryId(value);
    }

    public MainWindowViewModel(
        string storePath,
        LauncherStoreFile storeFile,
        LauncherLaunchWorkflow launchWorkflow,
        Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(storeFile);
        _storePath = storePath;
        _settings = storeFile.Settings;
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _launchWorkflow.CommandTemplates = _settings.CommandTemplates;
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
        Presets = new ReadOnlyObservableCollection<LauncherPresetViewModel>(_presets);
        SortModes = new ReadOnlyObservableCollection<LauncherSortModeViewModel>(_sortModes);
        DisplayModes = new ReadOnlyObservableCollection<LauncherDisplayModeViewModel>(_displayModes);
        ConfigPathDisplay = LauncherMainWindowText.StorePathDisplay(storePath);
        _filterText = RequireSettingsText(_settings.LastFilterText, "lastFilterText");
        _requireConfirmationForLaunches = _settings.RequireConfirmationForLaunches;
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        _selectedSortMode = ResolveSortMode(_settings.SortMode);
        _settings = _settings with { SortMode = _selectedSortMode.Id };
        _selectedDisplayMode = ResolveDisplayMode(_settings.EntryViewMode);
        _settings = _settings with { EntryViewMode = _selectedDisplayMode.Id };
        NotifyDisplayModeProjectionChanged();
        // Construct workflow AFTER settings normalization so it captures
        // the normalized values — DiscardDragReorderPreview syncs from it.
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _entries,
            SaveSettingsToStore);
        // Load entries and presets from the unified store.
        ReplaceEntries(storeFile.Entries);
        _presetFile = new LauncherPresetFile
        {
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        ApplyPresetFileState(_presetFile, preferredPresetId: null);
        // Sync custom entry order with loaded entries so ValidateCustomOrder
        // passes even when the stored CustomEntryOrder is empty or stale.
        _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds()) };
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
        StatusText = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
    }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    public ReadOnlyObservableCollection<LauncherPresetViewModel> Presets { get; }

    /// <summary>Current settings snapshot. Immutable — replaced on every mutation.
    /// Tests should read from this, not from a stale pre-construction reference.</summary>
    public LauncherSettings Settings => _settings;

    public IReadOnlyDictionary<string, string>? CommandTemplates => _launchWorkflow.CommandTemplates;


    public string ConfigPathDisplay { get; }

    [RelayCommand]
    private void Refresh()
    {
        try
        {
            DiscardDragReorderPreview();
            var storeFile = LauncherStore.Load(_storePath);
            ReplaceEntries(storeFile.Entries);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Refresh.LoadStore");
            StatusText = $"配置加载失败: {ExceptionMessageFormatter.ForUser(ex)}";
            return;
        }

        try
        {
            _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds()) };

            // Cross-validate $key command references against commandTemplates.
            ValidateCommandTemplateReferences();

            LoadPresetFile(preferredPresetId: null);

            ValidatePresetEntryReferences();
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Refresh.Validate");
            StatusText = $"配置验证失败: {ExceptionMessageFormatter.ForUser(ex)}";
            // Reload preset file to ensure _presetFile and UI state are consistent
            // after a partial failure during Phase 2 validation.
            try { LoadPresetFile(preferredPresetId: null); } catch { /* best-effort */ }
            RefreshSelectionState();
            ApplyFilter();
            return;
        }

        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
        StatusText = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
    }

    private void ValidateCommandTemplateReferences()
    {
        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(
            _entries.Select(ToLaunchEntry),
            _settings.CommandTemplates);

        if (missingKeys.Count > 0)
            StatusText = $"警告：以下命令模板键未定义: {string.Join(", ", missingKeys)}";
    }

    /// <summary>
    /// Cross-validate that every preset entry ID references a loaded entry.
    /// Logs dangling references to ErrorLog but does not block loading.
    /// </summary>
    private void ValidatePresetEntryReferences()
    {
        var entryIdSet = GetEntryIds().ToHashSet(EntryId.Comparer);
        foreach (var preset in _presetFile.Presets)
        {
            foreach (var entryId in preset.EntryIds)
            {
                if (!entryIdSet.Contains(entryId))
                {
                    ErrorLog.Write(
                        new InvalidDataException(
                            $"启动组 \"{preset.Name}\" 引用了不存在的启动项 ID: {entryId}"),
                        "PresetValidation");
                }
            }
        }
    }

    private void ApplyStatusText(string? statusText)
    {
        if (statusText is not null)
            StatusText = statusText;
    }

    private bool SaveConfig()
    {
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return false;
        }

        return true;
    }

    /// <summary>Builds a LauncherStoreFile snapshot from current in-memory state.</summary>
    private LauncherStoreFile BuildStoreFile() =>
        new()
        {
            Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = _settings,
            Presets = _presetFile.Presets,
            DefaultPresetId = _presetFile.DefaultPresetId,
        };

    /// <summary>Saves the given store file to disk. Returns null on success,
    /// or an error message on failure.</summary>
    private string? SaveStoreFile(LauncherStoreFile storeFile)
    {
        try { LauncherStore.Save(storeFile, _storePath); return null; }
        catch (Exception ex) { return LauncherPersistenceStatus.StoreSaveFailed(ex); }
    }

    /// <summary>Saves settings to the unified store. Used as callback by
    /// LauncherDragReorderWorkflow during drag commit.</summary>
    private string? SaveSettingsToStore(LauncherSettings settings)
    {
        var storeFile = new LauncherStoreFile
        {
            Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = settings,
            Presets = _presetFile.Presets,
            DefaultPresetId = _presetFile.DefaultPresetId,
        };
        return SaveStoreFile(storeFile);
    }

    private LauncherSettings CaptureSettingsMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return _settings with { SchemaVersion = 1 };
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    private static string RequireSettingsText(string? value, string fieldName) =>
        value ?? throw new InvalidDataException($"Launcher settings {fieldName} is null.");

    private string? SaveSettingsChange(LauncherSettings snapshot)
    {
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
            _settings = snapshot;
        return saveFailure;
    }

    internal void SetKeyboardFocusedEntryId(string? value)
    {
        if (EntryId.Same(_keyboardFocusedEntryId, value))
            return;

        // Clear old focus.
        if (_keyboardFocusedEntryId is not null)
        {
            foreach (var e in _entries)
            {
                if (e is null) continue;
                if (EntryId.Same(e.Id, _keyboardFocusedEntryId))
                    e.IsKeyboardFocused = false;
            }
        }

        _keyboardFocusedEntryId = value;

        // Set new focus.
        if (value is not null)
        {
            foreach (var e in _entries)
            {
                if (e is null) continue;
                if (EntryId.Same(e.Id, value))
                    e.IsKeyboardFocused = true;
            }
        }

        OnPropertyChanged(nameof(KeyboardFocusedEntryId));
    }

    public static string EmptyStateText => "没有匹配的启动项";

    private void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        _entries.Clear();
        foreach (var entry in entries)
        {
            _entries.Add(new LauncherEntryViewModel(entry, this));
        }

        TotalCount = _entries.Count;
        _keyboardFocusedEntryId = null;
        OnPropertyChanged(nameof(KeyboardFocusedEntryId));

        // Force card-size recalculation on next ApplyFilter — new entry VMs
        // start with default dimensions and must be measured against current available size.
        _lastAppliedCardsAvailableSize = new Avalonia.Size(0, 0);
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");

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
        var trimmed = value.Trim();
        if (trimmed.Length != value.Length)
        {
            _filterText = trimmed;
            OnPropertyChanged(nameof(FilterText));
        }
        if (_isHandlingSettingsChange) return;
        _isHandlingSettingsChange = true;
        try
        {
            if (TryPersistSettingsChange(s => s with { LastFilterText = trimmed }))
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
            _settings = snapshot;
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

        var normalized = EntryName.Normalize(term);

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
        if (newHistory.Count > 20)
            newHistory = newHistory.RemoveRange(20, newHistory.Count - 20);

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
            StatusText = LauncherSettingsStatus.WindowBoundsSaveFailed;
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

    private static LauncherSortModeViewModel RequireSortModeSelection(LauncherSortModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher sort mode selection is required.");

    private static LauncherDisplayModeViewModel RequireDisplayModeSelection(LauncherDisplayModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");

    private IEnumerable<string> GetEntryIds() =>
        _entries.Select(entry => entry.Id);

}
