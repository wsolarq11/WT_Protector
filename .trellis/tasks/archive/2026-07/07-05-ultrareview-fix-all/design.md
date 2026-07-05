# 修复 UltraReview 全部发现 — 技术设计

## 概述

本设计覆盖 55 个发现的修复方案，按严重级分组并考虑依赖关系。所有修复均为局部改动，不引入架构重构。

## 修复策略

### P0 — CRITICAL 级 (5项)

| ID | 文件 | 问题 | 修复方案 |
|----|------|------|----------|
| F01 | `LiteDbStore.cs` | storePath 被忽略 | 添加 `_dbPath` 字段，Load/Save 校验 storePath == _dbPath |
| F02 | `LiteDbStore.cs` | 非原子写入 | 用 `collection.Upsert(new StoreDocument { Id = 1, ... })` + `Checkpoint()` 替代 DeleteAll+Insert |
| F03 | `LauncherPresetWorkflow.cs` | Presets null NPE | 在每个公有方法首行添加 null guard |
| F04 | `ShellCommand.cs` + `LaunchArgumentBuilder.cs` | Shell 配置死代码 | 删除 `ShellCommand.cs`、`ShellCommandJsonConverter`、`ShellCommand.ArgumentsSuffix` 引用；LaunchArgumentBuilder 保留硬编码行为 |
| F05 | `MainWindow.axaml.cs:178` | Enter 键检测失效 | 移除 `ReferenceEquals(e.Source, SearchTextBox)` 检查，仅保留 `!IsDropDownOpen` |

### P1 — HIGH 级 (8项)

| ID | 文件 | 问题 | 修复方案 |
|----|------|------|----------|
| F06 | `MigrationTool.cs` | 旧三文件迁移缺失 | 在 `LauncherStore.Load` 中添加检测：store.json 不存在但 config.json 存在时读取合并并写入 store.json |
| F07 | `ApplyPresetFileState` | 坏 store 启动崩溃 | 当 defaultPresetId 为空有 presets 时选择第一个 preset |
| F08 | `ApplyPresetFileState` | 先写后验 | 将 `_presetFile = presetFile` 移到 `ResolveSelectedPreset` 验证之后 |
| F09 | `SaveSelectionAsPresetAsync` | 缺 try-catch | 添加 try-catch + RestorePresetSnapshotAndReportMissing |
| F10 | `LauncherStartupArguments.cs` | UNC 路径 NTLM | 添加 `\\`/`//` 路径拒绝检查，与 LaunchArgumentBuilder 一致 |
| F11 | `EditEntryAsync` | 回滚引用孤立 | 将 item 修改移到保存成功之后 |
| F12 | `LiteDbStore.Load` | 缺 Schema 版本检查 | 在 Load 中添加 `doc.SchemaVersion > LauncherStore.ExpectedStoreSchemaVersion` 检查 |
| F13 | Tests | 缺错误路径测试 | 添加 `LauncherStore.Load` 的损坏 JSON/空文件/缺失文件测试 |

### P2 — MEDIUM 级 (20项)

| ID | 文件 | 修复方案 |
|----|------|----------|
| M01 | `LauncherJson.cs` | 显式设置 `MaxDepth = 64` |
| M02 | `LauncherStartupArguments.cs` | 统一 `--config path` 的路径前缀检查与 `--config=path` 一致 |
| M03 | `LiteDbStore.cs` | (与 F02 同根因，合并处理) |
| M04 | `LauncherStore.cs` | 在 Normalize 中添加 schema version 兼容性断言或文档记录 |
| M05 | `ErrorLog.cs` | 在 log switch 前确保 Drain 已完成（无操作，接受当前行为） |
| M06 | `MainWindow.axaml.cs` | OnClosing 中清空 5 个 delegate 引用 |
| M07 | `MainWindow.Drag.cs` | 在 ClearDragHover 中添加 50ms 防抖或在 ResolveCardAtPointer 中做距离判定 |
| M08 | `MainWindow.Drag.cs` | ForceCleanup 添加 `_cleaningUp` 标志防止重入 |
| M09 | `MainWindowViewModel.Entries.cs` | GenerateUniqueCloneId 预先 normalize sourceId |
| M10 | `ExceptionFilters.cs` | InnerException null 时跳出循环 |
| M11 | `ShellCommand.cs` | (与 F04 合并处理，删除) |
| M12 | `LiteDbStore.cs` | 数据库加密（标记为设计决策，当前不实施） |
| M13 | `LauncherDragReorderSessionTests.cs` | 4 个测试的 TempDirectory 泄漏，改为 `using var` |
| M14 | `DragTests.cs` | 删除 2 处 dead `dragStore` 变量 |
| M15 | `.github/workflows/verify.yml` | dotnet test 添加 `--logger trx` |
| M16 | `.github/workflows/verify.yml` | 移除 shell 内冗余 powershell 包装 |
| M17 | `*.ps1` | 所有 .ps1 文件添加 `Set-StrictMode -Version Latest` |
| M18 | `MainWindowViewModel.Selection.cs` | OperationCanceledException handler 设置 DryRunText |
| M19 | (说明性) | schema version 兼容性注释 |
| M20 | `MainWindow.Drag.cs` | _lastDragInsertAfterDecision 使用防抖计时器 |

### P3 — LOW 级 (22项)

主要修复方向：
- `MainWindow.axaml.cs:270` — Closing 事件反注册自身
- `MainWindow.Drag.cs:321` — FontFamily.Parse 提取为 static readonly
- `EntryId.cs:32` — 改用 IsNullOrWhiteSpace
- `PresetId.cs:22` — 添加 Trim
- `config-lib.ps1:94` — 用 `$null -ne` 替代 `!$json.entries`
- `.github/workflows/verify.yml` — 添加 NuGet 缓存
- `.github/workflows/verify.yml:60` — full-gate 在 PR 也运行
- `src/avalonia-launcher/verify.ps1:43` — regex 放宽
- `src/avalonia-launcher/verify.ps1:41` — 添加 `-Encoding UTF8`
- `tools/build-release.ps1:26-31` — git tag 去除 v 前缀
- 其余为格式、注释、可读性改进

### 文档同步 (10项)

| ID | 文件 | 改动 |
|----|------|------|
| D01 | `README.md` | 更新为统一 LauncherStoreFile 架构描述 |
| D02 | `.trellis/spec/core/directory-structure.md` | 删除已不存在的 4 个文件引用 |
| D03 | `.trellis/spec/core/quality-guidelines.md` | 添加 LiteDB 例外注释或更新规则 |
| D04 | `feature-reality-north-star-audit.md` | 统一特征计数 |
| D05 | `.trellis/spec/tests/index.md` | 统一测试文件/属性计数 |
| D06 | `quality-guidelines.md` + `persistence.md` | 统一 File.Move 原子性声明 |
| D07 | `feature-first-class-upgrade-audit.md` | 更新 searchHistory 实现状态 |
| D08 | `design-benchmark-digest.md` | 更新证据引用和刷新日期 |
| D09 | 各 spec/index.md | 统一语言声明 |
| D10 | `LauncherConfigFile.cs` | 删除 obsolete 文件 |

## 数据流

所有修复不改变整体数据流架构：
- 配置加载：`LauncherStoreFile` JSON → `IStore.Load` → ViewModel → UI
- 配置保存：UI → ViewModel → `IStore.Save` → JSON 文件
- 启动：ViewModel → `LaunchArgumentBuilder.Build` → `Process.Start`

## 回滚策略

每个 P0/P1 修复应独立验证，确保不破坏现有测试。高风险改动：
- F02 (LiteDB 原子写入)：先验证 `Upsert` 行为与现有 DeleteAll+Insert 等价
- F06 (迁移)：新增迁移路径，不影响已迁移用户
- F11 (回滚引用)：需验证 `SaveEntryCollectionChanges` 失败路径的恢复正确性

## 边界与兼容性

- `LauncherSettingsSnapshot.SchemaVersion=1` 硬编码问题建议同步修复（改为 `ExpectedStoreSchemaVersion`）
- 所有文档同步保留原有格式和风格，仅更新事实性错误
