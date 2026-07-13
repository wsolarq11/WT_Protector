# Phase 2: 剩余 Services 提取

## Goal

作为 CQ-01 的复用实现轨道，完成 preset、selection、layout/filter 与 launch 的最小稳定 service 边界；不创建与 `07-13-cq01-thin-main-viewmodel` 平行的实现。

## Requirements

- 以 `07-13-cq01-thin-main-viewmodel/{prd,design,implement}.md` 为完整需求和设计权威。
- 复用现有 `IDialogService`、`IEntryService`、`IPresetService` 和 Core workflows/calculators。
- service 无长期 mutable aggregate、无 Views 依赖、无直接文件 I/O；ViewModel 继续拥有绑定状态、事务应用和通知顺序。
- 不新增 mediator、event bus、repository 或依赖。

## Acceptance Criteria

- [x] CQ-01 规划中 Slice 1–3 的稳定 owner 全部建立或复用，并具备直接契约测试与所有权审计。
- [x] 生产 composition root 注入具体实现，ViewModel 不直接 `new` Entry/Preset feature service。
- [x] CQ-02 rollback 与 single-store 不变量保持通过。
- [x] 本任务与 CQ-01 同步验收，不存在重复类型或平行调用路径。
