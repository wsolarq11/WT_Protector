# Codex Trellis 完整配置与理想典范差距审计

## Goal

按 Trellis 官方工作流完成本项目的 Codex 平台集成，并基于可追溯源码与现有设计研究，形成 WT Launcher 相对“理想典范”的代码质量、用户视觉和用户实操全维度差距清单与可执行重构路线。

## Requirements

- 先完成并验证子任务 `07-13-codex-trellis-setup`，再开展差距审计。
- 使用当前仓库、本机 Codex/Trellis 配置、官方 OpenAI Codex 文档、Trellis CLI 实际输出作为安装证据。
- 使用当前 WT Launcher 源码、测试、设计契约和已有外部研究作为本项目证据。
- “理想典范”采用复合基准，不把任一外部项目整体复制为目标：代码质量以本仓 5 层/FMA 契约为主，视觉以 CodexSwitchUI、Fluent/Windows 等成熟组件体系为参照，实操以现有 12 类 launcher/workspace 基准为参照。
- 审计必须区分应立即修复、可分阶段重构、仅在真实需求出现后再做三类，遵守 YAGNI 和最小变更原则。
- 输出必须包含证据、影响、可行性、依赖关系、风险、建议优先级和验证方式。

## Acceptance Criteria

- [x] 两个子任务均有收敛后的 `prd.md`、`design.md`、`implement.md` 和可验证交付物。
- [x] Codex/Trellis 集成的版本、项目信任、hooks 开关、hook 信任机制、skills、agents、任务脚本和官方 update 状态均有证据。
- [x] 差距审计覆盖架构、可维护性、测试、错误处理、性能、视觉层级、组件一致性、无障碍、响应式、发现性、反馈、批量流程和配置/发布实操。
- [x] 重构清单不引入未经需求证明的平台化、插件化或新依赖。
- [x] 最终全仓通过 `git diff --check` 与 `tools/verify.ps1`。

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
