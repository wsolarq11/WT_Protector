# Implementation Plan

1. 在 `.codex/hooks.json` 注册 `SessionStart`，调用 `python -X utf8 .codex/hooks/session-start.py`。
2. 解析 `hooks.json`，分别手工执行两个脚本，校验 JSON 和 `hookEventName`。
3. 运行 `trellis update --dry-run`、`get_context.py` 和 task validation。
4. 记录安装证据、首次 `/hooks` 信任要求和回滚方式。
