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
    private LauncherDragReorderSession? _dragReorderSession;

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
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.LastFilterText = value ?? string.Empty;
        if (!SaveSettings())
        {
            snapshot.Restore(_settings);
            _filterText = snapshot.LastFilterText;
            OnPropertyChanged(nameof(FilterText));
            ApplyFilter();
            return;
        }

        ApplyFilter();
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        OnPropertyChanged(nameof(IsCustomSortMode));
        if (value is null)
            return;

        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.SortMode = value.Id;
        EnsureCustomOrderContainsEntries();
        if (!SaveSettings())
        {
            snapshot.Restore(_settings);
            _selectedSortMode = SortModes.FirstOrDefault(mode => mode.Id == snapshot.SortMode) ?? SortModes[0];
            OnPropertyChanged(nameof(SelectedSortMode));
            OnPropertyChanged(nameof(IsCustomSortMode));
            ApplyFilter();
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

        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.EntryViewMode = value.Id;
        RefreshEntryLayoutPresentation();
        if (!SaveSettings())
        {
            snapshot.Restore(_settings);
            _selectedDisplayMode = DisplayModes.FirstOrDefault(mode => mode.Id == snapshot.EntryViewMode) ?? DisplayModes[0];
            OnPropertyChanged(nameof(SelectedDisplayMode));
            OnPropertyChanged(nameof(IsCardsViewMode));
            OnPropertyChanged(nameof(IsDetailedListMode));
            OnPropertyChanged(nameof(IsNamesViewMode));
            RefreshEntryLayoutPresentation();
            return;
        }

        StatusText = $"视图模式: {value.Label}（已保存）";
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.RequireConfirmationForLaunches = value;
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        RefreshEntryLayoutPresentation();
        ApplyFilter();
        if (!SaveSettings())
        {
            snapshot.Restore(_settings);
            _requireConfirmationForLaunches = snapshot.RequireConfirmationForLaunches;
            OnPropertyChanged(nameof(RequireConfirmationForLaunches));
            OnPropertyChanged(nameof(LaunchConfirmationToggleText));
            OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
            RefreshEntryLayoutPresentation();
            ApplyFilter();
            return;
        }

        StatusText = value
            ? "启动确认: 所有启动项启动前统一确认"
            : "启动确认: 所有启动项直接启动";
    }

    [RelayCommand]
    private void Refresh()
    {
        var summary = LauncherEntryCollection.Replace(
            Entries,
            LauncherConfigStore.Load(_configPath),
            entry => new LauncherEntryViewModel(entry, this));
        EnsureCustomOrderContainsEntries();

        TotalCount = summary.TotalCount;
        SelectedCount = summary.SelectedCount;
        var presetLoadError = LoadPresets();
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
        var targets = VisibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            StatusText = VisibleEntries.Count == 0
                ? "当前没有可勾选的可见项"
                : "可见项已全部勾选";
            return;
        }

        if (SetSelection(targets, selected: true))
            StatusText = $"已勾选可见 {targets.Count} 项（已保存）";
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        var targets = Entries.Where(entry => entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            StatusText = "当前没有已勾选项";
            return;
        }

        if (SetSelection(targets, selected: false))
            StatusText = $"已清空 {targets.Count} 个勾选项（已保存）";
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }

        var selection = GetPresetSelectionPreview(SelectedPreset);
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
        var presetSnapshot = LauncherPresetFiles.Clone(_presetFile);
        var preferredPresetId = SelectedPreset?.Id;
        var preset = LauncherPresetWorkflow.AddPreset(_presetFile, name, entryIds);
        if (!SavePresets())
        {
            RestorePresetFile(presetSnapshot, preferredPresetId);
            return;
        }

        LoadPresets(preset.Id);
        StatusText = $"已保存启动组: {preset.Name}（{preset.EntryIds.Count} 项）";
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

        var presetSnapshot = LauncherPresetFiles.Clone(_presetFile);
        var preferredPresetId = SelectedPreset.Id;
        var preset = LauncherPresetWorkflow.UpdatePreset(_presetFile, SelectedPreset.Id, SelectedPreset.Name, entryIds);
        if (preset is null)
        {
            RestorePresetFile(presetSnapshot, preferredPresetId);
            StatusText = "当前启动组已不存在，请重新加载";
            return;
        }
        if (!SavePresets())
        {
            RestorePresetFile(presetSnapshot, preferredPresetId);
            return;
        }

        LoadPresets(preset.Id);
        StatusText = $"已更新启动组: {preset.Name}（{preset.EntryIds.Count} 项）";
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

        var presetSnapshot = LauncherPresetFiles.Clone(_presetFile);
        var preferredPresetId = SelectedPreset.Id;
        var preset = LauncherPresetWorkflow.DeletePreset(_presetFile, SelectedPreset.Id, out var fallbackPresetId);
        if (preset is null)
        {
            RestorePresetFile(presetSnapshot, preferredPresetId);
            StatusText = "当前启动组已不存在，请重新加载";
            return;
        }

        if (!SavePresets())
        {
            RestorePresetFile(presetSnapshot, preferredPresetId);
            return;
        }

        LoadPresets(fallbackPresetId);
        StatusText = $"已删除启动组: {preset.Name}";
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

        var result = LauncherEntryWorkflow.AddEntry(
            draft,
            Entries,
            _settings.CustomEntryOrder,
            entry => new LauncherEntryViewModel(entry, this),
            NowUtcText());
        if (!result.Succeeded)
        {
            StatusText = result.Message ?? "新增按钮失败";
            return;
        }

        if (!SaveEntryCollectionChanges(savePresets: false, rollbackSnapshot: snapshot))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        StatusText = $"已新增按钮: {result.EntryName}（已保存）";
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

        var result = LauncherEntryWorkflow.EditEntry(
            item,
            draft,
            Entries,
            _presetFile.Presets,
            _settings.CustomEntryOrder,
            NowUtcText());
        if (!result.Succeeded)
        {
            StatusText = result.Message ?? "更新按钮失败";
            return;
        }

        if (!SaveEntryCollectionChanges(savePresets: true, rollbackSnapshot: snapshot))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        StatusText = $"已更新按钮: {result.EntryName}（已保存）";
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

        var entryName = LauncherEntryWorkflow.DeleteEntry(
            Entries,
            item,
            _presetFile.Presets,
            _settings.CustomEntryOrder);
        if (!SaveEntryCollectionChanges(savePresets: true, rollbackSnapshot: snapshot))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        StatusText = $"已删除按钮: {entryName}（已保存）";
    }

    private bool SaveEntryCollectionChanges(bool savePresets, LauncherEntryMutationSnapshot? rollbackSnapshot = null)
    {
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        TotalCount = Entries.Count;
        if (!SaveConfig())
            return false;
        if (savePresets && !SavePresets())
        {
            RestorePersistedEntrySnapshot(rollbackSnapshot, restorePresets: false);
            return false;
        }

        EnsureCustomOrderContainsEntries();
        if (!SaveSettings())
        {
            RestorePersistedEntrySnapshot(rollbackSnapshot, restorePresets: savePresets);
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
        var summary = LauncherEntryCollection.Replace(
            Entries,
            snapshot.CloneEntries(),
            entry => new LauncherEntryViewModel(entry, this));

        _settings.CustomEntryOrder = snapshot.CloneCustomEntryOrder();
        ApplyPresetFile(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        TotalCount = summary.TotalCount;
        RefreshSelectionState();
        ApplyFilter();
        RecalculateCardSize();
        RefreshEntryLayoutPresentation();
    }

    private void RestorePersistedEntrySnapshot(LauncherEntryMutationSnapshot? snapshot, bool restorePresets)
    {
        StatusText = LauncherEntryPersistenceRollback.Restore(
            snapshot,
            _configPath,
            _presetPath,
            restorePresets,
            StatusText);
    }

    public void MoveEntryBefore(string draggedEntryId, string targetEntryId)
    {
        if (!IsCustomSortMode)
        {
            StatusText = "请先切换到“自定义拖拽”排序模式";
            return;
        }
        if (string.Equals(draggedEntryId, targetEntryId, StringComparison.Ordinal))
            return;

        EnsureCustomOrderContainsEntries();
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        LauncherEntryOrder.MoveBefore(_settings.CustomEntryOrder, draggedEntryId, targetEntryId);
        if (!SaveSettings())
        {
            snapshot.RestoreCustomEntryOrder(_settings);
            ApplyFilter();
            return;
        }

        ApplyFilter();
        StatusText = "已保存自定义拖拽排序";
    }

    public void BeginDragReorder(string draggedEntryId)
    {
        if (!IsCustomSortMode)
            return;

        EnsureCustomOrderContainsEntries();
        ClearDragPreviewState();

        var source = Entries.FirstOrDefault(entry => string.Equals(entry.Id, draggedEntryId, StringComparison.Ordinal));
        _dragReorderSession = LauncherDragReorderSession.Start(
            draggedEntryId,
            LauncherSettingsSnapshot.Capture(_settings),
            source);
    }

    public bool PreviewMoveEntryBefore(string draggedEntryId, string targetEntryId)
    {
        return PreviewMoveEntry(draggedEntryId, targetEntryId, insertAfter: false);
    }

    public bool PreviewMoveEntry(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        if (!IsCustomSortMode)
            return false;
        var session = _dragReorderSession;
        if (session is null || !session.IsFor(draggedEntryId))
        {
            BeginDragReorder(draggedEntryId);
            session = _dragReorderSession;
        }
        if (session is null)
            return false;
        if (string.Equals(draggedEntryId, targetEntryId, StringComparison.Ordinal))
            return false;

        if (!session.TryRecordPreview(targetEntryId, insertAfter))
            return false;

        EnsureCustomOrderContainsEntries();
        LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, draggedEntryId, targetEntryId, insertAfter);
        ApplyFilter();

        var target = Entries.FirstOrDefault(entry => string.Equals(entry.Id, targetEntryId, StringComparison.Ordinal));
        session.SetTarget(target, insertAfter);
        return true;
    }

    public void ClearDragHover()
    {
        if (_dragReorderSession is null)
            return;

        _dragReorderSession.ClearHover();
    }

    public void CommitDragReorder()
    {
        if (!IsCustomSortMode)
            return;
        if (_dragReorderSession is null)
            return;

        var session = _dragReorderSession;
        if (!SaveSettings())
        {
            session.Snapshot.RestoreCustomEntryOrder(_settings);
            ApplyFilter();
            ClearDragPreviewState();
            return;
        }

        ClearDragPreviewState();
        StatusText = "已保存自定义拖拽排序";
    }

    public void CancelDragReorder()
    {
        var session = _dragReorderSession;
        if (session is null)
            return;

        session.Snapshot.RestoreCustomEntryOrder(_settings);
        ApplyFilter();
        ClearDragPreviewState();
        StatusText = "已取消: 拖拽排序";
    }

    private void ClearDragPreviewState()
    {
        var session = _dragReorderSession;
        if (session is null)
            return;

        _dragReorderSession = null;
        session.Clear();
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
        return SelectedPreset is not null
            && Entries.Count > 0
            && GetPresetSelectionPreview(SelectedPreset!).HasChanges;
    }

    private bool CanUpdateSelectedPreset()
    {
        return SelectedPreset is not null
            && !SelectedPreset.IsLocked
            && SelectedCount > 0
            && GetPresetSelectionPreview(SelectedPreset!).HasChanges;
    }

    private bool CanDeleteSelectedPreset()
    {
        if (SelectedPreset is null || SelectedPreset.IsLocked)
            return false;

        return LauncherPresetWorkflow.CanDeletePreset(_presetFile, SelectedPreset.Id);
    }

    private void ApplyFilter()
    {
        VisibleEntries.Clear();
        var filtered = Entries.Where(entry => LauncherEntrySearch.Matches(entry.Entry, FilterText));

        foreach (var entry in SortEntries(filtered))
        {
            VisibleEntries.Add(entry);
        }

        VisibleCount = VisibleEntries.Count;
        RecalculateCardSize();
    }

    private void RefreshEntryLayoutPresentation()
    {
        foreach (var entry in Entries)
        {
            entry.RefreshLayoutPresentation();
        }
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _cardsAvailableSize = new Size(
            Math.Max(220, availableSize.Width),
            Math.Max(140, availableSize.Height));
        RecalculateCardSize();
    }

    private void RecalculateCardSize()
    {
        var targets = VisibleEntries.Count > 0 ? VisibleEntries : Entries;
        var count = Math.Max(1, targets.Count);
        var layout = LauncherEntryLayoutCalculator.Calculate(
            _cardsAvailableSize.Width,
            _cardsAvailableSize.Height,
            count);
        foreach (var entry in Entries)
        {
            entry.CardWidth = layout.CardWidth;
            entry.CardHeight = layout.CardHeight;
            entry.NameTileWidth = layout.NameTileWidth;
            entry.NameTileHeight = layout.NameTileHeight;
        }
    }

    private IEnumerable<LauncherEntryViewModel> SortEntries(IEnumerable<LauncherEntryViewModel> entries)
    {
        return LauncherEntrySorter.Sort(
            entries,
            SelectedSortMode?.Id,
            _settings.CustomEntryOrder,
            Entries.Select(entry => entry.Id));
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
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
    }

    private void NotifySelectionCommands()
    {
        LaunchSelectedCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        ApplyPresetCommand.NotifyCanExecuteChanged();
        NotifyPresetManagementCommands();
    }

    private void UpdatePresetSelectionStatus()
    {
        var selection = GetPresetSelectionPreview(SelectedPreset);
        ApplyPresetButtonText = selection.ApplyButtonText;
        PresetSelectionStatusText = selection.StatusText;
    }

    private LauncherPresetSelectionPreview GetPresetSelectionPreview(LauncherPresetViewModel? selectedPreset)
    {
        return LauncherPresetCatalog.BuildSelectionPreview(
            selectedPreset,
            PresetCount,
            Entries.Where(entry => entry.IsSelected).Select(entry => entry.Entry.Id),
            Entries.Select(entry => entry.Entry.Id));
    }

    private List<string> GetSelectedEntryIds()
    {
        return LauncherPresetCatalog.GetSelectedEntryIds(Entries);
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }

    private void RestorePresetFile(LauncherPresetFile snapshot, string? preferredPresetId)
    {
        ApplyPresetFile(snapshot, preferredPresetId);
    }

    private string? LoadPresets(string? preferredPresetId = null)
    {
        var error = LauncherPresetPersistence.Load(_presetPath, out var presetFile);
        ApplyPresetFile(presetFile, preferredPresetId);
        return error;
    }

    private void ApplyPresetFile(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        _presetFile = presetFile;
        SelectedPreset = LauncherPresetCatalog.Apply(presetFile, Presets, preferredPresetId);
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

    private bool SaveSettings()
    {
        if (LauncherSettingsPersistence.Save(_settings, _settingsPath, out var errorMessage))
            return true;

        StatusText = errorMessage ?? "设置保存失败";
        return false;
    }
}
