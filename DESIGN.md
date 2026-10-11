Status: Active
Purpose: 设计契约：品牌、视觉语言、组件所有权、Token 与无障碍/响应式规则。
Update when: UI/UX、视觉 Token 或组件所有权变化时。

# Design

## Evidence

- Last refreshed: 2026-07-23
- Product surfaces: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` + code-behind + `ViewModels/*`
- Evidence: `src/avalonia-launcher/README.md`, `docs/adr/design-benchmark-digest.md`, mainline implementation, gate scripts, Microsoft Fluent 2 / Apple HIG / Material layout guidance

## Brand

- Personality: 清晰、克制、可信、工具型；快速找到、勾选、启动
- Trust signals: 可见配置路径、可验证发布物、统一确认策略、可回滚本地配置
- Avoid: 花哨装饰、不可读缩放、功能隐藏、拖拽黑盒

## Product Goals

- 快速启动保存的工作空间/命令
- 卡片在窗口变化时尽量直接展示常用动作
- 拖拽排序有跟随感、插入感、结果预演
- 三种视图各司其职：卡片=可读操作，详细列表=字段扫描，名称墙=一眼看全
- 启动确认是全局二元开关，不判断命令危险等级
- 顶部搜索、排序、视图、启动组四框同一行对齐

Non-goals: 不做跨平台设计系统；不迁移状态到 AppData/registry；不引入替代门禁的额外依赖。

## Visual Language

视觉系统遵循三条规则（画布中性、单一强调色、焦点不散）。唯一设计源与完整实现映射见 `docs/spec/visual-perspective-system.md`——颜色/间距/圆角/层级/响应式的具体值、决策阶梯、变化闭包均在该文件单一维护，本文不重复。

## Components

Reuse: `Border.card`、确认对话框、`ContextMenu`、右键菜单。

New/changed:

- 顶部 overflow 菜单承载高频动作；选择/刷新、数据管理、启动组维护用可键盘打开的菜单
- 右键复制：深拷贝为新条目，新 `Id`，名称追加 ` (副本)`，取消选择
- 拖拽 ghost（半透明覆盖层卡片）与插入指示（左/右高亮线）
- 拖拽排序命中区 = 整个卡片：删除独立手柄/六点 grip 图标（它标记「把手」却与「整卡热区」矛盾），affordance 由整卡 hover 上浮 + `Cursor=SizeAll` 单一承担；指针超过 4px 阈值后整行进入拖拽，点击/双击不受影响（`MainWindowDragThreshold.ThresholdPx` 单一源）
- 三档密度：Normal / Compact / Micro
- Cards / Details / Names 三视图
- 四框控制行：搜索、排序、视图、启动组，单个父 Grid 管理，列比例 `1.8* / 1* / 1* / 2.8*`
- 搜索 AutoCompleteBox：`searchHistory` 下拉，Enter 写入历史
- 全局启动确认 ToggleSwitch：立即保存，所有启动项同一规则
- 强调色选择器 AccentColorWindow：用户可调 accent 并编辑 12 色古典色板，立即持久化并热刷新

## Token / Component Ownership

- 密度唯一可变源：`LauncherEntryLayoutDensity`；`IsCompactLayout` / `IsMicroLayout` 是只读投影
- 三视图唯一可变源：`LauncherEntryViewMode`；`IsCardsViewMode` / `IsDetailedListMode` / `IsNamesViewMode` 是只读投影
- 排序唯一可变源：`SelectedSortMode`；`IsCustomSortMode` 是只读投影
- 拖拽提示唯一 entry-local 源：`DragCueState`；`IsDragPreviewTarget/Source/InsertBefore/InsertAfter` 是只读投影
- 拖拽排序唯一源：`DragReorderSession?`；snapshot/source/target/last preview 不散落
- 选择写入只有两个入口：用户勾选 `IsSelected` 立即保存；批量/预设/编辑 `SetSelectionFromOwner` 批量保存一次
- 拖拽视觉 token 集中在 MainWindow.axaml 两个共享样式：`Border.card.drag-target` 与 `Border.drag-insert`；三视图只保留方向/尺寸/定位差异
- 拖拽起始阈值唯一源：`MainWindowDragThreshold.ThresholdPx`；三视图拖拽命中区统一为整卡（`PointerPressed/Moved/Released` 于 `Border.card`）
- 拖拽光标单一源：`Border.card` 样式 `Cursor=SizeAll`；卡片内 `Button`/`CheckBox` 显式 `Cursor=Arrow` 钉回点击语义，不继承 SizeAll；不保留 `entry-drag-handle` 命中框与 grip 图标
- 卡片手势排除集单一源：`MainWindowCardGesture.ExcludedControlTypes`；`CursorRegion == DragRegion == DoubleTapLaunchRegion == card − 排除集`（契约测试锁定），双击启动与拖拽都经 `IsGestureEligible` 判定

## Accessibility

- 桌面工具可读、可点、可理解，不依赖颜色单独传达
- 键盘：主操作通过按钮/菜单完成；“⋯”是标准 Button，Space/Enter 打开同一 ContextMenu
- 拖拽是加速路径，不是唯一入口
- 全局确认开关可聚焦、可键盘切换、短标签说明状态
- 仅短过渡，避免眩晕动画

## Responsive

- 先收敛信息，再压缩字号/间距，最后允许滚动兜底
- 卡片尺寸变化自动切 Normal / Compact / Micro
- 详细列表优先压纵向高度，不显示更新时间
- 名称墙按可用宽度算列数：窄窗 1-2 列，常规窗 3+ 列，超长名称省略 + Tooltip
- 四框行单个 Grid 占满内容区，共享上下边线，不因形变错位

## Implementation Constraints

- Avalonia 12 + 现有 code-behind / MVVM / XAML；控件主题复用 Semi.Avalonia（`SemiTheme`），不新增独立设计系统，仅保留 accent/destructive 两个项目语义色类覆盖在 Semi 模板之上
- DragOver 只在目标或方向变化时更新，避免无差别重排
- 发布/verify receipt 至少记录 `WTLauncher.App.dll` hash；exe 只是 apphost 外壳
