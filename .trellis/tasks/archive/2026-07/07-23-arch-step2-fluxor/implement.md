# 架构改革第2步: Fluxor状态集中化 — 执行计划

## 执行顺序

### Step 1: Fluxor DI 注册

**文件**: `WTLauncher.App/Program.cs` 或 `App.axaml.cs`

```csharp
// 在 builder.Services 中添加
services.AddFluxor(config => config.ScanAssemblies(typeof(LayoutState).Assembly));
```

**验证**: 应用启动无异常，Fluxor 初始化成功。

---

### Step 2: 简化 SyncVisibleEntries 为全量重建

**文件**: `ViewModels/MainWindowViewModel.Layout.cs`

- 删除 `_syncSortedSet` 和 `_syncPositionByEntry` 字段
- `SyncVisibleEntries` 改为:
  1. `_list.MutableVisibleEntries.Clear()`
  2. `foreach (var entry in sorted) _list.MutableVisibleEntries.Add(entry)`
- 删除增量位置更新逻辑（~40 行 → ~5 行）

**验证**: `dotnet test` 696 全绿。

---

### Step 3: LayoutState 接入 MainWindowViewModel

**文件**: `ViewModels/MainWindowViewModel.cs` + `Layout.cs`

- 添加 `IState<LayoutState>` 和 `IDispatcher` 依赖注入
- VM 状态读取:
  - `_sortMode` → `_layoutState.Value.SortMode`
  - `_entryViewMode` → `_layoutState.Value.EntryViewMode`
  - `_lastFilterText` → `_layoutState.Value.FilterText`
- VM 状态写入:
  - 设置 sortMode → `Dispatcher.Dispatch(new SetSortModeAction(...))`
  - 设置 viewMode → `Dispatcher.Dispatch(new SetViewModeAction(...))`
  - 设置 filter → `Dispatcher.Dispatch(new SetFilterAction(...))`
- LayoutManager 读取也改走 `IState<LayoutState>`

**验证**: `dotnet test` 696 全绿，应用功能正常。

---

### Step 4: 统一 ReplaceEntries/ReplaceAllEntries 路径

**文件**: `Features/Entries/EntryListViewModel.cs` + `ViewModels/MainWindowViewModel.cs`

- `ReplaceEntries` 方法改为调用 `ReplaceAllEntries`（指向同一实现）
- 或直接删除 `ReplaceEntries`，所有调用点改为 `ReplaceAllEntries`
- 检查所有 `_list.ReplaceEntries(...)` 调用点（当前 4 处）

**验证**: `dotnet test` 696 全绿。

---

### Step 5: Fluxor Effect 持久化桥接

**文件**: `Infrastructure/Store/LauncherEffects.cs`

- 新增 Effect: 监听 LayoutState 变更，触发 AppStore 持久化
- 或保持当前持久化路径不变（VM 在 Dispatch 后显式调用 AppStore）

**决策**: 如果 Step 4 完成后 VM 在 Dispatch 后依然有显式 _appStore.Mutate 调用，则不需要额外 Effect。

**验证**: 修改 config 后 launcher.json 正确持久化。

---

### Step 6: 全面验证

```powershell
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release       # 696 全绿
powershell -NoProfile -ExecutionPolicy Bypass -File tools\verify.ps1        # 25/25 PASS
powershell -NoProfile -ExecutionPolicy Bypass -File src\avalonia-launcher\run.ps1  # GUI 可用
```

## 回滚计划

每步完成后 `git add -A && git commit`。若某步验证失败:
```bash
git checkout -- .  # 丢弃该步变更
git clean -fd      # 清理新文件
```

## 估计工时

| Step | 描述 | 估计 |
|------|------|------|
| 1 | Fluxor DI 注册 | 15 min |
| 2 | SyncVisibleEntries 简化 | 30 min |
| 3 | LayoutState 接入 VM | 2-3 h |
| 4 | ReplaceEntries 统一 | 30 min |
| 5 | Effect 持久化桥接 | 30 min |
| 6 | 全面验证 | 30 min |
| **总计** | | **4-6 h** |
