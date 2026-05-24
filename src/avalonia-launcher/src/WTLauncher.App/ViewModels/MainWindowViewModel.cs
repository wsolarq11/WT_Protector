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
    private DragReorderSession? _dragReorderSession;

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
        _selectedSortMode = SortModes.FirstOrDefault(mode => mode.Id == settings.SortMode) ?? SortModes[0];
        _settings.SortMode = _selectedSortMode.Id;
        var savedViewMode = LauncherEntryViewModes.Parse(settings.EntryViewMode);
        _selectedDisplayMode = DisplayModes.FirstOrDefault(mode => mode.Mode == savedViewMode) ?? DisplayModes[0];
        _settings.EntryViewMode = _selectedDisplayMode.Id;
        Refresh();
    }

    public ObservableCollection<LauncherEntryViewModel> Entries { get; } = [];

    public ObservableCollection<LauncherEntryViewModel> VisibleEntries { get; } = [];

    public ObservableCollection<LauncherPresetViewModel> Presets { get; } = [];

    public ObservableCollection<LauncherSortModeViewModel> SortModes { get; } =
    [
        new("custom", "自定义拖拽"),
        new("updated", "更新时间"),
        new("alphabetical", "首字母顺序"),
    ];

    public ObservableCollection<LauncherDisplayModeViewModel> DisplayModes { get; } =
    [
        new(LauncherEntryViewMode.Cards, "卡片"),
        new(LauncherEntryViewMode.Details, "详细列表"),
        new(LauncherEntryViewMode.Names, "名称墙"),
    ];

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
        var previous = _settings.LastFilterText;
        _settings.LastFilterText = value ?? string.Empty;
        if (!SaveSettings())
        {
            _settings.LastFilterText = previous;
            _filterText = previous;
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

        var previousSortMode = _settings.SortMode;
        var previousOrder = new List<string>(_settings.CustomEntryOrder);
        _settings.SortMode = value.Id;
        EnsureCustomOrderContainsEntries();
        if (!SaveSettings())
        {
            _settings.SortMode = previousSortMode;
            _settings.CustomEntryOrder = previousOrder;
            _selectedSortMode = SortModes.FirstOrDefault(mode => mode.Id == previousSortMode) ?? SortModes[0];
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

        var previousEntryViewMode = _settings.EntryViewMode;
        var previousDisplayMode = DisplayModes.FirstOrDefault(mode => mode.Id == previousEntryViewMode) ?? DisplayModes[0];
        _settings.EntryViewMode = value.Id;
        RefreshEntryLayoutPresentation();
        if (!SaveSettings())
        {
            _settings.EntryViewMode = previousEntryViewMode;
            _selectedDisplayMode = previousDisplayMode;
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
        var previousValue = _settings.RequireConfirmationForLaunches;
        _settings.RequireConfirmationForLaunches = value;
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        RefreshEntryLayoutPresentation();
        ApplyFilter();
        if (!SaveSettings())
        {
            _settings.RequireConfirmationForLaunches = previousValue;
            _requireConfirmationForLaunches = previousValue;
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
        Entries.Clear();
        foreach (var entry in LauncherConfigStore.Load(_configPath))
        {
            Entries.Add(new LauncherEntryViewModel(entry, this));
        }
        EnsureCustomOrderContainsEntries();

        TotalCount = Entries.Count;
        SelectedCount = Entries.Count(entry => entry.IsSelected);
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
        var preset = new LauncherPreset
        {
            Id = LauncherPresetIds.GenerateUnique(name, _presetFile.Presets.Select(item => item.Id)),
            Name = name,
            EntryIds = entryIds,
        };

        var presetSnapshot = LauncherPresetFiles.Clone(_presetFile);
        var preferredPresetId = SelectedPreset?.Id;
        _presetFile.Presets.Add(preset);
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

        var preset = _presetFile.Presets.FirstOrDefault(item => item.Id == SelectedPreset.Id);
        if (preset is null)
        {
            StatusText = "当前启动组已不存在，请重新加载";
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
        preset.Name = SelectedPreset.Name;
        preset.EntryIds = entryIds;
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
        if (SelectedPreset.IsLocked || !CanDeletePreset(SelectedPreset.Id))
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

        var preset = _presetFile.Presets.FirstOrDefault(item => item.Id == SelectedPreset.Id);
        if (preset is null)
        {
            StatusText = "当前启动组已不存在，请重新加载";
            return;
        }

        var presetSnapshot = LauncherPresetFiles.Clone(_presetFile);
        var preferredPresetId = SelectedPreset.Id;
        _presetFile.Presets.Remove(preset);
        if (_presetFile.DefaultPresetId == preset.Id)
            _presetFile.DefaultPresetId = _presetFile.Presets.FirstOrDefault()?.Id ?? string.Empty;

        if (!SavePresets())
        {
            RestorePresetFile(presetSnapshot, preferredPresetId);
            return;
        }

        var fallbackPresetId = _presetFile.DefaultPresetId;
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

        var entry = LauncherEntryDrafts.CreateEntry(draft, previousEntry: null);
        var validation = LauncherEntryDrafts.ValidateEntry(entry, originalId: null, Entries.Select(existing => existing.Entry));
        if (validation is not null)
        {
            StatusText = validation;
            return;
        }

        entry.UpdatedAtUtc = NowUtcText();
        Entries.Add(new LauncherEntryViewModel(entry, this));
        _settings.CustomEntryOrder.Add(entry.Id);
        if (!SaveEntryCollectionChanges(savePresets: false, rollbackSnapshot: snapshot))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        StatusText = $"已新增按钮: {entry.Name}（已保存）";
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        var snapshot = CaptureEntryMutationSnapshot();
        var oldId = item.Entry.Id;
        var draft = await RequestEntryDraftAsync(item.Entry).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = $"已取消: 编辑 {item.Name}";
            return;
        }

        var updated = LauncherEntryDrafts.CreateEntry(draft, item.Entry);
        var validation = LauncherEntryDrafts.ValidateEntry(updated, oldId, Entries.Select(existing => existing.Entry));
        if (validation is not null)
        {
            StatusText = validation;
            return;
        }

        item.Entry.Id = updated.Id;
        item.Entry.Name = updated.Name;
        item.Entry.Directory = updated.Directory;
        item.Entry.Command = updated.Command;
        item.Entry.Confirm = updated.Confirm;
        item.SetSelectionFromOwner(updated.Selected);
        item.Entry.UpdatedAtUtc = NowUtcText();
        item.RefreshDisplay();
        if (!string.Equals(oldId, item.Entry.Id, StringComparison.Ordinal))
            LauncherEntryReferences.Rename(oldId, item.Entry.Id, _presetFile.Presets, _settings.CustomEntryOrder);

        if (!SaveEntryCollectionChanges(savePresets: true, rollbackSnapshot: snapshot))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        StatusText = $"已更新按钮: {item.Name}（已保存）";
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

        Entries.Remove(item);
        LauncherEntryReferences.Remove(item.Entry.Id, _presetFile.Presets, _settings.CustomEntryOrder);
        if (!SaveEntryCollectionChanges(savePresets: true, rollbackSnapshot: snapshot))
        {
            RestoreEntryMutationSnapshot(snapshot);
            return;
        }

        StatusText = $"已删除按钮: {item.Name}（已保存）";
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
        Entries.Clear();
        foreach (var entry in snapshot.CloneEntries())
        {
            Entries.Add(new LauncherEntryViewModel(entry, this));
        }

        _settings.CustomEntryOrder = snapshot.CloneCustomEntryOrder();
        ApplyPresetFile(snapshot.ClonePresetFile(), snapshot.SelectedPresetId);
        TotalCount = Entries.Count;
        RefreshSelectionState();
        ApplyFilter();
        RecalculateCardSize();
        RefreshEntryLayoutPresentation();
    }

    private void RestorePersistedEntrySnapshot(LauncherEntryMutationSnapshot? snapshot, bool restorePresets)
    {
        if (snapshot is null)
            return;

        var originalStatus = StatusText;
        var restoreErrors = new List<string>();
        try
        {
            LauncherConfigStore.Save(snapshot.CloneEntries(), _configPath, backup: false);
        }
        catch (Exception ex)
        {
            restoreErrors.Add($"config: {ex.Message}");
        }

        if (restorePresets)
        {
            try
            {
                LauncherPresetStore.Save(snapshot.ClonePresetFile(), _presetPath, backup: false);
            }
            catch (Exception ex)
            {
                restoreErrors.Add($"presets: {ex.Message}");
            }
        }

        StatusText = restoreErrors.Count == 0
            ? originalStatus
            : $"{originalStatus}；回滚磁盘状态失败: {string.Join("；", restoreErrors)}";
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
        var snapshot = new List<string>(_settings.CustomEntryOrder);
        LauncherEntryOrder.MoveBefore(_settings.CustomEntryOrder, draggedEntryId, targetEntryId);
        if (!SaveSettings())
        {
            _settings.CustomEntryOrder = snapshot;
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
        _dragReorderSession = new DragReorderSession(
            draggedEntryId,
            new List<string>(_settings.CustomEntryOrder),
            source);
        if (source is not null)
            source.ShowDragPreviewSource();
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
        if (session is null || !string.Equals(session.DraggedEntryId, draggedEntryId, StringComparison.Ordinal))
        {
            BeginDragReorder(draggedEntryId);
            session = _dragReorderSession;
        }
        if (session is null)
            return false;
        if (string.Equals(draggedEntryId, targetEntryId, StringComparison.Ordinal))
            return false;

        var nextPreview = new DragInsertPreview(targetEntryId, insertAfter);
        if (session.LastPreview == nextPreview)
            return false;

        session.LastPreview = nextPreview;
        EnsureCustomOrderContainsEntries();
        LauncherEntryOrder.MoveRelative(_settings.CustomEntryOrder, draggedEntryId, targetEntryId, insertAfter);
        ApplyFilter();

        var target = Entries.FirstOrDefault(entry => string.Equals(entry.Id, targetEntryId, StringComparison.Ordinal));
        SetDragPreviewTarget(target, insertAfter);
        return true;
    }

    public void ClearDragHover()
    {
        if (_dragReorderSession is null)
            return;

        ClearDragPreviewTarget();
        _dragReorderSession.LastPreview = null;
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
            _settings.CustomEntryOrder = new List<string>(session.Snapshot);
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

        _settings.CustomEntryOrder = new List<string>(session.Snapshot);
        ApplyFilter();
        ClearDragPreviewState();
        StatusText = "已取消: 拖拽排序";
    }

    private void SetDragPreviewTarget(LauncherEntryViewModel? target, bool insertAfter)
    {
        var session = _dragReorderSession;
        if (session is null)
            return;

        if (ReferenceEquals(session.Target, target))
        {
            if (session.Target is not null)
                session.Target.ShowDragPreviewTarget(insertAfter);
            return;
        }

        ClearDragPreviewTarget();
        session.Target = target;
        if (session.Target is not null)
        {
            session.Target.ShowDragPreviewTarget(insertAfter);
        }
    }

    private void ClearDragPreviewTarget()
    {
        var session = _dragReorderSession;
        if (session?.Target is not { } target)
            return;

        target.ClearDragCue();
        session.Target = null;
    }

    private void ClearDragPreviewState()
    {
        var session = _dragReorderSession;
        if (session is null)
            return;

        ClearDragPreviewTarget();
        _dragReorderSession = null;
        if (session.Source is not null)
        {
            session.Source.ClearDragCue();
        }
    }

    private sealed record DragReorderSession(
        string DraggedEntryId,
        List<string> Snapshot,
        LauncherEntryViewModel? Source)
    {
        public LauncherEntryViewModel? Target { get; set; }

        public DragInsertPreview? LastPreview { get; set; }
    }

    private readonly record struct DragInsertPreview(string TargetEntryId, bool InsertAfter);

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

        return CanDeletePreset(SelectedPreset.Id);
    }

    private void ApplyFilter()
    {
        var filter = (FilterText ?? "").Trim();
        VisibleEntries.Clear();
        var filtered = Entries.Where(entry =>
            filter.Length == 0
            || entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || entry.Directory.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || entry.Command.Contains(filter, StringComparison.OrdinalIgnoreCase));

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
        if (SelectedPreset is null)
        {
            var noPresetPreview = LauncherPresetSelectionPreview.NoPreset(PresetCount);
            ApplyPresetButtonText = noPresetPreview.ApplyButtonText;
            PresetSelectionStatusText = noPresetPreview.StatusText;
            return;
        }

        var selection = GetPresetSelectionPreview(SelectedPreset);
        ApplyPresetButtonText = selection.ApplyButtonText;
        PresetSelectionStatusText = selection.StatusText;
    }

    private LauncherPresetSelectionPreview GetPresetSelectionPreview(LauncherPresetViewModel selectedPreset)
    {
        return LauncherPresetSelectionPreview.ForPreset(
            selectedPreset.Name,
            Entries.Where(entry => entry.IsSelected).Select(entry => entry.Entry.Id),
            Entries.Select(entry => entry.Entry.Id),
            selectedPreset.EntryIds);
    }

    private bool CanDeletePreset(string presetId)
    {
        if (_presetFile.Presets.Count == 0)
            return false;

        var target = _presetFile.Presets.FirstOrDefault(preset => preset.Id == presetId);
        if (target is null || target.Locked)
            return false;

        var remainingNonEmpty = _presetFile.Presets
            .Count(preset => preset.Id != presetId && preset.EntryIds.Count > 0);
        return remainingNonEmpty > 0;
    }

    private List<string> GetSelectedEntryIds()
    {
        return Entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToList();
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
        LauncherPresetFile presetFile;
        try
        {
            presetFile = LauncherPresetStore.Load(_presetPath);
        }
        catch (Exception ex)
        {
            ApplyPresetFile(new LauncherPresetFile(), null);
            return $"启动组加载失败: {ex.Message}";
        }

        ApplyPresetFile(presetFile, preferredPresetId);
        return null;
    }

    private void ApplyPresetFile(LauncherPresetFile presetFile, string? preferredPresetId)
    {
        _presetFile = presetFile;
        Presets.Clear();
        foreach (var preset in presetFile.Presets)
        {
            Presets.Add(new LauncherPresetViewModel(preset));
        }

        PresetCount = Presets.Count;
        var targetPresetId = preferredPresetId ?? presetFile.DefaultPresetId;
        SelectedPreset = Presets.FirstOrDefault(preset => preset.Id == targetPresetId)
            ?? Presets.FirstOrDefault();
        NotifyPresetManagementCommands();
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
            StatusText = $"启动组保存失败: {ex.Message}";
            return false;
        }
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
            StatusText = $"配置保存失败: {ex.Message}";
            return false;
        }
    }

    private bool SaveSettings()
    {
        try
        {
            LauncherSettingsStore.Save(_settings, _settingsPath);
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"设置保存失败: {ex.Message}";
            return false;
        }
    }
}
