# V03 对话框视觉与语义一致性

## Goal

让所有生产对话框复用同一标题、外边距、footer 节奏和默认按钮语义，消除程序化窗口之间的可见漂移与危险操作焦点冲突。

## Confirmed Facts

- `LauncherDialogText` 与 `LauncherDialogButtons` 已是共享 primitive，大多数窗口已经消费它们；不需要新组件库或全量 XAML 化。
- 标题字号当前存在 16/20/22 三档，外层 padding 存在 20/22/24 三档，footer 顶距存在 0/12/14/16/18/24 多档。
- `ConfirmEntryDeleteWindow` 与 `ConfirmPresetDeleteWindow` 将取消设为默认按钮、删除设为非默认按钮，却在 Opened 时聚焦删除按钮；键盘 Enter 的实际焦点语义与安全默认意图冲突。
- 保存、编辑、启动等安全确认使用 primary 默认按钮是现有正确模式。

## Requirements

- R1：新增最小稳定 dialog token：标题字号 20、外层 padding 24、footer 顶距 14；Core 保持无 Avalonia 依赖。
- R2：`LauncherDialogText.Title`、App resource 注入和 CommandManagementWindow XAML 消费同一标题字号权威值。
- R3：生产 dialog/window 的主内容外层统一为 24；局部列表、输入和卡片 padding 不受影响。
- R4：所有独立 footer 使用 14 顶距；若父 Grid 的 RowSpacing 已提供 14 间距，footer 自身保持 0，禁止双重间距。
- R5：危险删除对话框必须让取消按钮同时拥有 `IsDefault=true`、`IsCancel=true` 和初始焦点；删除按钮必须非默认。
- R6：安全保存/启动/确定操作继续使用 primary 默认按钮，不改变返回值、确认流程或业务命令。
- R7：不同时执行 V-04 全量无障碍审计、不把所有程序化窗口迁移到 XAML、不增加依赖。

## Acceptance Criteria

- [ ] AC1：标题字号、外层 padding 与 footer 顶距分别只有一个共享权威值和生产消费者。
- [ ] AC2：所有目标窗口的标题和主内容节奏一致，CommandManagementWindow 不再保留独立 20/22 literals。
- [ ] AC3：两个危险删除窗口默认/取消/初始焦点均指向取消，删除为非默认 destructive。
- [ ] AC4：结构契约测试枚举所有目标窗口，防止 16/22 标题、20/22 外层 padding、非标准 footer 顶距回归。
- [ ] AC5：现有 dialog 返回值、ViewModel 命令、持久化和状态内容均未修改。
- [ ] AC6：没有新增依赖、主题框架、窗口重写或 V-04/V-05 范围。
- [ ] AC7：Release tests、git diff --check、tools/verify.ps1、publish 与 GUI smoke 全绿。

## Out of Scope

- 对话框宽高统一（内容量不同，必须保持各自尺寸）。
- 内部卡片/列表/输入控件 padding 统一。
- 全量 AutomationProperties、Tab 顺序、对比度与 reduced-motion 审计（V-04）。
- 将 DirectoryManagementWindow、DiscoverAgentsWindow 等重写为 XAML。
