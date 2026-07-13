# Design

## Current evidence

- Trellis CLI 与 npm latest 均为 0.6.6。
- Codex CLI 0.144.1 将 hooks 标记为 stable。
- 用户配置已设置 `[features].hooks = true`，并信任本仓库。
- 项目 `UserPromptSubmit` hook 已有可信 hash。
- Trellis 生成了 `.codex/hooks/session-start.py`，但 `.codex/hooks.json` 未注册 `SessionStart`，这是唯一已确认的接线缺口。

## Minimal change

仅在 `.codex/hooks.json` 增加 `SessionStart` 事件，复用已生成并已手工验证的 `session-start.py`。不复制脚本、不增加依赖、不修改用户全局配置。

## Compatibility and rollback

- 事件使用 Codex 官方 `hooks.json` 格式，项目 trusted 后加载。
- 首次运行需要 Codex `/hooks` 信任新增项目 hook；非交互自动化可用官方 bypass 参数，但日常不默认绕过信任。
- 回滚只需删除新增 `SessionStart` 数组。
