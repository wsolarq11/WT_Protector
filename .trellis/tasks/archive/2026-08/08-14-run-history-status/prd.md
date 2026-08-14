# 启动运行历史与状态回执

## Goal

把最近启动/失败/跳过记录持久化，并与期望配置分离展示，让用户能回答“上次启动成功请求了吗、哪条失败了”。不做终端会话健康监控，不冒充“终端已健康完成”。

## Background（已确认事实）

- 当前已有 `launchedAtUtc`、`launchCount` 和瞬态启动回执，但没有持久化历史列表。
- `docs/giants-shoulders/launcher-first-class-patterns.md` 记录 Kubernetes spec/status 分离原则：desired config 与 observed status 不应混在同一个不可审计对象里。
- 仓库审计明确 external-process-limit：`wt.exe` 接受启动请求后无法证明终端会话健康；本任务只记录 repository-owned 边界内的 requested/failed/skipped。
- 配置不变量要求单文件权威源；运行历史若新增，应放入同一个 `launcher.json` 的可校验小节，而不是新建 AppData/DB 文件。

## Requirements

- R1 持久化最近 N 条启动记录：时间、entry id/name、动作来源（单条/勾选/启动组）、状态（requested/failed/skipped/canceled）。
- R2 记录数量有上限（建议 50-200 条），旧记录自动裁剪；不写入任意命令全文或敏感环境变量。
- R3 UI 提供“最近启动”只读视图/面板，按时间倒序，失败和跳过明确区分；不进入卡片主视图。
- R4 现有瞬态启动回执保留；持久历史由同一回执边界写入，不能伪造“成功完成”。
- R5 历史写盘走 `LauncherStore` 原子写入；历史损坏不应阻塞主配置加载（降级为空历史 + 状态提示）。

## Acceptance Criteria

- [ ] 单条/批量/启动组启动后产生持久历史记录；失败、跳过、取消可区分。
- [ ] 历史视图显示最近 N 条并可看出上次启动状态。
- [ ] 历史超上限后裁剪，不影响正常启动。
- [ ] 历史写盘失败不回滚启动动作，只显示历史保存失败。
- [ ] 新增测试覆盖记录写入、裁剪、状态语义、损坏降级；`tools/verify.ps1` 保持绿。

## Out of Scope

- 监控终端进程存活、退出码、日志输出。
- 成本统计、会话时长、云端同步。
- 把“已请求启动”改写成“启动成功/会话健康”。

## Open Questions

- 历史是否要支持“清空”和“只显示失败”。
- 记录是否包含解析后的 `wt.exe` 参数（便于审计但更长）。