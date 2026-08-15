# 布线接线图（Wiring Map）

> 唯一端口台账。源码新增端口或 workflow 后必须同步本节；门禁 `wiring-map-consistency`
> 会校验 workflow 类名与端口名均出现在本文件。

## 总图

```text
Views → MainWindowViewModel（薄外观 + UI 状态投影）
          ↓ 只调 workflow 公有命令
Commands/*.cs（六条业务 workflow + 辅助 workflow）
          ↓ 经窄端口
ICommandExecutionContext / ICommandHost / IDialogService
          ↓
StoreGrain（save-first-then-commit）→ FileLauncherJsonIO → config/launcher.json
```

## 端口

| 端口 | 用途 | 拥有方 |
|------|------|--------|
| `ICommandExecutionContext` | Store、Status、时间、投影同步 | MainWindowViewModel / LaunchOrchestrator |
| `IStatusSink` | 状态文本与 DryRun 文本 | MainWindowViewModel |
| `IProjectionSynchronizer` | 唯一投影刷新入口 | MainWindowViewModel |
| `ICommandHost` | 命令体需要的窄 UI/VM 状态操作 | MainWindowViewModel |
| `IEntryCardHost` | Entry 卡片 Owner 端口 | MainWindowViewModel |
| `ICommandEditApplier` | 命令管理 dialog 编辑回执 | MainWindowViewModel |
| `IDirectoryEditApplier` | 地址管理 dialog 编辑回执 | MainWindowViewModel |
| `ILaunchCoordinatorContext` | LaunchOrchestrator 启动上下文 | LaunchOrchestrator |

## Workflow 清单

### 1. Entry（`LauncherEntryCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`IEntryService`、`ICommandHost`
- 命令：发现流程应用、目录条目添加、条目集合保存
- 持久化：`ICommandHost.SaveStoreFile` → `StoreMutationSession`
- 投影：成功后 `IProjectionSynchronizer.SynchronizeProjections`

### 2. Preset（`LauncherPresetCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`PresetManager`、`IPresetService`、`LaunchOrchestrator`、`ICommandHost`
- 命令：应用/启动/预览 preset、CRUD、保存当前分组
- 持久化：`ICommandExecutionContext.Store` 与 `ICommandHost`
- 投影：单次 `IProjectionSynchronizer.SynchronizeProjections`

### 3. Layout（`LauncherLayoutWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`LayoutManager`、`ICommandHost`
- 命令：筛选、排序、视图模式、分组、搜索历史、窗口尺寸
- 持久化：`ICommandExecutionContext.Store` 与 `ICommandHost`
- 事件：订阅 `LayoutManager` 属性变更，不反向订阅 VM

### 4. Selection（`LauncherSelectionCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`LaunchOrchestrator`、`ICommandHost`
- 命令：勾选可见项、清空选择、批量启动
- 不变量：checkbox 单条目即时保存；批量/preset 一条保存路径；失败恢复原选择

### 5. Maintenance（`LauncherMaintenanceCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`IDialogService`、`LaunchOrchestrator`、`ICommandHost`
- 命令：命令/模板管理、地址管理
- 事务：每次 edit 一个 `StoreMutationSession`，失败恢复并同步 `LaunchOrchestrator.CommandTemplates`

### 6. Exchange（`LauncherExchangeCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`PresetManager`、`IDialogService`、`IConfigExchangeService`、`LaunchOrchestrator`、`ICommandHost`
- 命令：导入、导出全部、导出选中
- 事务：导入合并后一个 `StoreMutationSession`，成功后整 VM 状态从 store 恢复

### 7. Update（`LauncherUpdateCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`IDialogService`
- 命令：检查更新
- 约束：不直接持有 `IUpdateManifestReader`，读取动作留在 UpdateCheck 服务内

### 8. 辅助 Reorder（`LauncherReorderCommandWorkflow`）

- 输入：`ICommandExecutionContext`、`EntryListViewModel`、`LayoutManager`、`ICommandHost`
- 命令：Move Up / Move Down；拖拽状态机仍由 `LauncherDragReorderWorkflow` 拥有

## 边界

- 配置写入只经 `StoreGrain.Dispatch` / `StoreMutationSession`，没有第二条状态 owner。
- 投影刷新只经 `IProjectionSynchronizer.SynchronizeProjections`。
- workflow 不依赖 `MainWindowViewModel`，不使用 `Func`/`Action` 回调包；`Func<DateTimeOffset>` 仅作时间端口。
- 主 VM 仅保留命令表面、Can`*`、UI 状态投影与窄端口实现。
