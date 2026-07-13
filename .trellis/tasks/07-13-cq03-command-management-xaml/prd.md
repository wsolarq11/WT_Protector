# CQ03 CommandManagementWindow XAML化

## Goal

把 `CommandManagementWindow` 的稳定布局、tab、列表行和空状态迁移到编译型 XAML/DataTemplate，让 code-behind 只保留窗口生命周期和编辑子对话框路由，同时保持现有管理行为与持久化契约不变。

## Confirmed Facts

- 当前 `CommandManagementWindow.cs` 约 432 行，构造函数、`BuildCommandRow`、`BuildTemplateRow`、`Render*Rows` 和 `SyncTab` 手工构造并同步视觉树。
- `CommandManagementViewModel` 已拥有 commands、templates、active tab、status、empty-state projections 与 mutation callback，不需要新 service 或状态容器。
- `MainWindow` 是唯一窗口调用方；`DialogService` 构造 VM 和 Window。
- Rename/Add/Edit 子对话框仍属于 View 生命周期职责，迁移不应把 `Window` 引入 ViewModel。

## Requirements

- R1：新增 `CommandManagementWindow.axaml`，用编译绑定声明标题、tab、toolbar、命令列表、模板列表、空状态、状态栏与关闭按钮。
- R2：命令和模板行必须使用 DataTemplate/ItemsControl（或 ListBox），不在 code-behind 中逐行创建控件或监听集合重绘。
- R3：ActiveTab、empty state、count、status 和 button enablement 直接绑定现有 ViewModel projections；不得建立镜像 UI state。
- R4：code-behind 仅保留 `InitializeComponent`、窗口配置、关闭与打开四类编辑子对话框的事件路由。
- R5：现有 `CommandManagementEdit` callback、snapshot/save/rollback、主列表即时刷新和所有文案行为保持兼容。
- R6：所有颜色、圆角、间距优先复用现有 resources/tokens；不新增通用组件库或依赖。
- R7：删除迁移后无用的 `ToolButton`、`EmptyHint`、`LayeredArea`、`Render*Rows`、`Build*Row`、`SyncTab` 和手工 PropertyChanged 同步代码。

## Acceptance Criteria

- [x] AC1：存在编译型 `CommandManagementWindow.axaml`，窗口能由 `InitializeComponent` 正常加载。
- [x] AC2：命令与模板集合各由一个 DataTemplate 渲染，新增/重命名/删除后集合通知自动更新 UI。
- [x] AC3：tab 切换、空状态、计数、status、可用性均由 ViewModel 绑定驱动，没有 code-behind 镜像字段。
- [x] AC4：四类编辑子对话框仍可触发对应 ViewModel command；关闭窗口行为不变。
- [x] AC5：旧命令式视觉树 helper 全部删除，code-behind 明显缩减且无业务判断。
- [x] AC6：现有 command-management contract/integration tests 全绿，并新增 XAML 结构契约覆盖 DataTemplate、binding 与 automation name。
- [x] AC7：`git diff --check`、Release tests、`tools/verify.ps1`、publish 与 GUI smoke 全部通过。

## Out of Scope

- 重写 `CommandManagementViewModel` 或 mutation/persistence 逻辑。
- 将 `CommandEditDialog` / `TemplateEditDialog` 同步迁移为 XAML；由 V-03 对话框一致性处理。
- 把 DirectoryManagementWindow 一并迁移；除非复用同一现有 primitive 可一行完成，否则保持独立任务边界。
