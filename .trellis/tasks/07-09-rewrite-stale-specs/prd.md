# 重写 6 个严重过时 spec

## Goal

代码已多次重构(commit 3c394b3 三文件→单文件、87c2626 移除 Normalize pipeline、c740057 inline PresetMutationSnapshot、3ce3619 删除 LiteDB),但 6 个 spec 文档未同步,仍引用虚构文件、已删 API、错误 schemaVersion。修齐它们,让 spec 与代码一致。

## Files to Rewrite

### 1. `spec/core/directory-structure.md` (140 行)
- L25: "26 files" → "25 files"
- L33: `LauncherStore.cs` 描述去 "normalize"
- L51-55: 删除 IStore 抽象,改为直接调 `LauncherJson`
- L60-61: 删除重复 `LauncherEntryMutationSnapshot.cs` 行;无 `Save()`/`RestoreSnapshotOnPersistenceFailure()` 方法,只保留 `Capture`/`Clone*` API
- L67: "(9 partial class files)" → "(1 file, 2796 lines, god-class awaiting split)"
- L73-86: 删除虚构的 10 个 partial 文件表
- L108: "26 files" → "25 files"
- L112: "34 files" → "20 files"
- L113: "15 files" → "13 files"
- L114: "71 xUnit test files" → "61 xUnit test files"

### 2. `spec/core/persistence.md` (397 行)
- L83-87: Config Files 表(config/presets/settings.json)→ 单 launcher.json
- L95-103: Validation Pipeline 移除 NormalizeEntries/ValidateEntrySelectionFields 引用
- L106-150: First-Run Bootstrap + Backup Recovery 改为单文件 + `LauncherStoreFile`
- L152-167: ExpectedSchemaVersion = 2 (顶层 + settings)
- L176-203: Anti-Loop Pattern 改为单文件
- L205-227: Multi-File Atomic Save → Single-File Atomic Save (LauncherJson 内部 temp+rotate)
- L229-244: Schema Migration Contract 更新到 schemaVersion=2
- **L287-396: 整段 IStore + LiteDB + JSON→LiteDB Migration 全删**(commit 3ce3619 已彻底删除)

### 3. `spec/core/error-handling.md` (223 行)
- L104-126: Stage-All-Then-Commit → 单文件原子保存(LauncherJson.Save 内置 temp+rotate+atomic move)
- L106: `LauncherEntryMutationSnapshot.Save()` 方法不存在,改为引用实际位置(`MainWindowViewModel` 内的 save 路径或省略具体签名)
- L132: `RestoreSnapshotOnPersistenceFailure()` 不存在;实际 catch 在 `MainWindowViewModel` 内;改引用通用规则 + 现有 catch 位置
- L74-101: Parse/Normalize Pattern 描述准确(Parse/Normalize 方法仍存在),保留
- L11 "no custom Result<T,E>" — 仍准确

### 4. `spec/core/quality-guidelines.md` (172 行)
- L39: "49 test files (~410 tests)" → "61 test files"
- L40: "457 tests" → "451 tests"
- L46-50: Test category counts 全更新(用 wc 重新统计)
- L161: Multi-file saves checklist 项改为 "single-file atomic save"
- L88: IStore ponytail — 描述历史,保留

### 5. `spec/views/directory-structure.md` (59 行)
- L13-15: 重复 3 次的 `MainWindow.Drag.cs` 行去重
- 删除虚构 `(inline).cs` 文件
- 列出实际 13 个文件

### 6. `spec/views/drag-drop.md` (96 行)
- L13/L17/L20/L28/L36/L42: 删除所有 `(inline)` 后缀虚构文件
- L17 "MainWindow.Drag.cs (inline).cs" → `MainWindow.Drag.cs` (统一)
- L28 source path 去 `(inline)`
- L36 test path 去 `(inline)` 或确认实际文件名
- L56: `LauncherDragReorderSession.cs` ✓
- L73: `MainWindowViewModel.Reorder.cs` → `MainWindowViewModel.cs` (Reorder commands in main file until god-class split)
- 新增提及 `LauncherDragReorderWorkflow.cs`

## Requirements

- R1: 每个文件中描述的 API、文件路径、文件计数必须与代码一致(用 grep/ls 验证)
- R2: 不引入新设计、新规则 — 只同步现状
- R3: 保留历史标注如 "(removed 2026-07-06)" — 它们解释 why,不误导
- R4: 保留英文,与现有 spec 一致
- R5: persistence.md 的 LiteDB/IStore 整段直接删除,不归档(已 archive 在 retired/)

## Acceptance Criteria

- [ ] 6 个文件中,所有引用的 `.cs` 文件路径实际存在(grep 验证)
- [ ] 文件计数与 `ls | wc -l` 一致
- [ ] schemaVersion 引用统一为 2(launcher.json 顶层 + settings)
- [ ] 无 IStore / LiteDb / NormalizeEntries / ValidateEntrySelectionFields / RestoreSnapshotOnPersistenceFailure 等已删符号引用
- [ ] `dotnet test` 仍 451 passing(没改代码,只是文档)
- [ ] `tools/verify.ps1` 仍 PASS

## Notes

- 单次 commit 完成
- 不动代码,只动文档
- 不动 `data-types.md`、`README.md`、`feature-reality-north-star-audit.md`(没过时)
