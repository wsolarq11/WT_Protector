# Implement — 类型驱动架构重构

> 执行计划：有序检查清单、验证命令、风险文件和回滚点。

---

## Phase 1: 类型加固

### Step 1.0: Result 基础类型（预备）

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 1.0.1 | 创建 `Result<T, E>` 和 `Unit` | `Core/Result.cs` | 低 |
| 1.0.2 | 创建 `IdError`、`NameError`、`DirectoryError` 错误类型 | `Core/Errors.cs` | 低 |
| 1.0.3 | 为每个错误类型添加 `ToUserMessage()` 方法 | 同上 | 低 |
| 1.0.4 | 验证：`dotnet test -c Release --filter "Result"` | — | — |

### Step 1.1: Value Objects

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 1.1.1 | 创建 `EntryId` + `EntryIdJsonConverter` | `Core/EntryId.cs` | 低 |
| 1.1.2 | 创建 `EntryName` | `Core/EntryName.cs` | 低 |
| 1.1.3 | 创建 `EntryDirectory`（含 `TryParse` 的 `%` 注入检测） | `Core/EntryDirectory.cs` | **高** — C1 修复核心 |
| 1.1.4 | 创建 `PresetId` | `Core/PresetId.cs` | 低 |
| 1.1.5 | 创建 `PresetName`（含 256 限制） | `Core/PresetName.cs` | **中** — C3 修复核心 |
| 1.1.6 | 创建 `ShellCommand`（可配置 shell 路径） | `Core/ShellCommand.cs` | 低 |
| 1.1.7 | 验证：`dotnet build -c Release` 零错误 | — | — |

### Step 1.2: JSON 序列化兼容

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 1.2.1 | 为每个 value object 创建 `JsonConverter` | 各自的 .cs 文件 | 中 |
| 1.2.2 | 在 `LauncherJson.Options` 注册 converters | `Core/LauncherJson.cs` | 中 |
| 1.2.3 | 验证：`dotnet test -c Release` — 所有 JSON 反序列化测试通过 | — | — |

### Step 1.3: 旧代码标记 [Obsolete]

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 1.3.1 | `LauncherEntryIds` 方法委托给 `EntryId.TryParse` | `Core/LauncherEntryIds.cs` | 中 |
| 1.3.2 | `LauncherEntryText` 方法委托给 `EntryName.TryParse` | `Core/LauncherEntryText.cs` | 中 |
| 1.3.3 | `LauncherPresetIds` 方法委托给 `PresetId.TryParse` | `Core/LauncherPresetIds.cs` | 中 |
| 1.3.4 | `LaunchArgumentBuilder` 删除重复验证（保留 QuoteArgument/编码等安全逻辑） | `Core/LaunchArgumentBuilder.cs` | **高** |
| 1.3.5 | 验证：`dotnet build -c Release` 零错误 | — | — |

### Step 1.4: LaunchEntry/LauncherPreset 类型升级

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 1.4.1 | `LaunchEntry.Id` → `EntryId`（含 JsonConverter） | `Core/LaunchEntry.cs` | 中 |
| 1.4.2 | `LaunchEntry.Name` → `EntryName` | 同上 | 中 |
| 1.4.3 | `LaunchEntry.Directory` → `EntryDirectory` | 同上 | **高** |
| 1.4.4 | `LaunchEntry.Command` → `EntryName`（command 使用相同的 FormKC 规范化） | 同上 | 低 |
| 1.4.5 | `LauncherPreset.Id` → `PresetId` | `Core/LauncherPreset.cs` | 中 |
| 1.4.6 | `LauncherPreset.Name` → `PresetName` | 同上 | 中 |
| 1.4.7 | `LauncherSettings` 添加 `ShellPath` 字段（默认 "pwsh"） | `Core/LauncherSettings.cs` | 低 |
| 1.4.8 | 验证：`dotnet test -c Release` — 所有现有测试通过 | — | — |

### Step 1.5: Call Site 迁移

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 1.5.1 | 修复 ViewModels 层所有 `[Obsolete]` 警告 | ~15 个文件 | 中 |
| 1.5.2 | 修复 Views 层所有 `[Obsolete]` 警告 | ~3 个文件 | 低 |
| 1.5.3 | 修复 Tests 层所有 `[Obsolete]` 警告 | ~15-20 个文件 | 中 |
| 1.5.4 | 验证：`dotnet build -c Release` 零警告 | — | — |
| 1.5.5 | 验证：`dotnet test -c Release` 全部通过 | — | — |

### Step 1.6: 全门验证

```powershell
# Phase 1 完成门
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

---

## Phase 2: Result 类型推广

### Step 2.1: Core 层 API 改造

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 2.1.1 | `LauncherJson.Load<T>` → `Result<T, LoadError>` | `Core/LauncherJson.cs` | 中 |
| 2.1.2 | `LauncherJson.LoadRequired<T>` → `Result` 包装 | 同上 | 中 |
| 2.1.3 | `LauncherJson.Save<T>` → `Result<Unit, SaveError>` | 同上 | 中 |
| 2.1.4 | `LauncherStore.Load` → `Result<LauncherStoreFile, LoadError>` | `Core/LauncherStore.cs` | 中 |
| 2.1.5 | `LauncherStore.Save` → `Result<Unit, SaveError>` | 同上 | 中 |
| 2.1.6 | 验证：`dotnet test -c Release` — Core 测试通过 | — | — |

### Step 2.2: ViewModel 层改造

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 2.2.1 | `SaveStoreFile` 返回 `Result` 替代 `string?` | `MainWindowViewModel.cs` | 中 |
| 2.2.2 | `SaveConfig` 返回 `Result` | 同上 | 中 |
| 2.2.3 | `Refresh` 使用 `result.Match()` 替代 `try-catch` | 同上 | 中 |
| 2.2.4 | `LaunchAsync` 错误路径使用 `Result` | `MainWindowViewModel.Launch.cs` | 中 |
| 2.2.5 | 所有 `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` → `result.Match()` | ~10 个文件 | 中 |
| 2.2.6 | `ExceptionFilters.IsRecoverable` 统计调用次数 → 目标减少 ≥80% | — | — |
| 2.2.7 | 验证：`dotnet test -c Release` — 全部测试通过 | — | — |

### Step 2.3: 全门验证

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

---

## Phase 3: 持久化替换

### Step 3.1: LiteDB 集成

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 3.1.1 | 添加 `LiteDB` NuGet 包到 Core | `Core.csproj` | 低 |
| 3.1.2 | 创建 `IStore<T>` 接口 | `Core/IStore.cs` | 低 |
| 3.1.3 | 实现 `LiteDbStore : IStore<LauncherStoreFile>` | `Core/LiteDbStore.cs` | **高** |
| 3.1.4 | 为 LiteDB 注册 BSON mapper（EntryId 等 value object 的序列化） | 同上 | 中 |
| 3.1.5 | 验证：`dotnet build -c Release` | — | — |

### Step 3.2: 迁移工具

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 3.2.1 | 创建 `MigrationTool`：store.json → store.db | `Core/MigrationTool.cs` | **高** |
| 3.2.2 | 启动时自动检测 + 迁移（含备份） | `Program.cs` | **高** |
| 3.2.3 | 迁移后验证：读回数据对比 checksum | 同上 | 中 |
| 3.2.4 | 验证：创建临时 JSON → 迁移 → 验证数据完整性 | — | — |

### Step 3.3: ViewModel 适配

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 3.3.1 | `MainWindowViewModel` 构造函数接受 `IStore<LauncherStoreFile>` | 构造函数 | 中 |
| 3.3.2 | 所有 `LauncherStore.Save/Load` → `_store.Save/Load` | ~5 个文件 | 中 |
| 3.3.3 | `SaveStoreFile` → `_store.Save(storeFile, _storePath)` | `MainWindowViewModel.cs` | 中 |
| 3.3.4 | 验证：`dotnet test -c Release` — 全部测试通过（测试用 JsonStore） | — | — |

### Step 3.4: 清理

| # | 任务 | 文件 | 风险 |
|---|------|------|------|
| 3.4.1 | 删除 `CommitJournal.cs` 和 `Program.cs` 中的 `RecoverStaleCommitJournals()` | 2 个文件 | 低 |
| 3.4.2 | 删除 `LauncherJson.Save` 中的 temp-file/backup-rotation | `Core/LauncherJson.cs` | 中 |
| 3.4.3 | 删除 `LauncherJson.LoadWithRecovery` | 同上 | 中 |
| 3.4.4 | 删除 `LauncherJson.CleanupStaleTempFiles` | 同上 | 低 |
| 3.4.5 | 删除 Phase 1 中标记的 `[Obsolete]` 方法 | 5 个文件 | 低 |
| 3.4.6 | `LauncherJson` 缩减为仅 `Load<T>`（用于迁移）+ `Options` | `Core/LauncherJson.cs` | 中 |

### Step 3.5: 全门验证

```powershell
# Phase 3 完成门
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
# 额外检查：无 CommitJournal 引用
grep -r "CommitJournal" src/
# 结果应为空
```

---

## 风险文件清单

Phase 1 最高风险文件：
- `Core/EntryDirectory.cs` — C1 修复是否正确实现
- `Core/PresetName.cs` — C3 修复是否正确实现
- `Core/LaunchArgumentBuilder.cs` — 删除重复验证时不能误删安全逻辑

Phase 2 最高风险文件：
- `Core/LauncherJson.cs` — 改为 Result 返回不能改变语义
- `MainWindowViewModel.Launch.cs` — 错误路径改造后不能遗漏

Phase 3 最高风险文件：
- `Core/MigrationTool.cs` — 数据迁移不能丢数据
- `Program.cs` — 启动流程中的迁移逻辑不能阻塞正常启动
- `Core/LiteDbStore.cs` — BSON 映射必须与 JSON 语义一致

## 回滚点

每个 Phase 的 Step 完成后都可以回滚：
- Phase 1 Step 1.4 后 → store.json 兼容性验证
- Phase 2 Step 2.2 后 → ExceptionFilters 减少验证
- Phase 3 Step 3.2 后 → 迁移工具验证后再删除旧代码
