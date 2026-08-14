# 启动运行历史与状态回执 — Design

## Change Boundary

- 当前只有 `launchedAtUtc`、`launchCount` 和瞬态启动回执；用户无法持久回答上次请求/失败/跳过/取消状态。
- 目标：在同一个 `launcher.json` 中新增 `runHistory` 可校验小节，提供只读“最近启动”面板，不冒充终端健康完成。

## Data Flow

```text
LaunchOrchestrator (single/batch/group/preset)
  -> LauncherBatchLaunchReport (requested/failed/skipped + failure entry ids/names)
  -> LauncherRunHistoryEntry.Create
  -> MainWindowViewModel.HistoryRecorder -> _runHistory (cap 100)
  -> BuildStoreFile -> StoreGrain.Dispatch -> LauncherStore.Save
  -> 管理 -> 最近启动 -> RunHistoryWindow (只读, 倒序)
```

## Key Decisions

- `runHistory` 放在 `LauncherStoreFile` 顶层，保持单文件权威源，不新建 AppData/DB。
- 状态只有 requested/failed/skipped/canceled；`requested` 严格表示启动请求已提交，不做进程健康判断。
- 每条记录含 entry id/name、时间、动作来源、状态，不写命令全文或环境变量。
- 上限 100 条；加载时 `LauncherRunHistory.Sanitize` 丢弃损坏记录，损坏历史不阻塞主配置。
- 历史写盘失败只显示保存失败，不回滚已经发生的启动动作。

## Files Expected to Change

- Core: `LauncherRunHistory.cs`、`LauncherStoreFile.cs`、`LauncherJsonContext.cs`
- App: `LaunchOrchestrator.cs`、`LauncherBatchLaunchReport.cs`、`LauncherLaunchWorkflow.cs`、`MainWindowViewModel.History.cs`、`RunHistoryViewModel.cs`、`RunHistoryWindow.*`、`DialogService.cs`、`LauncherControlBar.axaml`
- Tests: Core history + VM launch history + report identity + JSON roundtrip + XAML/menu contract
- Docs: `README.md`

## Explicitly Not Doing

- 不做终端进程存活/退出码/日志输出监控。
- 不做成本统计、会话时长、云同步。
- 不把“已请求启动”改写成“启动成功/终端健康”。
