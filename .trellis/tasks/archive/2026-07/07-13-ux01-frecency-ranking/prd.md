# UX01 frecency 常用项排序

## Goal

关闭现代化审计中的 UX-01：让用户可选择“按使用频率”排序，并以真实启动次数与最近启动时间共同决定常用项顺序。

## Confirmed implementation owner

- 实现与详细设计由已归档任务 `.trellis/tasks/archive/2026-07/07-11-entry-launch-count-frecency` 拥有。
- 原始实现提交：`ac675a4 feat(modernize): ... frecency ...`；后续架构重命名将排序所有权收敛到 `LauncherEntryQuery`。
- 本任务只负责现代化父计划的缺口验收，不复制代码或建立平行统计模型。

## Requirements

- R1：`LaunchEntry.LaunchCount` 为 optional 持久化字段，旧配置缺失时为 0。
- R2：单次与批量实际启动均通过单一记录入口递增计数并更新时间。
- R3：`frequent` 使用 zoxide 风格时间衰减，按 score 降序并以 name/id 稳定打破平局。
- R4：UI 提供“按使用频率”，选择可持久化、重启恢复。
- R5：启动成功但统计保存失败时不伪造外部启动回滚；内存保留真实计数，磁盘保持旧值并显示失败。
- R6：不显示原始统计数字、不增加分析面板、同步服务或可配置评分公式。

## Acceptance Criteria

- [x] AC1：Core frecency 测试覆盖四个衰减桶、零次数、从未启动及稳定 tie-break。
- [x] AC2：ViewModel 测试覆盖单次/批量递增、跨重启持久化、旧配置兼容和保存失败语义。
- [x] AC3：排序选项与持久化恢复有直接测试/运行时证据。
- [x] AC4：复用任务已提交并归档，没有第二套实现。
- [x] AC5：从当前 HEAD 执行 `tools/verify.ps1` PASS：610 tests、win-x64 self-contained publish、GUI smoke。

## Out of scope

- 统计展示、历史报表、跨机同步、手动重置与公式配置。
