# Codex 平台 Trellis 完整安装配置

## Goal

让本项目的 Codex 平台 Trellis 集成达到“已安装、已接线、可触发、可验证、可维护”的状态，而不修改全局 npm 包或 Trellis 上游源码。

## Requirements

- 使用官方 `@mindfoldhq/trellis` 稳定版本和 `trellis update` 路径。
- 保留项目已有 Trellis 用户数据和本地定制，不使用强制覆盖。
- Codex 必须信任本仓库并启用 stable hooks 功能。
- `SessionStart` 必须注入 Trellis 会话概览；`UserPromptSubmit` 必须注入当前 workflow-state。
- `.agents/skills/`、`.codex/agents/`、`.trellis/workflow.md` 与 `.trellis/config.yaml` 必须语义一致。
- 默认保持 `codex.dispatch_mode=inline`，避免无必要的隔离式子代理上下文复制。

## Acceptance Criteria

- [x] npm latest、全局安装、项目模板版本均为 `0.6.6` 或当时最新稳定版。
- [x] `trellis update --dry-run` 报告模板版本一致、无 `.new` 冲突文件，并识别 intentional local customization。
- [x] `codex --version` 支持 stable hooks，用户配置已启用 hooks，项目被标记 trusted。
- [x] `.codex/hooks.json` 同时注册 `SessionStart` 与 `UserPromptSubmit`。
- [x] 两个 hook 可在 Windows UTF-8 环境手工运行并返回合法 JSON/预期上下文。
- [x] Trellis task/context 命令可读取开发者、任务、spec 和 git 状态。

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
