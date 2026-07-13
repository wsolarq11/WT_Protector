# CQ06 Service typed result 边界

## Goal

让 Entry 与 Preset feature service 的可恢复操作结果在编译期区分成功、用户取消和失败，消除 nullable tuple、bool + nullable status 以及隐含非法组合，使 ViewModel 只能显式处理每一种结果。

## Background

- `IEntryService` 的四个异步 CRUD 方法当前返回 nullable tuple；`IPresetService` 的五个分支型方法返回 nullable tuple 或 bool + nullable status。
- 调用方实际已经按成功、取消/未执行、失败分支处理，但现有签名允许 payload 与 status 同时为空或同时非空。
- `IDialogService` 是原始 UI 适配边界，nullable/bool 分别直接表达窗口关闭与确认选择；本任务不改造该边界。
- 状态文案仍由现有 `Launcher*Status` formatter 拥有；本任务只运输文案，不提前实施 CQ-05。

## Requirements

- R1：为需要调用方分支的 Entry/Preset service 操作提供封闭的类型化结果，至少区分 Success、Canceled、Failure。
- R2：成功结果必须携带非空业务载荷；Canceled 与 Failure 必须携带非空状态文案，不允许构造 tuple 式非法组合。
- R3：Entry add/clone/edit/delete 以及 Preset request-name/confirm-delete/add/update/delete 全部迁移；纯查询、资格判断和 ID 生成保持现状。
- R4：所有生产调用方使用穷尽模式匹配处理结果，不再以 nullable payload/status 推断状态。
- R5：保持现有用户可见文案、取消语义、错误日志、持久化、snapshot/rollback、默认 preset fallback 与焦点清理行为不变。
- R6：复用一个最小的共享结果模型；不引入依赖、异常框架、mediator/event bus 或通用业务错误体系。
- R7：为每种结果分支留下直接可运行的 service 契约测试，并保留现有 ViewModel 行为测试。

## Acceptance Criteria

- [x] AC1：目标 service 接口不再暴露 nullable tuple、out validation message 或 bool + nullable status 结果。
- [x] AC2：Success、Canceled、Failure 三种结果均只能通过合法构造方式创建，并由调用方显式匹配。
- [x] AC3：Entry 与 Preset service 测试覆盖成功、取消、验证失败/可恢复异常，以及删除成功载荷。
- [x] AC4：现有保存失败回滚、preset fallback、键盘焦点清理和状态文案测试继续通过。
- [x] AC5：`git diff --check` 与 `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` 全部通过。

## Verification evidence

- 598 tests passed in Release configuration.
- `tools/verify.ps1` PASS, including win-x64 self-contained publish and hidden-temp-config GUI smoke.
- Codebase graph plus compiler migration found and updated Entry, Preset, Grouping, and Discovery callers; no target nullable tuple signature remains.

## Out of scope

- CQ-05 的类型化状态内容与文案重构。
- Core 全局异常或 Result 化。
- 修改 `IDialogService`、配置 schema、持久化位置或 UI 布局。
