# Stage 7: Partial 整理 + 任务同步 + Spec 更新

## Goal

完成 MWVM 重构的最后清扫。\n\n1. 将小型 partial 文件的 RelayCommand 代码合并到 MainWindowViewModel.cs\n   - 目标: 删除已清空的 partial 文件\n2. 更新 6 个子任务 + 父任务状态为 completed\n3. 更新 .trellis/spec/ 反映 6-Manager 架构\n4. dotnet test 全绿验证\n5. verify.ps1 门禁验证

## Requirements

- TBD

## Acceptance Criteria

- [ ] TBD

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
