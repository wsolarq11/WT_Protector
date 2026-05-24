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

    public Func<Task<PresetDraft?>> RequestNewPresetAsync { get; set; } =
        static () => Task.FromResult<PresetDraft?>(null);

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
        if (!result.Succeeded)
        {
            StatusText = result.StatusText ?? StatusText;
            return;
        }

        ApplyFilter();
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
        if (!result.Succeeded)
        {
            StatusText = result.StatusText ?? StatusText;
            return;
        }

        ApplyFilter();
        StatusText = $"排序模式: {value.Label}（已保存）";
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
        if (!result.Succeeded)
        {
            StatusText = result.StatusText ?? StatusText;
            return;
        }

        StatusText = $"视图模式: {value.Label}（已保存）";
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
        if (!result.Succeeded)
        {
            StatusText = result.StatusText ?? StatusText;
            return;
        }

        StatusText = value
            ? "启动确认: 所有启动项启动前统一确认"
            : "启动确认: 所有启动项直接启动";
    }

    [RelayCommand]
    private void Refresh()
    {
        var summary = LauncherEntryReload.Load(
            _configPath,
            Entries,
            entry => new LauncherEntryViewModel(entry, this));
        EnsureCustomOrderContainsEntries();

        TotalCount = summary.TotalCount;
        SelectedCount = summary.SelectedCount;
        var presetLoadError = ReloadPresets(preferredPresetId: null);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        ApplyFilter();
        RecalculateCardSize();
        RefreshEntryLayoutPresentation();
        StatusText = presetLoadError
            ?? $"已加载 {TotalCount} 个启动项、{PresetCount} 个启动组；勾选自动保存到 config\\config.json";
    }

    [RelayCommand]
    private void SelectVisible()
    {
        var result = LauncherSelectionWorkflow.SelectVisible(VisibleEntries, RefreshSelectionState, SaveConfig);
        if (result.StatusText is not null)
            StatusText = result.StatusText;
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        var result = LauncherSelectionWorkflow.ClearSelection(Entries, RefreshSelectionState, SaveConfig);
        if (result.StatusText is not null)
            StatusText = result.StatusText;
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }

        var selection = LauncherPresetSelectionRules.BuildPreview(SelectedPreset, PresetCount, Entries);
        if (selection.MissingConfigIds.Count > 0)
        {
            StatusText = $"启动组引用了不存在的启动项: {string.Join(", ", selection.MissingConfigIds)}";
            return;
        }

        if (ReplaceSelection(selection.PresetIds))
            StatusText = $"已应用启动组: {SelectedPreset.Name}（{SelectedCount} 项，已保存）";
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private async Task SaveSelectionAsPresetAsync()
    {
        if (SelectedCount == 0)
        {
            StatusText = "未选择任何启动项";
            return;
        }

        var draft = await RequestNewPresetAsync().ConfigureAwait(true);
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

        var entryIds = GetSelectedEntryIds();
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
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        TotalCount = Entries.Count;
        var result = LauncherEntryMutationPersistence.Save(
            Entries.Select(entry => entry.Entry),
            _configPath,
            _presetFile,
            _presetPath,
            _settings,
            _settingsPath,
            savePresets,
            rollbackSnapshot,
            EnsureCustomOrderContainsEntries);
        if (!result.Succeeded)
        {
            StatusText = result.StatusText ?? StatusText;
            return false;
        }

        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        ApplyFilter();
        return true;
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
        RecalculateCardSize();
        RefreshEntryLayoutPresentation();
    }

    private void ApplyEntryManagementResult(LauncherEntryManagementResult result)
    {
        if (result.StatusText is not null)
            StatusText = result.StatusText;
    }

    public void MoveEntryBefore(string draggedEntryId, string targetEntryId)
    {
        var result = _dragReorderWorkflow.MoveBefore(IsCustomSortMode, draggedEntryId, targetEntryId);
        if (result.StatusText is not null)
            StatusText = result.StatusText;
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
        if (result.StatusText is not null)
            StatusText = result.StatusText;
    }

    public void CancelDragReorder()
    {
        var result = _dragReorderWorkflow.Cancel();
        if (result.StatusText is not null)
            StatusText = result.StatusText;
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
        ApplyLaunchResult(LauncherLaunchWorkflow.Preview(item.Entry, item.Name, RequireConfirmationForLaunches));
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        LauncherSelectionPersistence.UpdateSingle(item, selected, RefreshSelectionState, SaveConfig);
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        ApplyPresetCommand.NotifyCanExecuteChanged();
        UpdatePresetSelectionStatus();
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

    private void ApplyLaunchResult(LauncherLaunchResult result)
    {
        if (result.DryRunText is not null)
            DryRunText = result.DryRunText;
        StatusText = result.StatusText;
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
        VisibleCount = LauncherVisibleEntryCollection.Replace(
            VisibleEntries,
            Entries,
            FilterText,
            SelectedSortMode?.Id,
            _settings.CustomEntryOrder);
        RecalculateCardSize();
    }

    private void RefreshEntryLayoutPresentation()
    {
        LauncherEntryLayoutPresentation.Refresh(Entries);
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _cardsAvailableSize = LauncherEntryLayoutPresentation.NormalizeAvailableSize(availableSize);
        RecalculateCardSize();
    }

    private void RecalculateCardSize()
    {
        LauncherEntryLayoutPresentation.ApplyCardSize(Entries, VisibleEntries, _cardsAvailableSize);
    }

    private void EnsureCustomOrderContainsEntries()
    {
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, Entries.Select(entry => entry.Id));
    }

    private static string NowUtcText()
    {
        return DateTimeOffset.UtcNow.ToString("O");
    }

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        return LauncherSelectionPersistence.SetSelection(entries, selected, RefreshSelectionState, SaveConfig);
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        return LauncherSelectionPersistence.ReplaceSelection(Entries, selectedIds, RefreshSelectionState, SaveConfig);
    }

    private void RefreshSelectionState()
    {
        ApplyPresetSelectionState();
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

    private void UpdatePresetSelectionStatus()
    {
        ApplyPresetSelectionState();
    }

    private void ApplyPresetSelectionState()
    {
        var selection = LauncherPresetSelectionRules.BuildState(SelectedPreset, PresetCount, Entries);
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
        if (result.StatusText is not null)
            StatusText = result.StatusText;
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
        _presetFile = result.PresetFile;
        SelectedPreset = result.SelectedPreset;
        PresetCount = result.PresetCount;
        NotifyPresetManagementCommands();
        return result.ErrorMessage;
    }

    private void ApplyPresetFileFromSnapshot(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        var selectedPreset = LauncherPresetCatalog.Apply(presetFile, Presets, preferredPresetId);
        _presetFile = presetFile;
        SelectedPreset = selectedPreset;
        PresetCount = Presets.Count;
        NotifyPresetManagementCommands();
    }

    private bool SavePresets()
    {
        if (LauncherPresetPersistence.Save(_presetFile, _presetPath, out var errorMessage))
            return true;

        StatusText = errorMessage ?? "启动组保存失败";
        return false;
    }

    private bool SaveConfig()
    {
        if (LauncherConfigPersistence.Save(Entries.Select(entry => entry.Entry), _configPath, out var errorMessage))
            return true;

        StatusText = errorMessage ?? "配置保存失败";
        return false;
    }

}
