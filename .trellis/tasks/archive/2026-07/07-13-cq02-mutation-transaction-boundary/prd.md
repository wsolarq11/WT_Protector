# CQ02 统一 mutation 事务边界

## Goal

让所有需要持久化的用户 mutation 共享一个明确的提交边界：保存成功后保留新状态，保存失败时恢复该操作涉及的全部可观察内存状态，同时继续由 `LauncherStore` 独占磁盘写入。

## Confirmed facts

- `SaveStoreFile` 当前有 8 个生产调用方，分布在配置、settings、entry、preset、selection 和管理编辑路径。
- entry、preset、selection、settings 已各自实现 snapshot/restore，但提交、错误状态和派生状态刷新逻辑仍散落在多个 ViewModel partial。
- 现有测试已覆盖若干保存失败回滚场景；本任务必须保留这些行为并补足共享边界的对称契约。
- CQ-06 后续负责 service typed result；本任务不提前重写整个错误模型。

## Requirements

- R1：引入一个最小共享 mutation transaction helper/boundary，表达 mutate → persist → rollback，而不是为每个调用方复制控制流。
- R2：边界必须支持不同 snapshot 类型和恢复动作，不持有领域状态，也不成为第二个 store。
- R3：保存实现仍调用现有 `LauncherStore` 权威路径；不得改变原子写入、备份、错误日志或配置位置。
- R4：迁移所有实际具备 snapshot/rollback 语义的 mutation 家族；纯持久化或另有显式提交语义的调用方只有在语义等价时才迁移。
- R5：保存失败时保留具体持久化错误状态，不得被后续通用状态覆盖；恢复后派生列表、选择和计数与 mutation 前一致。
- R6：保存成功路径的可观察行为、配置 schema 与命令 API 保持兼容。
- R7：复用 `07-12-fma-p2-services` 的后续 service extraction 方向，但本任务只建立事务边界，不顺带抽取全部服务。

## Acceptance Criteria

- [x] AC1：代码中存在且仅存在一个无领域状态的共享 transaction control-flow owner，至少被 settings、entry、preset 和 selection mutation 家族复用；命令/目录管理及分组保存没有完整调用前 snapshot，未伪装为可回滚事务。
- [x] AC2：共享边界的聚焦测试同时证明 persist 成功不回滚、persist 失败恰好回滚并返回原失败结果。
- [x] AC3：现有 entry、preset、selection、settings、drag/search-history 等保存失败回滚测试全部通过。
- [x] AC4：`SaveStoreFile`/`LauncherStore` 仍是唯一磁盘提交链，没有新增直接文件写入或第二配置源。
- [x] AC5：没有提前引入 CQ-06 的全局 typed-result 重构、通用 Unit of Work、事件总线或新依赖。
- [x] AC6：聚焦测试、574 项完整测试、self-contained publish、GUI smoke 与仓库总门禁通过。

## Out of scope

- 抽取全部 preset/layout/batch/discovery service（CQ-01 与既有 FMA tasks）。
- 全面替换异常/布尔返回模型（CQ-06）。
- 改变配置 schema、磁盘位置或 atomic save 实现。
