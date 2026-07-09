# 重写 5 个严重过时 spec

## Goal

重写或归档:persistence.md(397行,多处代码示例与当前相反)、core/directory-structure.md(140行,虚构 9 partial 文件 + IStore)、error-handling.md(223行,引用不存在的 Save/RestoreSnapshotOnPersistenceFailure)、quality-guidelines.md(172行,LiteDB ponytail 注释 + 测试数过时)、views/directory-structure.md(59行,文件名重复列出)、drag-drop.md(96行,引用不存在的 Reorder.cs)。目标:让 spec 与当前代码一致,停止误导。

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
