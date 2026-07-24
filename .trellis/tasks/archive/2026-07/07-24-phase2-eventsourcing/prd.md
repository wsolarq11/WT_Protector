# Phase 2: Event Sourcing

## Requirements

1. **EventJournal** — Append-only event log with crash-safe writes.
2. **LauncherEvent types** — ADT of all state-changing events.
3. **LauncherStoreProjector** — Pure function: events -> LauncherStoreFile.
4. **Snapshot support** — Periodic snapshots for fast recovery.
5. **Backward compatibility** — Existing launcher.json still readable.

## Acceptance Criteria

- [ ] EventJournal.Append writes event to disk before returning
- [ ] LauncherStoreProjector correctly projects events to LauncherStoreFile
- [ ] dotnet build -warnaserror passes
- [ ] Tests pass: no regression

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
