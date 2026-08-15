# 验证台账 — docs/trellis-codex-setup-audit.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档日期: 2026-07-13（创建提交 90dacd8b）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| Date: 2026-07-13 | VERIFIED | git log: 90dacd8b 2026-07-13 | 保留 |
| SessionStart 加载 Trellis 概览 + UserPromptSubmit 加载工作流面包屑 | HISTORICAL(当日真) | git show 90dacd8b:.codex/hooks.json 含 SessionStart + UserPromptSubmit 两项注册 | 保留 + 顶部加当前状态注 |
| 当前接线状态 | STALE(现状) | 当前 .codex/hooks.json 仅 UserPromptSubmit（inject-workflow-state.py）+ SubagentStart（inject-subagent-context.py）；SessionStart 注册已于 2026-07-24 d6280974 移除 | 新增"复核（2026-08-15）"注 |
| Global package @mindfoldhq/trellis@0.6.6 / npm latest 0.6.6 / 项目模板 0.6.6 | HISTORICAL | 归档任务 07-13-codex-trellis-setup design.md: "Trellis CLI 与 npm latest 均为 0.6.6"；prd.md 勾选项佐证 | 保留 |
| Codex CLI 0.144.1 | HISTORICAL | 归档任务 07-13-codex-trellis-setup design.md: "Codex CLI 0.144.1 将 hooks 标记为 stable" | 保留 |
| Codex hooks 功能 stable、用户配置启用 | HISTORICAL(外部) | 用户机器配置，仓库内无法复核 | 保留为当日审计记录 |
| 项目信任 d:\_ai_\_projects\wt_protector = trusted | HISTORICAL(外部) | 用户机器配置，仓库内无法复核 | 保留为当日审计记录 |
| trellis update --dry-run 仅报告 hooks.json 定制 | HISTORICAL(外部) | 当日命令结果；hooks.json 现已变化 | 保留为当日审计记录 |
| 无 *.new 文件 | VERIFIED | 当前扫描 *.new = 0（当日同样为 0） | 保留 |
| session-start.py 返回 hookEventName=SessionStart | VERIFIED | .codex/hooks/session-start.py:542 "hookEventName": "SessionStart"（脚本仍存在，仅未注册） | 保留 |
| inject-workflow-state.py 返回 hookEventName=UserPromptSubmit | VERIFIED | .codex/hooks/inject-workflow-state.py:461/:466 hook_event_name = ... "UserPromptSubmit" | 保留 |
| 任务运行时（developer/git/任务树/journal/spec 索引）解析成功 | HISTORICAL | 当日记录 | 保留 |
| 维护命令路径 | VERIFIED | .trellis/scripts/get_context.py、.codex/hooks/session-start.py、.codex/hooks/inject-workflow-state.py 均存在 | 保留 |
| 附加路径 .codex/agents、.agents/skills | VERIFIED | 均存在 | 保留 |

处理汇总: 保留 2026-07-13 当日记录；顶部新增"复核（2026-08-15）"现状注（SessionStart 注册已移除，现仅 UserPromptSubmit + SubagentStart）；其余保留。
