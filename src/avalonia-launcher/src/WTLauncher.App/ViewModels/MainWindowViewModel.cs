using Avalonia;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IEntryViewContext
{
    private readonly string _configPath;
    private readonly string _presetPath;
    private readonly string _settingsPath;
    private readonly LauncherSettings _settings;
    private readonly LauncherProcess _launcherProcess;
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

    public MainWindowViewModel(string configPath, string settingsPath, LauncherSettings settings, LauncherProcess launcherProcess)
    {
        _configPath = configPath;
        _presetPath = LauncherPresetStore.ResolvePresetPath(configPath);
        _settingsPath = settingsPath;
        _settings = settings;
        _launcherProcess = launcherProcess;
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _settingsPath,
            Entries,
            EnsureCustomOrderContainsEntries,
            ApplyFilter);
        ConfigPathDisplay = $"配置: {configPath} · 启动组: {_presetPath}";
        FilterText = settings.LastFilterText;
        _requireConfirmationForLaunches = settings.RequireConfirmationForLaunches;
        _selectedSortMode = LauncherModeCatalog.ResolveSortMode(SortModes, settings.SortMode);
        _settings.SortMode = _selectedSortMode.Id;
        _selectedDisplayMode = LauncherModeCatalog.ResolveDisplayMode(DisplayModes, settings.EntryViewMode);
        _settings.EntryViewMode = _selectedDisplayMode.Id;
        Refresh();
    }

    public ObservableCollection<LauncherEntryViewModel> Entries { get; } = [];

    public ObservableCollection<LauncherEntryViewModel> VisibleEntries { get; } = [];

    public ObservableCollection<LauncherPresetViewModel> Presets { get; } = [];

    public ObservableCollection<LauncherSortModeViewModel> SortModes { get; } = LauncherModeCatalog.CreateSortModes();

    public ObservableCollection<LauncherDisplayModeViewModel> DisplayModes { get; } = LauncherModeCatalog.CreateDisplayModes();

    private LauncherEntryViewMode CurrentEntryViewMode => SelectedDisplayMode?.Mode ?? LauncherEntryViewMode.Cards;

    public bool IsCustomSortMode => SelectedSortMode?.Id == "custom";

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

    public Func<IReadOnlyList<string>, Task<PresetDraft?>> RequestNewPresetAsync { get; set; } =
        static _ => Task.FromResult<PresetDraft?>(null);

    public Func<LauncherPresetViewModel, Task<bool>> ConfirmDeletePresetAsync { get; set; } =
        static _ => Task.FromResult(false);

    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync { get; set; } =
        static _ => Task.FromResult<EntryDraft?>(null);

    public Func<LauncherEntryViewModel, Task<bool>> ConfirmDeleteEntryAsync { get; set; } =
        static _ => Task.FromResult(false);

    partial void OnFilterTextChanged(string value)
    {
        var result = LauncherSettingsWorkflow.ChangeFilterText(
            _settings,
            _settingsPath,
            value,
            snapshot =>
            {
                _filterText = snapshot.LastFilterText;
                OnPropertyChanged(nameof(FilterText));
                ApplyFilter();
            });
        ApplySettingsResult(result, ApplyFilter);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        OnPropertyChanged(nameof(IsCustomSortMode));
        if (value is null)
            return;

        var result = LauncherSettingsWorkflow.ChangeSortMode(
            _settings,
            _settingsPath,
            value.Id,
            EnsureCustomOrderContainsEntries,
            snapshot =>
            {
                _selectedSortMode = LauncherModeCatalog.ResolveSortMode(SortModes, snapshot.SortMode);
                OnPropertyChanged(nameof(SelectedSortMode));
                OnPropertyChanged(nameof(IsCustomSortMode));
                ApplyFilter();
            });
        ApplySettingsResult(
            result,
            () =>
            {
                ApplyFilter();
                StatusText = $"排序模式: {value.Label}（已保存）";
            });
    }

    partial void OnSelectedDisplayModeChanged(LauncherDisplayModeViewModel? value)
    {
        OnPropertyChanged(nameof(IsCardsViewMode));
        OnPropertyChanged(nameof(IsDetailedListMode));
        OnPropertyChanged(nameof(IsNamesViewMode));
        if (value is null)
            return;

        var result = LauncherSettingsWorkflow.ChangeDisplayMode(
            _settings,
            _settingsPath,
            value.Id,
            RefreshEntryLayoutPresentation,
            snapshot =>
            {
                _selectedDisplayMode = LauncherModeCatalog.ResolveDisplayMode(DisplayModes, snapshot.EntryViewMode);
                OnPropertyChanged(nameof(SelectedDisplayMode));
                OnPropertyChanged(nameof(IsCardsViewMode));
                OnPropertyChanged(nameof(IsDetailedListMode));
                OnPropertyChanged(nameof(IsNamesViewMode));
                RefreshEntryLayoutPresentation();
            });
        ApplySettingsResult(
            result,
            () => StatusText = $"视图模式: {value.Label}（已保存）");
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        var result = LauncherSettingsWorkflow.ChangeLaunchConfirmation(
            _settings,
            _settingsPath,
            value,
            () =>
            {
                OnPropertyChanged(nameof(LaunchConfirmationToggleText));
                OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
                RefreshEntryLayoutPresentation();
                ApplyFilter();
            },
            snapshot =>
            {
                _requireConfirmationForLaunches = snapshot.RequireConfirmationForLaunches;
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                OnPropertyChanged(nameof(LaunchConfirmationToggleText));
                OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
                RefreshEntryLayoutPresentation();
                ApplyFilter();
            });
        ApplySettingsResult(
            result,
            () => StatusText = value
                ? "启动确认: 所有启动项启动前统一确认"
                : "启动确认: 所有启动项直接启动");
    }

    [RelayCommand]
    private void Refresh()
    {
        var entrySummary = LauncherEntryReload.Load(
            _configPath,
            Entries,
            entry => new LauncherEntryViewModel(entry, this));
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, Entries.Select(entry => entry.Id));

        var presetResult = LauncherPresetReload.Load(_presetPath, Presets, preferredPresetId: null);

        TotalCount = entrySummary.TotalCount;
        SelectedCount = entrySummary.SelectedCount;
        _presetFile = presetResult.PresetFile;
        SelectedPreset = presetResult.SelectedPreset;
        PresetCount = presetResult.PresetCount;
        RefreshSelectionState();
        NotifyPresetManagementCommands();
        ApplyFilter();
        RefreshEntryLayoutPresentation();
        StatusText = presetResult.ErrorMessage
            ?? $"已加载 {entrySummary.TotalCount} 个启动项、{presetResult.PresetCount} 个启动组；勾选自动保存到 config\\config.json";
    }

    [RelayCommand]
    private void SelectVisible()
    {
        var result = LauncherSelectionWorkflow.SelectVisible(VisibleEntries, RefreshSelectionState, SaveConfig);
        ApplyStatusText(result.StatusText);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        var result = LauncherSelectionWorkflow.ClearSelection(Entries, RefreshSelectionState, SaveConfig);
        ApplyStatusText(result.StatusText);
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        var result = LauncherPresetApplyWorkflow.Apply(SelectedPreset, PresetCount, Entries, ReplaceSelection);
        ApplyStatusText(result.StatusText);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        if (SelectedCount == 0)
        {
            StatusText = "未选择任何启动项";
            return;
        }

        var entryIds = GetSelectedEntryIds();
        var draft = await RequestNewPresetAsync(entryIds).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = "已取消: 保存启动组";
            return;
        }

        var name = (draft.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = "启动组名称不能为空";
            return;
        }

        var preferredPresetId = SelectedPreset?.Id;
        var result = LauncherPresetManagementWorkflow.AddPreset(
            _presetFile,
            name,
            entryIds,
            preferredPresetId,
            SavePresets,
            ApplyPresetFileFromSnapshot);
        ApplyPresetManagementResult(result);
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }
        if (SelectedPreset.IsLocked)
        {
            StatusText = $"系统启动组不能更新: {SelectedPreset.Name}";
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            StatusText = "请先勾选至少一个启动项";
            return;
        }

        var preferredPresetId = SelectedPreset.Id;
        var result = LauncherPresetManagementWorkflow.UpdatePreset(
            _presetFile,
            SelectedPreset.Id,
            SelectedPreset.Name,
            entryIds,
            preferredPresetId,
            SavePresets,
            ApplyPresetFileFromSnapshot);
        ApplyPresetManagementResult(result);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }
        if (SelectedPreset.IsLocked || !LauncherPresetWorkflow.CanDeletePreset(_presetFile, SelectedPreset.Id))
        {
            StatusText = $"启动组不能删除: {SelectedPreset.Name}";
            return;
        }

        var accepted = await ConfirmDeletePresetAsync(SelectedPreset).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = $"已取消: 删除启动组 {SelectedPreset.Name}";
            return;
        }

        var preferredPresetId = SelectedPreset.Id;
        var result = LauncherPresetManagementWorkflow.DeletePreset(
            _presetFile,
            SelectedPreset.Id,
            preferredPresetId,
            SavePresets,
            ApplyPresetFileFromSnapshot);
        ApplyPresetManagementResult(result);
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = "已取消: 新增按钮";
            return;
        }

        var result = LauncherEntryManagementWorkflow.AddEntry(
            draft,
            snapshot,
            Entries,
            _settings.CustomEntryOrder,
            entry => new LauncherEntryViewModel(entry, this),
            NowUtcText(),
            SaveEntryCollectionChanges,
            RestoreEntryMutationSnapshot);
        ApplyEntryManagementResult(result);
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var draft = await RequestEntryDraftAsync(item.Entry).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = $"已取消: 编辑 {item.Name}";
            return;
        }

        var result = LauncherEntryManagementWorkflow.EditEntry(
            item,
            draft,
            snapshot,
            Entries,
            _presetFile.Presets,
            _settings.CustomEntryOrder,
            NowUtcText(),
            SaveEntryCollectionChanges,
            RestoreEntryMutationSnapshot);
        ApplyEntryManagementResult(result);
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var accepted = await ConfirmDeleteEntryAsync(item).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = $"已取消: 删除 {item.Name}";
            return;
        }

        var result = LauncherEntryManagementWorkflow.DeleteEntry(
            item,
            snapshot,
            Entries,
            _presetFile.Presets,
            _settings.CustomEntryOrder,
            SaveEntryCollectionChanges,
            RestoreEntryMutationSnapshot);
        ApplyEntryManagementResult(result);
    }

    private bool SaveEntryCollectionChanges(bool savePresets, LauncherEntryMutationSnapshot? rollbackSnapshot = null)
    {
        return LauncherEntryCollectionSaveStateWorkflow.Save(
            Entries,
            _configPath,
            _presetFile,
            _presetPath,
            _settings,
            _settingsPath,
            savePresets,
            rollbackSnapshot,
            EnsureCustomOrderContainsEntries,
            ApplyEntryCollectionSaveState,
            status => StatusText = status,
            () =>
            {
                RefreshSelectionState();
                ApplyFilter();
            });
    }

    private void ApplyEntryCollectionSaveState(LauncherEntryCollectionSaveState state)
    {
        SelectedCount = state.SelectedCount;
        TotalCount = state.TotalCount;
    }

    private LauncherEntryMutationSnapshot CaptureEntryMutationSnapshot()
    {
        return LauncherEntryMutationSnapshot.Capture(
            Entries.Select(entry => entry.Entry),
            _presetFile,
            _settings.CustomEntryOrder,
            SelectedPreset?.Id);
    }

    private void RestoreEntryMutationSnapshot(LauncherEntryMutationSnapshot snapshot)
    {
        var summary = LauncherEntryMutationRestore.Restore(
            snapshot,
            Entries,
            _settings,
            ApplyPresetFileFromSnapshot,
            entry => new LauncherEntryViewModel(entry, this));
        TotalCount = summary.TotalCount;
        RefreshSelectionState();
        ApplyFilter();
        RefreshEntryLayoutPresentation();
    }

    private void ApplyEntryManagementResult(LauncherEntryManagementResult result)
    {
        ApplyStatusText(result.StatusText);
    }

    public void MoveEntryBefore(string draggedEntryId, string targetEntryId)
    {
        var result = _dragReorderWorkflow.MoveBefore(IsCustomSortMode, draggedEntryId, targetEntryId);
        ApplyStatusText(result.StatusText);
    }

    public void BeginDragReorder(string draggedEntryId)
    {
        _dragReorderWorkflow.Begin(IsCustomSortMode, draggedEntryId);
    }

    public bool PreviewMoveEntryBefore(string draggedEntryId, string targetEntryId)
    {
        return PreviewMoveEntry(draggedEntryId, targetEntryId, insertAfter: false);
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
        ApplyStatusText(result.StatusText);
    }

    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        ApplyStatusText(result.StatusText);
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task LaunchSelectedAsync()
    {
        var result = await LauncherLaunchWorkflow.LaunchBatchAsync(
            Entries
                .Where(entry => entry.IsSelected)
                .Select(entry => new LauncherLaunchItem(entry.Name, entry.Entry))
                .ToList(),
            RequireConfirmationForLaunches,
            ConfirmBatchLaunchAsync,
            plan => _launcherProcess.Start(plan)).ConfigureAwait(true);
        ApplyLaunchResult(result);
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        var result = LauncherLaunchWorkflow.Preview(item.Entry, item.Name, RequireConfirmationForLaunches);
        ApplyLaunchResult(result);
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        var previousSelected = item.Entry.Selected;
        item.Entry.Selected = selected;
        RefreshSelectionState();
        if (SaveConfig())
            return;

        item.SetSelectionFromOwner(previousSelected);
        RefreshSelectionState();
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        RefreshSelectionState();
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        var result = await LauncherLaunchWorkflow.LaunchOneAsync(
            item.Entry,
            item.Name,
            RequireConfirmationForLaunches,
            ConfirmLaunchAsync,
            plan => _launcherProcess.Start(plan)).ConfigureAwait(true);
        ApplyLaunchResult(result);
    }

    private bool CanOperateOnSelection()
    {
        return SelectedCount > 0;
    }

    private bool CanApplyPreset()
    {
        return LauncherPresetSelectionRules.CanApply(SelectedPreset, PresetCount, Entries);
    }

    private bool CanUpdateSelectedPreset()
    {
        return LauncherPresetSelectionRules.CanUpdate(SelectedPreset, PresetCount, Entries);
    }

    private bool CanDeleteSelectedPreset()
    {
        return LauncherPresetSelectionRules.CanDelete(SelectedPreset, _presetFile);
    }

    private void ApplyFilter()
    {
        var result = LauncherEntryVisibilityWorkflow.ApplyFilter(
            VisibleEntries,
            Entries,
            FilterText,
            SelectedSortMode?.Id,
            _settings.CustomEntryOrder,
            _cardsAvailableSize);
        VisibleCount = result.VisibleCount;
    }

    private void RefreshEntryLayoutPresentation()
    {
        LauncherEntryLayoutPresentation.Refresh(Entries);
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _cardsAvailableSize = LauncherEntryVisibilityWorkflow.UpdateAvailableSize(
            Entries,
            VisibleEntries,
            availableSize);
    }

    private void EnsureCustomOrderContainsEntries()
    {
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, Entries.Select(entry => entry.Id));
    }

    private static string NowUtcText()
    {
        return DateTimeOffset.UtcNow.ToString("O");
    }

    private void ApplyStatusText(string? statusText)
    {
        if (statusText is not null)
            StatusText = statusText;
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        var snapshot = Entries
            .Select(entry => (Entry: entry, Selected: entry.IsSelected))
            .ToList();
        foreach (var entry in Entries)
        {
            entry.SetSelectionFromOwner(selectedIds.Contains(entry.Entry.Id));
        }

        RefreshSelectionState();
        if (SaveConfig())
            return true;

        foreach (var (entry, selected) in snapshot)
        {
            entry.SetSelectionFromOwner(selected);
        }

        RefreshSelectionState();
        return false;
    }

    private void RefreshSelectionState()
    {
        ApplyPresetSelectionState(LauncherPresetSelectionRules.BuildState(SelectedPreset, PresetCount, Entries));
        NotifySelectionCommands();
    }

    private void NotifySelectionCommands()
    {
        LauncherCommandNotifications.NotifySelection(
            LaunchSelectedCommand,
            ClearSelectionCommand,
            ApplyPresetCommand,
            SaveSelectionAsPresetCommand,
            UpdateSelectedPresetCommand,
            DeleteSelectedPresetCommand);
    }

    private void ApplyPresetSelectionState(LauncherPresetSelectionState selection)
    {
        SelectedCount = selection.SelectedCount;
        ApplyPresetButtonText = selection.ApplyPresetButtonText;
        PresetSelectionStatusText = selection.PresetSelectionStatusText;
    }

    private List<string> GetSelectedEntryIds()
    {
        return LauncherPresetCatalog.GetSelectedEntryIds(Entries);
    }

    private void ApplyPresetManagementResult(LauncherPresetManagementResult result)
    {
        if (result.ShouldReload)
            ReloadPresets(result.NextPresetId);
        ApplyStatusText(result.StatusText);
    }

    private void NotifyPresetManagementCommands()
    {
        LauncherCommandNotifications.NotifyPresetManagement(
            SaveSelectionAsPresetCommand,
            UpdateSelectedPresetCommand,
            DeleteSelectedPresetCommand);
    }

    private string? ReloadPresets(string? preferredPresetId)
    {
        var result = LauncherPresetReload.Load(_presetPath, Presets, preferredPresetId);
        ApplyPresetState(result.PresetFile, result.SelectedPreset, result.PresetCount);
        NotifyPresetManagementCommands();
        return result.ErrorMessage;
    }

    private void ApplyPresetFileFromSnapshot(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        var selectedPreset = LauncherPresetCatalog.Apply(presetFile, Presets, preferredPresetId);
        ApplyPresetState(presetFile, selectedPreset, Presets.Count);
        NotifyPresetManagementCommands();
    }

    private void ApplyPresetState(LauncherPresetFile presetFile, LauncherPresetViewModel? selectedPreset, int presetCount)
    {
        _presetFile = presetFile;
        SelectedPreset = selectedPreset;
        PresetCount = presetCount;
    }

    private bool SavePresets()
    {
        if (LauncherPresetPersistence.Save(_presetFile, _presetPath, out var errorMessage))
            return true;

        ApplyStatusText(errorMessage ?? "启动组保存失败");
        return false;
    }

    private bool SaveConfig()
    {
        try
        {
            LauncherConfigStore.Save(Entries.Select(entry => entry.Entry), _configPath);
            return true;
        }
        catch (Exception ex)
        {
            ApplyStatusText($"配置保存失败: {ex.Message}");
            return false;
        }
    }

    private void ApplySettingsResult(LauncherSettingsMutationResult result, Action onSuccess)
    {
        if (!result.Succeeded)
        {
            ApplyStatusText(result.StatusText);
            return;
        }

        onSuccess();
    }

    private void ApplyLaunchResult(LauncherLaunchResult result)
    {
        if (result.DryRunText is not null)
            DryRunText = result.DryRunText;
        StatusText = result.StatusText;
    }

}
