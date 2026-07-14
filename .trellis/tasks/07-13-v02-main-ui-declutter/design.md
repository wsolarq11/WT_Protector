# Design — V02 Main UI Declutter

## Boundary

改动仅位于 `LauncherControlBar.axaml` 和结构契约测试。现有 `MainWindowViewModel` 命令、`IDialogService`、管理窗口和持久化边界保持不动。

## Hierarchy

- Primary: 一键启动已勾选、新增按钮。
- Direct secondary: 发现、确认开关、Cards 分组、启动组应用/预演/启动。
- Overflow: 选择与刷新、数据管理、启动组维护。
- Passive status: 已勾选计数、可见/总数、启动组选择状态。

每个 overflow 入口使用带文字的 `Button`，其 `ContextMenu` 使用原有 `ICommand` 绑定。点击入口时由 `LauncherControlBar` code-behind 打开自身 ContextMenu；这属于窗口交互路由，不引入业务判断。

## Compatibility

- 菜单项保留明确中文 Header 与 `AutomationProperties.Name`。
- 所有命令实例仍来自同一 `MainWindowViewModel` DataContext。
- 不删除或重命名任何命令，因此测试和外部行为兼容。

## Rollback

若 ContextMenu DataContext 或键盘打开行为不满足 Avalonia 运行时，回滚到原按钮布局；不得通过复制命令或新增代理 ViewModel 绕过绑定。
