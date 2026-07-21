# Stage 4: EntryManager 提取

## Goal

将 _entries/_visibleEntries/_totalCount/_visibleCount/_selectedCount/_keyboardFocusedEntryId/_entryListViewModel 及全部 entry CRUD/ReplaceEntries/RefreshSelectionState/ApplyFilter callback 从 MainWindowViewModel 提取到独立 EntryManager 类。拆掉 EntryListBridge（17 lambda 消失）。事件：EntriesChanged, SelectionChanged, CountChanged。失败半径：大（持久化失败抛异常）

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
