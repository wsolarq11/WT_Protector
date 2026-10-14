# Directory Structure — Views Layer

> `Views/` 文件组织。计数由 `AGENTS.md` 结构基线 + `structure-baseline-up-to-date` 机器校验，本文件不硬编码漂移数字。

## 文件布局

- `MainWindow.axaml(.cs)`：主窗口（三视图）+ code-behind。
- `MainWindow.Drag.cs`：DragOver/Drop + ghost 工厂。
- `MainWindowEntryCardResolver.cs`：卡片视觉树解析。
- `LauncherControlBar.axaml(.cs)`：批量控制条。
- `LauncherEntriesView.axaml(.cs)`：entries 视图宿主。
- `FastWrapPanel.cs`：name-wall 布局自定义面板。
- `DialogService.cs`：对话框注入 helper；`LauncherDialogText.cs`：共享对话框文本。
- `Dialogs/DialogSharedStyles.axaml`：共享对话框样式；`Controls/StatusBar.axaml(.cs)`：状态栏。
- 对话框（均 `.axaml(.cs)`）：`CommandEditDialog`、`CommandManagementWindow`、`ConfirmBatchLaunchWindow`、`ConfirmEntryDeleteWindow`、`ConfirmLaunchWindow`、`ConfirmPresetDeleteWindow`、`DirectoryEditDialog`、`DirectoryManagementWindow`、`DiscoverAgentsWindow`、`EntryDetailsWindow`、`ImportMigrationWindow`、`PresetDetailsWindow`、`QuickLaunchWindow`、`RunHistoryWindow`、`UpdateCheckWindow`。

## 所有权

| 文件 | 职责 |
|------|------|
| `MainWindow.axaml` | XAML 布局（三视图、卡片模板、drag 视觉态） |
| `MainWindow.axaml.cs` | 构造、对话框注入、键盘导航、窗口生命周期 |
| `MainWindow.Drag.cs` | DragOver/Drop + ghost |
| `MainWindowEntryCardResolver.cs` | 视觉树 hit-test |
| `LauncherControlBar` | 批量控件 / 视图切换 |
| `LauncherEntriesView` | 条目列表 / 卡片宿主 |
| `*Window` / `*Dialog` | 每用户动作一窗 |
| `LauncherDialogText` / `DialogService` | 共享对话框 helper |
| `Controls/`、`Dialogs/` | 复用控件与共享样式 |

## Naming

主窗口 helper `MainWindow*.cs`；对话框 `Confirm*Window.cs` / `*DetailsWindow.cs` / `*EditDialog.axaml(.cs)`；View 层共享 `LauncherDialog*.cs`（`Launcher` 前缀）。

## Where to Put

| 新增 | 位置 |
|------|------|
| 新对话框 | `*Window.axaml(.cs)` 或 `*Dialog.axaml(.cs)` |
| 共享对话框逻辑 | `LauncherDialogText.cs` / `DialogService.cs` |
| drag 视觉 | `MainWindow.Drag.cs` |
| 键盘快捷键 | `MainWindow.axaml.cs` `OnWindowKeyDown` |
| 复用控件 | `Controls/` |
| XAML style/DataTemplate | `MainWindow.axaml` |
