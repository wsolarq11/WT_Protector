# 拆 MainWindowViewModel god class

## Goal

把 2796 行 MainWindowViewModel.cs 拆成 partial 文件,按 spec directory-structure.md L73-85 已规划的方向:MainWindowViewModel.{Entries,Launch,Selection,Layout,Presets,Presets.Persistence,Drag,Reorder}.cs。委托更多职责给已存在的 LauncherLaunchWorkflow / LauncherDragReorderWorkflow / LauncherPresetWorkflow。目标:每个 partial < 400 行,主 VM < 800 行。这是最大工作量 child,放最后。

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
