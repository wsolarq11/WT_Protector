# Trellis on DeepSeek Harness (dsh)

- Status: Active
- Purpose: dsh 平台的 Trellis 接入说明
- Update when: dsh 能力或技能布局变化时

dsh 是 class-2 pull-based 宿主：没有 session-start hook 自动注入，agent 通过 skill loader 按需加载 Trellis 技能。

| Capability | Status |
| --- | --- |
| Skills `.agents/skills/trellis-*/SKILL.md` | 可用，dsh 原生发现 |
| Entry skills `.dsh/skills/trellis-*/SKILL.md` | 可用，最高优先级 |
| Context hooks | 无，pull-based 直读 `.trellis/` |
| Sub-agents | 无，implement/check/research 走 workflow 技能内联 |

## Quick Start

```bash
trellis init --dsh -u your-name
dsh web
```

在 dsh 中：描述工作 → agent 加载 `trellis-start`（读 `.trellis/` 状态并路由到 brainstorm / before-dev / check / update-spec）。继续用 `trellis-continue`，收尾说 "finish the trellis task" 加载 `trellis-finish-work`。

## File Map

- `.agents/skills/` — 自动触发的 workflow 技能 + bundled meta/spec-bootstrap/session-insight
- `.dsh/skills/` — dsh 私有 entry skills：start / continue / finish-work
- `.trellis/` — specs、tasks、workspace memory、共享脚本（`get_context.py`、`task.py` 等）

## Notes

- 技能脚本传 `--platform dsh` 给 `get_context.py` 作为平台上下文键
- dsh 无项目级 sub-agent 定义面，因此不装 `trellis-implement/check/research` agent 提示
