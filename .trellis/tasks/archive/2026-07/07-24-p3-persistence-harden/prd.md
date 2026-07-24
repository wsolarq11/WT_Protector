# P3: 持久化加固 — 消除崩溃窗口与竞态

## Goal

Fix RotateBackups ordering, add SaveLock to CleanupStaleTempFiles, add Load<T> read synchronization, extend recovery chain to search .bak.1/.bak.2

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
