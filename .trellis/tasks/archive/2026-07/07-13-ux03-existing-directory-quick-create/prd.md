# UX03 已有目录快速创建

## Goal

完成已有目录到命令模板再到entry的最短创建路径

## Requirements

- 复用 `07-11-directory-scan-wizard` 的实现，不创建平行入口或第二套持久化。
- Discovery 对话框支持 PATH 扫描与已有目录快速创建两种模式，切换时保留各自状态。
- 已有目录来自 `DirectoryUsageAnalyzer`，命令来自当前 `settings.commandTemplates`，创建数据使用 `EntryDraft`。
- 创建必须经过 `EntryService.CreateEntryFromDraft`，并复用 MainWindow 的 snapshot/save/rollback 边界。
- 保持 Core 零 Avalonia 依赖、`config/launcher.json` 单一权威源以及 `LauncherStore` 唯一磁盘写入所有者。

## Acceptance Criteria

- [x] 对话框可从已有目录和命令模板生成类型化 `ExistingDirectorySelection`。
- [x] 目录按引用次数降序、路径升序展示；命令模板按 key 排序并显示命令预览。
- [x] 默认名称取目录末段且可编辑；确认条件要求目录与命令均有效。
- [x] 成功创建后主列表和磁盘立即包含新 entry。
- [x] 验证失败不修改集合；保存失败恢复 entries、settings 与派生投影。
- [x] Release 测试 622/622 通过。
- [x] `tools/verify.ps1` 通过，包括 win-x64 self-contained publish 与 GUI smoke。

## Evidence

- 实现提交：`7621164 feat(discovery): create entries from existing directories`
- 既有实施任务归档提交：`962a101 chore(task): archive 07-11-directory-scan-wizard`
- 聚焦契约：`DiscoverAgentsViewModelDirectoryModeTests`、`MainWindowViewModelTests.DiscoveryTests`
