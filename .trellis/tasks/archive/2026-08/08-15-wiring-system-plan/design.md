# 接线系统理顺设计（档位 2：接线契约化）

## 0. 目标与边界

- 目标：在不改变 5 层架构、不引入新框架、不改配置 schema 的前提下，把当前 21 条业务路径中的隐式接线收敛为显式契约。
- 成功标准：用户可见行为不变；`git diff --check` 通过；`tools/verify.ps1` 全绿（38 checks / 811 tests / GUI smoke）。
- 本设计只规划，不授权实现。

## 1. 调研结论

### 现状

- `StoreGrain.Dispatch` 已是唯一规范配置写入边界，`FileStorePersistence.Save` 使用 temp + `File.Replace` + backup，原子写语义正确。
- 当前接线脆弱点主要集中在“写路径分散”和“桥接委托不可审计”：
  - `SaveStoreFile(BuildStoreFile())` 在 Entry/Batch/Layout/Reorder/Preset/Directory/Command/Import 等多处手工重复。
  - `CommandManagement` 存在内层 `SaveStoreFile` + 外层再次保存，且内层返回值被忽略。
  - `LaunchOrchestrator` 通过 9 个 `Func/Action` 字段桥接 MainWindowViewModel。
  - `DialogService` 通过 `Func<CommandManagementEdit, ...>` / `Func<DirectoryManagementEdit, ...>` 把事务回调传入对话框。
  - `EntryDetailsWindow` code-behind 直接调用 Core `LauncherEntryDrafts`。
  - `StoreGrain.ReloadFromDisk` 不通知订阅者，刷新路径依赖 VM 手动重同步。
  - 启动先产生外部副作用，再保存运行历史；当前 `SaveConfig` 用 bool 表达结果，无法区分“已启动但保存失败”和“保存失败未启动”。

### 为什么不做 use-case/command 层

- 当前代码规模约为 115 个 App 源文件、21 条路径，核心问题不是缺少“命令层”，而是同一个事务/端口语义在多处重复实现。
- 新增 use-case 层会同时引入文件、测试、架构规则、DI 接线成本，收益需等待功能复杂到出现第二个编排入口再评估。
- 若接线契约化后仍频繁出现“同一编辑逻辑不同调用链”，再升级为档位 3。

## 2. 接线系统地图

| # | 接线系统 | 当前入口/主体 | 主要问题 | 目标接线 |
|---|---|---|---|---|
| 1 | 状态/持久化 | `StoreGrain`, `FileStorePersistence`, `BuildStoreFile`, `SaveStoreFile` | 手工事务重复、Reload 无通知 | `StoreMutationSession` + Reloaded 事件 |
| 2 | Entry/Preset/Command/Directory mutation | `MainWindowViewModel.*`, `IEntryService`, `IPresetService` | 多处 snapshot/save/rollback 各自实现；命令管理双保存 | 统一 mutation 事务，所有 edit 一次 Commit |
| 3 | Dialog/View 边界 | `IDialogService`, `DialogService`, dialog code-behind | EntryDetailsWindow 直接调 Core；Func 回调进 DialogService | `ICommandEditApplier`, `IDirectoryEditApplier`, EntryDraft 表单 ViewModel |
| 4 | Launch | `LaunchOrchestrator`, `LauncherLaunchWorkflow` | 9 个 Func/Action 桥接；进程边界未显式端口化 | `ILaunchCoordinatorContext`, `ILaunchProcessStarter`, 结果类型 |
| 5 | 投影/布局 | `EntryListViewModel`, `LayoutManager`, `VisibleGroups`, `StatusPresenter` | 刷新重同步手工链；状态订阅只同步 PresetManager | `IProjectionSynchronizer`，Reload 统一全量同步 |
| 6 | 选择/拖拽 | `IsSelected` 双路径, `LauncherDragReorderWorkflow` | 契约脆弱、`Func<ImmutableList<string>, string?>` 保存桥 | `IEntrySelectionOwner` + `IStoreMutationPort`，保留双路径但加测试 |
| 7 | 平台/外部 IO | `Win32HotkeyService`, `Win32TrayIcon`, `UpdateCheckService`, 导入导出 | App 手工接线；网络/文件读取未端口化 | `IUpdateManifestReader`, `IConfigExchangeService`，平台接线集中在 App 组合根 |
| 8 | 错误/回滚 | `ExceptionFilters`, `RestoreVmStateFromStore`, `SaveConfig` | bool 结果不表达外部副作用已发生 | `ErrorOr`/专用结果类型，明确“已启动但保存失败” |

## 3. 目标契约

### 3.1 `StoreMutationSession`

- 位置：`WTLauncher.App/Infrastructure/` 或 `Features/` 下新增小类。
- 职责：一个 mutation 只允许一次 Commit。
- API 草案：
  - `Begin(LauncherStoreFile current, Action restore)` 返回 session
  - `Commit(LauncherStoreFile next)` 内部走 `StoreGrain.Dispatch`；失败调用 restore 并返回错误
  - `Dispose()` 未 Commit 时恢复 snapshot
- 好处：`CommandManagement`、`DirectoryManagement`、Import、Entry、Preset、Batch 不再各自手写 `SaveStoreFile + RestoreVmStateFromStore`。

### 3.2 `Reloaded` 事件

- `StoreGrain.ReloadFromDisk()` 成功后触发 `Reloaded`（或带 kind 的 StateChanged）。
- `MainWindowViewModel` 的订阅改为统一 `RefreshFromStore`：ReplaceEntries → PresetManager.Load → EnsureCustomOrder → run history → selection → projections。
- `Refresh` 命令变为“Reload + 等待事件”，不再保留手工同步链。
- 回滚点：若事件顺序引发测试波动，先保留手动同步并仅新增事件，不删除旧调用。

### 3.3 `ILaunchCoordinatorContext`

- `LaunchOrchestrator` 构造改为接收接口，不再设置 9 个公开 `Func/Action` 字段。
- 接口职责：读取 settings/timestamp/entries、写 DryRun/status、ApplyFilter、DiscardDrag、persist settings、save config、record history。
- `MainWindowViewModel` 实现该接口（或提供 Adapter 类），测试用 Fake 实现。
- 同时新增 `ILaunchProcessStarter`：`LauncherLaunchWorkflow` 不再接收 `Action<LaunchPlan>`，而是接收进程启动端口；生产实现仍是 Process.Start。

### 3.4 Dialog 契约

- `IDialogService.OpenCommandManagementAsync` 参数改为 `ICommandEditApplier`。
- `IDialogService.OpenDirectoryManagementAsync` 参数改为 `IDirectoryEditApplier`。
- 两个接口各只有一个 `Apply(edit)` 方法；`MainWindowViewModel` 提供实现。
- `EntryDetailsWindow` 改为接收 `IEntryDraftValidator`（由 `EntryService` 实现）或引入 `EntryDetailsViewModel`；View code-behind 不再直接调 Core。

### 3.5 Projection/Status 同步

- `RefreshAllProjections`、`ApplyFilter`、`RebuildVisibleGroups`、`OnEntryCountChanged` 收敛为 `IProjectionSynchronizer` 或 MainWindowViewModel 内部显式方法。
- 目标不是消灭事件，而是让“刷新后必须重建哪些投影”成为单一函数，所有刷新/Reload/mutation 都调用同一入口。
- StatusPresenter 继续单一 owner；mutation 统一返回 `ErrorOr` 后再统一转状态文本，避免每个路径自己判断。

### 3.6 外部 IO 端口

- `IUpdateManifestReader`：抽象 HTTP(S)/本地文件读取，`UpdateCheckService` 只依赖端口。
- `IConfigExchangeService`：封装导入/导出路径，App 层不再直接调 `LauncherJson.Save`/`LauncherStore.Load`；但内部仍使用 Core schema。
- 平台服务（热键/托盘/QuickLaunch）可保持 `App.axaml.cs` 组合根，但新增一个窄接口 `IPlatformLauncherBootstrap` 描述“注册热键、创建托盘、打开快速启动”，便于测试与审计。

## 4. 分系统方案

### 4.1 状态/持久化

- 新增 `StoreMutationSession`，替换手工 `SaveStoreFile(BuildStoreFile())`。
- `SaveStoreFile` 保留为底层私有方法，但公开调用点收敛到 session。
- 删除 CommandManagement 模板 edit 的 4 处内层保存；`ApplyEditAndReturnStatus` 只 mutate，外层统一 Commit。
- `ReloadFromDisk` 增加事件；刷新路径改为单入口。
- 增加测试：双保存回归、Commit 失败恢复、未 Commit Dispose 恢复、Reload 通知。

### 4.2 Entry/Preset mutation

- EntryService/PresetService 保留“dialog + validate + mutate”职责，但 mutation 结果返回 `ErrorOr<LaunchEntry/PresetMutation>`。
- MainWindowViewModel 统一用 `StoreMutationSession` 提交，删除各 partial 里的手工快照模板。
- 修复 ID 重命名/删除时 preset 引用、customEntryOrder 的级联更新继续走 Core，不允许在 ViewModel 手工改引用关系。

### 4.3 Dialog/View 边界

- DialogService 只负责创建 Window 和传端口对象。
- `ICommandEditApplier`/`IDirectoryEditApplier` 消除 DialogService 接口里的 Func。
- Entry 表单从 code-behind 迁到 `EntryDetailsViewModel`（或至少 `IEntryDraftValidator`），业务校验只留一条服务路径。
- MainWindow code-behind 继续允许窗口、键盘、拖拽 hit-test；不得新增业务 if。

### 4.4 Launch

- `ILaunchCoordinatorContext` 替换 Func/Action 字段。
- `ILaunchProcessStarter` 显式进程边界；测试注入 fake 验证不会误启动。
- 结果类型改为 `ErrorOr` 或 `LaunchSaveResult`：`SaveSucceeded / SaveFailedAfterLaunch / SaveFailedBeforeLaunch / Canceled`。
- 取消确认、预演、批启动统一走同一状态回执生成函数，不重复拼文本。

### 4.5 投影/布局

- 保留 `EntryListViewModel` 作为 entry 集合 owner，但将“可见投影/分组重建”收敛为同步器接口。
- Reload 后只走一次全量同步；mutation 后走增量同步；两者使用同一组 Core 纯函数。
- 为 `VisibleGroups`、`VisibleCount`、`SelectedCount`、`IsVisibleEmpty` 增加投影一致性测试，防止跳过 ApplyFilter 的回归。

### 4.6 选择/拖拽

- 保留两条选择写路径，不引入全局抑制开关。
- 新增 `IEntrySelectionOwner` 接口，让 `LauncherEntryViewModel` 不再依赖具体 MainWindowViewModel。
- `LauncherDragReorderWorkflow` 的保存回调改为 `IStoreMutationPort`（或直接复用 `StoreMutationSession`）。
- 增加测试：批量选择不触发重复保存、拖拽中 Refresh 丢弃预览、Commit 保存失败恢复 order。

### 4.7 平台/外部 IO

- 平台接线保留在 `App.axaml.cs` 组合根，但通过窄端口注入。
- `UpdateCheckService` 依赖 `IUpdateManifestReader`，测试覆盖 HTTP 成功/失败、本地文件、无 URL。
- `IConfigExchangeService` 封装导入导出；规范 store 写入仍只走 StoreGrain，导出文件仍走外部路径但不再由 MainWindowViewModel 直接触碰 JSON IO。

### 4.8 错误/回滚

- 统一使用 `ErrorOr`/专用结果类型，弃用 bool 型 `SaveConfig`。
- 每个 mutation 的失败路径必须满足：StoreGrain 不变、VM 投影恢复、status 明确。
- 对“外部副作用已发生但保存失败”给出明确用户文案与内存策略（不假装可回滚外部进程）。

## 5. 阶段化实施顺序

| 阶段 | 内容 | 风险 | 验收 |
|---|---|---|---|
| 0 | 基线：确认当前 gate 绿，固化 21 路径接线测试基线 | 无 | verify 全绿 |
| 1 | StoreMutationSession + 删除双保存 + Reload 事件 | 低 | 事务失败测试、Reload 单入口测试 |
| 2 | Launch 端口化：ILaunchCoordinatorContext + ILaunchProcessStarter + 结果类型 | 中 | Fake 启动测试、保存失败语义测试 |
| 3 | Dialog 契约化：ICommandEditApplier/IDirectoryEditApplier + Entry 表单业务逻辑迁移 | 中 | 对话框无业务规则、无 Func 桥接 |
| 4 | Projection 同步收敛：RefreshFromStore 单入口、投影一致性测试 | 中 | 所有刷新/Reload/mutation 同一同步路径 |
| 5 | 外部 IO 端口化 + gate 加固 | 低-中 | Update/Import/Export fake 测试；门禁新增接线检查 |

## 6. 不做清单

- 不引入 use-case/command 层、消息总线、Fluxor/Redux、DI 容器。
- 不改变 `config/launcher.json` schema，不做数据迁移。
- 不重写 Views/XAML，不引入新 UI 框架。
- 不删除现有 selection 双路径，不用全局批量抑制开关替代它。
- 不承诺“接线理顺后零脆弱点”；只承诺每个已知接线点有明确 owner、端口和失败语义。

## 7. 验收与回滚

- 每个阶段单独提交；每阶段完成后跑 `git diff --check` + `tools/verify.ps1`。
- 回滚点：该阶段 commit 可整体 revert；无 schema 变化、无用户数据迁移，因此回滚成本低。
- 全部完成后更新 `docs/business-wiring-boundaries.md` 和 `.trellis/spec/`，把目标接线写入规范，防止未来重新长出 Func 桥接。