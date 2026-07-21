# Stage 5: LaunchOrchestrator 提取

## Goal

将 LaunchAsync/BatchLaunchAsync/DryRun/ShowDryRun/GetEntryLaunchCommand 从 MainWindowViewModel 提取到独立 LaunchOrchestrator 类。纯编排，不持有状态。依赖 EntryManager + PresetManager 的公开接口。事件：LaunchCompleted, BatchCompleted。失败半径：大（启动失败抛异常，不会吞掉 UX 反馈）

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
