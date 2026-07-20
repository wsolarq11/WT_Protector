# Implementation plan

## Stage 1 — Architecture safety

- [x] 完成 CQ-02，统一 mutation 的持久化与回滚边界。
- [x] 完成 CQ-01，并复用 FMA Phase 2/3 的 service extraction。
- [x] 完成 CQ-06，只在调用方确实需要分支的 service 边界引入 typed result。
- [x] 完成 CQ-07，为上述边界补公共行为契约测试。

## Stage 2 — Frequent workflows

- [x] 复用既有任务完成并验收 UX-01 frecency。
- [x] 复用既有任务完成并验收 UX-02 central management。
- [x] 复用既有任务完成并验收 UX-03 existing-directory quick create。
- [x] 完成 UX-04 contextual feedback。

## Stage 3 — Declarative and visual modernization

- [x] 完成 CQ-03 CommandManagementWindow XAML 化。
- [x] 完成 CQ-04 MainWindow XAML 模块化。
- [x] 完成 CQ-05 typed status content。
- [ ] 完成 V-01 token convergence、V-02 declutter、V-03 dialog consistency。

## Stage 4 — Production proof

- [ ] 完成 V-04 accessibility audit 与稳定自动化检查。
- [ ] 完成 V-06 responsive visual matrix。
- [ ] 完成 UX-05 first-run empty state。

## Per-child gate

1. 收敛 PRD；复杂任务补齐 design/implement。
2. `task.py start` 后加载 `trellis-before-dev` 与相关 package specs。
3. 通过 codebase-memory 追踪真实调用链，复用现有实现，完成最小正确差异。
4. 运行聚焦测试与 `trellis-check`；必要时更新 `.trellis/spec/`。
5. `git diff --check`，提交该子任务，归档并更新父任务证据。

## Final integration gate

- [ ] 逐项核对父 PRD 的 AC1–AC6 和 17 个子 PRD 的验收证据。
- [ ] 确认延期/拒绝项未发生范围漂移。
- [ ] 确认所有任务与 journal 已提交，工作树干净。
- [ ] 从干净 HEAD 运行 `git diff --check`。
- [ ] 从干净 HEAD 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`。

## Rollback points

- 每个子任务提交即一个回滚点。
- 配置 schema、持久化边界、启动安全和 XAML 资源解析失败时，不继续叠加后续阶段；先在当前子任务修复并重跑完整门禁。
