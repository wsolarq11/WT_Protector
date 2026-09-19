# Drag & Drop — Views Layer

> ghost 工厂、插入指示器、drag session 管理。FLIP 动画已移除（2026-06-23 B1）。

## 架构链

`MainWindow.Drag.cs`（事件：DragOver 更新指示器 / Drop 提交）→ `LauncherDragReorderWorkflow`（协调 ghost 布局 + reorder）→ `LauncherDragReorderSession`（全部 drag 状态唯一所有者）。

## Ghost 工厂

`MainWindow.Drag.cs`：克隆拖动卡片为 overlay，60-80% 透明度、同源卡片尺寸、固定指针偏移、全部颜色取自 `LauncherDesignTokens`。尺寸/偏移由 `LauncherDragGhostLayoutCalculator`（Core）计算。

## 插入指示器

状态由每个 `LauncherEntryViewModel` 的 `DragCueState` 管理。Token 所有权（DESIGN.md）：`drag-target` class 是颜色/圆角/透明度/hit-test 唯一源；三视图只改方向/尺寸/位置，不重定义颜色；`IsDragInsertBefore` / `IsDragInsertAfter` 是 `DragCueState` 只读投影。

## DragReorderSession

`LauncherDragReorderSession` 持有全部 drag 状态（snapshot / source / target / last preview），禁散字段。规则：一切 drag 状态在一个 `DragReorderSession?` 字段；想加 `_dragSource` / `_dragTarget` 就停手。测试：`LauncherDragReorderSessionTests.cs`、`LauncherDragReorderWorkflowTests.cs`。

## 键盘 reorder

`MoveUp()` / `MoveDown()` + `CanMoveUp()` / `CanMoveDown()` 是 drag 的键盘可访问替代（当前在 MainWindowViewModel 单文件）。

## 幽灵布局计算

`LauncherDragGhostLayoutCalculator`（Core 纯函数），测试 `LauncherDragGhostLayoutCalculatorTests.cs`。

## Forbidden Patterns

- ❌ drag 状态散在多个 VM 字段；用 `DragReorderSession`。
- ❌ 插入指示器颜色按视图复制；用 `drag-target` class。
- ❌ code-behind 硬编码 ghost 透明度/尺寸；用 `LauncherDragGhostLayoutCalculator`。
- ❌ ghost 颜色硬编码；用 `LauncherDesignTokens`（2026-06-23 Q7）。
- ❌ drag 是唯一 reorder 方式；保留 MoveUp/MoveDown。
- ❌ `_dragCardCache` fallback（2026-06-23 B2 删除）；用视觉 hit-test `GetVisualAt`。
- ❌ `CommitDragReorder` 成功后跳过 `ApplyFilter`；必须无条件 `ApplyFilter()`（卡片视图绑 `VisibleGroups`）。任何条件跳过须验证 `VisibleGroups` / `VisibleCount` / `IsVisibleEmpty` 等派生投影已重建。
