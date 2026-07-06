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
            SchemaVersion = 1,
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

    private void DiscardDragReorderPreview()
    {
        var result = _dragReorderWorkflow.Cancel();
        if (result.RefreshEntries)
        {
            _settings = _dragReorderWorkflow.Settings;
            ApplyFilter();
        }
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
    }    public static string EmptyStateText => "没有匹配的启动项";

    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    // These fields track drag-preview debounce state and are only accessed from the UI thread
    // (via PointerMoved → PreviewMoveEntry). No lock or volatile is needed.
    private (string DraggedId, string TargetId, bool InsertAfter)? _lastPreviewParams;
    private long _lastPreviewTick;

    // ponytail: View checks this in OnDragPointerMoved to detect ViewModel-side
    // cancellation (Escape, search, sort, preset, etc.) and flush View drag state.
    internal bool HasActiveDragReorder => _dragReorderWorkflow.HasActiveSession;

    public bool WouldChangePreview(string draggedEntryId, string targetEntryId, bool insertAfter) =>
        _dragReorderWorkflow.WouldChangePreview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);

    public void BeginDragReorder(string draggedEntryId)
    {
        _lastPreviewParams = null; // reset debounce on new drag
        if (_dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId))
        {
            _settings = _dragReorderWorkflow.Settings;
            ApplyFilter();
        }
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var result = _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
        _settings = _dragReorderWorkflow.Settings;
        if (result.RefreshEntries)
        {
            // Debounce: skip redundant reorder with same drag params within 16ms.
            var now = Environment.TickCount64;
            if (_lastPreviewParams is { DraggedId: var d, TargetId: var t, InsertAfter: var a }
                && d == draggedEntryId && t == targetEntryId && a == insertAfter
                && (now - _lastPreviewTick) < 16)
            {
                return result.Accepted;
            }
            _lastPreviewParams = (draggedEntryId, targetEntryId, insertAfter);
            _lastPreviewTick = now;

            // P0: Skip full ApplyFilter() during drag preview.
            // Only reorder the 2 affected entries in _visibleEntries.
            // Card sizes, filter text, and visible set do not change during drag.
            if (_dragReorderWorkflow.HasActiveSession)
                ReorderVisibleEntryForDrag(draggedEntryId, targetEntryId, insertAfter);
            else
                ApplyFilter();
        }

        return result.Accepted;
    }

    /// <summary>
    /// P0: Directly reorder 2 entries in _visibleEntries during drag.
    /// Avoids the full FilterAndSort → SyncVisibleEntries → ApplyCardSize chain.
    /// O(n) single pass to find indices, then one Move() call.
    /// </summary>
    /// <remarks>
    /// NOTE: This method's equivalence to "FilterAndSort + modified CustomEntryOrder"
    /// is verified by 7 example-based drag equivalence tests in
    /// MainWindowViewModelTests.DragTests.cs. The tests cover single-move, multi-move,
    /// insert-before, insert-after, and edge cases with filtered entries.
    /// A property-based test (e.g. FsCheck / CsCheck) would still be ideal
    /// for exhaustive future confidence.
    /// </remarks>
    private void ReorderVisibleEntryForDrag(string draggedId, string targetId, bool insertAfter)
    {
        int dragIdx = -1, targetIdx = -1;
        for (int i = 0; i < _visibleEntries.Count; i++)
        {
            var id = _visibleEntries[i].Id;
            if (EntryId.Same(id, draggedId)) dragIdx = i;
            if (EntryId.Same(id, targetId)) targetIdx = i;
            if (dragIdx >= 0 && targetIdx >= 0) break;
        }
        if (dragIdx < 0 || targetIdx < 0 || dragIdx == targetIdx) return;

        int newIdx;
        if (insertAfter)
            newIdx = dragIdx < targetIdx ? targetIdx : targetIdx + 1;
        else
            newIdx = dragIdx < targetIdx ? targetIdx - 1 : targetIdx;

        newIdx = Math.Max(0, Math.Min(newIdx, _visibleEntries.Count - 1));
        if (dragIdx == newIdx) return;

        _visibleEntries.Move(dragIdx, newIdx);
        // Update position map so SyncVisibleEntries reads correct indices.
        var from = Math.Min(dragIdx, newIdx);
        var to = Math.Max(dragIdx, newIdx);
        for (var j = from; j <= to; j++)
            _syncPositionByEntry[_visibleEntries[j]] = j;
    }

    public void ClearDragHover()
    {
        _dragReorderWorkflow.ClearHover();
    }

    /// <remarks>
    /// Exceptions from <see cref="LauncherDragReorderWorkflow.Commit"/> propagate
    /// to the UI event handler (<see cref="Views.MainWindow.OnEntryDragPointerPressed"/>
    /// / DoDragReorderAsync's catch block), which sets <c>DragDropEffects.None</c>.
    /// We intentionally do not swallow exceptions here — the caller owns the error path.
    /// </remarks>
    public void CommitDragReorder()
    {
        var result = _dragReorderWorkflow.Commit(IsCustomSortMode);
        _settings = _dragReorderWorkflow.Settings;
        if (result.RefreshEntries)
            ApplyFilter();
        ApplyStatusText(result.Status);
        _lastPreviewParams = null;
    }

    /// <remarks>
    /// Exceptions from <see cref="LauncherDragReorderWorkflow.Cancel"/> propagate
    /// to the UI event handler (same path as <see cref="CommitDragReorder"/>).
    /// The caller sets <c>DragDropEffects.None</c> on failure.
    /// </remarks>
    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        _settings = _dragReorderWorkflow.Settings;
        if (result.RefreshEntries)
            ApplyFilter();
        ApplyStatusText(result.Status);
        _lastPreviewParams = null;
    }
    private Func<LaunchEntry?, Task<EntryDraft?>> _requestEntryDraftAsync =
        static _ => Task.FromResult<EntryDraft?>(null);

    private Func<LauncherEntryDeleteTarget, Task<bool>> _confirmDeleteEntryAsync =
        static _ => Task.FromResult(false);

    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync
    {
        get => _requestEntryDraftAsync;
        set => _requestEntryDraftAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Func<LauncherEntryDeleteTarget, Task<bool>> ConfirmDeleteEntryAsync
    {
        get => _confirmDeleteEntryAsync;
        set => _confirmDeleteEntryAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        DiscardDragReorderPreview();
        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddEntry");
            StatusText = LauncherEntryStatus.AddDialogFailed(ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.AddCanceled;
            return;
        }

        var entry = CreateEntryFromDraftOrReportValidation(
            draft,
            originalId: null,
            previousEntry: null);
        if (entry is null)
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        entry = entry with { UpdatedAtUtc = CurrentTimestampUtc() };
        _entries.Add(new LauncherEntryViewModel(entry, this));
        _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

        if (!SaveEntryCollectionChanges(snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
    }

    internal async Task CloneEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var source = item.SnapshotEntry();
        var cloneSource = new LaunchEntry
        {
            Id = GenerateUniqueCloneId(source.Id, _entries.Select(e => e.Id)),
            Name = source.Name + " (副本)",
            Directory = source.Directory,
            Command = source.Command,
            Selected = false,
        };

        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(cloneSource).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CloneEntry");
            StatusText = LauncherEntryStatus.AddDialogFailed(ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.AddCanceled;
            return;
        }

        var entry = CreateEntryFromDraftOrReportValidation(
            draft,
            originalId: null,
            previousEntry: null);
        if (entry is null)
            return;

        var snapshot = CaptureEntryMutationSnapshot();
        entry = entry with { UpdatedAtUtc = CurrentTimestampUtc() };
        _entries.Add(new LauncherEntryViewModel(entry, this));
        _settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entry.Id) };

        if (!SaveEntryCollectionChanges(snapshot))
            return;

        ApplyStatusText($"已复制按钮: {source.Name} → {entry.Name}（已保存）");
    }

    public static string GenerateUniqueCloneId(string sourceId, IEnumerable<string> existingIds)
    {
        var normalizedSourceId = EntryId.Normalize(sourceId);
        var baseId = normalizedSourceId + "-copy";
        var candidate = baseId;
        var index = 2;
        var idSet = existingIds.ToHashSet(EntryId.Comparer);
        while (idSet.Contains(candidate))
        {
            if (index > 10000)
                throw new InvalidOperationException("Cannot generate a unique clone ID — too many copies exist.");
            candidate = $"{baseId}-{index++}";
        }
        return candidate;
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        EntryDraft? draft;
        try
        {
            draft = await RequestEntryDraftAsync(item.SnapshotEntry()).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "EditEntry.Draft");
            StatusText = LauncherEntryStatus.EditDialogFailed(item.Name, ex);
            return;
        }

        if (draft is null)
        {
            StatusText = LauncherEntryStatus.EditCanceled(item.Name);
            return;
        }

        var oldId = item.Id;
        var updated = CreateEntryFromDraftOrReportValidation(draft, oldId, item.SnapshotEntry());
        if (updated is null)
            return;

        // F11: Step 1 — Handle ID rename FIRST (before mutating the item)
        var snapshot = CaptureEntryMutationSnapshot();
        if (!EntryId.Same(oldId, updated.Id))
        {
            try
            {
                var (updatedPresetFile, updatedOrder, _) = LauncherEntryReferences.RenameEntryId(_presetFile, _settings.CustomEntryOrder, oldId, updated.Id);
                _presetFile = updatedPresetFile;
                _settings = _settings with { CustomEntryOrder = updatedOrder };
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "EditEntry.RenameReference");
                RestoreEntryMutationSnapshot(snapshot);
                StatusText = LauncherEntryStatus.EditDialogFailed(item.Name, ex);
                return;
            }
        }
        // F11: Step 2 — Now mutate the item (safe — rename already persisted)
        item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());

        if (!SaveEntryCollectionChanges(snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Updated(item.Name));
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var target = new LauncherEntryDeleteTarget(item.SnapshotEntry());
        if (!await ConfirmDeleteEntryIfRequiredAsync(target).ConfigureAwait(true))
            return;

        // Clear keyboard focus before removal so _keyboardFocusedEntryId does not
        // retain a stale reference to the deleted entry.
        if (EntryId.Same(_keyboardFocusedEntryId, target.EntryId))
            KeyboardFocusedEntryId = null;

        var snapshot = CaptureEntryMutationSnapshot();
        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, target.EntryId);
        _presetFile = updatedPresetFile;
        _settings = _settings with { CustomEntryOrder = updatedOrder };
        _entries.Remove(item);
        if (!SaveEntryCollectionChanges(snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Deleted(target.EntryName));
    }

    private async Task<bool> ConfirmDeleteEntryIfRequiredAsync(LauncherEntryDeleteTarget target)
    {
        bool accepted;
        try
        {
            accepted = await ConfirmDeleteEntryAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeleteConfirmation");
            StatusText = LauncherEntryStatus.DeleteConfirmationFailed(target.EntryName, ex);
            return false;
        }

        if (accepted)
            return true;

        StatusText = LauncherEntryStatus.DeleteCanceled(target.EntryName);
        return false;
    }

    private LaunchEntry? CreateEntryFromDraftOrReportValidation(
        EntryDraft draft,
        string? originalId,
        LaunchEntry? previousEntry)
    {
        var entry = LauncherEntryDrafts.CreateEntry(draft, previousEntry);
        var validation = LauncherEntryDrafts.ValidateEntry(
            entry,
            originalId,
            _entries.Select(ToLaunchEntry));
        if (validation is null)
            return entry;

        ApplyStatusText(validation);
        return null;
    }

    private bool SaveEntryCollectionChanges(LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = _entries.Count;
        EnsureCustomOrderContainsEntries();

        var saveFailure = SaveStoreFile(BuildStoreFile());

        if (saveFailure is not null)
        {
            StatusText = saveFailure;
            RestoreEntryMutationSnapshot(rollbackSnapshot);
            return false;
        }

        RefreshSelectionState();
        ApplyFilter();
        return true;
    }

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return LauncherEntryMutationSnapshot.Capture(
            _entries.Select(ToLaunchEntry),
            _presetFile,
            _settings,
            SelectedPreset?.Id);
    }

    private void RestoreEntryMutationSnapshot(LauncherEntryMutationSnapshot snapshot)
    {
        ReplaceEntries(snapshot.CloneEntries());
        _settings = snapshot.CloneSettings();
        ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
    }

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
    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches ? "启动需确认" : "启动无需确认";

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? "所有启动项启动前统一确认。"
        : "所有启动项直接启动，不弹确认。";

    private Func<LauncherSingleLaunchConfirmation, Task<bool>> _confirmLaunchAsync =
        static _ => throw new InvalidOperationException("ConfirmLaunchAsync is not bound by the View.");

    public Func<LauncherSingleLaunchConfirmation, Task<bool>> ConfirmLaunchAsync
    {
        get => _confirmLaunchAsync;
        set => _confirmLaunchAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        if (_isHandlingSettingsChange) return;
        _isHandlingSettingsChange = true;
        try
        {
            RefreshLaunchConfirmationPresentation();

            if (TryPersistSettingsChange(
                s => s with { RequireConfirmationForLaunches = value },
                LauncherSettingsStatus.LaunchConfirmationSaved(value)))
            {
                // Success — status and presentation already applied.
            }
            else
            {
                _requireConfirmationForLaunches = _settings.RequireConfirmationForLaunches;
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                RefreshLaunchConfirmationPresentation();
            }
        }
        finally
        {
            _isHandlingSettingsChange = false;
        }
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        DiscardDragReorderPreview();
        var entry = item.SnapshotEntry();
        var entryName = EntryName.Normalize(entry.Name);
        try
        {
            var plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
            DryRunText = plan.CommandLine;
            StatusText = LauncherSingleLaunchStatus.Preview(entryName, plan.RequiresConfirmation);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "ShowDryRun");
            if (entryName.Length == 0)
            {
                DryRunText = LauncherSingleLaunchReceiptText.PreviewFailed(ex);
                StatusText = LauncherSingleLaunchStatus.PreviewFailed(ex);
                return;
            }

            DryRunText = LauncherSingleLaunchReceiptText.PreviewFailed(entryName, ex);
            StatusText = LauncherSingleLaunchStatus.PreviewFailed(entryName);
        }
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        if (_isLaunching) return;
        _isLaunching = true;
        try
        {
            DiscardDragReorderPreview();
            var entry = item.SnapshotEntry();
            var entryName = EntryName.Normalize(entry.Name);
            LaunchPlan? plan = null;
            try
            {
                plan = _launchWorkflow.BuildPlan(entry, RequireConfirmationForLaunches);
                var confirmation = new LauncherSingleLaunchConfirmation(entry, plan);
                if (!await ConfirmSingleLaunchIfRequiredAsync(confirmation).ConfigureAwait(true))
                    return;

                _launchWorkflow.Start(plan);
                DryRunText = LauncherSingleLaunchReceiptText.Requested(entryName, plan);
                item.RecordLaunchFromOwner(CurrentTimestampUtc());
                if (!SaveConfig())
                    return;
                StatusText = LauncherSingleLaunchStatus.Requested(entryName);
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "Launch");
                if (entryName.Length == 0)
                {
                    DryRunText = LauncherSingleLaunchReceiptText.Failed(ex);
                    StatusText = LauncherSingleLaunchStatus.Failed(ex);
                    return;
                }

                DryRunText = plan is null
                    ? LauncherSingleLaunchReceiptText.Failed(entryName, ex)
                    : LauncherSingleLaunchReceiptText.Failed(entryName, plan, ex);
                StatusText = LauncherSingleLaunchStatus.Failed(entryName, ex);
            }
        }
        finally
        {
            _isLaunching = false;
        }
    }

    private async Task<bool> ConfirmSingleLaunchIfRequiredAsync(LauncherSingleLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var entryName = confirmation.EntryName;
        var plan = confirmation.Plan;
        if (!plan.RequiresConfirmation)
            return true;

        bool accepted;
        try
        {
            accepted = await ConfirmLaunchAsync(confirmation).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "LaunchConfirmation");
            DryRunText = LauncherSingleLaunchReceiptText.ConfirmationFailed(entryName, plan, ex);
            StatusText = LauncherSingleLaunchStatus.Failed(entryName, ex);
            return false;
        }

        if (accepted)
            return true;

        DryRunText = LauncherSingleLaunchReceiptText.Canceled(entryName, plan);
        StatusText = LauncherSingleLaunchStatus.Canceled(entryName);
        return false;
    }

    private void RefreshLaunchConfirmationPresentation()
    {
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        LauncherEntryLayoutPresentation.Refresh(_entries);
        ApplyFilter();
    }

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

    public int GetPresetReferenceCount(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return 0;

        var normalized = EntryId.Normalize(entryId);
        return _presetFile.Presets.Count(preset =>
            preset.EntryIds.Any(id => EntryId.Same(id, normalized)));
    }
    private bool TryGetApplicablePresetSelection(
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset,
        [NotNullWhen(true)] out LauncherPresetSelectionPreview? selection)
    {
        selectedPreset = GetSelectedPresetOrReportMissing();
        selection = null;
        if (selectedPreset is null)
            return false;

        selection = BuildPresetSelectionPreview(GetSelectedEntryIds());
        if (selection.MissingConfigIds.Count > 0)
        {
            ApplyStatusText(LauncherPresetStatus.MissingEntryReferences(selection.MissingConfigIds));
            return false;
        }

        return true;
    }

    private LauncherPresetViewModel? GetSelectedPresetOrReportMissing()
    {
        if (TryGetSelectedPreset(out var selectedPreset))
            return selectedPreset;

        StatusText = LauncherPresetStatus.NoPresetSelected;
        return null;
    }

    private bool TryGetSelectedPreset([NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset)
    {
        selectedPreset = SelectedPreset;
        return selectedPreset is not null;
    }

    private async Task<bool> ConfirmDeletePresetIfRequiredAsync(LauncherPresetDeleteTarget target)
    {
        bool accepted;
        try
        {
            accepted = await ConfirmDeletePresetAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            StatusText = LauncherPresetStatus.DeleteConfirmationFailed(target.PresetName, ex);
            return false;
        }

        if (accepted)
            return true;

        StatusText = LauncherPresetStatus.DeleteCanceled(target.PresetName);
        return false;
    }

    private async Task<string?> RequestPresetNameIfRequiredAsync(LauncherPresetSaveTarget target)
    {
        string? presetName;
        try
        {
            presetName = await RequestNewPresetAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            StatusText = LauncherPresetStatus.SaveDialogFailed(ex);
            return null;
        }

        if (presetName is null)
        {
            StatusText = LauncherPresetStatus.SaveCanceled;
            return null;
        }

        var normalizedName = EntryName.Normalize(presetName);
        if (normalizedName.Length == 0)
        {
            StatusText = LauncherPresetStatus.NameRequired;
            return null;
        }

        return normalizedName;
    }

    private (LauncherPresetFile PresetFile, string? SelectedPresetId) CapturePresetMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return (_presetFile, SelectedPreset?.Id);
    }

    private void RestorePresetMutationSnapshot((LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot) =>
        ApplyPresetFileState(snapshot.PresetFile, snapshot.SelectedPresetId);

    private void RestorePresetSnapshotAndReportMissing((LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot)
    {
        RestorePresetMutationSnapshot(snapshot);
        ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();
    }

    /// <summary>
    /// Always allow apply when a preset with entries is selected, even if the
    /// current selection exactly matches — re-applying a preset also forces a config
    /// save, which is useful after external modifications.
    /// </summary>
    private bool CanApplyPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.MissingConfigIds.Count == 0
        && selection.PresetIds.Count > 0;

    private bool CanLaunchSelectedPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.MissingConfigIds.Count == 0
        && selection.PresetIds.Count > 0;

    private bool CanUpdateSelectedPreset()
    {
        if (!TryBuildPresetSelectionPreviewForCommand(out var selectedPreset, out var selection))
            return false;

        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, selectedPreset.Id))
            return false;

        return selection.SelectedIds.Count > 0 && selection.HasChanges;
    }

    private bool CanDeleteSelectedPreset() =>
        TryGetSelectedPreset(out var selectedPreset)
        && LauncherPresetWorkflow.CanDeletePreset(_presetFile, selectedPreset.Id);

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(IReadOnlyList<string> selectedEntryIds)
    {
        if (!TryGetSelectedPreset(out var selectedPreset))
            return LauncherPresetSelectionPreview.NoPreset(PresetCount);

        return BuildPresetSelectionPreview(selectedPreset, selectedEntryIds);
    }

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(
        LauncherPresetViewModel selectedPreset,
        IReadOnlyList<string> selectedEntryIds)
    {
        return LauncherPresetSelectionPreview.ForPreset(
            selectedPreset.Name,
            selectedEntryIds,
            GetEntryIds(),
            selectedPreset.EntryIds);
    }

    private bool TryBuildPresetSelectionPreviewForCommand(
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset,
        [NotNullWhen(true)] out LauncherPresetSelectionPreview? selection)
    {
        selectedPreset = null;
        selection = null;
        if (!TryGetSelectedPreset(out selectedPreset) || _entries.Count == 0)
            return false;

        selection = BuildPresetSelectionPreview(selectedPreset, GetSelectedEntryIds());
        return true;
    }

    private List<LauncherEntryViewModel> GetPresetEntries(IEnumerable<string> entryIds)
    {
        var entriesById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
        foreach (var entry in _entries)
        {
            if (!entriesById.ContainsKey(entry.Id))
                entriesById[entry.Id] = entry;
        }

        var uniqueIds = EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds));
        var result = new List<LauncherEntryViewModel>(uniqueIds.Count);
        foreach (var id in uniqueIds)
        {
            // Use TryGetValue instead of indexer to give a clear error when
            // a preset references an entry ID that does not exist in _entries.
            if (!entriesById.TryGetValue(id, out var entry))
                throw new InvalidDataException($"Preset entry ID '{id}' does not match any loaded entry.");

            result.Add(entry);
        }

        return result;
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        PreviewSelectedPresetCommand.NotifyCanExecuteChanged();
        LaunchSelectedPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }
    private void LoadPresetFile(string? preferredPresetId)
    {
        LauncherStoreFile storeFile;
        try { storeFile = LauncherStore.Load(_storePath); }
        catch { storeFile = new LauncherStoreFile(); }
        _presetFile = new LauncherPresetFile
        {
            SchemaVersion = 1,
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        ApplyPresetFileState(_presetFile, preferredPresetId);
    }

    private void ApplyPresetFileState(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        var rawTargetId = preferredPresetId ?? presetFile.DefaultPresetId;
        var targetPresetId = PresetId.NormalizeReference(rawTargetId);
        var presetViewModels = presetFile.Presets
            .Select(preset => new LauncherPresetViewModel(preset))
            .ToList();

        // F07: If no valid default preset ID but presets exist, fall back to first preset
        if (targetPresetId.Length == 0 && presetViewModels.Count > 0)
            targetPresetId = presetViewModels[0].Id;

        var selectedPreset = ResolveSelectedPreset(presetViewModels, targetPresetId);

        _presetFile = presetFile;  // F08: write after validation

        _presets.Clear();
        foreach (var preset in presetViewModels)
            _presets.Add(preset);

        SelectedPreset = selectedPreset;
        PresetCount = _presets.Count;
    }

    private static LauncherPresetViewModel? ResolveSelectedPreset(
        IReadOnlyList<LauncherPresetViewModel> presets,
        string targetPresetId)
    {
        if (presets.Count == 0)
        {
            if (targetPresetId.Length != 0)
                throw new InvalidDataException(
                    $"Launcher selected preset id is invalid without presets: {targetPresetId}.");

            return null;
        }

        if (targetPresetId.Length == 0)
            throw new InvalidDataException("Launcher selected preset id is required when presets exist.");

        return presets.FirstOrDefault(preset => PresetId.Same(preset.Id, targetPresetId))
            ?? throw new InvalidDataException(
                $"Launcher selected preset id does not match any preset: {targetPresetId}.");
    }

    private bool SavePresetMutationAndApply(
        (LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot,
        string selectedPresetId,
        out LauncherPresetViewModel? selectedPreset)
    {
        // When no preset remains (e.g., deleting the last preset), selectedPresetId
        // is empty and SelectedPreset resolves to null — the dropdown shows nothing.
        selectedPreset = null;
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            ApplyPresetFileState(snapshot.PresetFile, snapshot.SelectedPresetId);
            return false;
        }

        if (PresetId.NormalizeReference(selectedPresetId).Length > 0)
        {
            ApplyPresetFileState(_presetFile, selectedPresetId);
            selectedPreset = SelectedPreset;
        }
        else
        {
            ApplyPresetFileState(_presetFile, null);
        }
        return true;
    }
    private Func<LauncherPresetSaveTarget, Task<string?>> _requestNewPresetAsync =
        static _ => Task.FromResult<string?>(null);

    private Func<LauncherPresetDeleteTarget, Task<bool>> _confirmDeletePresetAsync =
        static _ => Task.FromResult(false);

    public Func<LauncherPresetSaveTarget, Task<string?>> RequestNewPresetAsync
    {
        get => _requestNewPresetAsync;
        set => _requestNewPresetAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Func<LauncherPresetDeleteTarget, Task<bool>> ConfirmDeletePresetAsync
    {
        get => _confirmDeletePresetAsync;
        set => _confirmDeletePresetAsync = value ?? throw new ArgumentNullException(nameof(value));
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        DiscardDragReorderPreview();

        if (!TryGetApplicablePresetSelection(out var selectedPreset, out var selection))
        {
            // Update button text to reflect the failure reason (e.g. "检查启动组" for missing entries).
            RefreshSelectionState();
            return;
        }

        if (ReplaceSelection(selection.PresetIds))
            ApplyStatusText(LauncherPresetStatus.Applied(selectedPreset.Name, selection.PresetIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private async Task LaunchSelectedPresetAsync()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        await LaunchBatchAsync(
            GetPresetEntries(selectedPreset.EntryIds),
            $"启动组“{selectedPreset.Name}”条目",
            LauncherPresetStatus.NoPresetEntries(selectedPreset.Name),
            count => LauncherPresetStatus.LaunchCanceled(selectedPreset.Name, count),
            ex => LauncherPresetStatus.LaunchFailed(selectedPreset.Name, ex),
            report => LauncherPresetStatus.Launched(selectedPreset.Name, report)).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanLaunchSelectedPreset))]
    private void PreviewSelectedPreset()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        var entries = GetPresetEntries(selectedPreset.EntryIds);
        if (entries.Count == 0)
        {
            StatusText = LauncherPresetStatus.NoPresetEntries(selectedPreset.Name);
            return;
        }

        DiscardDragReorderPreview();
        try
        {
            var preflight = _launchWorkflow.PreflightBatch(
                entries.Select(ToLaunchEntry),
                RequireConfirmationForLaunches);
            if (preflight.Report.FailedCount > 0)
            {
                DryRunText = LauncherBatchLaunchPlanText.FailedPreflight(preflight);
                StatusText = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, preflight.Report);
                return;
            }

            DryRunText = LauncherBatchLaunchPlanText.Format(preflight.Items);
            StatusText = LauncherPresetStatus.Previewed(selectedPreset.Name, preflight.Items.Count);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "PresetPreview");
            DryRunText = LauncherBatchLaunchPlanText.Failed(ex);
            StatusText = LauncherPresetStatus.PreviewFailed(selectedPreset.Name, ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        DiscardDragReorderPreview();
        if (SelectedCount == 0)
        {
            StatusText = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        var target = new LauncherPresetSaveTarget(GetSelectedEntryIds());
        var presetName = await RequestPresetNameIfRequiredAsync(target).ConfigureAwait(true);
        if (presetName is null)
            return;

        var snapshot = CapturePresetMutationSnapshot();
        LauncherPreset? preset;
        try
        {
            preset = LauncherPresetWorkflow.AddPreset(ref _presetFile, presetName, target.EntryIds);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddPreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = $"新增启动组失败: {ExceptionMessageFormatter.ForUser(ex)}";
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset))
            return;

        if (savedPreset is null) return;
        ApplyStatusText(LauncherPresetStatus.Saved(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, selectedPreset.Id))
        {
            StatusText = LauncherPresetStatus.UpdateBlocked(selectedPreset.Name);
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            StatusText = LauncherSelectionStatus.SelectionRequired;
            return;
        }

        // Defense-in-depth: skip the update if the selection already matches
        // the preset — avoids unnecessary snapshot capture and file I/O.
        // CanUpdateSelectedPreset already guards against this (via HasChanges),
        // but repeating the check here protects against future callers that
        // might bypass the CanExecute guard.
        var preview = BuildPresetSelectionPreview(selectedPreset, entryIds);
        if (!preview.HasChanges)
        {
            StatusText = $"启动组已是最新状态: {selectedPreset.Name}";
            return;
        }

        var snapshot = CapturePresetMutationSnapshot();
        LauncherPreset? preset;
        try
        {
            preset = LauncherPresetWorkflow.UpdatePreset(ref _presetFile, selectedPreset.Id, selectedPreset.Name, entryIds);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "UpdatePreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = LauncherPresetStatus.UpdateFailed(selectedPreset.Name, ex);
            return;
        }
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, preset.Id, out var savedPreset))
            return;

        if (savedPreset is null) return;
        ApplyStatusText(LauncherPresetStatus.Updated(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        DiscardDragReorderPreview();
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        var target = new LauncherPresetDeleteTarget(
            selectedPreset.Id,
            selectedPreset.Name,
            selectedPreset.EntryIds);
        if (!LauncherPresetWorkflow.CanDeletePreset(_presetFile, target.PresetId))
        {
            StatusText = LauncherPresetStatus.DeleteBlocked(target.PresetName);
            return;
        }

        // Defer snapshot capture until after user confirmation — avoids
        // wasteful snapshot/restore cycle when the user cancels.
        if (!await ConfirmDeletePresetIfRequiredAsync(target).ConfigureAwait(true))
        {
            return;
        }

        var snapshot = CapturePresetMutationSnapshot();

        LauncherPreset? preset;
        string fallbackPresetId;
        try
        {
            preset = LauncherPresetWorkflow.DeletePreset(ref _presetFile, target.PresetId, out fallbackPresetId);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeletePreset");
            RestorePresetSnapshotAndReportMissing(snapshot);
            StatusText = $"删除启动组失败: {ExceptionMessageFormatter.ForUser(ex)}";
            return;
        }
        if (preset is null)
        {
            RestorePresetSnapshotAndReportMissing(snapshot);
            return;
        }
        if (!SavePresetMutationAndApply(snapshot, fallbackPresetId, out var fallbackPreset))
            return;

        ApplyStatusText(fallbackPreset is not null
            ? LauncherPresetStatus.Deleted(target.PresetName, fallbackPreset.Name)
            : LauncherPresetStatus.Deleted(target.PresetName, string.Empty));
    }
    public bool CanMoveEntryUp(LauncherEntryViewModel entry) =>
        IsCustomSortMode && _visibleEntries.IndexOf(entry) > 0;

    public bool CanMoveEntryDown(LauncherEntryViewModel entry)
    {
        var index = _visibleEntries.IndexOf(entry);
        return IsCustomSortMode && index >= 0 && index < _visibleEntries.Count - 1;
    }

    public void MoveEntryUp(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: -1, insertAfter: false);

    public void MoveEntryDown(LauncherEntryViewModel entry) =>
        MoveEntryRelative(entry, targetOffset: 1, insertAfter: true);

    private void MoveEntryRelative(LauncherEntryViewModel entry, int targetOffset, bool insertAfter)
    {
        // Sync entries first so the snapshot includes any newly-added IDs.
        EnsureCustomOrderContainsEntries();
        var snapshot = CaptureSettingsMutationSnapshot();

        if (!TryResolveVisibleMoveTarget(entry, targetOffset, out var target))
        {
            StatusText = LauncherDragReorderStatus.MoveUnavailable;
            RefreshReorderCommandState();
            return;
        }
        var (changed, updatedOrder) = LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, entry.Id, target.Id, insertAfter);
        if (!changed)
        {
            // Re-sync rather than restoring the snapshot: EnsureCustomOrderContainsEntries
            // may have added new entry IDs after the snapshot was captured, and restoring
            // the snapshot would lose them.
            EnsureCustomOrderContainsEntries();
            ApplyFilter();
            StatusText = LauncherDragReorderStatus.MoveUnavailable;
            return;
        }
        _settings = _settings with { CustomEntryOrder = updatedOrder };

        var saveFailure = SaveSettingsChange(snapshot);
        ApplyFilter();
        ApplyStatusText(saveFailure ?? LauncherDragReorderStatus.Moved(entry.Name));
    }

    private bool TryResolveVisibleMoveTarget(
        LauncherEntryViewModel entry,
        int targetOffset,
        out LauncherEntryViewModel target)
    {
        var sourceIndex = _visibleEntries.IndexOf(entry);
        var targetIndex = sourceIndex + targetOffset;
        if (!IsCustomSortMode || sourceIndex < 0 || targetIndex < 0 || targetIndex >= _visibleEntries.Count)
        {
            target = entry;
            return false;
        }

        target = _visibleEntries[targetIndex];
        return true;
    }

    private void RefreshReorderCommandState()
    {
        foreach (var entry in _entries)
            entry.RefreshReorderCommands();
    }
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
        LauncherBatchLaunchPreflight preflight = null!;
        try
        {
            OnPropertyChanged(nameof(IsBatchLaunching));
            var selectedEntries = entries.Select(ToLaunchEntry).ToList();
            preflight = _launchWorkflow.PreflightBatch(
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
            var entryById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
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
            // Save AFTER timestamps are recorded so launch history is persisted.
            if (startedCount > 0 && !SaveConfig())
                return;
            DryRunText = LauncherBatchLaunchReceiptText.Format(preflight, report);
            StatusText = completedStatus(report);
        }
        catch (OperationCanceledException)
        {
            DryRunText = LauncherBatchLaunchReceiptText.Canceled(preflight?.Items ?? []);
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
public sealed record LauncherSortModeViewModel(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record LauncherDisplayModeViewModel(LauncherEntryViewMode Mode, string Label)
{
    public string Id => LauncherEntryViewModes.ToId(Mode);

    public override string ToString() => Label;
}

public static class LauncherMainWindowText
{
    public static string Ready => "就绪";

    public static string DryRunPlaceholder => "选择一个启动项查看预演命令。";

    public static string PresetSelectionPlaceholder => "选择启动组后可查看与当前勾选的差异。";

    public static string StorePathDisplay(string storePath) =>
        $"存储: {storePath}";

    public static string Loaded(int entryCount, int presetCount) =>
        $"已加载 {entryCount} 个启动项、{presetCount} 个启动组；勾选自动保存到 config\\launcher.json";
}

public static class ExceptionMessageFormatter
{
    /// <summary>
    /// Sanitize exception messages for user-facing display.
    /// File-system exceptions leak paths in their Message property;
    /// this replaces them with stable, user-safe strings.
    /// </summary>
    public static string ForUser(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            // File-system exceptions leak paths in their Message — replace with safe text.
            FileNotFoundException => "找不到文件",
            DirectoryNotFoundException => "找不到目录",
            UnauthorizedAccessException => "没有访问权限",
            IOException => "文件操作失败",
            // Application-layer exceptions carry intentionally user-safe messages crafted
            // by the throwing code — pass them through.
            InvalidDataException or ArgumentException or InvalidOperationException => exception.Message,
            // Unknown exception types: use generic text to avoid leaking internal details.
            _ => "操作失败，请查看错误日志了解详情",
        };
    }
}

public static class LauncherPersistenceStatus
{
    public static string StoreSaveFailed(Exception exception) =>
        Failed("存储保存失败", exception);

    public static string ConfigSaveFailed(Exception exception) =>
        Failed("配置保存失败", exception);

    public static string PresetSaveFailed(Exception exception) =>
        Failed("启动组保存失败", exception);

    public static string SettingsSaveFailed(Exception exception) =>
        Failed("设置保存失败", exception);

    private static string Failed(string prefix, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{prefix}: {ExceptionMessageFormatter.ForUser(exception)}";
    }
}

public static class LauncherEntryStatus
{
    public static string AddCanceled => "已取消: 新增按钮";

    public static string AddDialogFailed(Exception exception) =>
        $"新增按钮对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string EditCanceled(string entryName) =>
        $"已取消: 编辑 {entryName}";

    public static string EditDialogFailed(string entryName, Exception exception) =>
        $"编辑 {entryName} 对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string DeleteCanceled(string entryName) =>
        $"已取消: 删除 {entryName}";

    public static string DeleteConfirmationFailed(string entryName, Exception exception) =>
        $"删除确认失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}";

    public static string Added(string entryName) =>
        $"已新增按钮: {entryName}（已保存）";

    public static string Updated(string entryName) =>
        $"已更新按钮: {entryName}（已保存）";

    public static string Deleted(string entryName) =>
        $"已删除按钮: {entryName}（已保存）";
}

public static class LauncherPresetStatus
{
    public static string NoPresetSelected => "未选择启动组";

    public static string SaveCanceled => "已取消: 保存启动组";

    public static string SaveDialogFailed(Exception exception) =>
        $"保存启动组对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string NameRequired => "启动组名称不能为空";

    public static string CurrentPresetMissing => "当前启动组已不存在，请重新加载";

    public static string MissingEntryReferences(IEnumerable<string> entryIds) =>
        $"启动组引用了不存在的启动项: {string.Join(", ", entryIds)}";

    public static string NoPresetEntries(string presetName) =>
        $"启动组\u201c{presetName}\u201d没有启动项";

    public static string Applied(string presetName, int entryCount) =>
        $"已应用启动组: {presetName}（{entryCount} 项，已保存）";

    public static string Previewed(string presetName, int entryCount) =>
        $"预演启动组: {presetName}（{entryCount} 项）";

    public static string PreviewFailed(string presetName, LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return $"启动组\u201c{presetName}\u201d预演失败: {report.FailedCount} 个失败";
    }

    public static string PreviewFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d预演失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Launched(string presetName, LauncherBatchLaunchReport report) =>
        $"启动组\u201c{presetName}\u201d: {LauncherSelectionStatus.BatchLaunchCompleted(report, "启动项")}";

    public static string LaunchCanceled(string presetName, int entryCount) =>
        $"已取消: 启动组 {presetName}（{entryCount} 项）";

    public static string LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Saved(string presetName, int entryCount) =>
        $"已保存启动组: {presetName}（{entryCount} 项）";

    public static string UpdateFailed(string presetName, Exception exception) =>
        $"更新启动组失败: {presetName} — {ExceptionMessageFormatter.ForUser(exception)}";

    public static string UpdateBlocked(string presetName) =>
        $"系统启动组不能更新: {presetName}";

    public static string Updated(string presetName, int entryCount) =>
        $"已更新启动组: {presetName}（{entryCount} 项）";

    public static string DeleteBlocked(string presetName) =>
        $"启动组不能删除: {presetName}";

    public static string DeleteCanceled(string presetName) =>
        $"已取消: 删除启动组 {presetName}";

    public static string DeleteConfirmationFailed(string presetName, Exception exception) =>
        $"删除启动组确认失败: {presetName} \u2014 {ExceptionMessageFormatter.ForUser(exception)}";

    public static string Deleted(string presetName, string selectedPresetName) =>
        $"已删除启动组: {presetName}；当前启动组: {selectedPresetName}";
}

public static class LauncherSelectionStatus
{
    public static string NoVisibleEntries => "当前没有可勾选的可见项";

    public static string VisibleAlreadySelected => "可见项已全部勾选";

    public static string NoSelectedEntries => "未选择任何启动项";

    public static string NoSelectedEntriesToClear => "当前没有已勾选项";

    public static string SelectionRequired => "请先勾选至少一个启动项";

    public static string VisibleSelected(int entryCount) =>
        $"已勾选可见 {entryCount} 项（已保存）";

    public static string Cleared(int entryCount) =>
        $"已清空 {entryCount} 个勾选项（已保存）";

    public static string BatchLaunchCanceled(int entryCount) =>
        $"已取消: 批量启动 {entryCount} 项";

    public static string BatchLaunchCompleted(LauncherBatchLaunchReport report)
    {
        return BatchLaunchCompleted(report, "勾选项");
    }

    public static string BatchLaunchCompleted(LauncherBatchLaunchReport report, string itemLabel)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);
        return report.FailedCount == 0
            ? $"已请求启动 {report.RequestedCount} 个{itemLabel}"
            : $"已请求启动 {report.RequestedCount} 个{itemLabel}，{report.FailedCount} 个失败{FormatSkipped(report)}：{FormatBatchFailures(report.FailureMessages)}";
    }

    private static string FormatSkipped(LauncherBatchLaunchReport report) =>
        report.SkippedCount == 0 ? "" : $"，{report.SkippedCount} 个未启动";

    public static string BatchLaunchFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    private static string FormatBatchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += $"；…另有 {failures.Count - 3} 项";

        return preview;
    }
}

public static class LauncherSettingsStatus
{
    public static string SortModeSaved(string modeLabel) =>
        $"排序模式: {modeLabel}（已保存）";

    public static string DisplayModeSaved(string modeLabel) =>
        $"视图模式: {modeLabel}（已保存）";

    public static string LaunchConfirmationSaved(bool requireConfirmation) =>
        requireConfirmation
            ? "启动确认: 所有启动项启动前统一确认"
            : "启动确认: 所有启动项直接启动";

    public static string WindowBoundsSaveFailed =>
        "窗口尺寸保存失败；下次将使用上次保存的尺寸";
}

public static class LauncherSingleLaunchStatus
{
    public static string Preview(string entryName, bool requiresConfirmation) =>
        requiresConfirmation
            ? $"预演: {entryName}（需要确认）"
            : $"预演: {entryName}";

    public static string PreviewFailed(string entryName) =>
        $"预演失败: {entryName}";

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Canceled(string entryName) =>
        $"已取消: {entryName}";

    public static string Requested(string entryName) =>
        $"已请求启动: {entryName}";

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }
}

public static class LauncherSingleLaunchReceiptText
{
    public static string Requested(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已请求启动: {RequireName(entryName)}",
            plan.CommandLine,
            $"（实际执行: {plan.ExecutionCommandLine}）");
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已取消: {RequireName(entryName)}",
            plan.CommandLine,
            $"（实际执行: {plan.ExecutionCommandLine}）");
    }

    public static string PreviewFailed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {RequireName(entryName)}",
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string ConfirmationFailed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动确认失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    private static string RequireName(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        var normalizedName = EntryName.Normalize(entryName);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Entry name is required for single launch receipt.", nameof(entryName));

        return normalizedName;
    }
}

public static class LauncherBatchLaunchPlanText
{
    public static string Format(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [];
        AppendPlanLines(lines, items, "• ");
        return JoinBlocks(lines);
    }

    public static string FailedPreflight(LauncherBatchLaunchPreflight preflight)
    {
        ArgumentNullException.ThrowIfNull(preflight);

        List<string> lines = [
            "预演回执",
            $"预演失败: 失败 {preflight.Report.FailedCount} 项，未启动 {preflight.Items.Count} 项",
        ];

        AppendPlanLines(lines, preflight.Items, "• 未启动: ");
        AppendFailureLines(lines, preflight.Report.FailureMessages);
        return JoinBlocks(lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return JoinBlocks(
        [
            "预演回执",
            $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}",
        ]);
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        string prefix)
    {
        foreach (var item in items)
            lines.Add($"{prefix}{item.EntryName}{Environment.NewLine}{item.Plan.CommandLine}{Environment.NewLine}（实际: {item.Plan.ExecutionCommandLine}）");
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("失败:");
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }

    private static string JoinBlocks(IReadOnlyList<string> lines) =>
        string.Join(Environment.NewLine + Environment.NewLine, lines);
}

public static class LauncherBatchLaunchReceiptText
{
    public static string Format(
        LauncherBatchLaunchPreflight preflight,
        LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(report);

        List<string> lines = [
            "启动回执",
            $"已请求启动 {report.RequestedCount} 项，失败 {report.FailedCount} 项，未启动 {report.SkippedCount} 项",
        ];

        AppendPlanLines(lines, preflight.Items, report);
        AppendFailureLines(lines, report.FailureMessages);
        return string.Join(Environment.NewLine, lines);
    }

    public static string Canceled(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [
            "启动回执",
            $"已取消: 已请求启动 0 项，未启动 {items.Count} 项",
        ];

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string ConfirmationFailed(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(exception);

        var lines = new List<string>
        {
            "启动回执",
            $"启动确认失败: {ExceptionMessageFormatter.ForUser(exception)}",
            $"已请求启动 0 项，未启动 {items.Count} 项",
        };

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report)
    {
        if (items.Count == 0)
            return;

        lines.Add("");
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lines.Add($"• {StatusFor(index, report)}: {item.EntryName}");
            lines.Add(item.Plan.CommandLine);
        }
    }

    private static string StatusFor(int index, LauncherBatchLaunchReport report)
    {
        // R9: With continue-on-failure, failures may be interspersed with successes.
        // Use the full failure-indices set rather than a single FailureIndex.
        if (report.FailureIndices.Contains(index))
            return "启动失败";

        // All non-failure items up through (RequestedCount + FailedCount - 1) are "已请求".
        // Items past that range are "未启动" (only happens when items are skipped).
        return index < report.RequestedCount + report.FailedCount ? "已请求" : "未启动";
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("");
        lines.Add("失败:");
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }

    private static void AppendUnstartedPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        if (items.Count == 0)
            return;

        lines.Add("");
        foreach (var item in items)
        {
            lines.Add($"• 未启动: {item.EntryName}");
            lines.Add(item.Plan.CommandLine);
        }
    }
}
public static class LauncherDragReorderStatus
{
    public static string Saved => "已保存自定义拖拽排序";

    public static string Canceled => "已取消: 拖拽排序";

    public static string Moved(string entryName) => $"已保存自定义排序: {entryName}";

    public static string MoveUnavailable => "当前条目不能继续移动";
}
