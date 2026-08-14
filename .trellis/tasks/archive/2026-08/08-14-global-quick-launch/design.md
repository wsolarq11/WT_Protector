# 全局快速调用 — Design

## Change Boundary

- 当前只能打开主窗口后搜索/启动；没有托盘、全局热键或独立快速启动 UI。
- 目标：托盘常驻、全局热键唤起快速启动窗口、输入即启动；启动仍走现有 LaunchPlan/preflight/confirmation/receipt。

## Data Flow

```text
settings.quickLaunchHotkey / settings.runInTray
  -> Core LauncherHotkey.Parse / LauncherQuickMatch.Score
  -> Win32HotkeyService (RegisterHotKey) / Win32TrayIcon (Shell_NotifyIcon)
  -> QuickLaunchViewModel (filter+rank over MainWindowViewModel.Entries)
  -> QuickLaunchWindow (Enter launch / Esc dismiss)
  -> existing LauncherEntryViewModel.LaunchCommand
```

## Key Decisions

- 托盘与全局热键用 Win32 P/Invoke 自实现，不引入第三方包，保持 Native AOT 和最小依赖。
- 快速启动窗口只做入口，不复制启动逻辑；`LauncherEntryViewModel.LaunchCommand` 是唯一启动出口。
- 热键解析与模糊评分放 Core，App 层只负责 Win32 接线和 UI。
- 新 settings 字段为可选字段，缺省值兼容现有 `launcher.json`。
- 关闭主窗口时若托盘启用则隐藏而非退出；托盘“退出”才是真正退出。

## Files Expected to Change

- Core: `LauncherHotkey.cs`、`LauncherQuickMatch.cs`、`LauncherSettings.cs`
- App Platform: `Win32HotkeyService.cs`、`Win32TrayIcon.cs`
- App ViewModels: `QuickLaunchViewModel.cs`
- App Views: `QuickLaunchWindow.axaml` + code-behind
- App wiring: `App.axaml.cs`
- Tests: Core parser/ranking + QuickLaunchViewModel + XAML/settings contract

## Explicitly Not Doing

- 不做登录自启、剪贴板工作流、语音、移动端。
- 不重写现有主窗口搜索。
- 不引入 broad launcher/plugin 平台能力。