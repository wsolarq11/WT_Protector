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
        var result = SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _filterText = snapshot.LastFilterText;
                OnPropertyChanged(nameof(FilterText));
                ApplyFilter();
            });
        ApplySettingsChange(result, ApplyFilter);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        OnPropertyChanged(nameof(IsCustomSortMode));
        if (value is null)
            return;

        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.SortMode = value.Id;
        EnsureCustomOrderContainsEntries();
        var result = SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _selectedSortMode = ResolveSortMode(snapshot.SortMode);
                OnPropertyChanged(nameof(SelectedSortMode));
                OnPropertyChanged(nameof(IsCustomSortMode));
                ApplyFilter();
            });
        ApplySettingsChange(
            result,
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
        var result = SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _selectedDisplayMode = ResolveDisplayMode(snapshot.EntryViewMode);
                OnPropertyChanged(nameof(SelectedDisplayMode));
                NotifyDisplayModeProjectionChanged();
                LauncherEntryLayoutPresentation.Refresh(Entries);
            });
        ApplySettingsChange(
            result,
            () => StatusText = $"视图模式: {value.Label}（已保存）");
    }

    partial void OnRequireConfirmationForLaunchesChanged(bool value)
    {
        var snapshot = LauncherSettingsSnapshot.Capture(_settings);
        _settings.RequireConfirmationForLaunches = value;
        RefreshLaunchConfirmationPresentation();
        var result = SaveSettingsChange(
            snapshot,
            snapshot =>
            {
                _requireConfirmationForLaunches = snapshot.RequireConfirmationForLaunches;
                OnPropertyChanged(nameof(RequireConfirmationForLaunches));
                RefreshLaunchConfirmationPresentation();
            });
        ApplySettingsChange(
            result,
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
                    ? "当前没有可勾选的可见项"
                    : "可见项已全部勾选");
            return;
        }

        if (SetSelection(targets, selected: true))
            ApplyStatusText($"已勾选可见 {targets.Count} 项（已保存）");
    }

    [RelayCommand(CanExecute = nameof(CanOperateOnSelection))]
    private void ClearSelection()
    {
        var targets = GetSelectedEntries();
        if (targets.Count == 0)
        {
            ApplyStatusText("当前没有已勾选项");
            return;
        }

        if (SetSelection(targets, selected: false))
            ApplyStatusText($"已清空 {targets.Count} 个勾选项（已保存）");
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        var selectedPreset = SelectedPreset;
        if (selectedPreset is null)
        {
            ApplyStatusText("未选择启动组");
            return;
        }

        var selection = BuildPresetSelectionPreview(GetSelectedEntryIds());
        if (selection.MissingConfigIds.Count > 0)
        {
            ApplyStatusText($"启动组引用了不存在的启动项: {string.Join(", ", selection.MissingConfigIds)}");
            return;
        }

        if (ReplaceSelection(selection.PresetIds))
            ApplyStatusText($"已应用启动组: {selectedPreset.Name}（{selection.PresetIds.Count} 项，已保存）");
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
        var presetName = await RequestNewPresetAsync(entryIds).ConfigureAwait(true);
        if (presetName is null)
        {
            StatusText = "已取消: 保存启动组";
            return;
        }

        var name = presetName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = "启动组名称不能为空";
            return;
        }

        var preferredPresetId = SelectedPreset?.Id;
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.AddPreset(_presetFile, name, entryIds);
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(preset.Id);
        ApplyStatusText($"已保存启动组: {preset.Name}（{preset.EntryIds.Count} 项）");
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }
        if (!LauncherPresetWorkflow.CanUpdatePreset(_presetFile, SelectedPreset.Id))
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
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.UpdatePreset(_presetFile, SelectedPreset.Id, SelectedPreset.Name, entryIds);
        if (preset is null)
        {
            ApplyPresetFileState(snapshot, preferredPresetId);
            ApplyStatusText("当前启动组已不存在，请重新加载");
            return;
        }
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(preset.Id);
        ApplyStatusText($"已更新启动组: {preset.Name}（{preset.EntryIds.Count} 项）");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }
        if (!LauncherPresetWorkflow.CanDeletePreset(_presetFile, SelectedPreset.Id))
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
        var snapshot = LauncherPresetFiles.Clone(_presetFile);
        var preset = LauncherPresetWorkflow.DeletePreset(_presetFile, SelectedPreset.Id, out var fallbackPresetId);
        if (preset is null)
        {
            ApplyPresetFileState(snapshot, preferredPresetId);
            ApplyStatusText("当前启动组已不存在，请重新加载");
            return;
        }
        if (!SavePresetMutationOrRestore(snapshot, preferredPresetId))
            return;

        LoadPresetFile(fallbackPresetId);
        ApplyStatusText($"已删除启动组: {preset.Name}");
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

        ApplyStatusText($"已新增按钮: {entry.Name}（已保存）");
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

        var oldId = item.Id;
        var updated = CreateEntryFromDraftOrReportValidation(draft, oldId, item.Entry);
        if (updated is null)
            return;

        item.ReplaceEntryFromOwner(updated, CurrentTimestampUtc());
        if (!string.Equals(oldId, item.Id, StringComparison.Ordinal))
            LauncherEntryReferences.RenameEntryId(_presetFile, _settings.CustomEntryOrder, oldId, item.Id);

        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText($"已更新按钮: {item.Name}（已保存）");
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

        var entryName = item.Name;
        Entries.Remove(item);
        LauncherEntryReferences.RemoveEntryId(_presetFile, _settings.CustomEntryOrder, item.Id);
        if (!SaveEntryCollectionChanges(savePresets: true, snapshot))
            return;

        ApplyStatusText($"已删除按钮: {entryName}（已保存）");
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
            StatusText = "未选择任何启动项";
            return;
        }

        try
        {
            if (RequireConfirmationForLaunches)
            {
                var accepted = await ConfirmBatchLaunchAsync(selected.Select(ToLaunchEntry).ToList())
                    .ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = $"已取消: 批量启动 {selected.Count} 项";
                    return;
                }
            }

            var ok = 0;
            var fail = 0;
            var failures = new List<string>();
            foreach (var item in selected)
            {
                try
                {
                    var plan = BuildLaunchPlan(item);
                    DryRunText = plan.CommandLine;
                    _launcherProcess.Start(plan);
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    failures.Add($"{item.Name}: {ex.Message}");
                }
            }

            StatusText = fail == 0
                ? $"已启动 {ok} 个勾选项"
                : $"已启动 {ok} 个勾选项，{fail} 个失败：{FormatLaunchFailures(failures)}";
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
            var plan = BuildLaunchPlan(item);
            DryRunText = plan.CommandLine;
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
            var plan = BuildLaunchPlan(item);
            dryRunText = plan.CommandLine;
            if (plan.RequiresConfirmation)
            {
                var accepted = await ConfirmLaunchAsync(item.Entry, plan).ConfigureAwait(true);
                if (!accepted)
                {
                    DryRunText = dryRunText;
                    StatusText = $"已取消: {item.Name}";
                    return;
                }
            }

            _launcherProcess.Start(plan);
            DryRunText = dryRunText;
            StatusText = $"已启动: {item.Name}";
        }
        catch (Exception ex)
        {
            if (dryRunText is not null)
                DryRunText = dryRunText;
            StatusText = $"启动失败: {item.Name} — {ex.Message}";
        }
    }

    private LaunchPlan BuildLaunchPlan(LauncherEntryViewModel item) =>
        LaunchArgumentBuilder.Build(
            item.Entry,
            validateDirectory: true,
            requireConfirmation: RequireConfirmationForLaunches);

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
            error = $"启动组加载失败: {ex.Message}";
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
        SelectedPreset = Presets.FirstOrDefault(preset => preset.Id == targetPresetId)
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
            ApplyStatusText($"启动组保存失败: {ex.Message}");
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
            ApplyStatusText($"配置保存失败: {ex.Message}");
            return false;
        }
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.Entry;

    private static string CurrentTimestampUtc() => DateTimeOffset.UtcNow.ToString("O");

    private string? SaveSettingsChange(
        LauncherSettingsSnapshot snapshot,
        Action<LauncherSettingsSnapshot> onRollback) =>
        LauncherSettingsMutationPersistence.Save(
            _settings,
            _settingsPath,
            snapshot,
            () => onRollback(snapshot));

    private void ApplySettingsChange(string? saveFailure, Action onSuccess)
    {
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return;
        }

        onSuccess();
    }

    private static string FormatLaunchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += $"；…另有 {failures.Count - 3} 项";

        return preview;
    }

}
