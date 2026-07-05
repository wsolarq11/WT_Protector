# 修复 UltraReview 全部发现 — 执行计划

## 批次划分

按依赖关系和安全风险分 6 批执行，每批独立验证。

---

## Batch 0: 文档先行 (D01-D10)

**目标**：同步文档与当前代码实现，为后续代码修复提供准确参考

| Step | 文件 | 改动 | 风险 |
|------|------|------|------|
| D01 | `README.md` | 更新统一 LauncherStoreFile 架构描述 | LOW |
| D02 | `.trellis/spec/core/directory-structure.md` | 删除已不存在文件引用 | LOW |
| D03 | `.trellis/spec/core/quality-guidelines.md` | 添加 LiteDB 例外注释 | LOW |
| D04 | `feature-reality-north-star-audit.md` | 统一特征计数 | LOW |
| D05 | `.trellis/spec/tests/index.md` | 统一测试计数 | LOW |
| D06 | `quality-guidelines.md` + `persistence.md` | 统一 File.Move 原子性声明 | LOW |
| D07 | `feature-first-class-upgrade-audit.md` | 更新 searchHistory 状态 | LOW |
| D08 | `design-benchmark-digest.md` | 更新刷新日期和证据 | LOW |
| D09 | 各 spec/index.md | 统一语言声明 | LOW |
| D10 | `LauncherConfigFile.cs` | 删除 obsolete 文件 | LOW |

**验证**: `git diff --check` + `tools/verify.ps1`

---

## Batch 1: CRITICAL 修复 (F01-F05)

**前提**：无

| Step | 文件 | 改动 | 验证 |
|------|------|------|------|
| F05 | `MainWindow.axaml.cs:178` | 最快修复：移除 `ReferenceEquals(e.Source, SearchTextBox)` | 手动验证 Enter 保存搜索历史 |
| F04 | `ShellCommand.cs` + `LaunchArgumentBuilder.cs` | 删除 ShellCommand.cs 和 json converter；LaunchArgumentBuilder 保持硬编码 | `dotnet build` |
| F03 | `LauncherPresetWorkflow.cs` | 在每个公有方法首行添加 `if (presetFile.Presets is null) throw` | 现有测试通过 |
| F01 | `LiteDbStore.cs` | 添加 `private readonly string _dbPath` + storePath 校验 | `dotnet test` |
| F02 | `LiteDbStore.cs:62-63` | 用 `collection.Upsert(...)` 替代 `DeleteAll`+`Insert` | `dotnet test` |

**验证**: `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln`

---

## Batch 2: HIGH 代码修复 (F06-F12)

**前提**：Batch 1 通过

| Step | 文件 | 改动 | 风险 |
|------|------|------|------|
| F10 | `LauncherStartupArguments.cs` | 添加 UNC 路径拒绝 | LOW |
| F07 | `MainWindowViewModel.Presets.Persistence.cs` | 坏 store 降级 | MEDIUM |
| F08 | `MainWindowViewModel.Presets.Persistence.cs` | 调整写入时序 | MEDIUM |
| F09 | `MainWindowViewModel.Presets.cs` | 添加 try-catch | LOW |
| F11 | `MainWindowViewModel.Entries.cs` | Edit/Delete 回滚引用修复 | HIGH |
| F12 | `LiteDbStore.cs` | 添加 schema 版本检查 | LOW |
| F06 | `LauncherStore.cs` + `MigrationTool.cs` | 旧三文件迁移 | MEDIUM |

**验证**: `dotnet test` + 手动启动验证配置加载

---

## Batch 3: HIGH 测试补充 (F13)

**前提**：Batch 2 通过

| Step | 改动 |
|------|------|
| 添加 `LauncherStoreLoadFailureTests.cs` | 损坏 JSON、空文件、缺失文件、权限拒绝测试 |
| 添加 `LauncherStoreBackupRecoveryTests.cs` | 备份恢复路径测试 |

**验证**: `dotnet test`

---

## Batch 4: MEDIUM 修复 (M01-M20)

**前提**：Batch 3 通过。按"低风险→中风险"顺序执行。

| Step | 文件 | 改动 |
|------|------|------|
| M10 | `ExceptionFilters.cs` | break on null InnerException |
| M15 | `verify.yml` | dotnet test + --logger trx |
| M16 | `verify.yml` | 移除冗余 powershell |
| M17 | `*.ps1` | 添加 StrictMode |
| M13 | `LauncherDragReorderSessionTests.cs` | TempDirectory using |
| M14 | `DragTests.cs` | 删除 dead code |
| M09 | `MainWindowViewModel.Entries.cs` | 规范化 cloneId |
| M18 | `MainWindowViewModel.Selection.cs` | DryRunText OnCancel |
| M06 | `MainWindow.axaml.cs` | 清空 delegate |
| M07 | `MainWindow.Drag.cs` | hover 防抖 |
| M08 | `MainWindow.Drag.cs` | ForceCleanup 重入保护 |
| M01 | `LauncherJson.cs` | MaxDepth = 64 |
| M04 | `LauncherStore.cs` | schema version 注释 |

**验证**: `dotnet test` + `tools/verify.ps1`

---

## Batch 5: LOW 修复 (L01-L22)

**前提**：Batch 4 通过。全部为低风险改动，可批量执行。

| Step | 文件 | 改动 |
|------|------|------|
| L03 | `MainWindow.axaml.cs` | Closing 反注册自身 |
| L04 | `MainWindow.Drag.cs` | FontFamily static readonly |
| L10 | `EntryId.cs` | IsNullOrWhiteSpace |
| L11 | `PresetId.cs` | Trim |
| L14 | `config-lib.ps1` | $null -ne |
| L16 | `verify.yml` | NuGet cache |
| L18 | `build-release.ps1` | v 前缀处理 |
| 其余 | 各处 | 格式/注释/可读性 |

**验证**: `dotnet build -warnaserror` + `dotnet test`

---

## 最终验证 Gate

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
git diff --check
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln
```

## 高风险文件清单

- `LiteDbStore.cs` — 数据持久化核心，F01/F02/F12 三处修改需确保向后兼容
- `MainWindowViewModel.Entries.cs` — F11 Edit/Delete 回滚引用修复，涉及 rollback 路径
- `MigrationTool.cs` + `LauncherStore.cs` — F06 新增迁移路径，不能破坏已有 store.json 用户
- `MainWindowViewModel.Presets.Persistence.cs` — F07/F08 影响 preset 选择逻辑

## 回滚点

每次 Batch 完成后创建 git commit：
```
git commit -m "fix(ultrareview): Batch N — <description>"
```
任一 Batch 失败则回退到上一个 commit。
