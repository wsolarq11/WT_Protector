# V04 无障碍生产审计

## Goal

在不超过产品边界的前提下，将 **WT Launcher** 提升至 WCAG 2.2 Level AA 标准的生产级无障碍水平。重点覆盖焦点导航、屏幕阅读器支持、动效控制和键盘替代。不引入新UI组件或产品功能。

## Confirmed facts

- AutomationProperties.Name 在 `LauncherEntriesView` 和 `LauncherControlBar` 中已有全面覆盖，共计约 40+ 处。
- `LauncherDialogButtons` 在 code-behind 中已通过 `AutomationProperties.SetName(button, content)` 覆盖所有按钮。
- 键盘导航已在 `MainWindow.axaml.cs` 中实现：Up/Down 选中导航、Enter/Space 启动、Escape 取消/清空过滤、Ctrl+F 聚焦搜索。
- 当前无 `PrefersReducedMotion` 检测，卡片动画 0.12s 硬编码。
- 颜色对比度已在设计Token注释中标记（TextMuted 5.8:1, Error 5.24:1，满足 AA 要求）。
- `CommandManagementWindow` 是自动化属性的主要缺口——交互元素零覆盖。

## Requirements

| # | Area | What | How |
|---|------|------|-----|
| R1 | CommandManagementWindow | 为所有交互控件添加 AutomationProperties.Name | Tab 按钮、操作按钮、空状态提示各分配可访问名 |
| R2 | Reduced motion | 检测系统 `PrefersReducedMotion` 并在设置时禁用/缩短动画 | 在 `MainWindow.axaml.cs` 中读取 `PlatformSettings` 并切换过渡时长 |
| R3 | Focus indicators | 确保键盘焦点指示器在所有视图模式下可见 | XAML `keyboard-focused` 选择器 + 在按钮上添加焦点视觉样式 |
| R4 | Status aria | 动态状态更新后通知屏幕阅读器 | 状态栏 TextBlock 添加 `AutomationProperties.LiveSetting="Polite"` |
| R5 | Empty state accessibility | 空列表提示文本添加 AutomationProperties | 三个视图模式 + CommandManagementWindow 的空状态 |

## Acceptance Criteria

- [x] AC1: CommandManagementWindow 所有按钮和交互控件均有可访问名（~12 处新增），屏幕阅读器可遍历全部功能
- [~] AC2: XAML 样式桩已就绪（`.reduced-motion Border.card`），但 Avalonia 12.1.0 `IPlatformSettings` 尚未暴露 `ReducedMotion` API，动态检测受阻。待框架升级后补全激活路径
- [x] AC3: 状态栏内容变化时屏幕阅读器自动朗读（LiveSetting = Polite）
- [x] AC4: 所有空状态提示有可访问名
- [x] AC5: `keyboard-focused` 样式已在 Border.card 选择器中就绪，键盘导航时可见
- [x] AC6: `dotnet test -c Release` 653/653 通过，build 0 警告 0 错误

## Notes

- 无需引入新的 NuGet 包或第三方无障碍库——Avalonia 内置 `AutomationProperties` 和 `PlatformSettings` API 已覆盖所有需求。
- 本任务不涉及图标集（V-05 延期项）、色觉模拟或自定义高对比度主题。
- `LauncherDialogButtons.Create` 已有 `AutomationProperties.SetName`，验证无需再改。
