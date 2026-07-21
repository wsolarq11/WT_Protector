using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IEntryListViewModel
{
    private readonly string _storePath;
    private LauncherSettings _settings;
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private readonly IDialogService _dialogService;
    private readonly IEntryService _entryService = null!;
    private readonly IPresetService _presetService = null!;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly ObservableCollection<LauncherEntryViewModel> _entries = [];
    private readonly ObservableCollection<LauncherEntryViewModel> _visibleEntries = [];

    private readonly StatusPresenter _statusPresenter = new();
    internal readonly PresetManager _presetManager = new();
    internal readonly LayoutManager _layoutManager = new();

    // Status forwarding — reads StatusPresenter, re-fires PropertyChanged via subscription
    public LauncherStatusContent StatusContent { get => _statusPresenter.StatusContent; set => _statusPresenter.StatusContent = value; }
    public string StatusText => _statusPresenter.StatusText;
    public LauncherStatusTone StatusTone => _statusPresenter.StatusTone;
    public string StatusIndicator => _statusPresenter.StatusIndicator;
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
        IDialogService dialogService,
        IEntryService entryService,
        IPresetService presetService,
        Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(storeFile);
        _storePath = storePath;
        _settings = storeFile.Settings;
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _launchWorkflow.CommandTemplates = _settings.CommandTemplates;
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
        _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
        ConfigPathDisplay = LauncherMainWindowText.StorePathDisplay(storePath);
        _statusPresenter.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName!);
        _requireConfirmationForLaunches = _settings.RequireConfirmationForLaunches;
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        // Init layout state from settings.
        _ = RequireSettingsText(_settings.LastFilterText, "lastFilterText");
        _layoutManager.LoadSettings(_settings);
        _settings = _settings with { SortMode = _layoutManager.SelectedSortMode!.Id };
        _settings = _settings with { EntryViewMode = _layoutManager.SelectedDisplayMode!.Id };
        _settings = _settings with { CardsGroupKey = _layoutManager.CardsGroupKey };
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
            _entries,
            SaveSettingsToStore);
        // Construct entry-list VM early so ReplaceEntries creates cards
        // that own EntryListViewModel (not MainWindowViewModel) as host.
        _entryListViewModel = CreateEntryListViewModel();
        // Load entries and presets from the unified store.
        ReplaceEntries(storeFile.Entries);
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
        _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds()) };
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
        StatusContent = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
        SubscribeToMessages();
    }

    private readonly EntryListViewModel _entryListViewModel;

    private EntryListViewModel CreateEntryListViewModel()
    {
        return new EntryListViewModel(
            _entries,
            _visibleEntries,
            _entryService,
            new EntryListBridge(
                Settings: () => _settings,
                PresetFile: () => _presetManager.File,
                CommandTemplates: () => CommandTemplates,
                GetPresetReferenceCount: GetPresetReferenceCount,
                GetKeyboardFocusedEntryId: () => _keyboardFocusedEntryId,
                SetKeyboardFocusedEntryId: value => _keyboardFocusedEntryId = value,
                CaptureSnapshot: () => new ShellSnapshot(_settings, _presetManager.File, _presetManager.SelectedPreset?.Id),
                RestoreSnapshot: RestoreShellSnapshot,
                ApplyChanges: ApplyEntryChanges,
                ApplyStatusText: ApplyStatusText,
                DiscardDragReorderPreview: DiscardDragReorderPreview,
                UpdateSelection: UpdateSelection,
                IsDetailedListMode: () => IsDetailedListMode,
                IsNamesViewMode: () => IsNamesViewMode,
                ShowDryRun: ShowDryRun,
                LaunchAsync: LaunchAsync,
                MoveEntryUp: MoveEntryUp,
                CanMoveEntryUp: CanMoveEntryUp,
                MoveEntryDown: MoveEntryDown,
                CanMoveEntryDown: CanMoveEntryDown),
            _utcNow);
    }

    private void RestoreShellSnapshot(ShellSnapshot snap)
    {
        _settings = snap.Settings;
        _presetManager.Load(snap.PresetFile, null);
        TotalCount = _entries.Count;
    }

    private string? ApplyEntryChanges(
        IEnumerable<LaunchEntry> entries,
        LauncherSettings settings,
        LauncherPresetFile presetFile)
    {
        _settings = settings;
        _presetManager.Load(presetFile, null);
        TotalCount = _entries.Count;
        return SaveStoreFile(BuildStoreFile());
    }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

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
        LauncherEntryLayoutPresentation.Refresh(_entries);
        StatusContent = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
    }

    private void ValidateCommandTemplateReferences()
    {
        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(
            _entries.Select(ToLaunchEntry),
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
            Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = _settings,
            Presets = _presetManager.File.Presets,
            DefaultPresetId = _presetManager.File.DefaultPresetId,
        };

    /// <summary>Saves the given store file to disk. Returns null on success,
    /// or an error message on failure.</summary>
    private string? SaveStoreFile(LauncherStoreFile storeFile)
    {
        try { LauncherStore.Save(storeFile, _storePath); return null; }
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
        => LauncherMutationTransaction.Commit(
            snapshot,
            () => SaveStoreFile(BuildStoreFile()),
            rollbackSnapshot => _settings = rollbackSnapshot);

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

    private void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        _entryListViewModel.ReplaceEntries(entries);
        TotalCount = _entries.Count;
        _keyboardFocusedEntryId = null;
        OnPropertyChanged(nameof(KeyboardFocusedEntryId));

        // Force card-size recalculation on next ApplyFilter — new entry VMs
        // start with default dimensions and must be measured against current available size.
        _layoutManager.InvalidateCardSizeCache();
    }

    private string CurrentTimestampUtc() => _utcNow().ToString("O");

    private IEnumerable<string> GetEntryIds() =>
        _entries.Select(entry => entry.Id);

    /// <summary>Refreshes all in-memory projections after a mutation.
    /// Call at the end of any mutation that changes entry/preset state
    /// to keep selection state and filter consistent.</summary>
    private void RefreshAllProjections()
    {
        RefreshSelectionState();
        ApplyFilter();
    }
}
