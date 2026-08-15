using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using WTLauncher.App.Features;
using WTLauncher.App.Features.ConfigExchange;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IEntryListViewModel, ILaunchCoordinatorContext, ICommandExecutionContext, IStatusSink, ICommandHost, ICommandEditApplier, IDirectoryEditApplier, IProjectionSynchronizer, IDisposable
{
    internal readonly StoreGrain _storeGrain;
    private readonly string _storePath;
    private bool _initialized;
    private bool _isApplyingStoreReload;
    private readonly IDisposable? _storeSubscription;
    /// <summary>Read-only settings snapshot from StoreGrain. Eliminates the old dual-source _settings field.
    /// Phase 1c: Closed Flux Architecture (07-25).</summary>
    internal LauncherSettings CurrentSettings => _storeGrain.Select(s => s.Settings);
    internal readonly LaunchOrchestrator _launchOrchestrator;
    private readonly IDialogService _dialogService;
    private readonly IEntryService _entryService = null!;
    private readonly IPresetService _presetService = null!;
    private readonly IConfigExchangeService _configExchangeService;
    private readonly Func<DateTimeOffset> _utcNow;
    private ImmutableList<LauncherRunHistoryEntry> _runHistory = ImmutableList<LauncherRunHistoryEntry>.Empty;
    private readonly StatusPresenter _statusPresenter = new();
    internal readonly EntryListViewModel _list = null!;
    internal readonly PresetManager _presetManager = new();
    internal readonly LayoutManager _layoutManager = new();
    internal readonly LauncherPresetCommandWorkflow _presetWorkflow;
    internal readonly LauncherLayoutWorkflow _layoutWorkflow;

    // Status forwarding — reads StatusPresenter, re-fires PropertyChanged via subscription
    public LauncherStatusContent StatusContent { get => _statusPresenter.StatusContent; set => _statusPresenter.StatusContent = value; }
    public string StatusText => _statusPresenter.StatusText;
    public LauncherStatusTone StatusTone => _statusPresenter.StatusTone;
    public bool IsStatusNeutral => _statusPresenter.IsStatusNeutral;
    public bool IsStatusSuccess => _statusPresenter.IsStatusSuccess;
    public bool IsStatusWarning => _statusPresenter.IsStatusWarning;
    public bool IsStatusError => _statusPresenter.IsStatusError;
    public string DryRunText { get => _statusPresenter.DryRunText; set => _statusPresenter.DryRunText = value; }
    public string PresetSelectionStatusText { get => _statusPresenter.PresetSelectionStatusText; set => _statusPresenter.PresetSelectionStatusText = value; }
    public string ApplyPresetButtonText { get => _statusPresenter.ApplyPresetButtonText; set => _statusPresenter.ApplyPresetButtonText = value; }
    public int TotalCount { get => _statusPresenter.TotalCount; set => _statusPresenter.TotalCount = value; }
    public int VisibleCount { get => _statusPresenter.VisibleCount; set => _statusPresenter.VisibleCount = value; }
    public bool IsVisibleEmpty => _statusPresenter.IsVisibleEmpty;
    public bool IsEmptyEntries => _statusPresenter.IsEmptyEntries;
    public bool IsFilteredEmpty => _statusPresenter.IsFilteredEmpty;
    public int SelectedCount { get => _statusPresenter.SelectedCount; set => _statusPresenter.SelectedCount = value; }
    public int PresetCount { get => _statusPresenter.PresetCount; set => _statusPresenter.PresetCount = value; }
    public int StaleDirectoryCount => _list.StaleDirectoryCount;
    public bool HasStaleDirectories => _list.HasStaleDirectories;
    public ReadOnlyObservableCollection<LauncherPresetViewModel> Presets => _presetManager.Presets;
    public LauncherPresetViewModel? SelectedPreset => _presetManager.SelectedPreset;
    public ReadOnlyObservableCollection<LauncherSortModeViewModel> SortModes => _layoutManager.SortModes;
    public ReadOnlyObservableCollection<LauncherDisplayModeViewModel> DisplayModes => _layoutManager.DisplayModes;
    public string FilterText { get => _layoutManager.FilterText; set => _layoutManager.FilterText = value; }
    public LauncherSortModeViewModel? SelectedSortMode { get => _layoutManager.SelectedSortMode; set => _layoutManager.SelectedSortMode = value; }
    public LauncherDisplayModeViewModel? SelectedDisplayMode { get => _layoutManager.SelectedDisplayMode; set => _layoutManager.SelectedDisplayMode = value; }
    public bool IsCustomSortMode => _layoutManager.IsCustomSortMode;
    public bool IsCardsViewMode => _layoutManager.IsCardsViewMode;
    public bool IsDetailedListMode => _layoutManager.IsDetailedListMode;
    public bool IsNamesViewMode => _layoutManager.IsNamesViewMode;
    public IReadOnlyList<LauncherCardsGroupOption> CardsGroupOptions => _layoutManager.CardsGroupOptions;
    public LauncherCardsGroupOption? SelectedCardsGroupOption { get => _layoutManager.SelectedCardsGroupOption; set => _layoutManager.SelectedCardsGroupOption = value; }
    public string CardsGroupKey { get => _layoutManager.CardsGroupKey; set => _layoutManager.CardsGroupKey = value; }

    public string? KeyboardFocusedEntryId
    {
        get => _list.KeyboardFocusedEntryId;
        set => SetKeyboardFocusedEntryId(value);
    }

    // ── LaunchOrchestrator 转发属性 ──

    public bool RequireConfirmationForLaunches
    {
        get => _launchOrchestrator.RequireConfirmationForLaunches;
        set => _launchOrchestrator.RequireConfirmationForLaunches = value;
    }

    public string LaunchConfirmationToggleText => _launchOrchestrator.LaunchConfirmationToggleText;

    public string LaunchConfirmationToggleTip => _launchOrchestrator.LaunchConfirmationToggleTip;

    public bool IsBatchLaunching => _launchOrchestrator.IsBatchLaunching;

    public int RunHistoryCount => _runHistory.Count;

    public MainWindowViewModel(
        string storePath,
        StoreGrain storeGrain,
        LauncherStoreFile storeFile,
        LauncherLaunchWorkflow launchWorkflow,
        IDialogService dialogService,
        IEntryService entryService,
        IPresetService presetService,
        Func<DateTimeOffset> utcNow,
        IConfigExchangeService? configExchangeService = null)
    {
        ArgumentNullException.ThrowIfNull(storeFile);
        _storeGrain = storeGrain ?? throw new ArgumentNullException(nameof(storeGrain));
        _storePath = storePath;
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
        _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        _configExchangeService = configExchangeService ?? new ConfigExchangeService();
        ConfigPathDisplay = LauncherMainWindowText.StorePathDisplay(storePath);
        _statusPresenter.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        // Construct LaunchOrchestrator early so bridge delegates are ready.
        _launchOrchestrator = new LaunchOrchestrator(launchWorkflow, dialogService, this);
        _launchOrchestrator.CommandTemplates = CurrentSettings.CommandTemplates;
        _launchOrchestrator.RequireConfirmationForLaunches = CurrentSettings.RequireConfirmationForLaunches;
        // Construct EntryListViewModel early so the coordinator context can read entries.
        _list = new EntryListViewModel(_entryService, _utcNow, _storeGrain, _layoutManager.CardLayout);
        _list.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(EntryListViewModel.StaleDirectoryCount) or nameof(EntryListViewModel.HasStaleDirectories))
                OnPropertyChanged(e.PropertyName!);
        };
        _launchOrchestrator.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LaunchOrchestrator.RequireConfirmationForLaunches))
            {
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                OnPropertyChanged(nameof(LaunchConfirmationToggleText));
                OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
                SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
            }
            else
            {
                OnPropertyChanged(e.PropertyName!);
            }
        };
        _launchOrchestrator.MarkContextReady();
        _list.StatusApplier = s => StatusContent = s;
        _list.DragPreviewDiscarder = DiscardDragReorderPreview;
        _list.SelectionUpdater = UpdateSelection;
        _list.DetailedListModeChecker = () => IsDetailedListMode;
        _list.NamesModeChecker = () => IsNamesViewMode;
        _list.DryRunInitiator = ShowDryRun;
        _list.AsyncLauncher = e => LaunchAsync(e);
        _list.EntryMoverUp = MoveEntryUp;
        _list.EntryMoverUpChecker = CanMoveEntryUp;
        _list.EntryMoverDown = MoveEntryDown;
        _list.EntryMoverDownChecker = CanMoveEntryDown;
        _list.CountChanged += OnEntryCountChanged;
        // Init layout state from settings.
        _ = RequireSettingsText(CurrentSettings.LastFilterText, "lastFilterText");
        _layoutManager.LoadSettings(CurrentSettings);
        _layoutManager.NotifyDisplayModeProjectionChanged();
        // Subscribe to layout events AFTER init so construction doesn't trigger persistence.
        _layoutManager.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        _presetWorkflow = new LauncherPresetCommandWorkflow(this, _list, _presetManager, _presetService, _launchOrchestrator, this);
        _layoutWorkflow = new LauncherLayoutWorkflow(this, _list, _layoutManager, this);
        // Construct workflow AFTER settings normalization so it captures
        // the normalized values — DiscardDragReorderPreview syncs from it.
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            CurrentSettings.CustomEntryOrder,
            _list.MutableEntries,
            SaveDragCustomEntryOrder);
        // Load entries and presets from the unified store.
        _list.ReplaceEntries(storeFile.Entries);
        _runHistory = LauncherRunHistory.Sanitize(storeFile.RunHistory);
        OnPropertyChanged(nameof(RunHistoryCount));
        TotalCount = _list.MutableEntries.Count;
        VisibleCount = _list.VisibleCount;
        var presetFile = new LauncherPresetFile
        {
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        _presetManager.Load(presetFile, preferredPresetId: null);
        _presetManager.FileChanged += () => PresetCount = _presetManager.PresetCount;
        _presetManager.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PresetManager.SelectedPreset)) return;
            OnPropertyChanged(nameof(SelectedPreset));
            RefreshSelectionState();
        };
        PresetCount = _presetManager.PresetCount;
        // ValidateCustomOrder requires every entry ID in CustomEntryOrder.
        // Sync via dispatch if order is empty or stale.
        EnsureCustomOrderContainsEntries();
        RefreshSelectionState();
        SynchronizeProjections(ProjectionSyncMode.VisibleOnly);
        // Notify initial UI state for launch confirmation toggle (bypassed direct init).
        OnPropertyChanged(nameof(RequireConfirmationForLaunches));
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        StatusContent = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
        // Phase 1c: Closed Flux Architecture (07-25).
        // _settings field eliminated. All state reads use CurrentSettings (StoreGrain.Select).
        // Preset sync still needs to update _presetManager on external state changes.
        _storeSubscription = _storeGrain.Subscribe(state =>
        {
            _presetManager.Load(
                new LauncherPresetFile { Presets = state.Presets, DefaultPresetId = state.DefaultPresetId },
                _presetManager.SelectedPreset?.Id);
        });
        _storeGrain.Reloaded += OnStoreReloaded;
    }
    
    /// <summary>Post-construction initialization. Subscribes to entry change
    /// events that must be suppressed during construction.</summary>
    internal void InitializeAsync()
    {
        _list.EntriesChanged += OnEntriesChanged;
        _initialized = true;
    }
    
    private void OnEntriesChanged()
    {
        if (!_initialized || _isApplyingStoreReload) return;
        SynchronizeProjections(ProjectionSyncMode.Mutation);
    }

    private void OnEntryCountChanged()
    {
        TotalCount = _list.MutableEntries.Count;
        VisibleCount = _list.VisibleCount;
    }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries => _list.Entries;

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries => _list.VisibleEntries;

    /// <summary>Current settings snapshot — reads from StoreGrain (CurrentSettings).
    /// Tests should read from this, not from a stale pre-construction reference.</summary>
    public LauncherSettings Settings => CurrentSettings;

    public IReadOnlyDictionary<string, string>? CommandTemplates => _launchOrchestrator.CommandTemplates;


    public string ConfigPathDisplay { get; }

    [RelayCommand]
    private void Refresh()
    {
        try
        {
            DiscardDragReorderPreview();
            _storeGrain.ReloadFromDisk();
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Refresh.LoadStore");
            StatusContent = LauncherMainWindowText.ConfigLoadFailed(ExceptionMessageFormatter.ForUser(ex));
        }
    }

    private void OnStoreReloaded(LauncherStoreFile storeFile)
    {
        _isApplyingStoreReload = true;
        try
        {
            try
            {
                _list.ReplaceEntries(storeFile.Entries);
                _list.KeyboardFocusedEntryId = null;
                EnsureCustomOrderContainsEntries();

                // Cross-validate $key command references against commandTemplates.
                ValidateCommandTemplateReferences();

                LoadPresetFile(preferredPresetId: null);

                ValidatePresetEntryReferences();
                SynchronizeProjections(ProjectionSyncMode.FullReload);
                StatusContent = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
            }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, "Refresh.Validate");
                StatusContent = LauncherMainWindowText.ConfigValidationFailed(ExceptionMessageFormatter.ForUser(ex));
                // Reload preset file to ensure _presetFile and UI state are consistent
                // after a partial failure during Phase 2 validation.
                try { LoadPresetFile(preferredPresetId: null); }
                catch (Exception presetEx) when (ExceptionFilters.IsRecoverable(presetEx))
                {
                    ErrorLog.Write(presetEx, "Refresh.ReloadPresetFile");
                }
                SynchronizeProjections(ProjectionSyncMode.FullReload);
            }
        }
        finally
        {
            _isApplyingStoreReload = false;
        }
    }

    private void ValidateCommandTemplateReferences()
    {
        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(
            _list.MutableEntries.Select(ToLaunchEntry),
            CurrentSettings.CommandTemplates);

        if (missingKeys.Count > 0)
            StatusContent = LauncherMainWindowText.UndefinedCommandTemplateKeys(string.Join(", ", missingKeys));
    }

    /// <summary>
    /// Cross-validate that every preset entry ID references a loaded entry.
    /// Logs dangling references to ErrorLog but does not block loading.
    /// </summary>
    private void ValidatePresetEntryReferences()
    {
        var entryIdSet = GetEntryIds().ToHashSet(EntryId.Comparer);
        foreach (var preset in _presetManager.File.Presets)
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
            StatusContent = LauncherStatusContent.Error(statusText);
    }

    private void ApplyStatusText(LauncherStatusContent? status)
    {
        if (status is not null)
            StatusContent = status;
    }

    private LaunchSaveResult SaveConfig(LaunchSaveResultKind kind)
    {
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            var status = kind == LaunchSaveResultKind.SaveFailedAfterLaunch
                ? LauncherStatusContent.Error($"已启动，但运行历史保存失败：{saveFailure}")
                : LauncherStatusContent.Error(saveFailure);
            ApplyStatusText(status);
            return new LaunchSaveResult(kind, saveFailure);
        }

        return new LaunchSaveResult(LaunchSaveResultKind.SaveSucceeded, null);
    }

    /// <summary>Builds a LauncherStoreFile snapshot from current in-memory state.</summary>
    private LauncherStoreFile BuildStoreFile() =>
        new()
        {
            Entries = _list.MutableEntries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = CurrentSettings,
            Presets = _presetManager.File.Presets,
            DefaultPresetId = _presetManager.File.DefaultPresetId,
            RunHistory = _runHistory,
        };

    /// <summary>Saves the given store file to disk via StoreGrain. Returns null on
    /// success, or an error message on failure. Routes through StoreGrain.Dispatch so
    /// the store's internal state stays in sync and subscribers are notified.
    /// Callers that use snapshot-based rollback are still responsible for restoring
    /// their own ObservableCollection state after a failure.</summary>
    private string? SaveStoreFile(LauncherStoreFile storeFile)
    {
        try { _storeGrain.Dispatch(_ => storeFile); return null; }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "SaveStoreFile");
            return LauncherPersistenceStatus.StoreSaveFailed(ex).Text;
        }
    }

    /// <summary>Saves settings to the unified store. Used as callback by
    /// LauncherDragReorderWorkflow during drag commit.</summary>
    // CFA Step 1c: uses BuildStoreFile() with { Settings = settings } to persist
    // the workflow's committed CustomEntryOrder into the store via Dispatch.
    private string? SaveSettingsToStore(LauncherSettings settings)
    {
        return SaveStoreFile(BuildStoreFile() with { Settings = settings });
    }

    /// <summary>Callback for LauncherDragReorderWorkflow (CFA Step 1d).
    /// Constructs full settings from CurrentSettings with the committed order,
    /// then persists atomically.</summary>
    private string? SaveDragCustomEntryOrder(System.Collections.Immutable.ImmutableList<string> order)
    {
        return SaveStoreFile(BuildStoreFile() with { Settings = CurrentSettings with { CustomEntryOrder = order } });
    }

    private LauncherSettings CaptureSettingsMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return CurrentSettings;
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    private static string RequireSettingsText(string? value, string fieldName) =>
        value ?? throw new InvalidDataException($"Launcher settings {fieldName} is null.");

    /// <summary>Rebuild ViewModel-level state from StoreGrain after a save failure.
    /// StoreGrain already reverted its internal state; this method syncs the
    /// ViewModel's in-memory copies back to match.</summary>
    private void RestoreVmStateFromStore()
    {
        _presetManager.Load(
            new LauncherPresetFile { Presets = _storeGrain.Select(s => s.Presets), DefaultPresetId = _storeGrain.Select(s => s.DefaultPresetId) },
            SelectedPreset?.Id);
        // Use ReplaceEntries (no event) then sync CustomEntryOrder before refresh,
        // so ValidateCustomOrder doesn't fail on inconsistent state.
        _list.ReplaceEntries(_storeGrain.Select(s => s.Entries));
        _runHistory = LauncherRunHistory.Sanitize(_storeGrain.Select(s => s.RunHistory));
        OnPropertyChanged(nameof(RunHistoryCount));
        EnsureCustomOrderContainsEntries();
        SynchronizeProjections(ProjectionSyncMode.Mutation);
    }

    internal void SetKeyboardFocusedEntryId(string? value)
    {
        if (EntryId.Same(_list.KeyboardFocusedEntryId, value))
            return;

        // Clear old focus.
        if (_list.KeyboardFocusedEntryId is not null)
        {
            foreach (var e in _list.MutableEntries)
            {
                if (e is null) continue;
                if (EntryId.Same(e.Id, _list.KeyboardFocusedEntryId))
                    e.IsKeyboardFocused = false;
            }
        }

        _list.KeyboardFocusedEntryId = value;

        // Set new focus.
        if (value is not null)
        {
            foreach (var e in _list.MutableEntries)
            {
                if (e is null) continue;
                if (EntryId.Same(e.Id, value))
                    e.IsKeyboardFocused = true;
            }
        }

        OnPropertyChanged(nameof(KeyboardFocusedEntryId));
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");

    private IEnumerable<string> GetEntryIds() =>
        _list.MutableEntries.Select(entry => entry.Id);

    /// <summary>Single projection sync entry. VisibleOnly is safe for filter/sort/
    /// group changes; Mutation and FullReload also refresh selection-derived state.</summary>
    private void SynchronizeProjections(ProjectionSyncMode mode)
    {
        if (mode != ProjectionSyncMode.VisibleOnly)
            RefreshSelectionState();
        RebuildVisibleProjection();
    }

    void IProjectionSynchronizer.SynchronizeProjections(ProjectionSyncMode mode) =>
        SynchronizeProjections(mode);

    StoreGrain ICommandExecutionContext.Store => _storeGrain;

    IStatusSink ICommandExecutionContext.Status => this;

    string ICommandExecutionContext.GetTimestampUtc() => CurrentTimestampUtc();

    void IStatusSink.Apply(LauncherStatusContent status) => StatusContent = status;

    void IStatusSink.SetDryRunText(string text) => DryRunText = text;

    void ICommandHost.DiscardDragReorderPreview() => DiscardDragReorderPreview();

    void ICommandHost.RefreshSelectionState() => RefreshSelectionState();

    void ICommandHost.NotifyPresetManagementCommands() => NotifyPresetManagementCommands();

    void ICommandHost.RefreshReorderCommandState() => RefreshReorderCommandState();

    void ICommandHost.RestoreVmStateFromStore() => RestoreVmStateFromStore();

    void ICommandHost.NotifySearchHistoryChanged() => OnPropertyChanged(nameof(SearchHistoryItems));

    bool ICommandHost.ReplaceSelection(IReadOnlySet<string> entryIds) => ReplaceSelection(entryIds);

    LauncherSettings ILaunchCoordinatorContext.GetSettings() => CurrentSettings;

    string ILaunchCoordinatorContext.GetTimestampUtc() => CurrentTimestampUtc();

    void ILaunchCoordinatorContext.SetDryRunText(string text) => DryRunText = text;

    void ILaunchCoordinatorContext.ApplyStatus(LauncherStatusContent status) => StatusContent = status;

    void ILaunchCoordinatorContext.DiscardDragReorderPreview() => DiscardDragReorderPreview();

    bool ILaunchCoordinatorContext.PersistSettings(
        Func<LauncherSettings, LauncherSettings> mutator,
        LauncherStatusContent? successStatus) =>
        TryPersistSettingsChange(mutator, successStatus);

    IEnumerable<LauncherEntryViewModel> ILaunchCoordinatorContext.GetEntries() => _list.MutableEntries;

    LaunchSaveResult ILaunchCoordinatorContext.SaveConfig(LaunchSaveResultKind kind) => SaveConfig(kind);

    void ILaunchCoordinatorContext.RecordRunHistory(IReadOnlyList<LauncherRunHistoryEntry> entries) =>
        AppendRunHistory(entries);

    /// <summary>释放 StoreGrain 订阅和其他托管资源。</summary>
    public void Dispose()
    {
        _storeSubscription?.Dispose();
        _storeGrain.Reloaded -= OnStoreReloaded;
        _list.Dispose();
        _launchOrchestrator.Dispose();
    }
}
