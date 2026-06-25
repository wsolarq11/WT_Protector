# Implementation Plan — 清零 UltraReview 全部发现项

## Phase 概览

```
Phase P0 (6 项) → dotnet test → verify.ps1 门禁
Phase P1 (12 项) → dotnet test → verify.ps1 门禁  
Phase P2-P3 (12 项) → dotnet test → verify.ps1 最终全量门禁
```

每阶段用并行子代理扇出独立文件修改，阶段完成后统一验证。

---

## Phase P0: 严重问题（6 项，预计修改 ~8 文件）

### P0.1 — R1: 激活 Settings SchemaVersion
- **文件**: `LauncherSettings.cs:11`, `LauncherSettingsStore.cs:27`, `LauncherSettingsCopies.cs:23`
- **改动**: 移除 `LauncherSettings.cs:11` `[JsonIgnore]`；添加 `[JsonPropertyName("schemaVersion")]`；确认 `LauncherSettingsStore.cs:27` 版本检查生效
- **测试**: 新增 `LauncherSettingsStoreTests.SchemaVersionRoundTripsThroughJson()` + `SchemaVersionMismatchThrows()`
- **风险**: 低 — 旧 settings.json 无该字段 → 默认值 1

### P0.2 — R2: 修复 ToDictionary 重复 ID 崩溃
- **文件**: `MainWindowViewModel.Selection.cs:119`
- **改动**: 将 `_entries.ToDictionary(e => e.Id, LauncherEntryIds.Comparer)` 替换为手动 `foreach` + `TryAdd` 模式。重复 ID 时记录 ErrorLog 并用第一个而不是崩溃
- **测试**: 新增 `LaunchBatchAsync_DuplicateEquivalentEntryIds_LogsErrorAndContinues()`
- **风险**: 低 — 数据层已保证 ID 唯一性，这是防御性修复

### P0.3 — R3: Store.Load 损坏文件时回退 .bak
- **文件**: `LauncherConfigStore.cs:16-26`, `LauncherPresetStore.cs:11-22`, `LauncherSettingsStore.cs:10-28`
- **改动**: 每个 Load 方法中，将 `LauncherJson.LoadRequired<T>` + 验证包装在 `try-catch (JsonException | InvalidDataException)` 中。捕获后检查 `.bak` → 恢复 → 重试加载
- **测试**: 新增三个测试类验证损坏 JSON 时 bak 恢复路径。覆盖：bak 可用恢复成功、bak 不可用传播原始异常、恢复后验证通过
- **风险**: 中 — 需注意备份恢复覆盖主文件后用恢复的数据重新验证

### P0.4 — R4: ErrorLog 添加大小限制
- **文件**: `ErrorLog.cs`
- **改动**: 在 `Write()` 方法开头添加文件大小检查。若 `errors.log` > 1MB → `File.Move(errors.log, errors.old.log, overwrite: true)` → 新建 `errors.log`
- **测试**: 新增 `ErrorLogTests.RotatesWhenFileExceeds1MB()`
- **风险**: 低 — `File.Move` + `overwrite` 逻辑简单

### P0.5 — R5: App.axaml.cs 添加 IsRecoverable 过滤器
- **文件**: `App.axaml.cs:141`
- **改动**: `catch (Exception ex)` → `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))`
- **测试**: 无新增测试（异常处理路径难以在单元测试中模拟 App 生命周期）
- **风险**: 极低 — 一行改动，与项目其余 20 处一致

### P0.6 — R6: LaunchAsync 检查 SaveConfig() 返回值
- **文件**: `MainWindowViewModel.Launch.cs:91-93`
- **改动**: 检查 `SaveConfig()` 返回值。失败时不覆盖 `StatusText` 为"已请求启动"
- **测试**: 新增 `LaunchAsync_SaveConfigFailure_ShowsSaveErrorNotRequested()`
- **风险**: 低

### P0 验证
```powershell
dotnet test -c Release --filter "FullyQualifiedName~WTLauncher.Tests" --no-restore
# 验证点: 556+ 测试全部通过，新增测试均通过
```

---

## Phase P1: 中等问题（12 项，预计修改 ~15 文件）

### P1.1 — R7: UpdatePreset 规范化 presetId
- **文件**: `LauncherPresetWorkflow.cs:42-47`
- **改动**: 在第 45 行名称冲突检查前，用 `LauncherPresetIds.NormalizeReference(presetId)` 替换 `presetId`
- **测试**: 新增 `UpdatePreset_NameConflictCheck_UsesNormalizedId()`

### P1.2 — R8: SearchHistory 截断方向确认 + 注释
- **文件**: `LauncherSettingsStore.cs:136-137`, `MainWindowViewModel.Layout.cs:247-273`
- **改动**: 代码审查确认行为正确（新条目插入索引 0）。在 `RequireSearchHistory` 和 `RecordSearchTerm` 添加注释说明方向约定
- **测试**: 新增 `RecordSearchTerm_KeepsNewestEntriesWhenCapped()`

### P1.3 — R9: 统一 StartBatch 失败策略
- **文件**: `LauncherLaunchWorkflow.cs:74-89`
- **改动**: `break` → `continue`；修复 `FailureIndex` 记录最后一个失败索引
- **测试**: 新增 `StartBatch_ContinuesAfterFirstFailure()` + `StartBatch_FailureIndexRecordsLastFailure()`

### P1.4 — R10: 统一 ID 规范化行为
- **文件**: `LauncherPresetIds.cs`（Normalize 方法），`LauncherEntryIds.cs`（Normalize 方法）
- **改动**: `LauncherPresetIds.Normalize` 超长时静默截断 256 字符 (移除 `InvalidDataException` 抛出)
- **测试**: 新增 `Normalize_TruncatesOver256Chars()` 确保两个类的 Normalize 对超长行为一致

### P1.5 — R11: ReplaceEntries 后重置 _keyboardFocusedEntryId
- **文件**: `MainWindowViewModel.Entries.cs:281-290` 或 `MainWindowViewModel.cs`
- **改动**: 在 `ReplaceEntries` 末尾：`_keyboardFocusedEntryId = null; OnPropertyChanged(nameof(KeyboardFocusedEntryId));`
- **测试**: 新增 `ReplaceEntries_ResetsKeyboardFocusedEntryId()`

### P1.6 — R12: 选择操作前调用 DiscardDragReorderPreview
- **文件**: `MainWindowViewModel.Selection.cs:19-72`
- **改动**: 在 `SelectVisible`、`ClearSelection`、`ClearVisibleSelection` 方法开头添加 `DiscardDragReorderPreview();`
- **测试**: 新增 `SelectVisible_DiscardsDragPreview()`（验证拖拽预览期间执行选择后预览被取消）

### P1.7 — R13: 添加 DragDrop handler Remove 调用
- **文件**: `MainWindow.axaml.cs`
- **改动**: 添加 `OnUnloaded` 或 `Closing` 事件处理，在其中调用 `DragDrop.RemoveDragOverHandler(this, OnEntryDragOver); DragDrop.RemoveDropHandler(this, OnEntryDrop);`
- **测试**: 无新增（UI 生命周期测试复杂，结构验证即可）

### P1.8 — R14: 消除 LauncherStartupArguments 静态可变状态
- **文件**: `LauncherStartupArguments.cs`, `App.axaml.cs`
- **改动**: `ReadConfigPath(string?[]?)` 返回 `(string? configPath, IReadOnlyList<string> unknownArgs)`；移除 `static List<string> _unknownArgs`；`App.axaml.cs` 更新调用方
- **测试**: 更新现有 `LauncherStartupArgumentsTests`

### P1.9 — R15: RenameEntryId 在旧 ID 不存在时返回 false
- **文件**: `LauncherEntryReferences.cs`
- **改动**: 在 `RenameEntryId` 开始时检查旧 ID 存在于 customEntryOrder 或任意 preset 中；不存在时直接返回 `(presetFile, order, false)`
- **测试**: 新增 `RenameEntryId_ReturnsFalseWhenOldIdNotFound()`

### P1.10 — R16: UI 层添加 MaxLength
- **文件**: `EntryDetailsWindow.cs` 或对应的 `.axaml` 文件
- **改动**: 在 id/name/directory/command 文本框上设置 `MaxLength`（id:256, name:4096, directory:4096, command:4096）
- **测试**: 无新增（XAML 属性，结构级变更）

### P1.11 — R17: 空模板字典时 $key 引用检测
- **文件**: `LauncherCommandTemplateResolver.cs:23-24`
- **改动**: 移除 `templates.Count == 0` 的早期退出；空模板时继续扫描 `$key` 引用 → 报告全部缺失
- **测试**: 新增 `ValidateTemplateReferences_ReportsMissingKeysWhenTemplatesEmpty()`

### P1.12 — R18: (保留为空，P1 原为 12 项设计调整为 11 项，实际 R1-R6 P0, R7-R17 P1)

### P1 验证
```powershell
dotnet test -c Release
```

---

## Phase P2-P3: 代码质量优化（12 项，预计修改 ~10 文件）

### P2.1 — R19: 消除 Clone 冗余遍历
- **文件**: `LauncherPresetFiles.cs:10`
- **改动**: `Clone()` 直接调用 `Validate(presets)` 而非先 `ClonePresets()` 再 `Validate()`
- **测试**: 现有测试确保行为不变

### P2.2 — R20: 合并双重 JSON 解析
- **文件**: `LauncherConfigStore.cs:28-41`
- **改动**: 添加注释说明双重解析的防御性设计价值（先验证 selected 字段再反序列化），不修改行为但记录原因
- **测试**: 无需新增

### P2.3 — R21: DRY 消除 RenameEntryId/RemoveEntryId 重复
- **文件**: `LauncherEntryReferences.cs`
- **改动**: 提取 `TransformAllLists(Func<List<string>, List<string>> transform)` 辅助方法
- **测试**: 现有测试确保行为不变

### P2.4 — R22: Zip 操作显式排序保护
- **文件**: `LauncherEntryReferences.cs:47`
- **改动**: 在 Zip 前添加 `Debug.Assert(presetFile.Presets.Count == mutablePresetLists.Count)` 或验证 + 异常
- **测试**: 无需新增（断言/验证防御性保护）

### P2.5 — R23: ValidateEntry O(n²) 优化
- **文件**: `LauncherEntryDrafts.cs:60-62`
- **改动**: 将 `existingEntries.Any()` 替换为预构建的 `HashSet<string>`（在 ValidateEntry 外构建，传递进方法）。O(n²) → O(n)
- **测试**: 现有测试确保行为不变

### P2.6 — R24: ErrorLog 并发丢弃注释
- **文件**: `ErrorLog.cs`
- **改动**: 在 `if (_isWriting == 1) return;` 添加注释说明并发丢弃的设计理由
- **测试**: 无需新增

### P2.7 — R25: ExceptionFilters StackOverflowException 注释
- **文件**: `ExceptionFilters.cs`
- **改动**: 在 `StackOverflowException` 行添加注释：`// In .NET 6+, SOE terminates process before catch executes. Kept for documentation.`
- **测试**: 无需新增

### P2.8 — R26: Unicode 正规化
- **文件**: `LauncherEntryText.cs`
- **改动**: 在 `Normalize()` 末尾添加 `string.Normalize(NormalizationForm.FormKC)`；使用 `using System.Text`
- **测试**: 新增 `Normalize_AppliesFormKCUnicodeNormalization()`

### P2.9 — R27: 命令行长度检查
- **文件**: `LaunchArgumentBuilder.cs`
- **改动**: 在 `Build()` 方法中，最终命令行字符串超过 32767 字符时抛出 `InvalidDataException`
- **测试**: 新增 `Build_RejectsCommandLineExceeding32767Characters()`

### P2.10 — R28: PresetFiles 10000 项限制优化
- **文件**: `LauncherPresetFiles.cs:94`
- **改动**: 将 `ToRequiredUniqueList`（先完整克隆再去重验证）改为流式验证（边迭代边加到 HashSet，检测重复，限制数量）
- **测试**: 现有测试确保行为不变

### P2.11 — R29: DragGhostLayoutCalculator 负值防护
- **文件**: `LauncherDragGhostLayoutCalculator.cs`
- **改动**: 在 CalculatePosition 开头添加 `Math.Max(0, containerWidth/Height)` 防护
- **测试**: 新增 `Calculate_ClampsNegativeContainerSize()`

### P2.12 — R30: 内部注释标记替换
- **文件**: 多处（`LauncherEntryReferences.cs`, `LauncherPresetIds.cs`, 等）
- **改动**: 将 `// H4`, `// H15`, `// L32`, `// M11`, `// E8`, `// H11`, `// L30`, `// L33` 等内部标记替换为描述性注释或删除
- **测试**: 无需新增（注释变更）

### P2-P3 验证
```powershell
dotnet test -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
# 最终验证: 556+ 测试全部通过，verify.ps1 全量门禁 PASS
```

---

## Rollback Points

每个 Phase 结束后，`git diff` 检查变更范围。如门禁失败：
1. `git checkout -- .` 回滚当前 Phase 所有变更
2. 定位失败原因
3. 修复后重新进入该 Phase

## Validation Commands（每阶段执行）

```powershell
# 1. 测试
dotnet test -c Release

# 2. 全量门禁 (P2-P3 阶段最终执行)
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1

# 3. 编译检查
dotnet build -c Release --no-restore

# 4. Core 层依赖检查
dotnet list .\src\avalonia-launcher\src\WTLauncher.Core\WTLauncher.Core.csproj package
# 必须输出: 无包引用
```

## Risky Files

| 文件 | 风险 | 缓解 |
|------|------|------|
| `LauncherSettings.cs` | P0.1 改变 JSON 序列化 | 向后兼容默认值 |
| `LauncherConfigStore.cs` | P0.3 改变加载路径 | try-catch 包装，bak 恢复 |
| `LauncherStartupArguments.cs` | P1.8 API 签名变更 | 仅一处调用方 |
| `LauncherLaunchWorkflow.cs` | P1.3 改变批处理行为 | `break`→`continue` 充分测试 |
| `LauncherEntryReferences.cs` | P2.3 重构 | 现有测试保护 |
