# 轻量清理:注释/改名/模板漏洞

## Goal

1) ErrorLog.EnsureLogDirectory L82-93 删除'Cached by directory path'陈旧注释(缓存已删);2) MainWindow.axaml.cs chrome 常量加 ponytail: 注释标注 recalibrate 触发条件;3) tests/test-quality.md L41 空字符串文件名模板漏洞修复;4) LauncherEntryVisibility 改名 LauncherEntryQuery(含 sort+filter+visibility 职责);5) 清理 (inline) 文件名中间态引用(views/quality-guidelines.md L128, drag-drop.md L17/28/36)。

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
