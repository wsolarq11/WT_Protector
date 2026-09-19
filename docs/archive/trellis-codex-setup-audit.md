# Codex platform Trellis setup audit

Date: 2026-07-13

> 复核（2026-08-15）：本文为 2026-07-13 当日状态记录。当前 `.codex/hooks.json` 仅注册 `UserPromptSubmit`（inject-workflow-state.py）与 `SubagentStart`（inject-subagent-context.py）；`SessionStart` 注册已于 2026-07-24（提交 d6280974）移除，`session-start.py` 脚本仍在但未挂载。其余路径（`.codex/agents`、`.agents/skills`、`.trellis/scripts/get_context.py`）复核均存在。当日逐项证据见验证台账 `.trellis/tasks/archive/2026-08/08-15-docs/ledgers/trellis-codex-setup-audit.md`。

## Outcome

The project uses the current stable Trellis release and now has both required Codex lifecycle entry points wired:

- `SessionStart` loads the compact Trellis session/task/spec overview.
- `UserPromptSubmit` loads the current workflow-state breadcrumb.

## Evidence

| Check | Result |
| --- | --- |
| Global package | `@mindfoldhq/trellis@0.6.6` |
| npm latest | `0.6.6` |
| Project template | `0.6.6` |
| Codex CLI | `0.144.1` |
| Codex hooks feature | `stable`, enabled in user config |
| Project trust | `d:\_ai_\_projects\wt_protector = trusted` |
| Trellis drift | `trellis update --dry-run` reports only the intentional local `.codex/hooks.json` customization |
| Sidecar conflicts | no `*.new` files |
| SessionStart script | returns JSON with `hookEventName=SessionStart` |
| Per-turn script | returns JSON with `hookEventName=UserPromptSubmit` |
| Task runtime | developer, git state, active task tree, journal and spec indexes resolve successfully |

## Intentional local customization

Trellis 0.6.6 generated `.codex/hooks/session-start.py` but did not register it in `.codex/hooks.json`. The project adds the missing `SessionStart` registration and otherwise retains the generated files unchanged.

Codex treats project-local hooks as trusted-project configuration. After this file changes, a fresh interactive Codex session may require one `/hooks` review before the new hook runs. That trust decision is intentionally left to Codex's official hook-trust UI rather than hand-editing internal trusted hashes.

## Maintenance commands

```powershell
trellis --version
trellis update --dry-run
python ./.trellis/scripts/get_context.py
'{"cwd":"D:/<PROJECT_ROOT>/WT_Protector"}' | python -X utf8 .codex/hooks/session-start.py
'{"cwd":"D:/<PROJECT_ROOT>/WT_Protector"}' | python -X utf8 .codex/hooks/inject-workflow-state.py
```

Do not use `trellis update --force`: `.codex/hooks.json` is now an intentional local customization. Review future upstream changes and merge them deliberately.
