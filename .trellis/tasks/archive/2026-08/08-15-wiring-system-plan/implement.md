# 接线契约化实施计划

## 前置条件

- 用户已确认档位 2 接线契约化。
- 规划任务 PRD/design 已定稿。
- 实施前每个阶段必须得到独立推进许可，不允许本规划授权一次全量重构。

## Phase 0：基线固化

- [x] 确认 `git diff --check` + `tools/verify.ps1` 全绿。
- [x] 为 8 个接线系统各建立最小 wiring 测试文件（仅当现有测试未覆盖时才新增）。
- [x] 记录每阶段回滚 commit 基线。

## Phase 1：事务保存与双保存

### 任务

- 新增 `StoreMutationSession` 于 `Infrastructure/`。
- 替换 Entry/Preset/Batch/Layout/Reorder/Directory/Import 中手工 `SaveStoreFile(BuildStoreFile())`。
- 删除 CommandManagement 模板 edit 的 4 处内层保存。
- 为 `StoreGrain.ReloadFromDisk` 增加 Reloaded 事件，并把 MainWindowViewModel 刷新收敛为 `RefreshFromStore`。

### 验收

- 命令管理模板编辑只保存一次。
- 所有 mutation 失败路径恢复 VM 投影。
- Reload 后 entries/presets/selection/projections 全部同步。
- `dotnet test -c Release` + `tools/verify.ps1` 全绿。

### 回滚

- 本阶段为单 commit；出现问题 revert 该 commit。

## Phase 2：Launch 端口化

### 任务

- 新增 `ILaunchCoordinatorContext` 与 `ILaunchProcessStarter`。
- `LaunchOrchestrator` 移除 9 个公开 `Func/Action` 字段。
- `LauncherLaunchWorkflow` 不再接收 `Action<LaunchPlan>`。
- 将 `SaveConfig` bool 改为可表达“已启动但保存失败”的结果类型。

### 验收

- 单启/批启/预演/取消路径全部通过 Fake context 测试。
- 新增“启动成功但历史保存失败”测试，验证状态与内存策略。
- 门禁全绿。

### 回滚

- 回滚 Phase 2 commit；Phase 1 保留。

## Phase 3：Dialog 契约化

### 任务

- 新增 `ICommandEditApplier`、`IDirectoryEditApplier`。
- `IDialogService` 移除 `Func<...>` 参数。
- Entry 表单业务逻辑迁移到 `EntryDetailsViewModel` 或 `IEntryDraftValidator`，Views code-behind 不再直接调 Core。

### 验收

- DialogService 接口不再出现 Func/Action 桥接。
- `EntryDetailsWindow` 不再直接引用 `LauncherEntryDrafts`。
- 新增对话框 ViewModel 测试；门禁全绿。

### 回滚

- 回滚 Phase 3 commit；Phase 1/2 保留。

## Phase 4：Projection 同步收敛

### 任务

- 收敛 `RefreshAllProjections`、`ApplyFilter`、`RebuildVisibleGroups` 为单一同步入口。
- 为 VisibleGroups/VisibleCount/SelectedCount/IsVisibleEmpty 增加投影一致性测试。
- 明确 mutation 增量同步与 Reload 全量同步的分界。

### 验收

- 刷新/Reload/mutation 三条路径使用同一同步契约。
- 无“跳过 ApplyFilter 但未重建派生投影”的回归。
- 门禁全绿。

### 回滚

- 回滚 Phase 4 commit；Phase 1-3 保留。

## Phase 5：外部 IO 端口化与 gate 加固

### 任务

- 新增 `IUpdateManifestReader`、`IConfigExchangeService`。
- 更新检查、导入导出通过端口注入。
- `tools/verify.ps1` 新增接线检查：DialogService 无 Func/Action、Views 无直接 Core 业务调用、无重复保存模式（可先做静态正则/analyzer 检查）。

### 验收

- Update/Import/Export 测试不依赖真实网络/磁盘路径。
- gate 从 38 项增加接线检查（同步更新 AGENTS 结构基线表）。
- 门禁全绿。

### 回滚

- 回滚 Phase 5 commit；Phase 1-4 保留。

## Phase 6：文档与 spec 同步

### 任务

- 更新 `docs/business-wiring-boundaries.md`，将“已知缝隙”改为“已契约化”状态。
- 更新 `.trellis/spec/views/mvvm-pattern.md`、`dialogs.md`、`core/persistence.md` 等接线规范。
- 归档本任务或保留为已完成任务记录。

### 验收

- README/AGENTS/结构基线一致。
- `tools/verify.ps1` 全绿。

## 测试矩阵

| 接线系统 | 成功路径 | 失败路径 | 边界路径 |
|---|---|---|---|
| 状态/持久化 | 一次 Commit | Save 失败恢复 | 未 Commit Dispose 恢复、Reload 事件 |
| Mutation | Entry/Preset/Command/Directory/Import 各自成功 | 各失败恢复 | 双保存回归、ID 级联 |
| Dialog | 各对话框返回 | 取消 | Func 桥接不再出现 |
| Launch | 单启/批启/预演 | 启动失败/保存失败 | 已启动但保存失败、取消 |
| Projection | Filter/Sort/View/Group | Reload 失败 | 跳过 ApplyFilter 的派生投影 |
| Selection/Drag | checkbox/批量/拖拽 | Commit 失败 | 拖拽中 Refresh、取消 |
| Platform/IO | Update/Import/Export | 网络/文件失败 | 无 URL、外部路径 |
| Error | ErrorOr 成功 | 各类 recoverable | 外部副作用后保存失败 |