# 架构改革第2步: Fluxor状态集中化 — 技术设计

## 当前架构 (重构前)

```
MainWindowViewModel
  ├── _appStore (AppStore) — 读写 launcher.json 持久化
  ├── _settings (LauncherSettings) — 直接内存字段
  ├── _list (EntryListViewModel) — entry 列表管理
  │   ├── ReplaceEntries(entries) — 无事件替换
  │   └── MutableVisibleEntries — ObservableCollection
  ├── LayoutManager — 排序/筛选/视图模式控制
  │   ├── _lastFilterText, _sortMode, _entryViewMode — 字段
  │   └── OnPropertyChanged — 手工通知
  └── Layout.cs 中的 SyncVisibleEntries
      ├── _syncSortedSet — HashSet 增量维护
      └── _syncPositionByEntry — Dictionary 增量维护
```

## 目标架构 (重构后)

```
MainWindowViewModel
  ├── _appStore (AppStore) — 仅持久化（写磁盘+回滚）
  ├── IState<LayoutState> — 读取 entries/sort/filter/viewMode
  ├── IDispatcher — Dispatch Action 修改状态
  ├── _settings (LauncherSettings) — 保持直接字段（settings 不是 UI 状态）
  ├── _list (EntryListViewModel) — 保持（列表管理逻辑不变）
  │   └── ReplaceAllEntries(entries) — 唯一路径（触发事件）
  └── Layout.cs 中的 RefreshVisibleEntries
      └── 全量重建，无 _syncSortedSet/_syncPositionByEntry
```

### 数据流

```
User Action → Command → Dispatch(SetEntriesAction)
                                      ↓
                              LayoutReducers.ReduceSetEntries
                                      ↓
                              LayoutState (不可变 record)
                                      ↓
                              VM 通过 IState<LayoutState> 读取
                                      ↓
                              RefreshVisibleEntries (全量重建)
```

### 持久化流（不变）

```
VM → _appStore.Dispatch(reducer) → LauncherStore.Save → 通知 subscribers
```

## 关键设计决策

### 决策 1: Fluxor State vs AppStore 分工

| | Fluxor LayoutState | AppStore |
|---|---|---|
| 职责 | UI 状态（entries, filter, sort, viewMode） | 持久化 launcher.json |
| 数据源 | VM Dispatch → Reducer 产生 | LauncherStore.Load from disk |
| 写路径 | Dispatch(Action) → Reducer | Mutate/Dispatch/reducer |
| 读路径 | IState<LayoutState>.Value | Select/SelectAsync |
| 生命周期 | ViewModel 关联 | 应用级单例 |

**结论**: 两者共存。所有 UI 状态的变更先走 Fluxor Dispatch，Fluxor Effect 触发 AppStore 持久化。

### 决策 2: SyncVisibleEntries 简化为全量重建

当前增量算法（_syncSortedSet + _syncPositionByEntry）仅在 <200 条数据下有效。全量重建直接在 MutableVisibleEntries 上 Clear + AddRange，代码量约 10 行（vs 当前 60 行）。Avalonia 的 ItemContainerGenerator 在此数据量下足以处理 CollectionChanged 事件。

### 决策 3: Fluxor 注册位置

在 `Program.cs` 或 `App.axaml.cs` 中 `builder.Services.AddFluxor(config => config.ScanAssemblies(...))`。

## 变更文件清单

| 文件 | 变更 |
|------|------|
| `WTLauncher.App/App.axaml.cs` 或 `Program.cs` | 添加 AddFluxor 注册 |
| `ViewModels/MainWindowViewModel.cs` | 注入 IState<LayoutState>, IDispatcher；替换状态读取 |
| `ViewModels/MainWindowViewModel.Layout.cs` | 替换 DirectField 读写；简化 SyncVisibleEntries |
| `ViewModels/MainWindowViewModel.Drag.cs` | 拖拽操作改走 Dispatch |
| `ViewModels/LayoutManager.cs` | 可选：布局状态改走 IState<LayoutState> |
| `Features/Entries/EntryListViewModel.cs` | 统一为 ReplaceAllEntries 单一路径 |
| `Features/LayoutState.cs` | 可能需扩展字段 |
| `Features/LayoutActions.cs` | 可能需新增 Action |
| `Features/LayoutReducers.cs` | 可能需新增 Reducer |
| `Infrastructure/Store/LauncherEffects.cs` | 新增 Effect：LayoutState 变更时自动持久化 |

## 兼容性

- XAML 绑定: 零变更（VM 公共属性签名不变）
- 测试: EntryListViewModel 的 ReplaceEntries 改为 ReplaceAllEntries 需确认调用路径
- AppStore: 不变，仍处理持久化+回滚
