Status: Archived
Purpose: 记录 WT Launcher 重新布线生产蓝图的契约、迁移矩阵与验收标准。
Original scope: 全量接线契约化，Phase A-E 全量落地（2026-08-15）。

# WT Launcher 重新布线生产蓝图

## 结论

- 状态：Production-ready blueprint v1，Phase A-E 已全量落地；上游 `docs/wiring-rewiring-plan.md`，端口台账 `docs/wiring-map.md`。

## 生产定义（DoD）

1. 每个接口、文件、迁移阶段可直接交给实现者执行。
2. 每个 workflow 有明确职责、依赖、返回类型、测试文件和门禁规则。
3. MainWindowViewModel 行预算 `MainWindowViewModel*.cs` 总和 <= 1200，台账 `docs/wiring-map.md` 由门禁校验。

## Phase A 端口契约

| 端口 | 文件 | 职责 |
| --- | --- | --- |
| IStatusSink | `ViewModels/IStatusSink.cs` | `Apply(LauncherStatusContent)` + `SetDryRunText` |
| ICommandExecutionContext | `ViewModels/ICommandExecutionContext.cs` | `Store`、`Status`、`GetTimestampUtc`，继承 `IProjectionSynchronizer` |
| ILaunchCoordinatorContext | 继承 ICommandExecutionContext | `GetSettings`、`DiscardDragReorderPreview`、`PersistSettings`、`SaveConfig`、`RecordRunHistory` |

规则：`Store` 直接暴露 `StoreGrain`（不包薄 `IStoreGateway`）；workflow 禁止接收 `MainWindowViewModel` 或 `Func`/`Action` 回调包（时间端口除外）。

## 命令面迁移矩阵

| 源文件 | 目标 workflow |
| --- | --- |
| Entries.cs | LauncherEntryCommandWorkflow |
| Presets.cs | LauncherPresetCommandWorkflow |
| Layout.cs | LauncherLayoutCommandWorkflow |
| Batch.cs | LauncherSelectionCommandWorkflow |
| CommandManagement.cs + DirectoryManagement.cs | LauncherMaintenanceCommandWorkflow |
| Migration.cs + Update.cs | LauncherExchangeCommandWorkflow |
| LaunchOrchestrator.cs | 保留为唯一启动执行者 |

关键边界：Entry 不持有 ObservableCollection；Preset 经 `LaunchOrchestrator` 唯一启动；Layout 由 `LayoutManager` 单一只读投影 owner；Selection 不新增 bulk flag；Maintenance Dialog 只发 edit；Exchange 外部 IO 与 store 事务分离。

## 门禁规则

| 门禁 | 规则 |
| --- | --- |
| wiring-map-consistency | 每个新增端口/workflow 依赖必须出现在台账 |
| main-vm-line-budget | `MainWindowViewModel*.cs` 求和，Phase D 后 <= 1200 |
| workflow-no-callback-bag | 禁 `MainWindowViewModel` / `Func`/`Action` 参数 |

## 验收数字

`dotnet test` >= 820；门禁 checks 40 → 43；ViewModels 文件数 54 → 61。验证命令：`git diff --check`；`dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore -warnaserror --nologo`；`dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore --nologo`。

## 风险与回滚

`ICommandExecutionContext` 超 8 个成员视为坏味道，先拆端口再继续；禁止新建 partial 逃避行预算门禁。每阶段只迁命令体，不改业务规则；黑盒测试是回归基线；本次 `config/launcher.json` 不迁移；未过门禁即 `git revert`。

## 关联路径

- 计划：`docs/archive/wiring-rewiring-plan.md`；端口台账：`docs/wiring-map.md`
- 组合根：`src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs`
