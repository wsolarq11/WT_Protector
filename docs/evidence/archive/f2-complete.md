Status: Archived
Purpose: 记录安全审计 F2（preset entryId 运行时交叉验证）的实现与验收。
Original scope: 2026-07-23 完成，677 个测试通过。

# 安全审计 F2: Preset entryId 运行时交叉验证 — 完成报告

## 结果

已完成 preset entryId 运行时交叉验证。677 个测试通过，0 失败；构建 0 错误、0 警告。

## 关键证据

| 文件 | 改动 |
| --- | --- |
| `IPresetService.cs` | `AddPreset` / `UpdatePreset` 新增 `IEnumerable<string> knownEntryIds` 参数 |
| `PresetService.cs` | 调用 `LauncherPresetWorkflow` 前验证 entryIds；新增 `FindInvalidEntryIds` 私有静态方法，用 `EntryId.Comparer` 归一化比较 |
| `LauncherStatusText.cs` | 新增 `AddFailedInvalidEntryIds` / `UpdateFailedInvalidEntryIds` |
| `MainWindowViewModel.Presets.cs` | 调用时传入 `GetEntryIds()`；`GetPresetEntries` 改为日志警告 + 跳过缺失条目，不再抛 `InvalidDataException` |
| `MainWindowViewModel.Grouping.cs` | `SaveGroupAsPresetAsync` 传入 `GetEntryIds()` |
| `PresetServiceTests.cs` | 更新调用 + 新增 4 个测试（无效/多无效/更新无效/空集合） |

## 设计决策

1. 验证位置：PresetService 层（App 层），经 `knownEntryIds` 传入，不增加 service 依赖。
2. 优雅降级：返回 `ServiceFailure` 带描述性状态，不抛异常。
3. 归一化比较：`EntryId.Comparer` 确保大小写与空格不敏感。

## 验证

- `dotnet build`：0 错误，0 警告。
- `dotnet test`：677/677 通过。
- PresetService 测试：17/17 通过（13 原有 + 4 新增）。

## 后续建议

在静态验证脚本中补充对 launcher.json 中 preset entryId 的检查（回归门禁已由 `security-audit-f2-preset-entryid-validate` 承担）。
