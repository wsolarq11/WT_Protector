Status: Archived
Purpose: 记录 2026-07-13 Codex 平台 Trellis 配置审计结果与本地定制。
Original scope: Trellis release + Codex lifecycle hooks 接线。

# Codex platform Trellis setup audit

## Outcome

项目使用当前稳定 Trellis release，两个必需 Codex lifecycle 入口点均已接线：

- `SessionStart` 加载 compact Trellis session/task/spec overview。
- `UserPromptSubmit` 加载 workflow-state breadcrumb。

## 关键证据

| Check | Result |
| --- | --- |
| Global package | `@mindfoldhq/trellis@0.6.6` |
| npm latest / project template | `0.6.6` |
| Codex CLI | `0.144.1` |
| Project trust | `d:\_ai_\_projects\wt_protector = trusted` |
| Trellis drift | `trellis update --dry-run` 仅报告本地 `.codex/hooks.json` 定制 |
| SessionStart / UserPromptSubmit | 均返回对应 `hookEventName` JSON |

## 本地定制

Trellis 0.6.6 生成了 `.codex/hooks/session-start.py` 但未在 `.codex/hooks.json` 注册。项目补上 `SessionStart` 注册，其余生成文件保持不变。hook 信任决策交给 Codex 官方 `/hooks` UI。

## 维护命令

```powershell
trellis --version
trellis update --dry-run
python ./.trellis/scripts/get_context.py
```

不要用 `trellis update --force`：`.codex/hooks.json` 已是本地定制，需人工 review 上游变更后合并。

## 复核注记（2026-08-15）

- 当前 `.codex/hooks.json` 仅注册 `UserPromptSubmit`（inject-workflow-state.py）与 `SubagentStart`（inject-subagent-context.py）。
- `SessionStart` 注册已于 2026-07-24（`d6280974`）移除，`session-start.py` 仍在但未挂载。
- 其余路径（`.codex/agents`、`.agents/skills`、`.trellis/scripts/get_context.py`）复核存在；逐项证据见 `.trellis/tasks/archive/2026-08/08-15-docs/ledgers/trellis-codex-setup-audit.md`。
