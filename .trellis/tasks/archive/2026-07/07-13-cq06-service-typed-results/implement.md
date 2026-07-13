# Implementation plan

## Implementation

- [x] 增加封闭 `ServiceResult<T>` 与最小 operation payload。
- [x] 迁移 `IEntryService`、`EntryService`，包括 draft validation 与 delete approval。
- [x] 迁移 `IPresetService`、`PresetService`，包括 name request、delete confirmation 和三种 mutation。
- [x] 将 `MainWindowViewModel.Entries.cs`、`.Presets.cs`、`.Grouping.cs`、`.Discovery.cs` 改为穷尽 switch，并保持原持久化/回滚顺序。
- [x] 更新现有 test doubles 与 service tests，覆盖每个结果分支和关键 payload。

## Validation

- [x] 运行 Entry/Preset service 与相关 MainWindowViewModel 聚焦测试。
- [x] 运行完整 `dotnet test`。
- [x] 执行 `trellis-check` 的 spec、静态检查、跨层数据流与一致性审计。
- [x] 更新 service result 所有权 spec。
- [x] 运行 `git diff --check`。
- [x] 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`，包含 publish 与 GUI smoke。

## Risk and rollback points

- 调用方遗漏是首要风险：接口签名先改，让编译器列出全部生产和测试调用点。
- Preset update/delete 的 null workflow 结果必须映射为 Failure，而不是伪造 Success。
- 每完成一个 service 家族即运行聚焦测试；若行为回退，只回滚该家族迁移，不改 CQ-02 transaction boundary。
