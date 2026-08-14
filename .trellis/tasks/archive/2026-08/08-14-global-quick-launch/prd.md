# 全局快速调用：托盘常驻 + 热键唤起 + 输入即启动

## Goal

把“启动工作区”从“先找到并打开主窗口”提升为“任意时刻按热键，输入几个字符直接启动”。这是当前爽度提升最大的一步。

## Background（已确认事实）

- 当前应用是普通 Avalonia 主窗口，没有托盘常驻、全局热键或 overlay 快速输入。
- 当前搜索已支持名称/目录/命令的多词匹配，但没有模糊排名；搜索仍需要进入主窗口。
- `docs/giants-shoulders/launcher-first-class-patterns.md` 记录 Raycast Quicklinks/Snippets 模式：可搜索、带标签、可重复执行的命令对象。
- 现有启动链路已很成熟：搜索投影 → LaunchPlan → preflight → confirmation → receipt。新入口应复用，不应新建第二套启动逻辑。
- Avalonia/.NET 10 的托盘/全局热键需要先做技术调研（原生 API、现有库、AOT 兼容性），再进 design.md。

## Requirements

- R1 托盘常驻：最小化为托盘；托盘菜单包含“打开”“退出”，不改变现有单文件配置语义。
- R2 全局热键：可配置默认热键；热键触发时唤起快速启动窗口并聚焦输入框；冲突时在 UI 给出可读错误。
- R3 快速启动 overlay：单行输入 + 结果列表；按名称/目录/命令模糊匹配；Enter 启动选中项，Esc 关闭。
- R4 启动仍走现有 LaunchPlan/preflight/confirmation/receipt；确认开关生效。
- R5 设置（热键、是否常驻）持久化到 `launcher.json` settings；不新增第二状态文件。
- R6 失效条目在快速启动结果中可见但明确标记（与 `08-14-stale-directory-recovery` 联动）。

## Acceptance Criteria

- [ ] 应用从任意前台状态按全局热键可唤起快速启动窗口。
- [ ] 输入 2-4 个字符能按名称/目录/命令匹配；Enter 可启动；Esc 可关闭。
- [ ] 托盘最小化/恢复/退出可用；关闭主窗口不等于退出（按设置决定）。
- [ ] 快速启动使用既有启动链路；requireConfirmationForLaunches 生效。
- [ ] 热键与托盘设置持久化，重开仍生效。
- [ ] 新增/调整测试覆盖匹配、启动、取消、设置持久化；`tools/verify.ps1` 保持绿。

## Out of Scope

- 插件、剪贴板工作流、语音、移动端。
- 自动启动系统；系统级登录自启不默认开启（若用户决定再加）。
- 重写现有主窗口；overlay 作为新入口，不替代 Cards/Details/Names。

## Open Questions

- 托盘/全局热键实现选型：原生 Avalonia 控件 vs 平台 API vs 第三方库；需以 AOT 兼容和最小依赖为门槛。
- 默认热键与冲突策略是否允许用户完全禁用。