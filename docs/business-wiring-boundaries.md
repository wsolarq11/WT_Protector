# 业务接线与边界总账

- 状态：Active
- 对齐基线：当前 Avalonia 主线源码（HEAD `c269fdf4`，2026-08-15）
- 范围：`src/avalonia-launcher/` 主线代码 + `config/launcher.json` 统一存储契约
- 方法：按“View 入口 → ViewModel/Manager/Service → StoreGrain → 文件持久化 → 外部进程/系统能力”逐条溯源；只记录当前事实和已观察到的脆弱接线点，不新增架构决策。

## 统一接线图

```text
Avalonia Views
  ├─ [RelayCommand] 绑定 / DialogService / MainWindow code-behind
  ▼
MainWindowViewModel
  ├─ EntryListViewModel（entry 集合与投影）
  ├─ PresetManager（preset 集合与选中项）
  ├─ LayoutManager（筛选/排序/视图/分组/滚动）
  ├─ StatusPresenter（状态文本/计数/回执）
  └─ LaunchOrchestrator（单启/批启/取消/预演）
        ├─ IEntryService / IPresetService → DialogService
        ├─ StoreGrain.Dispatch → FileStorePersistence → config/launcher.json
        └─ LauncherLaunchWorkflow → LaunchArgumentBuilder → Process.Start
                    ▼
            快速启动窗口 / 托盘 / 热键 / 运行历史 / 导入导出 / 更新检查
```

## 统一状态与持久化边界

- 规范配置只有 `config/launcher.json` 一个权威源：`entries + presets + settings + defaultPresetId + runHistory` 一次原子写入。
- 规范配置的写入路径统一经过 `StoreGrain.Dispatch`：reducer 产生新 state → `FileStorePersistence.Save`（temp + `File.Replace` + backup）→ 成功后才更新内存 state。保存失败时 StoreGrain 内存不变。
- ViewModel 层的快照回滚只负责把 `ObservableCollection`、选择态、PresetManager 等 UI 投影恢复到 StoreGrain 状态；真正的“内存不提交”由 StoreGrain 保证。
- 导出文件通过 `LauncherJson.Save` 直接写用户选择的路径，这是“外部文件写入”，不是规范配置写入，也不进 StoreGrain。
- 平台接线（热键、托盘、Mica/Acrylic）不持久化业务数据；失败时降级日志，不阻断主流程。

## 每条业务接线

### 1. 启动与配置加载

- 入口：`App.OnFrameworkInitializationCompleted`
- 接线：`LauncherStartupArguments.ReadConfigPath` → `FileStorePersistence.ResolveStorePath` → 配置目录可写探测 → `StoreGrain` 加载 → `LauncherSettingsDefaults.ApplyMissingSettingsDefaults` → 构造 `MainWindowViewModel`、`MainWindow`、`DialogService`、`EntryService`、`PresetService`
- 边界：
  - `--config <path>` 必须指向已存在文件；否则启动失败并显示错误窗口。
  - 无显式路径时按 当前目录 → 应用目录 向上查找仓库标记与 `config/launcher.json`；找不到则使用默认路径的空 store。
  - 加载时先做旧版文件迁移（`LegacyMigration`），再校验 schema；缺失的可选设置（快速启动热键、托盘、更新清单 URL）按声明默认值补齐。
  - 不读 AppData、不读 registry、不用数据库。

### 2. 刷新与重新加载

- 入口：`MainWindowViewModel.Refresh`
- 接线：先 `DiscardDragReorderPreview` → `StoreGrain.ReloadFromDisk` → `EntryListViewModel.ReplaceEntries` → `EnsureCustomOrderContainsEntries` → 校验 `$key` 模板引用 → 重载 preset → 校验 preset 引用 → `RefreshSelectionState` + `ApplyFilter`
- 边界：
  - `ReloadFromDisk` 不触发 StoreGrain 订阅；VM 必须手动同步 UI 投影。
  - 缺失模板 key 只显示状态，不阻止加载；preset 悬空引用写 ErrorLog，不抛异常。
  - 任何 mutation 入口都必须先丢弃拖拽预览，防止幽灵插入状态泄漏到刷新后的视图。

### 3. Entry CRUD（新增/克隆/编辑/删除/修复目录）

- 入口：卡片/名称/详情视图按钮 → `LauncherEntryViewModel` 命令 → `IEntryCardHost`
- 接线：`EntryListViewModel` → `IEntryService` → `DialogService` → `EntryDetailsWindow` → Core `LauncherEntryDrafts`/`LauncherEntryReferences` → `StoreGrain.Dispatch`
- 新增/克隆：draft 经服务校验后生成 `LaunchEntry`，同步 customEntryOrder，Dispatch 后新建 `LauncherEntryViewModel` 并刷新投影。
- 编辑：服务生成新 entry；若 ID 变更，先通过 `LauncherEntryReferences.RenameEntryId` 同步 preset 与 customEntryOrder，再 Dispatch；成功后 `ReplaceEntryFromOwner`。
- 删除：确认后 `RemoveEntryId` 同步清理 preset 引用与 customEntryOrder，Dispatch，再移除并 Dispose 对应 VM。
- “修复失效目录”是 `RepairDirectoryAsync → EditEntryAsync` 的复用路径，不单独写文件。
- 边界：
  - 保存失败发生在 Dispatch 之前时，VM 集合尚未改动；Dispatch 失败时 store 不变，VM 通过 `RestoreVmStateFromStore` 恢复。
  - 每个 mutation 第一步丢弃拖拽预览。
  - 已知接线缝隙：`EntryDetailsWindow` code-behind 已调用 `LauncherEntryDrafts.TryNormalizeRequiredDraft`，服务层随后还会再校验一次；这是双重校验路径，不是双重写盘。

### 4. 勾选与批量选择

- 路径 1（用户点击 checkbox）：`LauncherEntryViewModel.IsSelected` setter → `MainWindowViewModel.UpdateSelection` → `ReplaceEntrySelectionFromOwner` → 立即 `SaveSelectionChangeOrRestore`
- 路径 2（全选/清空/预设/导入等批量）：`SetSelectionFromOwner` 或 `ReplaceSelection` 修改多个 entry → 只调用一次 `SaveSelectionChangeOrRestore`
- 边界：
  - 没有全局“批量抑制保存”开关；两条路径靠 owner-only setter 区分，避免 checkbox 触发循环保存。
  - 保存失败时按快照恢复每个 entry 的选择态，再刷新选择投影。
  - 可见批量操作只影响 `MutableVisibleEntries`；清空全部与清空可见是两个显式路径。

### 5. 单条启动

- 入口：`LauncherEntryViewModel.LaunchCommand` / 快速启动 Enter / 主窗口键盘 Enter
- 接线：`IEntryCardHost.LaunchAsync` → `LaunchOrchestrator.LaunchAsync` → `LauncherLaunchWorkflow.BuildPlan` → 需要时 `ConfirmLaunchAsync` → `_launchWorkflow.Start(plan)` → `RecordLaunchFromOwner` + `RecordRunHistory` → `SaveConfig`
- 边界：
  - `Start` 是唯一外部进程边界：`Process.Start` 只表示“启动请求已发出”，不表示终端会话健康。
  - 启动参数由 Core `LaunchArgumentBuilder` 生成（wt.exe 路径探测、目录存在性、统一确认开关、`$key` 模板解析）。
  - 进程已经启动后发生的配置保存失败无法回滚外部副作用；代码会显示保存错误状态，但运行历史/启动次数已在内存中变化。
  - 取消确认会写入 `Canceled` 运行历史并保存。

### 6. 批量启动（勾选/预设/分组）

- 入口：`LaunchSelectedAsync`、`LaunchSelectedPresetAsync`、卡片分组 `LaunchGroupAsync`
- 接线：`LaunchOrchestrator.LaunchBatchAsync` → `PreflightBatch` → 需要时 `ConfirmBatchLaunchAsync` → `StartBatch(preflight, CancellationToken)` → 按条目生成报告 → 更新启动时间/次数 → 写运行历史 → `SaveConfig`
- 边界：
  - 预演与实际启动共用 `LauncherLaunchWorkflow.PreflightBatch`，避免“预演路径 ≠ 实际路径”。
  - 单个条目失败不会中止其余条目；取消通过 batch-level `CancellationTokenSource` 停止后续启动。
  - 运行历史状态只有 `Requested / Failed / Skipped / Canceled`，来源区分 `Individual / Selection / Preset / Group`。
  - 批启动执行期间 `IsBatchLaunching=true`，同一时刻只允许一个批启动。

### 7. 启动组（Preset）

- 状态：`PresetManager` 持有 `Presets/SelectedPreset/DefaultPresetId` 的 VM 投影；StoreGrain 订阅会同步外部 state 变化。
- 命令：
  - 应用 preset → `ReplaceSelection`（一次批量保存）
  - 启动选中 preset → `LaunchOrchestrator.LaunchBatchAsync`，source=Preset
  - 预演 preset → `PreflightBatch` 后写 DryRunText/Status
  - 保存勾选为 preset、更新、删除 → `IPresetService` + Core `LauncherPresetWorkflow` + `SavePresetMutationAndApply`
- 边界：
  - 保存流程先应用新 preset 文件到 `PresetManager`，再 `SaveStoreFile`；保存失败时从快照恢复 manager 并显示错误。
  - 新增/更新 preset 会校验 entry ID 是否存在；加载时悬空引用只记日志。
  - 分组“存为启动组”复用同一 preset 保存路径，不直接改 store。

### 8. 命令管理

- 入口：`OpenCommandManagementAsync`
- 接线：`CommandUsageAnalyzer` 生成命令/模板快照 → `DialogService` 打开 `CommandManagementWindow` → `CommandManagementViewModel` 只发 edit → `MainWindowViewModel.ApplyCommandManagementEdit` 作为事务回调执行 mutation → 保存 → 刷新命令行/模板行
- 边界：
  - 对话框自身不持 store、不写盘；所有变更回主 VM 执行。
  - 模板 key 重命名会级联更新 entry 的 `$key` 引用；模板新增/删除/改值只改 settings，不碰 entry 命令文本（删除只清理未引用视图）。
  - 保存成功后必须重新同步 `LaunchOrchestrator.CommandTemplates`，否则后续启动仍吃到旧模板。
  - 已观察到的接线缝隙：模板相关 edit 在 `ApplyEditAndReturnStatus` 内部先调了一次 `SaveStoreFile`，外层 `ApplyCommandManagementEdit` 再保存一次；当前是“双保存”事实，需要后续决策是否收敛为单次事务保存。

### 9. 地址管理

- 入口：`OpenDirectoryManagementAsync`
- 接线：`DirectoryUsageAnalyzer` 生成地址引用快照 → `DirectoryManagementWindow` → `MainWindowViewModel.ApplyDirectoryManagementEdit` → 批量 rename/merge/delete 对应 entry 的 directory → `SaveStoreFile`
- 边界：
  - rename/merge 会同步更新所有匹配 entry 的 `UpdatedAtUtc`；delete 只清理未引用地址视图，不删除 entry。
  - 保存失败时 `RestoreVmStateFromStore` 恢复集合。

### 10. Agent 发现与现有目录快速创建

- 入口：`DiscoverAgentsAsync`
- 接线：`DialogService` → `DiscoverAgentsViewModel` → `AgentDiscoveryScanner.Scan`（Core，异步、可取消）→ 选择结果 → `AgentRecommendationFactory.ToDraft` → `IEntryService.CreateEntryFromDraft` → 批量加入 `EntryListViewModel` → `SaveEntryCollectionChanges`
- 现有目录模式：从目录列表 + 命令模板生成 `EntryDraft`，走 `AddExistingDirectoryEntry`，复用 EntryService 校验。
- 边界：
  - 扫描会探测已安装 CLI；对话框内不写规范配置，只返回选择结果。
  - 已添加的同名 exec 在对话框内被禁用；owner 侧再次用 EntryService 校验，避免绕过服务层。
  - 添加多个 agent 时逐个创建 VM，最后统一保存；保存失败走 `RestoreVmStateFromStore`。

### 11. 卡片分组

- 状态：`VisibleGroups` 是 `VisibleEntries` 的派生渲染投影，分组规则在 Core `LauncherEntryGrouper`。
- 接线：分组 VM `LauncherCardGroupViewModel` 只持有 callbacks；`LaunchAllAsync` 回 `LaunchGroupAsync`，`SaveAsPresetAsync` 回 `SaveGroupAsPresetAsync`。
- 边界：分组头不直接写 store；分组启动 source=Group，存为启动组复用 PresetService。

### 12. 快速启动

- 入口：全局热键 → `App.WireQuickLaunchAndTray` → `Win32HotkeyService` → `QuickLaunchWindow`
- 接线：`QuickLaunchViewModel` 对 owner 的全量 entries 做 `LauncherQuickMatch` 过滤/评分，选中后执行该 entry 的 `LaunchCommand`
- 边界：
  - 快速启动是纯读路径，不修改筛选/排序/选择状态；启动仍走统一单条启动链。
  - 热键注册失败只禁用热键并记日志，不阻止应用启动。

### 13. 托盘

- 入口：`settings.runInTray` → `Win32TrayIcon`
- 接线：`OpenRequested` → 显示/激活主窗口；`QuitRequested` → 置 `trayQuitting` 后 Close；主窗口 Closing 时若未退出则 Cancel + Hide
- 边界：托盘是 Windows 平台 P/Invoke 接线；创建失败时降级为普通窗口，不写业务状态。

### 14. 运行历史

- 写入：`LaunchOrchestrator.RecordRunHistory` → `MainWindowViewModel.AppendRunHistory` → Core `LauncherRunHistory.AppendRange`（上限 100）→ 随下一次 `SaveConfig` 落盘
- 查看：`OpenRunHistoryAsync` → `DialogService` → `RunHistoryViewModel` → 只读窗口
- 边界：
  - 历史是“仓库观察记录”，不是终端健康报告；`Requested` 只表示启动请求到达 Process.Start 边界。
  - 来源与状态枚举受 Core 校验；加载时会过滤非法记录。

### 15. 导入/导出

- 导出：`ExportAllConfigAsync` / `ExportSelectedConfigAsync` → `PickExportPathAsync` → `LauncherConfigExchange.BuildExportFile` → `LauncherJson.Save`（写用户选择的外部路径）
- 导入：`ImportConfigAsync` → `PickImportPathAsync` → `LauncherStore.Load` → `ImportMigrationWindow` 生成 `LauncherImportPlan` → `LauncherConfigExchange.ApplyImport` → `SaveStoreFile(merged)` → `ApplyStoreFileToVm`
- 边界：
  - 导入对话框预览重复 ID、缺失目录、无效 preset 引用，并允许跳过/目录覆盖。
  - 导入合并结果先经 StoreGrain 保存，成功后同步 VM；保存失败先 `RestoreVmStateFromStore`。
  - 导出写外部文件不经过 StoreGrain；这是有意为之的外部 artifact 路径，不是规范配置写入。

### 16. 更新检查

- 入口：`CheckForUpdatesAsync`
- 接线：`DialogService` → `UpdateCheckWindow` → `UpdateCheckViewModel` → `UpdateCheckService` → `settings.updateManifestUrl`（HTTP(S) 或本地文件）→ Core manifest 解析/版本比较
- 边界：
  - 只读检查，不下载安装；无 manifest URL 时返回配置提示。
  - 网络/文件读取错误转成窗口内错误状态，不影响主 store。

### 17. 筛选/排序/视图/分组/搜索历史

- 状态：`LayoutManager` 是单一可写 owner；`IsCustomSortMode/IsCardsViewMode/...` 都是投影。
- 接线：LayoutManager 事件 → MWVM 持久化回调：
  - FilterText → `DispatchErrorOr` 保存 `lastFilterText`
  - SortMode → 保存 `sortMode`
  - EntryViewMode → 保存 `entryViewMode`
  - CardsGroupKey → 保存 `cardsGroupKey` 并重建分组
  - Enter 提交搜索 → `LauncherSearchHistory.Record` 保存 `searchHistory`（上限 20）
- 边界：
  - 保存失败时把 LayoutManager 回滚到 StoreGrain 值，再刷新。
  - customEntryOrder 在新增/刷新/拖拽提交时通过 `EnsureCustomOrderContainsEntries` 同步。
  - 滚动偏移只在视图切换时保存到 LayoutManager 内存，不写 launcher.json。

### 18. 拖拽排序与键盘移动

- 拖拽：MainWindow code-behind pointer capture → `MainWindowViewModel.BeginDragReorder/PreviewMoveEntry/CommitDragReorder` → `LauncherDragReorderWorkflow`（状态机 + working-copy order）→ `SaveDragCustomEntryOrder` → StoreGrain
- 键盘移动：仅 custom sort 模式下 MoveUp/MoveDown → `LauncherEntryOrder.MoveRelative` → 同样保存 customEntryOrder
- 边界：
  - 拖拽预览期间任何 refresh/mutation 必须先 `DiscardDragReorderPreview`。
  - Commit 保存失败会从 session snapshot 恢复 order；View 层 ghost/insert cue 与 VM session 通过 `HasActiveDragReorder` 双向守卫。
  - 拖拽布局数学（ghost 位置、插入点、卡片尺寸）在 Core 纯计算。

### 19. 窗口边界与状态

- 入口：MainWindow Opened/Closing/SizeChanged
- 接线：Opened 恢复 `window.x/y/state`；Closing 调 `TrySaveWindowBounds` → StoreGrain.Dispatch
- 边界：窗口位置/尺寸是 UI code-behind 与 StoreGrain 的直接接线；保存失败显示状态但不阻止窗口关闭。

### 20. 状态栏与回执

- 状态：`StatusPresenter` 是唯一 owner；MWVM 转发 PropertyChanged。
- 接线：各 mutation/launch/refresh 路径通过 `StatusContent`、`DryRunText`、计数更新状态；非中性回执进入最多 50 条的内存 ReceiptHistory。
- 边界：status 失败半径为零，不参与业务状态判断，不能替代错误返回。

### 21. 错误与回滚

- 捕获：统一 `ExceptionFilters.IsRecoverable`（App）/ `LauncherErrors.IsRecoverable`（Core），禁止裸 catch。
- 回滚：
  - StoreGrain：save-first-then-commit
  - Entry/Preset/Selection：VM 快照 + 失败恢复
  - Layout：回读 StoreGrain 值
  - Drag：session snapshot
  - Import：先保存后 ApplyStoreFileToVm，失败 RestoreVmStateFromStore
- 边界：`Process.Start` 之后没有外部副作用回滚；此时只能报告保存失败。错误日志写在本地 ErrorLog，不属于规范配置。

## 接线理顺方案

- 规划任务：`.trellis/tasks/08-15-wiring-system-plan/design.md`（档位 2：接线契约化）
- 实施计划：`.trellis/tasks/08-15-wiring-system-plan/implement.md`

## 当前需要人工决策的接线点

- 命令管理模板 edit 存在内层保存 + 外层保存的双保存路径。
- `EntryDetailsWindow` code-behind 直接调用 Core draft 规范化，服务层再校验；功能上安全，但接线边界不干净。
- `StoreGrain.ReloadFromDisk` 不通知订阅者，刷新路径依赖 VM 手动全量重同步。
- 启动副作用先于配置保存；若历史/启动次数保存失败，内存中已变化而磁盘未保存，当前只显示错误，不做内存回滚。
- 导入/导出通过 `LauncherJson.Save` 写外部文件，与规范 store 的 StoreGrain 写入不是同一事务边界。

## 证据文件

- 入口/接线：`src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs`
- 编排：`src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel*.cs`
- 启动/持久化：`src/avalonia-launcher/src/WTLauncher.App/ViewModels/LaunchOrchestrator.cs`、`Infrastructure/StoreGrain.cs`、`Infrastructure/FileStorePersistence.cs`
- Entry/Preset/布局/状态：`Features/Entries/EntryListViewModel.cs`、`ViewModels/PresetManager.cs`、`ViewModels/LayoutManager.cs`、`ViewModels/StatusPresenter.cs`
- 平台接线：`Platform/Win32HotkeyService.cs`、`Platform/Win32TrayIcon.cs`、`Views/MainWindow.Drag.cs`
