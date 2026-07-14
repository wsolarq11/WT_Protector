# Implement — V02 Main UI Declutter

## Steps

- [x] 在 LauncherControlBar 建立三个文字 overflow 入口及其原命令菜单项。
- [x] 删除被折叠的直接按钮，保持高频动作和计数状态可见。
- [x] 添加最小 code-behind 菜单打开路由，复用一个事件处理器。
- [x] 新增/更新结构契约测试，固定动作层级、绑定、自动化名称和卡片“启动 + 更多”契约。
- [x] build、聚焦测试与键盘/菜单运行时 smoke。
- [x] trellis-check、完整 Release tests、git diff --check、tools/verify.ps1。
- [x] 更新 views/tests spec，提交并归档。

## Rollback Points

- 先完成单个菜单并 build，确认 ContextMenu 绑定继承正常后再迁移其余动作。
- 若菜单入口无法通过 Enter/Space 打开，修复入口事件路由；不增加新的命令代理。
