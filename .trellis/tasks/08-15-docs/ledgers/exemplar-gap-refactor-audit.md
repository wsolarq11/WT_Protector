# 验证台账 — docs/exemplar-gap-refactor-audit.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档日期: 2026-07-13（创建提交 90dacd8b）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| Date: 2026-07-13 | VERIFIED | git log: 90dacd8b 2026-07-13 | 保留 |
| CodexSwitchUI 索引 MainWindowViewModel.cs 5291 行 | HISTORICAL | 仓库内仅本文引用；docs/giants-shoulders/codexswitch-avalonia-native-aot.md 无该数字（外部基准快照，仓库无第二来源） | 保留为审计当日外部观测 |
| WTLauncher.Core 独立于 Avalonia/VM，由 ArchUnitNET 强制 | VERIFIED | tests/WTLauncher.Tests/Architecture/ArchitectureRules.cs 使用 ArchUnitNET；结构基线 Core 53 文件零 Avalonia 依赖 | 保留 |
| config/launcher.json 单一权威 + 原子替换 + 备份恢复 | VERIFIED | WTLauncher.Core/LauncherJson.cs Save(backup)/RotateBackups/CleanupStaleTempFiles；CLAUDE.md: temp-file + File.Replace；coverage-gate.json rationale: atomic config writes and backup rotation | 保留 |
| 用户变更走 snapshot → mutate → save → rollback | HISTORICAL | 当日描述准确；快照类 LauncherEntryMutationSnapshot/LauncherSelectionMutationSnapshot 已于 2026-07-21 删除（协议已知事实），现回滚 = StoreGrain.Dispatch save-first-then-commit | 保留并注明后续变更 |
| 拖拽用 pointer capture 而非 OLE 嵌套消息循环 | VERIFIED | Views/MainWindow.Drag.cs、LauncherEntriesView.axaml.cs 含 pointer 事件处理；Views 下无 OLE DragDrop 用法 | 保留 |
| 三视图模式/键盘替代/全局确认策略/启动回执/发布+冒烟门禁已存在 | VERIFIED | MainWindowViewModel 投影 IsCardsViewMode/IsDetailedListMode/IsNamesViewMode；config/launcher.json settings.requireConfirmationForLaunches；LauncherLaunchReceiptText.cs/LauncherBatchLaunchReport.cs；avalonia-mainline-verify + smoke-critical-journey | 保留 |
| 热点表: MainWindow.axaml 992 行 | HISTORICAL | 当日测量；当前 341 行 | 保留 + "当日测量"注 |
| CommandManagementWindow.cs 432 行（构造 32-192） | HISTORICAL | 当日 C# 文件；现为 CommandManagementWindow.axaml(168) + .axaml.cs(158) | 保留 + "当日测量"注 |
| MainWindowViewModel.Presets.cs 473 / Layout.cs 427 / Batch.cs 338 行 | HISTORICAL | 当日测量；当前 444 / 226 / 190 | 保留 + "当日测量"注 |
| LauncherDesignTokens 尚未成为完整视觉事实源 | HISTORICAL | 当日评估；V-01 项后续落地 | 保留 |
| CQ 矩阵引用的任务存在 | VERIFIED | 归档: .trellis/tasks/archive/2026-07/07-12-fma-p2-services、07-12-fma-p3-thin-vm、07-11-centralize-declutter、07-12-fma-p4-quality、07-11-entry-launch-count-frecency、07-11-directory-scan-wizard 全部存在 | 保留 |
| 推荐序列/拒绝项/可行性总结 | 保留 | 当日计划 | 保留 |

处理汇总: 仅把两处 "Current" 小节标题改为 "(as of 2026-07-13)" 并在热点表加"当日测量"注；其余保留。
