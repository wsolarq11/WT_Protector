# 删 LauncherPresetFile 冗余抽象 + 改名 LauncherPresetFiles

## Goal

删 LauncherPresetFile(LauncherPreset.cs:8-21,VM 中间容器,已冗余,因 LauncherStoreFile 已统一 entries+presets+settings+defaultPresetId)。MainWindowViewModel 直接用 store 的 Presets + DefaultPresetId。LauncherPresetFiles 静态类改名为 LauncherPresetValidator(实际只做 Validate)。同步改 LauncherPresetWorkflow / LauncherEntryMutationSnapshot 引用。约删 60-80 行 + 命名清晰。

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
