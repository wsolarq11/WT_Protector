# P1: 状态单源化 — 设计文档

## 问题

`_settings` 字段在三个位置存在副本：`MainWindowViewModel._settings`、`LauncherDragReorderWorkflow._settings`、`AppStore._state.Settings`。三个副本各自写入，无仲裁机制，导致 7/8 个 HIGH/CRITICAL 缺陷。

## 设计方案

### 1. 消除 MWVM._settings 字段

**当前**: `private LauncherSettings _settings;`（字段，被 20+ 处的 `_settings = ...` 赋值修改）

**目标**: MWVM 不持有 `_settings` 字段，所有读取通过 `_appStore.Select(s => s.Settings)`

**迁移策略**:
- 将 `_settings` 改为计算属性 `CurrentSettings => _appStore.Select(s => s.Settings)`
- 所有 `_settings.XXX` 读取替换为 `CurrentSettings.XXX`
- 所有 `_settings = _settings with { ... }` 替换为通过 `Dispatch` 写入
- `SaveSettingsToStore` 直接传入新值，不依赖字段

### 2. 消除 DragWorkflow._settings 字段

**当前**: `LauncherDragReorderWorkflow` 持有 `_settings` 副本，构造时快照，永不更新

**目标**: Workflow 读写全部通过 `AppStore.Dispatch` 和 `AppStore.Select`

**迁移策略**:
- Workflow 构造函数改为接收 `AppStore` 引用（而不是 `LauncherSettings` 值）
- `Preview` 通过 `Dispatch` 写入 CustomEntryOrder
- 删除 MWVM.Drag.cs 中 5 处的 `_settings = _dragReorderWorkflow.Settings` 同步代码
- 快照由 AppStore 的 Dispatch 回滚机制提供，不再需要 workflow 自己的字段

### 3. TrySaveWindowBounds → AppStore.Dispatch

**当前**: 直接调 `LauncherStore.Save(nextStore, _storePath)`

**目标**: 通过 `AppStore.Dispatch` 写入，保持状态一致

**迁移**: 改为 `_appStore.Dispatch(state => state with { Settings = nextSettings })`

### 4. 删除 Subscription 覆盖

**当前**: `_appStore.Subscribe(state => { _settings = state.Settings; ... })` 无条件覆盖

**目标**: 删除 `_settings = state.Settings` 行——VM 不再有 `_settings` 字段可覆盖。Preset 加载逻辑保留（不依赖于 `_settings`）
