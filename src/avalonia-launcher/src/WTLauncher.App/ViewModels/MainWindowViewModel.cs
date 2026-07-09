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

    private IEnumerable<string> GetEntryIds() =>
        _entries.Select(entry => entry.Id);
}
