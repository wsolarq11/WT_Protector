Status: Archived
Purpose: 记录 WT Launcher 目标接线拓扑规划及已落地结论。
Original scope: 5 层架构 + StoreGrain 单一状态源 + 显式端口 + 无服务定位器。

# 重新布线图：WT Launcher 目标接线拓扑

## 结论

- 规划已定稿，并已按生产蓝图全量落地（Phase A-E）。
- 生产蓝图：`docs/wiring-rewiring-production-blueprint.md`。

## 现状诊断

`MainWindowViewModel` 仍承担大量命令编排：主体 511 行，Presets 446 行，Layout 226 行，Batch 199 行，CommandManagement 204 行。partial 只是物理切文件，不是接线边界。

## 目标布线原则

- MainWindowViewModel 保留可绑定状态、命令表面、投影所有权。
- 命令体下沉到有真实行为的 workflow，禁止只转发的薄包装；组合根显式 new 接线，不引入服务定位器或 DI 容器。
- 状态写入仍经 StoreGrain，多步 mutation 用 `StoreMutationSession`；投影刷新只走 `IProjectionSynchronizer` 三种模式（VisibleOnly、Mutation、FullReload）。
- 外部副作用端口显式：`IConfigExchangeService`、`IUpdateManifestReader`、`IEntryDraftValidator`；保留两条选择写入路径，不增加全局批量抑制开关。

## Command Workflow 拆解

| Workflow | 取代/承载 | 真实行为 |
| --- | --- | --- |
| LauncherEntryWorkflow | Entries.cs / Discovery.cs 落库 | draft 校验、ID/顺序同步、单次保存、失败恢复 |
| LauncherPresetWorkflow | Presets.cs 命令体 | 引用校验、apply、CRUD、预览、启动决策 |
| LauncherLayoutWorkflow | Layout.cs / Grouping.cs | 布局持久化、分组重建、搜索历史、回滚 |
| LauncherSelectionWorkflow | Batch.cs 选择部分 | 两条写入路径、批量保存、恢复 |
| LauncherMaintenanceWorkflow | CommandManagement / DirectoryManagement | 批量编辑、目录 rename/merge/delete、单次 Commit |
| LauncherExchangeWorkflow | Migration.cs / Update.cs | 导入导出、更新检查编排 |
| LaunchOrchestrator | 既有 | 单启/批启/取消/预演、历史、LaunchSaveResult |

## 迁移阶段

| 阶段 | 内容 | 验收 |
| --- | --- | --- |
| Phase 0 | 锁定六词设计语言（拓扑/契约/状态/数据/路径/验证） | — |
| Phase A | 定义 `ICommandExecutionContext` | 消除“取状态→改状态→保存→刷新”重复骨架 |
| Phase B | 先迁最高风险 Preset 与 Layout | Preset/Layout 测试全绿，MainWindowViewModel 降 600+ 行 |
| Phase C | 迁 Maintenance/Selection/Entry | 总行数降至 1200 以下 |
| Phase D | 外部 IO 与启动统一 | 不再直接触碰 IConfigExchangeService/IUpdateManifestReader |
| Phase E | 接线图机器化 | 门禁 40 → 43，结构基线同步 |

## 禁止事项

不引入服务定位器/DI 容器/全局 EventBus；不新增第二个状态 owner；不做转发薄壳；不删两条选择写入路径；不把外部 IO 放进 Views；不绕过 StoreGrain 写规范配置。

## 最终验收

`dotnet build -warnaserror` 0 警告；`dotnet test` 820+ 全绿；`tools/verify.ps1` 全绿；`git diff --check` 干净；MainWindowViewModel 总行数 < 1200；每个 workflow 有独立测试文件。

## 关联路径

- 生产蓝图：`docs/archive/wiring-rewiring-production-blueprint.md`
- 端口台账：`docs/wiring-map.md`
- 业务接线总账：`docs/business-wiring-boundaries.md`
