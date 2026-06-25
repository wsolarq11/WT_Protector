# Design: UltraReview 全部发现清零

## Architecture — 修复边界

```
Core (纯规则层)          App (ViewModel+View)       Tests (xUnit)
─────────────────       ─────────────────────       ────────────
LaunchArgumentBuilder   MainWindowViewModel         *Tests.cs
LauncherEntryIds        MainWindow.axaml.cs
LauncherEntryVisibility LauncherLaunchWorkflow
LauncherJson            各 *Persistence.cs
LauncherConfigStore     LauncherDragReorderWorkflow
LauncherPresetStore     MainWindow.Drag.cs
LauncherSettingsStore   LauncherEntryViewModel
LauncherEntryOrder      各 *Snapshot.cs
LaunchPlan              LauncherBatchLaunchReport
LauncherEntryReferences FastWrapPanel
LauncherPresetWorkflow  LauncherPresetWorkflow (引用)
LauncherCommandTemplateResolver
```

修复原则：
1. Core 层修复 → 纯函数级别，每个修复有对应单元测试
2. App 层修复 → ViewModel 契约级别，通过现有集成测试回归验证
3. 所有修改通过 `with { }` 不可变模式
4. 零破坏 config/presets/settings JSON schema

## Phase 1: Critical + High (20项)

### 安全修复

**UNC 路径阻止 (R2.1)**
- 位置: `LaunchArgumentBuilder.Build()`, directory 计算后
- 方案: 添加 UNC 路径检测 `directory.StartsWith(@"\\")` → throw
- 测试: 新增 `LaunchArgumentBuilderTests.Build_RejectsUncDirectory()`

**wt.exe 路径加固 (R2.2)**
- 位置: `ResolveTerminalExecutable()`
- 方案: 移除裸名 `"wt.exe"` 回退；搜索多个已知位置；最终找不到则 throw FileNotFoundException
- 测试: 新增 `ResolveTerminalExecutable_ThrowsWhenNotFound()`

**全角% 防御 (R2.3)**
- 位置: `LaunchArgumentBuilder.Build()`, ExpandEnvironmentVariables 之前
- 方案: 检测全角百分号 U+FF05 并抛出明确错误；仅在规范化后且展开前检测
- 测试: 新增 `Build_RejectsFullwidthPercentInDirectory()`

**BiDi/格式字符剥离 (R2.4)**
- 位置: `StripControlCharacters()`
- 方案: 扩展为同时剥离 Cf 类 (UnicodeCategory.Format) 字符
- 测试: 新增 `StripControlCharacters_StripsBidiFormatChars()`

### 数据完整性修复

**ID Unicode 规范化 (R2.5)**
- 位置: `LauncherEntryIds.Normalize()`
- 方案: 在 Trim 后添加 `Normalize(NormalizationForm.FormC)`
- 注意: 使用 FormC（组合）而非 FormKC（兼容），因为 ID 不应进行兼容分解
- 测试: 新增 `Normalize_PreservesSemanticIdentity()`

**LastUsedPresetId 修复 (R2.6)**
- 选项: 完全移除 dead feature（推荐）
- 位置: `LauncherSettings.cs:42-44` 移除属性；`MainWindowViewModel.cs:134` 无需修改；`Presets.Helpers.cs:106-118` 移除持久化代码
- 理由: 属性是 [JsonIgnore] + 无消费者 = 死代码，每次预设选择产生不必要 I/O
- 测试: 验证 OnSelectedPresetChanged 不触发 settings 写入

**UpdatePreset IndexOf → ID 查找 (R2.7)**
- 位置: `LauncherPresetWorkflow.cs:50-51`
- 方案: 替换为基于 ID 的索引查找循环
- 测试: 现有 PresetViewModel 测试回归

### 异常安全修复

**备份无限循环 (R1.1)**
- 位置: 所有 3 个 Store 的 Load 方法
- 方案: 将备份恢复调用移到 try/catch 外部，或添加 `bool alreadyAttemptedRecovery` 标志
- 推荐: 提取 recovery lambda，在 try 外部调用一次
- 测试: 新增 `Load_CorruptMainAndBackup_ThrowsWithoutLooping()`

**代理对截断 (R1.2)**
- 位置: `LauncherEntryIds.Normalize()`, `LauncherPresetIds.Normalize()`
- 方案: 截断前检查 `char.IsHighSurrogate(span[^1])`，必要时调整截断位置
- 测试: 新增 `Normalize_TruncatesAtCodePointBoundary()`

**TOCTOU 竞态 (R1.3)**
- 位置: 所有 Store Load 方法
- 方案: 扩展 catch 过滤条件包含 `FileNotFoundException`
- 测试: 模拟文件在 Exists 后删除的场景

**MutationPersistence InvalidDataException 一致性 (R2.8)**
- 位置: `LauncherConfigMutationPersistence.Save`, `LauncherSettingsMutationPersistence.Save`
- 方案: 添加 `InvalidDataException` 到 catch 过滤条件（与 Preset 版本对齐）
- 测试: 现有测试回归

**Release 构建诊断 (R2.9, R2.10)**
- 位置: `LauncherJson.cs`, `ErrorLog.cs`
- 方案: 将 `Debug.WriteLine` 替换为 `ErrorLog.Write`（备份轮换失败、清理失败）
- 将空 catch 块替换为 `ErrorLog.Write` 调用
- 测试: 验证错误日志在损坏文件场景下有输出

**键盘焦点泄漏 (R2.11)**
- 位置: `MainWindow.axaml.cs:156-162`
- 方案: 在 visibleCount==0 Escape 处理中添加 `viewModel.KeyboardFocusedEntryId = null`
- 测试: 现有键盘导航测试回归

**拖拽决策泄漏 (R2.12)**
- 位置: `MainWindow.Drag.cs:23`
- 方案: 在 `DoDragReorderAsync` 开始处重置 `_lastDragInsertAfterDecision = false`
- 测试: 现有拖拽测试回归

**async void 异常安全 (R2.13)**
- 位置: `MainWindow.Drag.cs:27`
- 方案: 添加最外层 `catch (Exception)` 包裹，通过 `ErrorLog.Write` 记录
- 测试: 现有拖拽测试回归

**NRE 防御 (R2.14)**
- 位置: `MainWindowViewModel.cs:236-250`
- 方案: 在 `foreach` 循环体内添加 `if (e is null) continue`
- 测试: 现有 ViewModel 测试回归

**ErrorLog 并发丢弃诊断 (R2.15)**
- 位置: `ErrorLog.cs:52`
- 方案: 在 `return` 前添加 `Debug.WriteLine`（或添加 droppedCount 计数器）
- 测试: 无（并发测试昂贵且不可靠）

## Phase 2: Medium (22项)

### 文件 I/O 原子性

**非原子备份 → temp-file-then-rename (R3.1, R3.2)**
- 位置: `LauncherJson.cs:57-68`, 所有 Store 恢复路径
- 方案: Copy 到 tempPath → Move 到 .bak（与主文件相同的原子模式）
- 测试: 现有持久化测试回归

**fsync (R3.3)**
- 位置: `LauncherJson.cs:52`
- 方案: WriteAllText 后使用 `using var fs = File.OpenRead(tempPath); fs.Flush(true);`
- 注意: Windows 上 `Flush(true)` 调用 `FlushFileBuffers`

**NaN 窗口坐标 (R3.4)**
- 位置: `LauncherSettingsStore.cs:203-206`
- 方案: 将 `IsInfinity` 检查替换为 `!double.IsFinite`（同时拒绝 NaN）
- 测试: 新增 `NormalizeWindow_RejectsNaN()`

**多文件原子性 (R3.5)**
- 位置: `LauncherEntryMutationPersistence.cs`
- 方案: 引入 stage-all-then-commit 模式——所有文件先写入 temp，全部成功后依次 rename
- 风险: 中等（现有 best-effort rollback 对单进程桌面应用已足够）
- 优先级: 可在 Phase 2 末尾实现，或记录为已知限制

### 其他 Medium 项按推荐方案修复

(设计细节在 implement.md 中逐项展开)

## Phase 3: Low (30+项)

### 批量处理策略
- 同类型项合并为单次提交
- 代码质量项（注释、命名、readonly）→ 一次 pass
- 无障碍项（AutomationProperties）→ 一次 pass
- 设计令牌项 → 一次 pass
- 测试补充 → 一次 pass

## 不变约束

1. 每次修改后跑 `dotnet test`（542 baseline + 新增）
2. 每次修改后跑 `git diff --check`
3. Phase 完成后跑 `tools\verify.ps1`
4. JSON schema 零破坏——config/presets/settings 向后兼容
5. 不降低测试标准——不删除/弱化任何现有测试
