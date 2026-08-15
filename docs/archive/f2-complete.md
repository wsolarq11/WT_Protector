> Historical record — completed 2026-07-23; archived 2026-08-15 to docs/archive/. Content is a point-in-time record, not current state.
# 安全审计 F2: Preset entryId 运行时交叉验证 — 完成报告

## 结果

已完成 preset entryId 运行时交叉验证的实现。所有 677 个测试通过，0 个失败。

## 改动清单

### 1. `IPresetService.cs`
**文件**: `src/avalonia-launcher/src/WTLauncher.App/Features/Presets/IPresetService.cs`

- `AddPreset` 方法新增 `IEnumerable<string> knownEntryIds` 参数
- `UpdatePreset` 方法新增 `IEnumerable<string> knownEntryIds` 参数
- 添加 XML 文档说明参数用途

### 2. `PresetService.cs`
**文件**: `src/avalonia-launcher/src/WTLauncher.App/Features/Presets/PresetService.cs`

- `AddPreset` 实现：在调用 `LauncherPresetWorkflow.AddPreset` 前验证 entryIds 是否全部存在于 knownEntryIds 中
- `UpdatePreset` 实现：在调用 `LauncherPresetWorkflow.UpdatePreset` 前验证 entryIds 是否全部存在于 knownEntryIds 中
- 新增 `FindInvalidEntryIds` 私有静态方法，使用 `EntryId.Comparer` 进行归一化比较
- 验证失败时返回 `ServiceFailure`，不抛出异常

### 3. `LauncherStatusText.cs`
**文件**: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherStatusText.cs`

- 新增 `AddFailedInvalidEntryIds(IReadOnlyList<string>)` 状态方法
- 新增 `UpdateFailedInvalidEntryIds(IReadOnlyList<string>)` 状态方法

### 4. `MainWindowViewModel.Presets.cs`
**文件**: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Presets.cs`

- `SaveSelectionAsPresetAsync` 调用 `AddPreset` 时传入 `GetEntryIds()`
- `UpdateSelectedPreset` 调用 `UpdatePreset` 时传入 `GetEntryIds()`
- `GetPresetEntries` 改为优雅跳过缺失条目（日志警告 + continue），不再抛出 `InvalidDataException`

### 5. `MainWindowViewModel.Grouping.cs`
**文件**: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Grouping.cs`

- `SaveGroupAsPresetAsync` 调用 `AddPreset` 时传入 `GetEntryIds()`

### 6. `PresetServiceTests.cs`
**文件**: `src/avalonia-launcher/tests/WTLauncher.Tests/PresetServiceTests.cs`

- 更新所有现有 `AddPreset`/`UpdatePreset` 调用以传递 `knownEntryIds` 参数
- 新增 4 个测试方法：
  - `AddPreset_WithInvalidEntryIds_ReturnsFailure`
  - `AddPreset_WithMultipleInvalidEntryIds_ReportsAll`
  - `UpdatePreset_WithInvalidEntryIds_ReturnsFailure`
  - `AddPreset_WithEmptyKnownEntryIds_RejectsNonExistentIds`

## 设计决策

1. **验证位置**: PresetService 层面（App 层），通过 `knownEntryIds` 参数传入，不增加 service 依赖
2. **优雅降级**: 返回 `ServiceFailure` 带描述性状态消息，不抛出异常
3. **归一化比较**: 使用 `EntryId.Comparer` 确保大小写和空格不敏感
4. **GetPresetEntries 降级**: 从抛出 `InvalidDataException` 改为日志警告 + 跳过缺失条目

## 验证

- 构建: `dotnet build` — 0 错误，0 警告
- 测试: `dotnet test` — 677/677 通过
- 新增测试: 17/17 PresetService 测试通过（13 个原有 + 4 个新增）

## 风险/后续

- 无已知风险
- 建议后续在 `verify.ps1` 或静态验证脚本中补充对 launcher.json 中 preset entryId 的检查
