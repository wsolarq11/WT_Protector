# P5-收尾清理

## Goal

P1/P3 完成后的死代码清理和标准化。

## Completed

- [x] `MainWindowViewModel.Entries.cs` — 旧 CRUD 实现替换为 ELVM 委托，未使用的 using 清理
- [x] `MainWindowViewModel.cs` — `RestoreShellSnapshot` 修复（原来为空桩，现在正确恢复 `_settings`、`_presetFile`、`TotalCount`）
- [x] `_entryListViewModel` 字段设为 `readonly` (IDE0044)
- [x] 无裸 `catch (Exception)` — 门禁验证通过
- [x] 全量门禁 — PASS (653 测试, 0 警告, 0 错误)

## Remaining (deferred)

以下清理需要先迁移其他 partial 文件（Discovery.cs、CommandManagement.cs、DirectoryManagement.cs、Grouping.cs）到 ELVM，当前不阻塞架构目标：

- 从 shell 移除 `IEntryCardHost` 实现
- 从 `Entries.cs` 移除 `SaveEntryCollectionChanges` / `CaptureEntryMutationSnapshot` / `RestoreEntryMutationSnapshot`
- 从 shell 移除 `_entryService` 字段
