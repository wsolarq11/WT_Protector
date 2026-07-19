# P4-拖拽/布局提取

## Goal

评估拖拽排序和布局管理从 MainWindowViewModel 提取到独立模块的必要性。

## Decision

**跳过。** 拖拽工作流 (`LauncherDragReorderWorkflow`) 已是独立类 (197行)。布局逻辑 (`Layout.cs` 408行) 与 shell 状态 (`_settings`、`_entries`、`_visibleEntries`、`_sortModes` 等) 紧耦合，提取需要建大量桥梁回调而收益有限。当前模块化程度已满足架构目标。
