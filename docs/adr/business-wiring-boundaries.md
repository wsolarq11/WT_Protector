Status: Active
Purpose: View → VM → StoreGrain → 外部进程/系统能力接线总账与边界。
Update when: 接线、持久化或平台能力变化时。

# 业务接线与边界总账

范围：`src/avalonia-launcher/` 主线 + `config/launcher.json` 统一存储。只记录当前事实与脆弱点，不新增决策。

## 统一边界

- 规范配置只有 `config/launcher.json`：entries + presets + settings + defaultPresetId + runHistory 一次原子写入。
- 写路径统一经 `StoreGrain.Dispatch`：reducer → `FileStorePersistence.Save`（temp + `File.Replace` + backup）→ 成功才更新内存；失败时内存不变。
- VM 快照回滚只恢复 UI 投影；"内存不提交"由 StoreGrain 保证。
- 导出文件经 `LauncherJson.Save` 写用户选择路径，是外部文件写入，不进 StoreGrain。
- 平台接线（热键/托盘/Mica/Acrylic）不持久化业务数据；失败降级日志不阻断主流程。

## 接线清单

| # | 场景 | 入口 | 主路径 | 关键边界 |
| --- | --- | --- | --- | --- |
| 1 | 启动与配置加载 | `App.OnFrameworkInitializationCompleted` | `LauncherStartupArguments.ReadConfigPath` → `FileStorePersistence.ResolveStorePath` → 可写探测 → `StoreGrain` 加载 → `LauncherSettingsDefaults.ApplyMissingSettingsDefaults` → 构造 MainWindowViewModel / MainWindow / Dialog / Entry / Preset 服务 | `--config` 必须指向存在文件；无路径时向上找仓库标记与 `config/launcher.json`；先 `LegacyMigration` 再校验 schema；不读 AppData/registry/数据库 |
| 2 | 刷新与重新加载 | `MainWindowViewModel.Refresh` | `DiscardDragReorderPreview` → `StoreGrain.ReloadFromDisk` → `Reloaded` → `EntryListViewModel.ReplaceEntries` → `EnsureCustomOrderContainsEntries` → 校验 `$key` → 重载并校验 preset → `SynchronizeProjections(ProjectionSyncMode.FullReload)` | 任何 mutation 入口先丢弃拖拽预览；缺失模板 key 只显示状态，preset 悬空引用记日志不抛异常 |
| 3 | Entry CRUD | 卡片/名称/详情按钮 → `LauncherEntryViewModel` 命令 → `IEntryCardHost` | `EntryListViewModel` → `IEntryService` → `DialogService` → `EntryDetailsWindow` → Core `LauncherEntryDrafts`/`LauncherEntryReferences` → `StoreGrain.Dispatch` | 保存失败时 store 不变，VM 用 `RestoreVmStateFromStore` 恢复；ID 变更先 `RenameEntryId` 同步 preset/customEntryOrder 再 Dispatch；表单经 `IEntryDraftValidator` 即时反馈 |
| 4 | 勾选与批量选择 | checkbox / 全选清空预设导入 | checkbox：`IsSelected` → `UpdateSelection` → `ReplaceEntrySelectionFromOwner` → 立即 `SaveSelectionChangeOrRestore`；批量：`SetSelectionFromOwner`/`ReplaceSelection` → 一次保存 | 无全局批量抑制开关；保存失败按快照恢复每个 entry 选择态 |
| 5 | 单条启动 | `LaunchCommand` / 快速启动 Enter / 主窗口 Enter | `IEntryCardHost.LaunchAsync` → `LaunchOrchestrator.LaunchAsync` → `LauncherLaunchWorkflow.BuildPlan` → 需要时 `ConfirmLaunchAsync` → `Start(plan)` → `RecordLaunchFromOwner` + `RecordRunHistory` → `SaveConfig(LaunchSaveResultKind.SaveFailedAfterLaunch)` | `Start` 是唯一外部进程边界；启动后保存失败只报告，不回滚外部副作用；取消写 `Canceled` 运行历史 |
| 6 | 批量启动 | `LaunchSelectedAsync` / `LaunchSelectedPresetAsync` / `LaunchGroupAsync` | `LaunchBatchAsync` → `PreflightBatch` → 需要时 `ConfirmBatchLaunchAsync` → `StartBatch(preflight, CancellationToken)` → 逐条报告 → 更新启动时间/次数 → 写历史 → 保存 | 预演与实际共用 `PreflightBatch`；单条失败不中止其余；`IsBatchLaunching=true` 时只允许一个批启动 |
| 7 | 启动组 Preset | 应用/启动/预览/保存/更新/删除 | `PresetManager` 持投影；应用 → `ReplaceSelection`；启动 → `LaunchBatchAsync`(source=Preset)；预览 → `PreflightBatch` 写 DryRunText；保存/更新/删除 → `IPresetService` + Core `LauncherPresetWorkflow` + `SavePresetMutationAndApply` | 新增/更新校验 entry id 存在；加载时悬空引用只记日志 |
| 8 | 命令管理 | `OpenCommandManagementAsync` | `CommandUsageAnalyzer` → `IDialogService.OpenCommandManagementAsync(ICommandEditApplier)` → `CommandManagementViewModel` 只发 edit → `ICommandEditApplier.Apply` → `StoreMutationSession` 单次 Commit | 对话框不持 store、不写盘；模板 key 重命名级联更新 `$key` 引用；保存后重新同步 `LaunchOrchestrator.CommandTemplates` |
| 9 | 地址管理 | `OpenDirectoryManagementAsync` | `DirectoryUsageAnalyzer` → `IDialogService.OpenDirectoryManagementAsync(IDirectoryEditApplier)` → `ApplyDirectoryManagementEdit` → 批量 rename/merge/delete directory → `StoreMutationSession` 单次 Commit | rename/merge 同步更新匹配 entry 的 `UpdatedAtUtc`；delete 只清未引用视图；失败 `RestoreVmStateFromStore` |
| 10 | Agent 发现与目录创建 | `DiscoverAgentsAsync` | `DialogService` → `DiscoverAgentsViewModel` → `AgentDiscoveryScanner.Scan`（异步可取消）→ `AgentRecommendationFactory.ToDraft` → `IEntryService.CreateEntryFromDraft` → 批量入 VM → `SaveEntryCollectionChanges` | 扫描探测已装 CLI；对话框不写规范配置；同名 exec 禁用且 owner 侧再用 EntryService 校验 |
| 11 | 卡片分组 | 分组启动/存为启动组 | `VisibleGroups` 是 `VisibleEntries` 的派生渲染投影，分组规则在 Core `LauncherEntryGrouper`；`LaunchAllAsync` → `LaunchGroupAsync`，`SaveAsPresetAsync` → `SaveGroupAsPresetAsync` | 分组头不直接写 store；source=Group，存为启动组复用 PresetService |
| 12 | 快速启动 | 全局热键 → `App.WireQuickLaunchAndTray` → `Win32HotkeyService` → `QuickLaunchWindow` | `QuickLaunchViewModel` 对全量 entries 做 `LauncherQuickMatch` 过滤/评分，选中后执行 `LaunchCommand` | 纯读路径，不改筛选/排序/选择；热键注册失败只禁用并记日志 |
| 13 | 托盘 | `settings.runInTray` → `Win32TrayIcon` | `OpenRequested` 显示/激活主窗口；`QuitRequested` 置 `trayQuitting` 后 Close；Closing 未退出则 Cancel + Hide | P/Invoke 接线；创建失败降级为普通窗口 |
| 14 | 运行历史 | `RecordRunHistory` → `AppendRunHistory` → Core `LauncherRunHistory.AppendRange`（上限 100）→ 随下次保存落盘 | `OpenRunHistoryAsync` → `DialogService` → `RunHistoryViewModel` 只读窗口 | 历史是仓库观察记录，非健康报告；`Requested` 只表示请求到达 `Process.Start`；加载时过滤非法记录 |
| 15 | 导入/导出 | 导出 `ExportAllConfigAsync`/`ExportSelectedConfigAsync`；导入 `ImportConfigAsync` | 导出：`PickExportPathAsync` → `IConfigExchangeService.BuildExport` → `SaveExport`；导入：`LoadImport` → `ImportMigrationWindow` 生成 `LauncherImportPlan` → `ApplyImport` → StoreGrain 保存 merged → `ApplyStoreFileToVm` | 导入预览重复 ID/缺失目录/无效 preset 引用，允许跳过/目录覆盖；导出写外部路径不进 StoreGrain |
| 16 | 更新检查 | `CheckForUpdatesAsync` | `DialogService` → `UpdateCheckWindow` → `UpdateCheckViewModel` → `UpdateCheckService(IUpdateManifestReader)` → `settings.updateManifestUrl` → Core manifest 解析/版本比较 | 只读检查不下载安装；无 URL 返回配置提示；错误转窗口内状态 |
| 17 | 筛选/排序/视图/分组/搜索历史 | `LayoutManager` 是单一可写 owner | FilterText/SortMode/EntryViewMode/CardsGroupKey → `DispatchErrorOr` 保存 → `SynchronizeProjections(ProjectionSyncMode.VisibleOnly)`；Enter 提交 → `LauncherSearchHistory.Record`（上限 20） | 保存失败把 LayoutManager 回滚到 StoreGrain 值；滚动偏移只在视图切换时存内存 |
| 18 | 拖拽排序与键盘移动 | MainWindow code-behind pointer capture / MoveUp/MoveDown | 拖拽：`BeginDragReorder/PreviewMoveEntry/CommitDragReorder` → `LauncherDragReorderWorkflow` → `SaveDragCustomEntryOrder` → StoreGrain；键盘：`LauncherEntryOrder.MoveRelative` | Commit 保存失败从 session snapshot 恢复 order；预览期间任何 refresh/mutation 先 `DiscardDragReorderPreview` |
| 19 | 窗口边界与状态 | MainWindow Opened/Closing/SizeChanged | Opened 恢复 `window.x/y/state`；Closing 调 `TrySaveWindowBounds` → `StoreGrain.Dispatch` | 保存失败显示状态但不阻止关闭 |
| 20 | 状态栏与回执 | `StatusPresenter` 唯一 owner | 各 mutation/launch/refresh 经 `StatusContent`/`DryRunText`/计数更新；非中性回执入内存 ReceiptHistory（上限 50） | status 失败半径为零，不参与业务判断 |
| 21 | 错误与回滚 | 统一 `ExceptionFilters.IsRecoverable` / `LauncherErrors.IsRecoverable` | StoreGrain save-first-then-commit；Entry/Preset/Selection VM 快照恢复；Layout 回读 store；Drag session snapshot；Import 先保存后 `ApplyStoreFileToVm` | 禁裸 catch；`Process.Start` 之后无外部副作用回滚；错误日志写本地 ErrorLog |

## 证据文件

- 入口/接线：`src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs`
- 编排：`src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel*.cs`
- 启动/持久化：`ViewModels/LaunchOrchestrator.cs`、`Infrastructure/StoreGrain.cs`、`Infrastructure/FileStorePersistence.cs`、`Infrastructure/StoreMutationSession.cs`
- 投影/协作端口：`ViewModels/IProjectionSynchronizer.cs`、`ViewModels/LaunchOrchestrator.cs`（`ILaunchCoordinatorContext`）
- Entry/Preset/布局/状态：`Features/Entries/EntryListViewModel.cs`、`ViewModels/PresetManager.cs`、`ViewModels/LayoutManager.cs`、`ViewModels/StatusPresenter.cs`
- 外部 IO：`Features/ConfigExchangeService.cs`、`Features/Updates/UpdateCheckService.cs`
- 平台接线：`Platform/Win32HotkeyService.cs`、`Platform/Win32TrayIcon.cs`、`Views/MainWindow.Drag.cs`
