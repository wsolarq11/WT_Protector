using Avalonia;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly string _configPath;
    private readonly string _presetPath;
    private readonly string _settingsPath;
    private readonly LauncherSettings _settings;
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private LauncherPresetFile _presetFile = new();
    private Size _cardsAvailableSize = new(900, 420);
    private readonly LauncherDragReorderWorkflow _dragReorderWorkflow;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _dryRunText = "选择一个启动项查看预演命令。";

    [ObservableProperty]
    private string _presetSelectionStatusText = "选择启动组后可查看与当前勾选的差异。";

    [ObservableProperty]
    private string _applyPresetButtonText = "应用启动组";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    [ObservableProperty]
    private LauncherPresetViewModel? _selectedPreset;

    [ObservableProperty]
    private LauncherSortModeViewModel? _selectedSortMode;

    [ObservableProperty]
    private LauncherDisplayModeViewModel? _selectedDisplayMode;

    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    public MainWindowViewModel(string configPath, string settingsPath, LauncherSettings settings, LauncherLaunchWorkflow launchWorkflow)
    {
        _configPath = configPath;
        _presetPath = LauncherPresetStore.ResolvePresetPath(configPath);
        _settingsPath = settingsPath;
        _settings = settings;
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _settingsPath,
            Entries,
            EnsureCustomOrderContainsEntries,
            ApplyFilter);
        ConfigPathDisplay = $"配置: {configPath} · 启动组: {_presetPath}";
        _filterText = settings.LastFilterText ?? string.Empty;
        _requireConfirmationForLaunches = settings.RequireConfirmationForLaunches;
        _selectedSortMode = ResolveSortMode(settings.SortMode);
        _settings.SortMode = _selectedSortMode.Id;
        _selectedDisplayMode = ResolveDisplayMode(settings.EntryViewMode);
        _settings.EntryViewMode = _selectedDisplayMode.Id;
        Refresh();
    }

    public ObservableCollection<LauncherEntryViewModel> Entries { get; } = [];

    public ObservableCollection<LauncherEntryViewModel> VisibleEntries { get; } = [];

    public ObservableCollection<LauncherPresetViewModel> Presets { get; } = [];

    public ObservableCollection<LauncherSortModeViewModel> SortModes { get; } =
    [
        new(LauncherEntryVisibility.CustomSortModeId, "自定义拖拽"),
        new(LauncherEntryVisibility.UpdatedSortModeId, "更新时间"),
        new(LauncherEntryVisibility.AlphabeticalSortModeId, "首字母顺序"),
    ];

    public ObservableCollection<LauncherDisplayModeViewModel> DisplayModes { get; } =
    [
        new(LauncherEntryViewMode.Cards, "卡片"),
        new(LauncherEntryViewMode.Details, "详细列表"),
        new(LauncherEntryViewMode.Names, "名称墙"),
    ];

    private LauncherSortModeViewModel ResolveSortMode(string? sortModeId) =>
        SortModes.FirstOrDefault(mode => mode.Id == sortModeId) ?? SortModes[0];

    private LauncherDisplayModeViewModel ResolveDisplayMode(string? entryViewMode) =>
        DisplayModes.FirstOrDefault(mode => mode.Id == entryViewMode) ?? DisplayModes[0];

    private LauncherEntryViewMode CurrentEntryViewMode => SelectedDisplayMode?.Mode ?? LauncherEntryViewMode.Cards;

    public bool IsCustomSortMode => SelectedSortMode?.Id == LauncherEntryVisibility.CustomSortModeId;

    public bool IsCardsViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Cards;

    public bool IsDetailedListMode => CurrentEntryViewMode == LauncherEntryViewMode.Details;

    public bool IsNamesViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Names;

    public string LaunchConfirmationToggleText => RequireConfirmationForLaunches ? "启动需确认" : "启动无需确认";

    public string LaunchConfirmationToggleTip => RequireConfirmationForLaunches
        ? "所有启动项启动前统一确认。"
        : "所有启动项直接启动，不弹确认。";

    public string ConfigPathDisplay { get; }

    public Func<LaunchEntry, LaunchPlan, Task<bool>> ConfirmLaunchAsync { get; set; } =
        static (_, _) => Task.FromResult(false);

    public Func<IReadOnlyList<LaunchEntry>, Task<bool>> ConfirmBatchLaunchAsync { get; set; } =
        static _ => Task.FromResult(false);

    public Func<IReadOnlyList<string>, Task<string?>> RequestNewPresetAsync { get; set; } =
        static _ => Task.FromResult<string?>(null);

    public Func<LauncherPresetViewModel, Task<bool>> ConfirmDeletePresetAsync { get; set; } =
        static _ => Task.FromResult(false);

    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync { get; set; } =
        static _ => Task.FromResult<EntryDraft?>(null);

    public Func<LauncherEntryViewModel, Task<bool>> ConfirmDeleteEntryAsync { get; set; } =
        static _ => Task.FromResult(false);

    partial void OnFilterTextChanged(string value)
    {
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.LastFilterText = value ?? string.Empty;
        SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _filterText = snapshot.LastFilterText;
                OnPropertyChanged(nameof(FilterText));
                ApplyFilter();
            },
            ApplyFilter);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        OnPropertyChanged(nameof(IsCustomSortMode));
        if (value is null)
            return;

        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.SortMode = value.Id;
        EnsureCustomOrderContainsEntries();
        SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _selectedSortMode = ResolveSortMode(snapshot.SortMode);
                OnPropertyChanged(nameof(SelectedSortMode));
                OnPropertyChanged(nameof(IsCustomSortMode));
                ApplyFilter();
            },
            () =>
            {
                ApplyFilter();
                StatusText = $"排序模式: {value.Label}（已保存）";
            });
    }

    partial void OnSelectedDisplayModeChanged(LauncherDisplayModeViewModel? value)
    {
        NotifyDisplayModeProjectionChanged();
        if (value is null)
            return;

        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.EntryViewMode = value.Id;
        LauncherEntryLayoutPresentation.Refresh(Entries);
        SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _selectedDisplayMode = ResolveDisplayMode(snapshot.EntryViewMode);
                OnPropertyChanged(nameof(SelectedDisplayMode));
                NotifyDisplayModeProjectionChanged();
                LauncherEntryLayoutPresentation.Refresh(Entries);
            },
            () => StatusText = $"视图模式: {value.Label}（已保存）");
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.RequireConfirmationForLaunches = value;
        RefreshLaunchConfirmationPresentation();
        SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _requireConfirmationForLaunches = snapshot.RequireConfirmationForLaunches;
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                RefreshLaunchConfirmationPresentation();
            },
            () => StatusText = value
                ? "启动确认: 所有启动项启动前统一确认"
                : "启动确认: 所有启动项直接启动");
    }

    [RelayCommand]
    private void Refresh()
    {
        ReplaceEntries(LauncherConfigStore.Load(_configPath));
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds());

        var presetError = LoadPresetFile(preferredPresetId: null);

        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(Entries);
        StatusText = presetError
            ?? $"已加载 {TotalCount} 个启动项、{PresetCount} 个启动组；勾选自动保存到 config\\config.json";
    }

    [RelayCommand]
    private void SelectVisible()
    {
        var targets = VisibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            ApplyStatusText(
                VisibleEntries.Count == 0
                    ? LauncherSelectionStatus.NoVisibleEntries
                    : LauncherSelectionStatus.VisibleAlreadySelected);
            return;
        }

        if (SetSelection(targets, selected: true))
            ApplyStatusText(LauncherSelectionStatus.VisibleSelected(targets.Count));
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        var targets = GetSelectedEntries();
        if (targets.Count == 0)
        {
            ApplyStatusText(LauncherSelectionStatus.NoSelectedEntriesToClear);
            return;
        }

        if (SetSelection(targets, selected: false))
            ApplyStatusText(LauncherSelectionStatus.Cleared(targets.Count));
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null)
        {
            ApplyStatusText(LauncherPresetStatus.NoPresetSelected);
            return;
        }

        var selection = BuildPresetSelectionPreview(GetSelectedEntryIds());
        if (selection.MissingConfigIds.Count > 0)
        {
            ApplyStatusText(LauncherPresetStatus.MissingEntryReferences(selection.MissingConfigIds));
            return;
        }

        if (ReplaceSelection(selection.PresetIds))
            ApplyStatusText(LauncherPresetStatus.Applied(selectedPreset.Name, selection.PresetIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        if (SelectedCount == 0)
        {
            StatusText = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        var entryIds = GetSelectedEntryIds();
        var presetName = await RequestNewPresetAsync(entryIds).ConfigureAwait(true);
        if (presetName is null)
        {
            StatusText = LauncherPresetStatus.SaveCanceled;
            return;
        }

        var name = presetName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = LauncherPresetStatus.NameRequired;
            return;
        }

        var preferredPresetId = SelectedPreset?.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.AddPreset(_presetFile, name, entryIds);
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(preset.Id);
        ApplyStatusText(LauncherPresetStatus.Saved(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = LauncherPresetStatus.NoPresetSelected;
            return;
        }
        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, SelectedPreset.Id))
        {
            StatusText = LauncherPresetStatus.UpdateBlocked(SelectedPreset.Name);
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            StatusText = LauncherSelectionStatus.SelectionRequired;
            return;
        }

        var preferredPresetId = SelectedPreset.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.UpdatePreset(_presetFile, SelectedPreset.Id, SelectedPreset.Name, entryIds);
        if (preset is null)
        {
            ApplyPresetFileState(snapshot, preferredPresetId);
            ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
            return;
        }
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(preset.Id);
        ApplyStatusText(LauncherPresetStatus.Updated(preset.Name, preset.EntryIds.Count));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        if (SelectedPreset is null)
        {
            StatusText = LauncherPresetStatus.NoPresetSelected;
            return;
        }
        if (!LauncherPresetWorkflow.CanDeletePreset(_presetFile, SelectedPreset.Id))
        {
            StatusText = LauncherPresetStatus.DeleteBlocked(SelectedPreset.Name);
            return;
        }

        var accepted = await ConfirmDeletePresetAsync(SelectedPreset).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = LauncherPresetStatus.DeleteCanceled(SelectedPreset.Name);
            return;
        }

        var preferredPresetId = SelectedPreset.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.DeletePreset(_presetFile, SelectedPreset.Id, out var fallbackPresetId);
        if (preset is null)
        {
            ApplyPresetFileState(snapshot, preferredPresetId);
            ApplyStatusText(LauncherPresetStatus.CurrentPresetMissing);
            return;
        }
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(fallbackPresetId);
        ApplyStatusText(LauncherPresetStatus.Deleted(preset.Name));
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
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

        entry.UpdatedAtUtc = CurrentTimestampUtc();
        Entries.Add(new LauncherEntryViewModel(entry, this));
        _settings.CustomEntryOrder.Add(entry.Id);

        if (!SaveEntryCollectionChanges(savePresets: false, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Added(entry.Name));
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var draft = await RequestEntryDraftAsync(item.Entry).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = LauncherEntryStatus.EditCanceled(item.Name);
            return;
        }

        var oldId = item.Id;
        var updated = CreateEntryFromDraftOrReportValidation(draft, oldId, item.Entry);
        if (updated is null)
            return;

        item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());
        if (!LauncherEntryIds.Same(oldId, item.Id))
            LauncherEntryReferences.RenameEntryId(_presetFile, _settings.CustomEntryOrder, oldId, item.Id);

        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Updated(item.Name));
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var accepted = await ConfirmDeleteEntryAsync(item).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = LauncherEntryStatus.DeleteCanceled(item.Name);
            return;
        }

        var entryName = item.Name;
        Entries.Remove(item);
        LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, item.Id);
        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText(LauncherEntryStatus.Deleted(entryName));
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
            Entries.Select(ToLaunchEntry));
        if (validation is null)
            return entry;

        ApplyStatusText(validation);
        return null;
    }

    private bool SaveEntryCollectionChanges(bool savePresets, LauncherEntryMutationSnapshot rollbackSnapshot)
    {
        TotalCount = Entries.Count;

        var saveFailure = LauncherEntryMutationPersistence.Save(
            Entries.Select(ToLaunchEntry),
            _configPath,
            _presetFile,
            _presetPath,
            _settings,
            _settingsPath,
            savePresets,
            rollbackSnapshot,
            EnsureCustomOrderContainsEntries);

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

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot() =>
        LauncherEntryMutationSnapshot.Capture(
            Entries.Select(ToLaunchEntry),
            _presetFile,
            _settings.CustomEntryOrder,
            SelectedPreset?.Id);

    private void RestoreEntryMutationSnapshot(LauncherEntryMutationSnapshot snapshot)
    {
        ReplaceEntries(snapshot.CloneEntries());
        _settings.CustomEntryOrder = snapshot.CloneCustomEntryOrder();
        ApplyPresetFileState(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(Entries);
    }

    private void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(new LauncherEntryViewModel(entry, this));
        }

        TotalCount = Entries.Count;
    }

    public void BeginDragReorder(string draggedEntryId)
    {
        _dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId);
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        return _dragReorderWorkflow.Preview(IsCustomSortMode, draggedEntryId, targetEntryId, insertAfter);
    }

    public void ClearDragHover()
    {
        _dragReorderWorkflow.ClearHover();
    }

    public void CommitDragReorder()
    {
        var result = _dragReorderWorkflow.Commit(IsCustomSortMode);
        ApplyStatusText(result);
    }

    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        ApplyStatusText(result);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task LaunchSelectedAsync()
    {
        var selected = GetSelectedEntries();
        if (selected.Count == 0)
        {
            StatusText = LauncherSelectionStatus.NoSelectedEntries;
            return;
        }

        try
        {
            var selectedEntries = selected.Select(ToLaunchEntry).ToList();
            if (RequireConfirmationForLaunches)
            {
                var accepted = await ConfirmBatchLaunchAsync(selectedEntries)
                    .ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = LauncherSelectionStatus.BatchLaunchCanceled(selected.Count);
                    return;
                }
            }

            var report = _launchWorkflow.StartBatch(
                selectedEntries,
                RequireConfirmationForLaunches,
                plan => DryRunText = plan.CommandLine);
            StatusText = report.ToStatusText();
        }
        catch (Exception ex)
        {
            StatusText = LauncherSelectionStatus.BatchLaunchFailed(ex);
        }
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        try
        {
            var plan = _launchWorkflow.BuildPlan(item.Entry, RequireConfirmationForLaunches);
            DryRunText = plan.CommandLine;
            StatusText = LauncherSingleLaunchStatus.Preview(item.Name, plan.RequiresConfirmation);
        }
        catch (Exception ex)
        {
            DryRunText = ex.Message;
            StatusText = LauncherSingleLaunchStatus.PreviewFailed(item.Name);
        }
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        var previousSelected = item.ReplaceEntrySelectionFromOwner(selected);
        SaveSelectionChangeOrRestore([(Entry: item, Selected: previousSelected)]);
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        string? dryRunText = null;
        try
        {
            var plan = _launchWorkflow.BuildPlan(item.Entry, RequireConfirmationForLaunches);
            dryRunText = plan.CommandLine;
            if (plan.RequiresConfirmation)
            {
                var accepted = await ConfirmLaunchAsync(item.Entry, plan).ConfigureAwait(true);
                if (!accepted)
                {
                    DryRunText = dryRunText;
                    StatusText = LauncherSingleLaunchStatus.Canceled(item.Name);
                    return;
                }
            }

            _launchWorkflow.Start(plan);
            DryRunText = dryRunText;
            StatusText = LauncherSingleLaunchStatus.Started(item.Name);
        }
        catch (Exception ex)
        {
            if (dryRunText is not null)
                DryRunText = dryRunText;
            StatusText = LauncherSingleLaunchStatus.Failed(item.Name, ex);
        }
    }

    private void NotifyDisplayModeProjectionChanged()
    {
        OnPropertyChanged(nameof(IsCardsViewMode));
        OnPropertyChanged(nameof(IsDetailedListMode));
        OnPropertyChanged(nameof(IsNamesViewMode));
    }

    private void RefreshLaunchConfirmationPresentation()
    {
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        LauncherEntryLayoutPresentation.Refresh(Entries);
        ApplyFilter();
    }

    private bool CanOperateOnSelection() => SelectedCount > 0;

    private bool CanApplyPreset()
    {
        if (SelectedPreset is null || Entries.Count == 0)
            return false;

        return BuildPresetSelectionPreview(GetSelectedEntryIds()).HasChanges;
    }

    private bool CanUpdateSelectedPreset()
    {
        if (SelectedPreset is null || Entries.Count == 0)
            return false;

        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, SelectedPreset.Id))
            return false;

        var selectedEntryIds = GetSelectedEntryIds();
        if (selectedEntryIds.Count == 0)
            return false;

        return BuildPresetSelectionPreview(selectedEntryIds).HasChanges;
    }

    private bool CanDeleteSelectedPreset() =>
        SelectedPreset is not null
        && LauncherPresetWorkflow.CanDeletePreset(_presetFile, SelectedPreset.Id);

    private void ApplyFilter()
    {
        var sorted = LauncherEntryVisibility.FilterAndSort(
            Entries,
            FilterText,
            SelectedSortMode?.Id,
            _settings.CustomEntryOrder,
            ToLaunchEntry);

        VisibleEntries.Clear();
        foreach (var entry in sorted)
        {
            VisibleEntries.Add(entry);
        }

        VisibleCount = VisibleEntries.Count;
        LauncherEntryLayoutPresentation.ApplyCardSize(Entries, VisibleEntries, _cardsAvailableSize);
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _cardsAvailableSize = LauncherEntryLayoutPresentation.NormalizeAvailableSize(availableSize);
        LauncherEntryLayoutPresentation.ApplyCardSize(Entries, VisibleEntries, _cardsAvailableSize);
    }

    private void EnsureCustomOrderContainsEntries() =>
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds());

    private void ApplyStatusText(string? statusText)
    {
        if (statusText is not null)
            StatusText = statusText;
    }

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        var snapshot = CaptureSelectionSnapshot(entries);
        foreach (var entry in entries)
        {
            entry.SetSelectionFromOwner(selected);
        }

        return SaveSelectionChangeOrRestore(snapshot);
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        var snapshot = CaptureSelectionSnapshot(Entries);
        foreach (var entry in Entries)
        {
            entry.SetSelectionFromOwner(selectedIds.Contains(entry.Id));
        }

        return SaveSelectionChangeOrRestore(snapshot);
    }

    private static List<(LauncherEntryViewModel Entry, bool Selected)> CaptureSelectionSnapshot(
        IEnumerable<LauncherEntryViewModel> entries)
    {
        return entries
            .Select(entry => (Entry: entry, Selected: entry.IsSelected))
            .ToList();
    }

    private bool SaveSelectionChangeOrRestore(
        IReadOnlyList<(LauncherEntryViewModel Entry, bool Selected)> snapshot)
    {
        RefreshSelectionState();
        if (SaveConfig())
            return true;

        RestoreSelection(snapshot);
        RefreshSelectionState();
        return false;
    }

    private static void RestoreSelection(IEnumerable<(LauncherEntryViewModel Entry, bool Selected)> snapshot)
    {
        foreach (var (entry, selected) in snapshot)
        {
            entry.SetSelectionFromOwner(selected);
        }
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
        ApplyPresetCommand.NotifyCanExecuteChanged();
        NotifyPresetManagementCommands();
    }

    private LauncherPresetSelectionPreview BuildPresetSelectionPreview(IReadOnlyList<string> selectedEntryIds)
    {
        if (SelectedPreset is null)
            return LauncherPresetSelectionPreview.NoPreset(PresetCount);

        return LauncherPresetSelectionPreview.ForPreset(
            SelectedPreset.Name,
            selectedEntryIds,
            GetEntryIds(),
            SelectedPreset.EntryIds);
    }

    private IEnumerable<string> GetEntryIds() =>
        Entries.Select(entry => entry.Id);

    private List<LauncherEntryViewModel> GetSelectedEntries() =>
        Entries
            .Where(entry => entry.IsSelected)
            .ToList();

    private List<string> GetSelectedEntryIds() =>
        GetSelectedEntries()
            .Select(entry => entry.Id)
            .ToList();

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }

    private string? LoadPresetFile(string? preferredPresetId)
    {
        LauncherPresetFile presetFile;
        string? error;
        try
        {
            presetFile = LauncherPresetStore.Load(_presetPath);
            error = null;
        }
        catch (Exception ex)
        {
            presetFile = new LauncherPresetFile();
            error = LauncherPersistenceStatus.PresetLoadFailed(ex);
        }

        ApplyPresetFileState(presetFile, preferredPresetId);
        return error;
    }

    private void ApplyPresetFileState(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        Presets.Clear();
        foreach (var preset in presetFile.Presets)
        {
            Presets.Add(new LauncherPresetViewModel(preset));
        }

        var targetPresetId = preferredPresetId ?? presetFile.DefaultPresetId;
        _presetFile = presetFile;
        SelectedPreset = Presets.FirstOrDefault(preset => LauncherPresetIds.Same(preset.Id, targetPresetId))
            ?? Presets.FirstOrDefault();
        PresetCount = Presets.Count;
        NotifyPresetManagementCommands();
    }

    private bool SavePresetMutationOrRestore(LauncherPresetFile snapshot, string? preferredPresetId)
    {
        if (SavePresets())
            return true;

        ApplyPresetFileState(snapshot, preferredPresetId);
        return false;
    }

    private bool SavePresets()
    {
        try
        {
            LauncherPresetStore.Save(_presetFile, _presetPath);
            return true;
        }
        catch (Exception ex)
        {
            ApplyStatusText(LauncherPersistenceStatus.PresetSaveFailed(ex));
            return false;
        }
    }

    private bool SaveConfig()
    {
        try
        {
            LauncherConfigStore.Save(Entries.Select(ToLaunchEntry), _configPath);
            return true;
        }
        catch (Exception ex)
        {
            ApplyStatusText(LauncherPersistenceStatus.ConfigSaveFailed(ex));
            return false;
        }
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.Entry;

    private static string CurrentTimestampUtc() => DateTimeOffset.UtcNow.ToString("O");

    private void SaveSettingsChange(
        LauncherSettingsSnapshot snapshot,
        Action<LauncherSettingsSnapshot> onRollback,
        Action onSuccess)
    {
        var saveFailure = LauncherSettingsMutationPersistence.Save(
            _settings,
            _settingsPath,
            snapshot,
            () => onRollback(snapshot));
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return;
        }

        onSuccess();
    }

}
