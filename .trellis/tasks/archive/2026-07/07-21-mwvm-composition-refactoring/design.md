# Design: MWVM 组合重构

## 目标架构

```
MainWindowViewModel (orchestrator ~500 行)
  ├── StatusPresenter      — 状态文本/干运行/预设选择/按钮文字 + 计数投影
  ├── PresetManager        — 预设 CRUD + _presetFile + _presets + _selectedPreset
  ├── LayoutManager        — 排序/过滤/视图模式/分组/卡片尺寸 + 滚动偏移
  ├── EntryManager         — entry CRUD + _entries + _visibleEntries + 选择 + EntryListBridge 消除
  ├── LaunchOrchestrator   — 单启动/批启动/干运行（纯编排，不持有状态）
  └── LauncherDragReorderWorkflow (已有，不改)
```

## Manager 间通信

采用事件拓扑（Event Topology），不是方法串联：

```
// orchestrator 声明式事件注册
_entryManager.EntriesChanged += () => _layoutManager.Rebuild();
_layoutManager.FilterChanged += () => _entryManager.ApplyFilter(_layoutManager.FilterCriteria);
_entryManager.CountChanged += (total, visible) => _statusPresenter.RefreshCount(total, visible);
```

每个 Manager 公开 `event Action<...>` 或 `event EventHandler<...>`。Orchestrator 做事件注册拓扑，不持有任何 Manager 的 mutable 引用。

## 每个 Manager 的契约

### StatusPresenter

```csharp
// 失败半径: Zero — status 更新失败不影响程序功能
public sealed class StatusPresenter : ObservableObject
{
    // 持有: _statusContent, _dryRunText, _presetSelectionStatusText, _applyPresetButtonText
    // 投影: StatusText, StatusTone, StatusIndicator, IsStatusNeutral/Success/Warning/Error
    
    public void SetStatus(LauncherStatusContent content);
    public void SetDryRun(string text);
    public void SetPresetSelection(string statusText, string applyButtonText);
    public void RefreshCount(int total, int visible, int selected, int presets);
    
    // 无事件输出 — StatusPresenter 是纯消费者
}
```

### PresetManager

```csharp
// 失败半径: Large — 持久化失败必须抛异常
public sealed class PresetManager
{
    // 持有: _presetFile, _presets (ObservableCollection), _selectedPreset
    // 公开: Presets (ReadOnlyObservableCollection), SelectedPreset, PresetCount
    
    public event Action? PresetsChanged;
    public event Action? SelectedPresetChanged;
    
    // 构造注入: IPresetService, Func<string> saveStoreFile
    public PresetManager(IPresetService presetService, Func<string?> saveStoreFile);
    
    public void Load(LauncherPresetFile presetFile, string? preferredPresetId);
    public string? ApplyPreset(string presetId);
    public int GetPresetReferenceCount(string entryId);
    public LauncherPresetSelectionPreview BuildSelectionPreview(...);
    // 所有 preset CRUD: Add/Rename/Delete/Update + save
}
```

### LayoutManager

```csharp
// 失败半径: Medium — 无效参数抛 InvalidOperationException
public sealed class LayoutManager : ObservableObject
{
    // 持有: _filterText, _selectedSortMode, _selectedDisplayMode, _cardsGroupKey
    //        _cardsAvailableSize, _scrollOffsets, _sortModes, _displayModes
    // 不持有 entry 数据
    
    public event Action? SortChanged;
    public event Action? FilterChanged;
    public event Action? ViewModeChanged;
    public event Action? GroupKeyChanged;
    
    // 构造注入: 无（纯内存状态操作）
    public LayoutManager();
    
    public LauncherEntryQuery BuildQuery(); // 返回当前 filter/sort/group 组合
    public void LoadSettings(LauncherSettings settings);
}
```

### EntryManager

```csharp
// 失败半径: Large — 持久化失败必须抛异常
public sealed class EntryManager
{
    // 持有: _entries, _visibleEntries, _keyboardFocusedEntryId, _entryListViewModel
    // 公开: Entries, VisibleEntries, TotalCount, VisibleCount, SelectedCount
    
    public event Action? EntriesChanged;
    public event Action? CountChanged;
    public event Action<SelectionChangedArgs>? SelectionChanged;
    public event Func<LauncherEntryQuery, IReadOnlyList<LauncherEntryViewModel>>? BuildVisibleEntries;
    
    // 构造注入: IEntryService, IEntryCardHost (this), Func<LauncherStoreFile> buildStoreFile
    
    public void ReplaceEntries(IEnumerable<LaunchEntry> entries);
    public void ApplyFilter(LauncherEntryQuery query);
    public void SetKeyboardFocusedEntryId(string? id);
    // Entry CRUD: Add, Edit, Delete, Clone delegated through EntryListViewModel
    // Selection: UpdateSelection, SetSelection, ReplaceSelection, ClearSelection
    // Count: RefreshSelectionState 更新计数
}
```

### LaunchOrchestrator

```csharp
// 失败半径: Large — 启动失败必须上报 UX
public sealed class LaunchOrchestrator
{
    // 不持有状态，纯编排
    // 构造注入: LauncherLaunchWorkflow, IDialogService
    
    public Task<LaunchResult> LaunchAsync(LaunchEntry entry, bool requireConfirmation);
    public Task<BatchLaunchResult> LaunchBatchAsync(...);
    public DryRunResult BuildDryRun(LaunchEntry entry, bool requireConfirmation);
}
```

### Orchestrator (MainWindowViewModel)

```csharp
public sealed partial class MainWindowViewModel : ObservableObject
{
    // 持有: _storePath, _launchWorkflow, _dialogService, _utcNow, _settings
    // 持有: 6 个 Manager 的 readonly 引用
    // 不持有: _entries, _presets, _statusContent 等（在 Manager 里）
    
    // 构造: 创建 6 个 Manager → 注册事件拓扑 → 初始化数据
    // 公开: RelayCommand → 分派到对应的 Manager
    // 持久化: BuildStoreFile 委托给 Manager 收集状态
}
```

## 实现顺序

每个阶段独立提交，门禁全绿后进入下一阶段。

1. **StatusPresenter** — 无依赖，纯提取（验证事件拓扑模式可行）
2. **PresetManager** — 依赖 StatusPresenter（通过事件更新 status）
3. **LayoutManager** — 依赖 StatusPresenter, EntryManager（通过回调）
4. **EntryManager + 拆 Bridge** — 依赖 LayoutManager, StatusPresenter（最大改动）
5. **LaunchOrchestrator** — 依赖 EntryManager（读取 entries）
6. **Orchestrator 组装 + Partial 删除** — 依赖全部 5 个 Manager
