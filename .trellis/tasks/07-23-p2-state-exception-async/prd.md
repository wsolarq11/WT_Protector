# Phase 2: 异常合并 + AppStore 清理 + 并发安全

## Goal

在 Phase 1（死代码清理 + 序列化修复）基础上，解决异常处理分裂（R3）、AppStore 冗余 API、以及并发安全缺失（R8）三个问题。

## Requirements

### 1. 异常处理统一
- 消除 `ExceptionFilters.IsRecoverable` 与 `LauncherErrors.IsRecoverable` 之间的解包逻辑重复
  - `UnwrapAggregates` 在 `ExceptionFilters` 中重复实现，Core 层 `IsRecoverable` 已包含该逻辑
  - 简化 `ExceptionFilters.IsRecoverable`：保留 DEBUG 守卫（逻辑错误 crash），解包只委托给 Core
- 确保所有 `catch when (ExceptionFilters.IsRecoverable(ex))` 行为不变

### 2. AppStore 冗余 API 清理
- 删除 `Mutate` / `MutateAsync` / `MutateErrorOr` 这三个遗留包装方法
- 所有调用方改为直接使用 `Dispatch` / `DispatchAsync` / `DispatchErrorOr`
- 测试代码中的 `store.Mutate` → `store.Dispatch`

### 3. 并发安全修复
- 给 `LauncherJson.Save` 加 `private static SemaphoreSlim(1,1)` 锁，防止并发写覆盖
- 不改动 LauncherJson.Load（读不需要锁）

## Acceptance Criteria

- [ ] Build 0 errors 0 warnings
- [ ] 696+ 测试全部 PASS
- [ ] `verify.ps1` 全绿
- [ ] `ExceptionFilters.IsRecoverable` 不再有独立的解包逻辑
- [ ] `AppStore` 中无 `Mutate`/`MutateAsync`/`MutateErrorOr` 方法
- [ ] `LauncherJson.Save` 有 `SemaphoreSlim` 保护

## Notes

- 0 新 NuGet 依赖
- 不涉及 ViewModel 或 XAML 改动
- 行为变化：LauncherJson.Save 并发安全（语义等价），AppStore API 精简（调用方语法变化）
