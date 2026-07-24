# P1: 状态单源化 — 消除 _settings 字段

## Goal

Eliminate MWVM._settings and DragWorkflow._settings fields. All reads through AppStore.Select, all writes through Dispatch. Route TrySaveWindowBounds through AppStore.

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
