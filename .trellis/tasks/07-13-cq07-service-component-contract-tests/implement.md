# Implementation plan

## Coverage audit and tests

- [x] 列出 `IEntryService`、`IPresetService` 和 `LauncherCardGroupViewModel` 公共成员与现有直接测试映射。
- [x] 扩展 `EntryServiceTests`，覆盖 clone/edit/delete、draft validation 和 unique clone ID 边界。
- [x] 扩展 `PresetServiceTests`，覆盖空名称、确认取消/异常、update/delete missing/locked/failure 与 aggregate 不变性。
- [x] 新增 `LauncherCardGroupViewModelTests`，覆盖属性、通知与 callback payload 快照。
- [x] 运行聚焦测试；未发现需要修改生产代码的根因。

## Validation

- [x] 运行完整 Release tests 和 ArchitectureRules。
- [x] 执行 `trellis-check`，核对测试质量、复用和跨层覆盖边界。
- [x] 更新 tests spec 中的 service/component contract matrix。
- [x] 运行 `git diff --check`。
- [x] 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`。

## Rollback points

- Service 测试和 component 测试可分别回滚；不因测试便利改变生产可见性。
- 若 GUI smoke 与单元契约冲突，以真实运行时行为和现有产品契约为准，修正错误测试而非绕过门禁。
