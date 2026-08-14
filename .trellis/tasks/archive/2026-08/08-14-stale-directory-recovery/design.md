# 失效目录可见化与一键修复 — Design

## Change Boundary

- 当前行为：目录缺失只会在启动预检或 root gate 时暴露；主 UI 不标记、不汇总。
- 目标行为：目录缺失在 Cards/Details/Names 三种视图可见，顶部显示失效计数，并提供“重新选择路径”入口；删除继续走现有确认/引用同步/回滚。

## Data Flow

```text
LaunchEntry.Directory
  -> LauncherDirectoryHealth.IsMissing(entry)   (Core, file-system read)
  -> LauncherEntryViewModel.IsStaleDirectory    (read-only computed projection)
  -> EntryListViewModel.RefreshDirectoryHealth() (summary + PropertyChanged)
  -> MainWindowViewModel.StaleDirectoryCount    (forwarded binding)
  -> XAML badge / border / context menu          (presentation only)
```

## Key Decisions

- `IsStaleDirectory` 是计算属性，不存第二份 writable bool；目录变化后由 `RefreshDisplay()` 通知刷新。
- `StaleDirectoryCount` 同样按需从 `_entries` 计算，只在 mutation/refresh 后通知，避免字段漂移。
- 修复路径复用 `EditEntryAsync`：用户点击“重新选择路径”打开现有编辑对话框，目录框已有“浏览...”能力。
- 删除路径复用现有 `DeleteCommand`：确认、presets/customEntryOrder 清理、snapshot/rollback 都不复制第二套。
- 代码不得静默写 `launcher.json`；只读健康扫描不触发保存。

## Files Expected to Change

- `src/avalonia-launcher/src/WTLauncher.Core/LauncherDirectoryHealth.cs` — 新增纯目录健康规则。
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherDesignTokens.cs` — 新增 warning badge 颜色 token。
- `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs` — 注入 warning badge brushes。
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryViewModel.cs` — 只读 stale projection + Repair 命令。
- `src/avalonia-launcher/src/WTLauncher.App/Features/Entries/EntryListViewModel.cs` — 汇总计数与变更通知。
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` — 转发 stale 绑定属性。
- `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` — stale card style。
- `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml` — 三视图标记 + 修复菜单项。
- `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherControlBar.axaml` — 失效计数摘要。
- `tests/WTLauncher.Tests/LauncherDirectoryHealthTests.cs` — Core 规则测试。
- `tests/WTLauncher.Tests/MainWindowViewModelTests.StaleDirectoryTests.cs` — VM/UI 数据流测试。

## Explicitly Not Doing

- 不自动移动/创建/猜测目录。
- 不新增第二配置/状态文件。
- 不监控文件系统变化。
- 不重写启动预检或 gate 的 fail-fast 边界。