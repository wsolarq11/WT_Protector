using Avalonia;
using System.Collections.ObjectModel;
using System.Text;
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
        _settings.LastFilterText = value ?? string.Empty;
        SaveSettings();
        ApplyFilter();
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        OnPropertyChanged(nameof(IsCustomSortMode));
        if (value is null)
            return;

        _settings.SortMode = value.Id;
        EnsureCustomOrderContainsEntries();
        SaveSettings();
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

        _settings.EntryViewMode = value.Id;
        RefreshEntryLayoutPresentation();
        SaveSettings();
        StatusText = $"视图模式: {value.Label}（已保存）";
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        _settings.RequireConfirmationForLaunches = value;
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        RefreshEntryLayoutPresentation();
        ApplyFilter();
        SaveSettings();
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

        var selection = GetPresetSelectionSnapshot(SelectedPreset);
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
            Id = GenerateUniquePresetId(name),
            Name = name,
            EntryIds = entryIds,
        };

        _presetFile.Presets.Add(preset);
        if (!SavePresets())
            return;

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

        preset.Name = SelectedPreset.Name;
        preset.EntryIds = entryIds;
        if (!SavePresets())
            return;

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

        _presetFile.Presets.Remove(preset);
        if (_presetFile.DefaultPresetId == preset.Id)
            _presetFile.DefaultPresetId = _presetFile.Presets.FirstOrDefault()?.Id ?? string.Empty;

        if (!SavePresets())
            return;

        var fallbackPresetId = _presetFile.DefaultPresetId;
        LoadPresets(fallbackPresetId);
        StatusText = $"已删除启动组: {preset.Name}";
    }

    [RelayCommand]
    private async Task AddEntryAsync()
    {
        var draft = await RequestEntryDraftAsync(null).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = "已取消: 新增按钮";
            return;
        }

        var entry = CreateEntryFromDraft(draft, previousEntry: null);
        var validation = ValidateEntry(entry, originalId: null);
        if (validation is not null)
        {
            StatusText = validation;
            return;
        }

        entry.UpdatedAtUtc = NowUtcText();
        Entries.Add(new LauncherEntryViewModel(entry, this));
        _settings.CustomEntryOrder.Add(entry.Id);
        if (!SaveEntryCollectionChanges(savePresets: false))
            return;

        StatusText = $"已新增按钮: {entry.Name}（已保存）";
    }

    internal async Task EditEntryAsync(LauncherEntryViewModel item)
    {
        var oldId = item.Entry.Id;
        var draft = await RequestEntryDraftAsync(item.Entry).ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = $"已取消: 编辑 {item.Name}";
            return;
        }

        var updated = CreateEntryFromDraft(draft, item.Entry);
        var validation = ValidateEntry(updated, oldId);
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
            RenameEntryReferences(oldId, item.Entry.Id);

        if (!SaveEntryCollectionChanges(savePresets: true))
            return;

        StatusText = $"已更新按钮: {item.Name}（已保存）";
    }

    internal async Task DeleteEntryAsync(LauncherEntryViewModel item)
    {
        var accepted = await ConfirmDeleteEntryAsync(item).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = $"已取消: 删除 {item.Name}";
            return;
        }

        Entries.Remove(item);
        RemoveEntryReferences(item.Entry.Id);
        if (!SaveEntryCollectionChanges(savePresets: true))
            return;

        StatusText = $"已删除按钮: {item.Name}（已保存）";
    }

    private bool SaveEntryCollectionChanges(bool savePresets)
    {
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        TotalCount = Entries.Count;
        if (!SaveConfig())
            return false;
        if (savePresets && !SavePresets())
            return false;

        EnsureCustomOrderContainsEntries();
        SaveSettings();
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        ApplyFilter();
        return true;
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
        MoveEntryBeforeInternal(draggedEntryId, targetEntryId);
        SaveSettings();
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
        MoveEntryRelativeInternal(draggedEntryId, targetEntryId, insertAfter);
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

        SaveSettings();
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

    private void MoveEntryBeforeInternal(string draggedEntryId, string targetEntryId)
    {
        var order = _settings.CustomEntryOrder;
        order.RemoveAll(id => string.Equals(id, draggedEntryId, StringComparison.Ordinal));
        var targetIndex = order.FindIndex(id => string.Equals(id, targetEntryId, StringComparison.Ordinal));
        if (targetIndex < 0)
            targetIndex = order.Count;
        order.Insert(targetIndex, draggedEntryId);
    }

    private void MoveEntryRelativeInternal(string draggedEntryId, string targetEntryId, bool insertAfter)
    {
        var order = _settings.CustomEntryOrder;
        order.RemoveAll(id => string.Equals(id, draggedEntryId, StringComparison.Ordinal));
        var targetIndex = order.FindIndex(id => string.Equals(id, targetEntryId, StringComparison.Ordinal));
        if (targetIndex < 0)
            targetIndex = order.Count;
        if (insertAfter)
            targetIndex++;
        targetIndex = Math.Min(targetIndex, order.Count);
        order.Insert(targetIndex, draggedEntryId);
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
        var selected = Entries.Where(entry => entry.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "未选择任何启动项";
            return;
        }

        try
        {
            if (RequireConfirmationForLaunches)
            {
                var accepted = await ConfirmBatchLaunchAsync(selected.Select(entry => entry.Entry).ToList()).ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = $"已取消: 批量启动 {selected.Count} 项";
                    return;
                }
            }

            var ok = 0;
            var fail = 0;
            foreach (var item in selected)
            {
                try
                {
                    var plan = BuildLaunchPlan(item.Entry);
                    DryRunText = $"{plan.Executable} {plan.Arguments}";
                    _launcherProcess.Start(plan);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }

            StatusText = fail == 0
                ? $"已启动 {ok} 个勾选项"
                : $"已启动 {ok} 个勾选项，{fail} 个失败";
        }
        catch (Exception ex)
        {
            StatusText = $"批量启动失败: {ex.Message}";
        }
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        try
        {
            var plan = BuildLaunchPlan(item.Entry);
            DryRunText = $"{plan.Executable} {plan.Arguments}";
            StatusText = plan.RequiresConfirmation
                ? $"预演: {item.Name}（需要确认）"
                : $"预演: {item.Name}";
        }
        catch (Exception ex)
        {
            DryRunText = ex.Message;
            StatusText = $"预演失败: {item.Name}";
        }
    }

    private LaunchPlan BuildLaunchPlan(LaunchEntry entry, bool validateDirectory = true)
    {
        return LaunchArgumentBuilder.Build(entry, validateDirectory, RequireConfirmationForLaunches);
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        item.Entry.Selected = selected;
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        SaveConfig();
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        ApplyPresetCommand.NotifyCanExecuteChanged();
        UpdatePresetSelectionStatus();
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        try
        {
            var plan = BuildLaunchPlan(item.Entry);
            DryRunText = $"{plan.Executable} {plan.Arguments}";
            if (plan.RequiresConfirmation)
            {
                var accepted = await ConfirmLaunchAsync(item.Entry, plan).ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = $"已取消: {item.Name}";
                    return;
                }
            }

            _launcherProcess.Start(plan);
            StatusText = $"已启动: {item.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"启动失败: {item.Name} — {ex.Message}";
        }
    }

    private bool CanOperateOnSelection()
    {
        return SelectedCount > 0;
    }

    private bool CanApplyPreset()
    {
        return SelectedPreset is not null
            && Entries.Count > 0
            && GetPresetSelectionSnapshot(SelectedPreset!).HasChanges;
    }

    private bool CanUpdateSelectedPreset()
    {
        return SelectedPreset is not null
            && !SelectedPreset.IsLocked
            && SelectedCount > 0
            && GetPresetSelectionSnapshot(SelectedPreset!).HasChanges;
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
        return SelectedSortMode?.Id switch
        {
            "updated" => entries
                .OrderByDescending(entry => ParseUpdatedAt(entry.Entry.UpdatedAtUtc))
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Id, StringComparer.Ordinal),
            "alphabetical" => entries
                .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Id, StringComparer.Ordinal),
            _ => SortEntriesByCustomOrder(entries),
        };
    }

    private IEnumerable<LauncherEntryViewModel> SortEntriesByCustomOrder(IEnumerable<LauncherEntryViewModel> entries)
    {
        EnsureCustomOrderContainsEntries();
        var order = _settings.CustomEntryOrder
            .Select((id, index) => new { id, index })
            .GroupBy(item => item.id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.Ordinal);
        return entries
            .OrderBy(entry => order.TryGetValue(entry.Id, out var index) ? index : int.MaxValue)
            .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal);
    }

    private void EnsureCustomOrderContainsEntries()
    {
        var knownIds = Entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        _settings.CustomEntryOrder.RemoveAll(id => !knownIds.Contains(id));
        foreach (var id in Entries.Select(entry => entry.Id))
        {
            if (!_settings.CustomEntryOrder.Contains(id, StringComparer.Ordinal))
                _settings.CustomEntryOrder.Add(id);
        }
    }

    private static DateTimeOffset ParseUpdatedAt(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
    }

    private static string NowUtcText()
    {
        return DateTimeOffset.UtcNow.ToString("O");
    }

    private LaunchEntry CreateEntryFromDraft(EntryDraft draft, LaunchEntry? previousEntry)
    {
        return new LaunchEntry
        {
            Id = NormalizeEntryId(draft.Id),
            Name = (draft.Name ?? string.Empty).Trim(),
            Directory = (draft.Directory ?? string.Empty).Trim(),
            Command = (draft.Command ?? string.Empty).Trim(),
            Confirm = false,
            Selected = draft.Selected,
            UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty,
        };
    }

    private string? ValidateEntry(LaunchEntry entry, string? originalId)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
            return "按钮 ID 不能为空";
        if (string.IsNullOrWhiteSpace(entry.Name))
            return "按钮名称不能为空";
        if (string.IsNullOrWhiteSpace(entry.Directory))
            return "按钮目录不能为空";
        if (string.IsNullOrWhiteSpace(entry.Command))
            return "按钮命令不能为空";
        if (Entries.Any(existing =>
                !string.Equals(existing.Id, originalId, StringComparison.Ordinal)
                && string.Equals(existing.Id, entry.Id, StringComparison.Ordinal)))
            return $"按钮 ID 已存在: {entry.Id}";

        return null;
    }

    private static string NormalizeEntryId(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    private void RenameEntryReferences(string oldId, string newId)
    {
        foreach (var preset in _presetFile.Presets)
        {
            for (var i = 0; i < preset.EntryIds.Count; i++)
            {
                if (string.Equals(preset.EntryIds[i], oldId, StringComparison.Ordinal))
                    preset.EntryIds[i] = newId;
            }
        }

        for (var i = 0; i < _settings.CustomEntryOrder.Count; i++)
        {
            if (string.Equals(_settings.CustomEntryOrder[i], oldId, StringComparison.Ordinal))
                _settings.CustomEntryOrder[i] = newId;
        }
    }

    private void RemoveEntryReferences(string entryId)
    {
        foreach (var preset in _presetFile.Presets)
        {
            preset.EntryIds.RemoveAll(id => string.Equals(id, entryId, StringComparison.Ordinal));
        }

        _settings.CustomEntryOrder.RemoveAll(id => string.Equals(id, entryId, StringComparison.Ordinal));
    }

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        foreach (var entry in entries)
        {
            entry.SetSelectionFromOwner(selected);
        }

        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        return SaveConfig();
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        foreach (var entry in Entries)
        {
            entry.SetSelectionFromOwner(selectedIds.Contains(entry.Entry.Id));
        }

        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        return SaveConfig();
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
            ApplyPresetButtonText = "应用启动组";
            PresetSelectionStatusText = PresetCount == 0
                ? "暂无启动组；可以继续手动勾选。"
                : "请选择一个启动组查看差异。";
            return;
        }

        var selection = GetPresetSelectionSnapshot(SelectedPreset);
        var presetIds = selection.PresetIds;
        var missingConfigIds = selection.MissingConfigIds;
        if (missingConfigIds.Count > 0)
        {
            ApplyPresetButtonText = "检查启动组";
            PresetSelectionStatusText = $"启动组“{SelectedPreset.Name}”引用了 {missingConfigIds.Count} 个不存在的启动项；请先修 config\\presets.json。";
            return;
        }

        var matched = selection.MatchedCount;
        var missing = selection.PresetIds.Count - matched;
        var extra = selection.SelectedIds.Count - matched;

        if (presetIds.Count == 0)
        {
            ApplyPresetButtonText = extra == 0 ? "手动模式" : "清空勾选";
            PresetSelectionStatusText = extra == 0
                ? $"已是“{SelectedPreset.Name}”：当前没有勾选项。"
                : $"“{SelectedPreset.Name}”会清空当前 {extra} 个勾选项。";
            return;
        }

        ApplyPresetButtonText = missing == 0 && extra == 0 ? "已匹配" : "应用启动组";
        PresetSelectionStatusText = missing == 0 && extra == 0
            ? $"已匹配启动组“{SelectedPreset.Name}”：{matched}/{presetIds.Count} 项。"
            : $"启动组“{SelectedPreset.Name}”：已匹配 {matched}/{presetIds.Count}，缺 {missing}，额外 {extra}；应用会替换当前勾选。";
    }

    private PresetSelectionSnapshot GetPresetSelectionSnapshot(LauncherPresetViewModel selectedPreset)
    {
        var selectedIds = Entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToHashSet(StringComparer.Ordinal);
        var knownIds = Entries.Select(entry => entry.Entry.Id).ToHashSet(StringComparer.Ordinal);
        var presetIds = selectedPreset.EntryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var missingConfigIds = presetIds.Where(id => !knownIds.Contains(id)).ToList();

        return new PresetSelectionSnapshot(selectedIds, presetIds, missingConfigIds);
    }

    private sealed record PresetSelectionSnapshot(
        HashSet<string> SelectedIds,
        HashSet<string> PresetIds,
        List<string> MissingConfigIds)
    {
        public bool HasChanges => !SelectedIds.SetEquals(PresetIds);

        public int MatchedCount => SelectedIds.Count(PresetIds.Contains);
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

    private string GenerateUniquePresetId(string name)
    {
        var baseId = NormalizePresetId(name);
        var candidate = baseId;
        var index = 2;
        var existing = _presetFile.Presets.Select(preset => preset.Id).ToHashSet(StringComparer.Ordinal);
        while (existing.Contains(candidate))
        {
            candidate = $"{baseId}-{index++}";
        }

        return candidate;
    }

    private static string NormalizePresetId(string value)
    {
        var builder = new StringBuilder(value.Length);
        var separatorPending = false;
        foreach (var ch in value.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                separatorPending = false;
            }
            else if (ch is '-' or '_')
            {
                builder.Append(ch);
                separatorPending = false;
            }
            else if (char.IsWhiteSpace(ch) || ch is '/' or '\\' or '.' or ':')
            {
                if (!separatorPending && builder.Length > 0)
                {
                    builder.Append('-');
                    separatorPending = true;
                }
            }
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "preset" : normalized;
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }

    private string? LoadPresets(string? preferredPresetId = null)
    {
        Presets.Clear();
        LauncherPresetFile presetFile;
        try
        {
            presetFile = LauncherPresetStore.Load(_presetPath);
            _presetFile = presetFile;
        }
        catch (Exception ex)
        {
            PresetCount = 0;
            SelectedPreset = null;
            return $"启动组加载失败: {ex.Message}";
        }

        foreach (var preset in presetFile.Presets)
        {
            Presets.Add(new LauncherPresetViewModel(preset));
        }

        PresetCount = Presets.Count;
        var targetPresetId = preferredPresetId ?? presetFile.DefaultPresetId;
        SelectedPreset = Presets.FirstOrDefault(preset => preset.Id == targetPresetId)
            ?? Presets.FirstOrDefault();
        NotifyPresetManagementCommands();
        return null;
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

    private void SaveSettings()
    {
        try
        {
            LauncherSettingsStore.Save(_settings, _settingsPath);
        }
        catch (Exception ex)
        {
            StatusText = $"设置保存失败: {ex.Message}";
        }
    }
}
