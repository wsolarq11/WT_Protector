# Drag & Drop — Views Layer

> ghost 工厂、插入指示器、drag session 管理。FLIP 动画已移除（2026-06-23 B1）。

## 架构链

`MainWindow.Drag.cs`（事件：DragOver 更新指示器 / Drop 提交）→ `LauncherDragReorderWorkflow`（协调 ghost 布局 + reorder）→ `LauncherDragReorderSession`（全部 drag 状态唯一所有者）。

## 拖拽起始（Pointer capture + threshold）

三视图拖拽命中区统一为整张 `Border.card`（非独立手柄 well）：`PointerPressed/Moved/Released` 接在卡片上。按下仅「待定」候选，指针越过 `MainWindowDragThreshold.ThresholdPx`（唯一源，默认 4px）才 `BeginDrag`；点击/双击不受影响（`MainWindowCardGesture.IsGestureEligible` 排除 `Button`/`CheckBox`）。

- 核心等式（`MainWindowCardGestureTests.CursorRegion_EqualsGestureRegion` 锁定）：`CursorRegion == DragRegion == DoubleTapLaunchRegion == card − ExcludedControlTypes`。三域合一，靠 `MainWindowCardGesture.ExcludedControlTypes` 单一源 + 光标样式两端同源推导，禁止各自复制。
- 旧 `entry-drag-handle` 命中框与六点 `IconGripVertical` 图标已删——前者制造 32×32 / 18×18 跨视图命中框漂移与「绘制框≠命中框」隐形环；后者是「把手」隐喻，与「整卡即热区」矛盾（任何固定位置的 grip 都不可能位于整卡热区中心），留着就是视觉锚点≠热区的误导。
- 拖拽 affordance 单一源 = 整卡：hover 上浮（`CardHoverLiftTransform`）+ `Cursor=SizeAll`（四向移动光标，光标位置==热区位置）。
- 光标单一源在 `Border.card` 样式；卡片内 `Button`/`CheckBox` 显式 `Cursor=Arrow` 钉回点击/勾选语义，不继承 SizeAll（Avalonia `Cursor` 是 `inherits:true`，子控件 null 会向上泄漏）。

- 阈值单一源：`MainWindowDragThreshold.ThresholdPx`；测试 `MainWindowDragThresholdTests.cs`。
- 候选拖拽字段 `_pendingDrag` 是唯一 pending 源；`OnEntryDragPointerReleased` 与「按钮未按下」分支都清空它，防死候选 re-arm。
- ghost 起始位置取候选起点 `Point`，不再依赖 `PointerPressedEventArgs`。

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
- ❌ 恢复 `entry-drag-handle` 独立命中框；整卡即命中区，threshold 是唯一起始闸。
- ❌ 恢复 grip 图标；affordance = 整卡 hover 上浮 + `Cursor=SizeAll`。
- ❌ 硬编码拖拽起始阈值；用 `MainWindowDragThreshold.ThresholdPx`。
- ❌ `_dragCardCache` fallback（2026-06-23 B2 删除）；用视觉 hit-test `GetVisualAt`。
- ❌ `CommitDragReorder` 成功后跳过 `ApplyFilter`；必须无条件 `ApplyFilter()`（卡片视图绑 `VisibleGroups`）。任何条件跳过须验证 `VisibleGroups` / `VisibleCount` / `IsVisibleEmpty` 等派生投影已重建。
