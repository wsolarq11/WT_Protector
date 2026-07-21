# Stage 2: PresetManager 提取

## Goal

将 _presets/_presetFile/_presetCount/_selectedPreset 及全部 preset CRUD（ApplyPreset/AddPreset/RenamePreset/DeletePreset/SetDefaultPreset/GetPresetReferenceCount）从 MainWindowViewModel 提取到独立 PresetManager 类。事件：PresetsChanged, SelectedPresetChanged。失败半径：大（持久化失败抛异常）

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
