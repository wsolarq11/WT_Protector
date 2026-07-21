# Stage 1: StatusPresenter 提取

## Goal

将 _statusContent/_dryRunText/_presetSelectionStatusText/_applyPresetButtonText 及全部 status 投影属性从 MainWindowViewModel 提取到独立 StatusPresenter 类。公开 LauncherStatusContent？事件，被 Orchestrator 注册到主 VM。失败半径：零（静默）

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
