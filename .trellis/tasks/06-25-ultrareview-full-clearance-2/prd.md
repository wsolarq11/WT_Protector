# 清零 UltraReview 全部发现项

## Goal

修复 2026-06-25 UltraReview 深度审查报告中的全部发现项，将代码质量提升至置信度逼近 100%，零遗留问题。

## 背景

2026-06-25 完成了一次 16 维度并行子代理 UltraReview，覆盖 ~150 源文件 + 50 测试文件，共发现 **30 项问题**（6 P0 + 12 P1 + 12 P2-P3）。本任务清算全部 30 项，不遗漏。

## 绝对铁律

1. **绝对禁止降低测试标准来迁就产品代码的缺陷。** 如果修复引入新测试失败，必须修复代码直至全部通过。不允许禁用测试、放宽断言、或删除测试覆盖。
2. **禁止通过 `#pragma warning disable` 或 `catch {}` 吞掉问题来"修复"警告。**
3. **所有修改必须通过 `dotnet test -c Release` 全量测试（556 个测试全部通过）。**
4. **所有修改必须遵守 `.trellis/spec/core/quality-guidelines.md` 的 Forbidden Patterns。**
5. **不破坏 Core 层零依赖约束**（Core 不得引用 Avalonia 或 CommunityToolkit.Mvvm）。
6. **不破坏 DESIGN.md 的单源真理规则。**

## Requirements — P0（立即修复，6 项）

### R1: 激活 `LauncherSettings.SchemaVersion` 版本检查
- **问题**: `[JsonIgnore]` 使版本检查变成死代码
- **修复**: 移除 `LauncherSettings.SchemaVersion` 的 `[JsonIgnore]`，确保版本号写入 JSON 并在加载时校验
- **文件**: `LauncherSettings.cs:11`, `LauncherSettingsStore.cs:27`
- **验证**: 新增测试验证 schema version 序列化/反序列化和不匹配检测

### R2: 修复 `ToDictionary` 重复 ID 崩溃
- **问题**: `MainWindowViewModel.Selection.cs:119` 的 `ToDictionary` 在重复等效 ID 时抛出未处理的 `ArgumentException`
- **修复**: 使用 `GroupBy` 或添加去重守卫，在重复时给出清晰错误而非崩溃
- **文件**: `MainWindowViewModel.Selection.cs`

### R3: Store.Load 损坏文件时回退到 .bak
- **问题**: `LauncherConfigStore.Load` / `LauncherPresetStore.Load` / `LauncherSettingsStore.Load` 仅在文件丢失时恢复备份，文件存在但损坏时不尝试
- **修复**: 在 `try-catch (JsonException | InvalidDataException)` 中捕获反序列化失败，回退到 .bak 恢复
- **文件**: `LauncherConfigStore.cs`, `LauncherPresetStore.cs`, `LauncherSettingsStore.cs`

### R4: ErrorLog 添加大小限制
- **问题**: 日志无限增长，无轮转无大小限制
- **修复**: 添加基于文件大小的轮转（如 1MB 上限，保留 3 个历史文件），或添加按日期归档
- **文件**: `ErrorLog.cs`

### R5: App.axaml.cs 启动异常捕获添加 IsRecoverable 过滤器
- **问题**: `catch (Exception ex)` 缺少 `when (ExceptionFilters.IsRecoverable(ex))`，与项目其余 ~20 处不一致
- **修复**: 添加异常过滤器
- **文件**: `App.axaml.cs:141`

### R6: LaunchAsync 检查 SaveConfig() 返回值
- **问题**: `SaveConfig()` 失败时静默丢失启动时间戳
- **修复**: 检查返回值，失败时保持错误状态不覆盖
- **文件**: `MainWindowViewModel.Launch.cs:91-93`

## Requirements — P1（近期修复，12 项）

### R7: UpdatePreset 规范化 presetId
- **修复**: 在名称冲突检查前对 `presetId` 执行 `LauncherPresetIds.NormalizeReference`
- **文件**: `LauncherPresetWorkflow.cs:45`

### R8: SearchHistory 截断方向验证
- **问题**: `RemoveRange(MaxSearchHistoryTerms, ...)` 保留前 20 条，需确认是否符合预期
- **修复**: 代码审查确认 `RecordSearchTerm` 中新条目插入到索引 0（开头），因此前 20 条是最新的。行为正确，但需加注释说明方向约定
- **文件**: `LauncherSettingsStore.cs:136-137`, `MainWindowViewModel.Layout.cs:247-273`

### R9: 统一 StartBatch 和 PreflightBatch 失败策略
- **问题**: PreflightBatch 继续处理后续条目，StartBatch 一次失败后停止
- **修复**: 将 `break` 改为 `continue`（如果启动失败不阻止后续条目），或至少在代码注释中明确说明设计意图
- **文件**: `LauncherLaunchWorkflow.cs:74-89`

### R10: 统一 ID 规范化行为
- **问题**: `LauncherEntryIds.Normalize`（静默截断）vs `LauncherPresetIds.Normalize`（抛异常）
- **修复**: 统一为一致行为——推荐静默截断 + 日志警告
- **文件**: `LauncherEntryIds.cs`, `LauncherPresetIds.cs`

### R11: ReplaceEntries 后重置 _keyboardFocusedEntryId
- **修复**: 在 `ReplaceEntries` 末尾添加 `_keyboardFocusedEntryId = null` + PropertyChanged 通知
- **文件**: `MainWindowViewModel.Entries.cs:281-290` 或 `MainWindowViewModel.cs`

### R12: 选择操作前调用 DiscardDragReorderPreview
- **修复**: 在 `SelectVisible`、`ClearSelection`、`ClearVisibleSelection` 开头添加取消调用
- **文件**: `MainWindowViewModel.Selection.cs`

### R13: 添加 DragDrop handler 的 Remove 调用
- **修复**: 在 MainWindow 的 `Closing` 或 `OnUnloaded` 中添加 `DragDrop.RemoveDragOverHandler` / `RemoveDropHandler`
- **文件**: `MainWindow.axaml.cs:62-63`

### R14: 消除 LauncherStartupArguments 静态可变状态
- **修复**: 重构为返回 `(string? configPath, IReadOnlyList<string> unknownArgs)` 元组
- **文件**: `LauncherStartupArguments.cs` 及所有调用方

### R15: RenameEntryId 在旧 ID 不存在时给出反馈
- **修复**: 在 `RenameEntryId` 开始时验证旧 ID 存在，不存在时返回 false 或抛异常
- **文件**: `LauncherEntryReferences.cs`

### R16: UI 层添加 MaxLength 限制
- **修复**: 在 `EntryDetailsWindow` 的文本框上添加与 Store 层一致的 MaxLength（command 4096, id 256 等）
- **文件**: `EntryDetailsWindow.cs`（或其 .axaml）

### R17: 空模板字典时的 $key 引用检测
- **修复**: 即使 templates 为空/Count==0，也扫描 command 中的 `$key` 引用并报告缺失
- **文件**: `LauncherCommandTemplateResolver.cs`

### R18: 暂无（P1 共 12 项）

## Requirements — P2-P3（代码质量优化，12 项）

### R19: 消除 LauncherPresetFiles.Clone 冗余遍历
### R20: 合并 LauncherConfigStore 双重 JSON 解析
### R21: DRY 消除 RenameEntryId/RemoveEntryId 重复代码
### R22: Zip 操作显式排序保护
### R23: LauncherEntryDrafts.ValidateEntry O(n²) 优化
### R24: ErrorLog 并发丢弃添加注释说明
### R25: ExceptionFilters 移除 StackOverflowException 死代码或加注释
### R26: LauncherEntryText 添加 Unicode 正规化
### R27: LaunchArgumentBuilder 添加命令行长度检查
### R28: LauncherPresetFiles 10000 项限制优化
### R29: LauncherDragGhostLayoutCalculator 负值防护
### R30: 内部注释标记替换为可理解的注释

## Acceptance Criteria

1. ✅ `dotnet test -c Release` 全部 556+ 测试通过（零回归）
2. ✅ `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` 全量门禁通过
3. ✅ 所有 6 项 P0 修复完成，有对应测试覆盖
4. ✅ 所有 12 项 P1 修复完成
5. ✅ 所有 12 项 P2-P3 完成或明确记录为有意不修
6. ✅ 零违反 quality-guidelines.md 的 Forbidden Patterns
7. ✅ 零破坏 DESIGN.md 单源真理规则
8. ✅ 无新增 `#pragma warning disable` 无注释
9. ✅ 无新增 `catch {}` 吞异常
10. ✅ Core 层零外部依赖（编译时验证）

## Out of Scope

- 不新增功能特性
- 不修改 .axaml 视觉布局
- 不调整 DESIGN.md 设计契约

## Open Questions

1. **R9 失败策略**: `StartBatch` 的 `break`→`continue` 是否与产品意图一致？推荐改为 `continue`（能启动多少就启动多少），与 `PreflightBatch` 行为统一。
2. **R10 规范化行为**: `LauncherPresetIds.Normalize` 超长抛异常→改为静默截断是否可接受？推荐统一为静默截断 + 日志。
3. **R8 SearchHistory 方向**: 已通过代码审查确认方向正确（新条目插入索引 0），只需添加注释。是否需要额外测试覆盖？
