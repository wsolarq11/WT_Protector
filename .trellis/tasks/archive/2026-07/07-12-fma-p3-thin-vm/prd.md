# Phase 3: 瘦身 MainWindowViewModel

## Goal

作为 CQ-01 的复用验收轨道，删除已被 feature services 取代的业务所有权，使 `MainWindowViewModel` 只保留绑定状态、命令入口、结果映射、事务协调和通知顺序。

## Requirements

- 以 `07-13-cq01-thin-main-viewmodel/{prd,design,implement}.md` 为完整权威。
- 不用新增 partial 或机械移动代码冒充瘦身。
- 不把 ViewModel 本身或大量 callback bag 传给 service。
- 保持所有 public binding/command 与运行时行为兼容。

## Acceptance Criteria

- [x] CQ-01 AC1–AC6 全部满足。
- [x] 剩余直接 Core workflow 调用均有“状态映射/组合”理由并在所有权审计中记录。
- [x] 完整生产门禁通过，本任务与 CQ-01 使用同一代码证据（589 tests + publish + GUI smoke）。
