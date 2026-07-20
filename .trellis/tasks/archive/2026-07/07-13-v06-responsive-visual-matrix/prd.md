# V06 响应式视觉验真矩阵

## Goal

在不改变现有布局算法的前提下，建立 WT Launcher 的响应式视觉验证矩阵，涵盖三视图模式在窗口最小/默认/宽屏下的布局正确性、文本溢出处理和列数计算。

## Confirmed facts

- 布局算法已在 `LauncherEntryLayoutCalculator` 中实现（CardMinWidth=200, NameTileMinWidth=132, golden ratio 裁剪）
- 三视图模式（Cards/Details/Names）均有独立的布局计算
- 窗口约束：Default 1180×760, Min 940×600（由 `LauncherDesignTokens` 定义）
- Details 视图列比例：Name:Directory:Command = 2:2:3
- All layout calculators are pure functions in Core layer — testable without UI

## Requirements

| # | Area | What | How |
|---|------|------|-----|
| R1 | Window sizing matrix | 三视图在 Min/Default/Wide 窗口宽度下的布局正确性 | 单元测试矩阵覆盖 |
| R2 | Text overflow | 长名称/目录/命令在 Details & Names 视图的截断行为 | TextTrimming CharacterEllipsis 验证 |
| R3 | Cards column count | 不同可用宽度下的卡片列数计算正确 | 验证 ColumnCount 与 availableWidth 的关系 |
| R4 | Name tiles column count | 不同可用宽度下的名称瓦片列数计算正确 | 验证 Columns 与 availableWidth 的关系 |
| R5 | Details header sync | 表头列宽与数据行列宽一致 | 列宽计算验证 |

## Acceptance Criteria

- [ ] AC1: `LauncherEntryLayoutCalculator` 在 Min/Default/Wide 三种宽度下返回正确的 ColumnCount 和卡片尺寸
- [ ] AC2: Name tiles 在 Min/Default/Wide 三种宽度下的 columns 计算正确
- [ ] AC3: 所有视图模式在最小窗口宽度（940）下无水平滚动条（内容折叠到可用宽度内）
- [ ] AC4: 长文本溢出使用 CharacterEllipsis，不破坏布局
- [ ] AC5: Details 视图列比例（Name:Directory:Command = 2:2:3）保持不变
- [ ] AC6: All existing tests still pass (653+)
