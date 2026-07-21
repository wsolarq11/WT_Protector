# Stage 6: Orchestrator 组装 + Partial 清理

## Goal

将 MainWindowViewModel 裁剪为纯编排者（~500行）：构造 6 个 Manager，注册事件拓扑。删除 12 个 partial 文件。确认全部 670 测试通过。失败半径：全部委托给各个 Manager，本层不新增 catch。文件：删除 Batch/CommandManagement/DirectoryManagement/Discovery/Drag/Entries/Grouping/IEntryCardHost/IEntryListViewModel/Launch/Layout/Messaging/Presets/Reorder.cs

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
