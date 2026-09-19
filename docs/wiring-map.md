Status: Active
Purpose: workflow 与端口的唯一台账；门禁 `wiring-map-consistency` 校验覆盖完整。
Update when: 新增/删除 workflow 或端口时。

# 布线接线图

总图：

```text
Views → MainWindowViewModel（命令表面 + UI 状态投影）
  → Commands/*.cs（业务 workflow）
  → ICommandExecutionContext / ICommandHost / IDialogService（窄端口）
  → StoreGrain（save-first-then-commit）→ FileLauncherJsonIO → config/launcher.json
```

## 端口

| 端口 | 用途 | 拥有方 |
| --- | --- | --- |
| `ICommandExecutionContext` | Store、Status、时间、投影同步 | MainWindowViewModel / LaunchOrchestrator |
| `IStatusSink` | 状态文本与 DryRun 文本 | MainWindowViewModel |
| `IProjectionSynchronizer` | 唯一投影刷新入口 | MainWindowViewModel |
| `ICommandHost` | 命令体需要的窄 UI/VM 状态操作 | MainWindowViewModel |
| `IEntryCardHost` | Entry 卡片 Owner 端口 | MainWindowViewModel |
| `ICommandEditApplier` | 命令管理 dialog 编辑回执 | MainWindowViewModel |
| `IDirectoryEditApplier` | 地址管理 dialog 编辑回执 | MainWindowViewModel |
| `ILaunchCoordinatorContext` | LaunchOrchestrator 启动上下文 | LaunchOrchestrator |

## Workflow 清单

| workflow | 命令 | 输入 | 持久化/投影 |
| --- | --- | --- | --- |
| `LauncherEntryCommandWorkflow` | 发现流程应用、目录条目添加、条目集合保存 | `ICommandExecutionContext`、`EntryListViewModel`、`IEntryService`、`ICommandHost` | `ICommandHost.SaveStoreFile` → `StoreMutationSession`；成功后 `IProjectionSynchronizer.SynchronizeProjections` |
| `LauncherPresetCommandWorkflow` | 应用/启动/预览 preset、CRUD、保存当前分组 | `ICommandExecutionContext`、`EntryListViewModel`、`PresetManager`、`IPresetService`、`LaunchOrchestrator`、`ICommandHost` | `ICommandExecutionContext.Store` 与 `ICommandHost` |
| `LauncherLayoutWorkflow` | 筛选、排序、视图模式、分组、搜索历史、窗口尺寸 | `ICommandExecutionContext`、`EntryListViewModel`、`LayoutManager`、`ICommandHost` | `ICommandExecutionContext.Store` 与 `ICommandHost`；订阅 `LayoutManager` 属性变更 |
| `LauncherSelectionCommandWorkflow` | 勾选可见项、清空选择、批量启动 | `ICommandExecutionContext`、`EntryListViewModel`、`LaunchOrchestrator`、`ICommandHost` | checkbox 单条目即时保存；批量/preset 一条保存路径 |
| `LauncherMaintenanceCommandWorkflow` | 命令/模板管理、地址管理 | `ICommandExecutionContext`、`EntryListViewModel`、`IDialogService`、`LaunchOrchestrator`、`ICommandHost` | 每次 edit 一个 `StoreMutationSession` |
| `LauncherExchangeCommandWorkflow` | 导入、导出全部、导出选中 | `ICommandExecutionContext`、`EntryListViewModel`、`PresetManager`、`IDialogService`、`IConfigExchangeService`、`LaunchOrchestrator`、`ICommandHost` | 导入合并后一个 `StoreMutationSession`，成功后整 VM 从 store 恢复 |
| `LauncherUpdateCommandWorkflow` | 检查更新 | `ICommandExecutionContext`、`IDialogService` | 不直接持有 `IUpdateManifestReader` |
| `LauncherReorderCommandWorkflow` | Move Up / Move Down | `ICommandExecutionContext`、`EntryListViewModel`、`LayoutManager`、`ICommandHost` | 拖拽状态机仍由 `LauncherDragReorderWorkflow` 拥有 |

## 边界

- 配置写入只经 `StoreGrain.Dispatch` / `StoreMutationSession`。
- 投影刷新只经 `IProjectionSynchronizer.SynchronizeProjections`。
- workflow 不依赖 `MainWindowViewModel`，不用 `Func`/`Action` 回调包；`Func<DateTimeOffset>` 仅作时间端口。
- 主 VM 仅保留命令表面、Can`*`、UI 状态投影与窄端口实现。
