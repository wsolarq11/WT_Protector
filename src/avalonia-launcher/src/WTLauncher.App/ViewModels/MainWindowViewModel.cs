using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IEntryListViewModel
{
    internal readonly StoreGrain _storeGrain;
    private readonly string _storePath;
    private bool _initialized;
    /// <summary>Single source of settings state — always reads from AppStore.
    /// Direct field access is being phased out; prefer CurrentSettings.</summary>
    private LauncherSettings _settings;
    /// <summary>Read-only settings snapshot from AppStore. Replaces direct _settings field reads.
    /// Phase 1: state single-sourcing (07-24-triple-cut-arch).</summary>
    internal LauncherSettings CurrentSettings => _storeGrain.Select(s => s.Settings);
    internal readonly LaunchOrchestrator _launchOrchestrator;
    private readonly IDialogService _dialogService;
    private readonly IEntryService _entryService = null!;
    private readonly IPresetService _presetService = null!;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly StatusPresenter _statusPresenter = new();
    internal readonly EntryListViewModel _list = null!;
    internal readonly PresetManager _presetManager = new();
    internal readonly LayoutManager _layoutManager = new();

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
    public int SelectedCount { get => _statusPresenter.SelectedCount; set => _statusPresenter.SelectedCount = value; }
    public int PresetCount { get => _statusPresenter.PresetCount; set => _statusPresenter.PresetCount = value; }
    public string EmptyStateText => _statusPresenter.EmptyStateText;
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

    public MainWindowViewModel(
        string storePath,
        StoreGrain storeGrain,
        LauncherStoreFile storeFile,
        LauncherLaunchWorkflow launchWorkflow,
        IDialogService dialogService,
        IEntryService entryService,
        IPresetService presetService,
        Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(storeFile);
        _storeGrain = storeGrain ?? throw new ArgumentNullException(nameof(storeGrain));
        _storePath = storePath;
        _settings = storeFile.Settings;
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
        _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        ConfigPathDisplay = LauncherMainWindowText.StorePathDisplay(storePath);
        _statusPresenter.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        // Construct LaunchOrchestrator early so bridge delegates are ready.
        _launchOrchestrator = new LaunchOrchestrator(launchWorkflow, dialogService);
        _launchOrchestrator.CommandTemplates = _settings.CommandTemplates;
        _launchOrchestrator.RequireConfirmationForLaunches = _settings.RequireConfirmationForLaunches;
        _launchOrchestrator.SettingsGetter = () => _settings;
        _launchOrchestrator.TimestampGetter = () => CurrentTimestampUtc();
        _launchOrchestrator.DryRunTextSetter = s => DryRunText = s;
        _launchOrchestrator.StatusApplier = s => StatusContent = s;
        _launchOrchestrator.FilterApplier = ApplyFilter;
        _launchOrchestrator.ReorderDiscarder = DiscardDragReorderPreview;
        _launchOrchestrator.SettingsPersister = TryPersistSettingsChange;
        // Construct EntryListViewModel early so bridge delegates are ready.
        _list = new EntryListViewModel(_entryService, _utcNow, _storeGrain, _layoutManager.CardLayout);
        _launchOrchestrator.EntryGetter = () => _list.MutableEntries;
        _launchOrchestrator.SaveConfig = SaveConfig;
        _launchOrchestrator.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LaunchOrchestrator.RequireConfirmationForLaunches))
            {
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                OnPropertyChanged(nameof(LaunchConfirmationToggleText));
                OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
                ApplyFilter();
            }
            else
            {
                OnPropertyChanged(e.PropertyName!);
            }
        };
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
        _ = RequireSettingsText(_settings.LastFilterText, "lastFilterText");
        _layoutManager.LoadSettings(_settings);
        _settings = _settings with { SortMode = _layoutManager.SelectedSortMode!.Id }; // Phase 1: constructor-local working copy
        _settings = _settings with { EntryViewMode = _layoutManager.SelectedDisplayMode!.Id }; // Phase 1: constructor-local working copy
        _settings = _settings with { CardsGroupKey = _layoutManager.CardsGroupKey }; // Phase 1: constructor-local working copy
        _layoutManager.NotifyDisplayModeProjectionChanged();
        // Subscribe to layout events AFTER init so construction doesn't trigger persistence.
        _layoutManager.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        _layoutManager.FilterChanged += OnLayoutFilterChanged;
        _layoutManager.SortChanged += OnLayoutSortChanged;
        _layoutManager.ViewModeChanged += OnLayoutViewModeChanged;
        _layoutManager.GroupKeyChanged += OnLayoutGroupKeyChanged;
        // Construct workflow AFTER settings normalization so it captures
        // the normalized values — DiscardDragReorderPreview syncs from it.
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _list.MutableEntries,
            SaveSettingsToStore);
        // Load entries and presets from the unified store.
        _list.ReplaceEntries(storeFile.Entries);
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
        // Sync custom entry order with loaded entries so ValidateCustomOrder
        // passes even when the stored CustomEntryOrder is empty or stale.
        // Phase 1: _settings still used as working copy during construction.
        // Full CurrentSettings transition will happen once DragWorkflow is refactored.
        _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds()) };
        RefreshSelectionState();
        ApplyFilter();
        // Notify initial UI state for launch confirmation toggle (bypassed direct init).
        OnPropertyChanged(nameof(RequireConfirmationForLaunches));
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        StatusContent = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
        // Phase 1: State single-sourcing (07-24-triple-cut-arch).
        // _settings field is being eliminated. State reads use CurrentSettings (AppStore.Select).
        // Preset sync still needs to update _presetManager on external state changes.
        _storeGrain.Subscribe(state =>
        {
            _settings = state.Settings; // Phase 1: ponytail — kept for transition
            _presetManager.Load(
                new LauncherPresetFile { Presets = state.Presets, DefaultPresetId = state.DefaultPresetId },
                _presetManager.SelectedPreset?.Id);
        });
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
        if (!_initialized) return;
        RefreshAllProjections();
    }

    private void OnEntryCountChanged()
    {
        TotalCount = _list.MutableEntries.Count;
        VisibleCount = _list.VisibleCount;
    }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries => _list.Entries;

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries => _list.VisibleEntries;

    /// <summary>Current settings snapshot. Immutable — replaced on every mutation.
    /// Tests should read from this, not from a stale pre-construction reference.</summary>
    public LauncherSettings Settings => _settings;

    public IReadOnlyDictionary<string, string>? CommandTemplates => _launchOrchestrator.CommandTemplates;


    public string ConfigPathDisplay { get; }

    [RelayCommand]
    private void Refresh()
    {
        try
        {
            DiscardDragReorderPreview();
            var storeFile = LauncherStore.Load(_storePath);
            _list.ReplaceEntries(storeFile.Entries);
            _list.KeyboardFocusedEntryId = null;
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Refresh.LoadStore");
            StatusContent = LauncherMainWindowText.ConfigLoadFailed(ExceptionMessageFormatter.ForUser(ex));
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
            StatusContent = LauncherMainWindowText.ConfigValidationFailed(ExceptionMessageFormatter.ForUser(ex));
            // Reload preset file to ensure _presetFile and UI state are consistent
            // after a partial failure during Phase 2 validation.
            try { LoadPresetFile(preferredPresetId: null); }
            catch (Exception presetEx) when (ExceptionFilters.IsRecoverable(presetEx))
            {
                ErrorLog.Write(presetEx, "Refresh.ReloadPresetFile");
            }
            RefreshSelectionState();
            ApplyFilter();
            return;
        }

        RefreshSelectionState();
        ApplyFilter();
        StatusContent = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
    }

    private void ValidateCommandTemplateReferences()
    {
        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(
            _list.MutableEntries.Select(ToLaunchEntry),
            _settings.CommandTemplates);

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
            Entries = _list.MutableEntries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = _settings,
            Presets = _presetManager.File.Presets,
            DefaultPresetId = _presetManager.File.DefaultPresetId,
        };

    /// <summary>Saves the given store file to disk via AppStore. Returns null on
    /// success, or an error message on failure. Routes through AppStore.Mutate so
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
    // ponytail: mutate _settings first, then route through BuildStoreFile —
    // eliminates the hand-assembly dual path that existed here pre-2026-07-09
    // (task 07-09-settings-single-source). The 5 `_settings = _dragReorderWorkflow.Settings`
    // sites in Drag.cs remain (workflow still owns working/rollback state for
    // non-commit paths) but become no-ops in the commit-success case.
    private string? SaveSettingsToStore(LauncherSettings settings)
    {
        _settings = settings;
        return SaveStoreFile(BuildStoreFile());
    }

    private LauncherSettings CaptureSettingsMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return _settings;
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

    /// <summary>Rebuild ViewModel-level state from AppStore after a save failure.
    /// AppStore already reverted its internal state; this method syncs the
    /// ViewModel's in-memory copies back to match.</summary>
    private void RestoreVmStateFromStore()
    {
        _settings = _storeGrain.Select(s => s.Settings);
        _presetManager.Load(
            new LauncherPresetFile { Presets = _storeGrain.Select(s => s.Presets), DefaultPresetId = _storeGrain.Select(s => s.DefaultPresetId) },
            SelectedPreset?.Id);
        // Use ReplaceEntries (no event) then sync CustomEntryOrder before refresh,
        // so ValidateCustomOrder doesn't fail on inconsistent state.
        _list.ReplaceEntries(_storeGrain.Select(s => s.Entries));
        EnsureCustomOrderContainsEntries();
        RefreshAllProjections();
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

    /// <summary>Refreshes all in-memory projections after a mutation.
    /// Call at the end of any mutation that changes entry/preset state
    /// to keep selection state and filter consistent.</summary>
    private void RefreshAllProjections()
    {
        RefreshSelectionState();
        ApplyFilter();
    }
}
