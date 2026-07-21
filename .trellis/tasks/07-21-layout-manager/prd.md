# Stage 3: LayoutManager 提取

## Goal

将 _selectedSortMode/_selectedDisplayMode/_filterText/_cardsGroupKey 及全部排序/分组/过滤/视图模式/卡片布局逻辑从 MainWindowViewModel 提取到独立 LayoutManager 类。事件：SortChanged, FilterChanged, ViewModeChanged, GroupKeyChanged。不持有 entry 数据本身。失败半径：中（抛出无效排序/过滤参数）

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
