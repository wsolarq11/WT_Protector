# Fix UltraReview CRITICAL/HIGH findings — Design

## Overview

修复 UltraReview #8 发现的 7 个代码缺陷和 1 个文档腐烂问题。
所有修复独立、低风险，可在同一任务中按顺序实施。

## Changes

### C1: 修复 store.json 文件名不匹配

**文件：** `LauncherStore.cs:9`, `config-lib.ps1:68`, `run.ps1:12-20`

将 `LauncherStore.StoreFileName` 常量从 `"store.json"` 改为 `"launcher.json"`：
```csharp
public const string StoreFileName = "launcher.json";
```

同步更新：
- `tools/config-lib.ps1:68` — `Get-LauncherStorePath` 返回路径改为 `config\launcher.json`
- `src/avalonia-launcher/run.ps1:12-20` — `Get-RepoRoot` 寻找 `config\launcher.json`

**验证：** 应用启动后加载 `config/launcher.json` 中的全部条目，状态栏显示正确条目数。

---

### C2: 同 C1（run.ps1 同一类问题）

---

### C3: LiteDB 异常兜底

**文件：** `App.axaml.cs:126-128`

将 `LiteDbStore.Open` 包裹 try/catch：

```csharp
IStore store;
try
{
    store = string.IsNullOrEmpty(storeDir)
        ? new JsonStore()
        : LiteDbStore.Open(storeDir);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LiteException)
{
    ErrorLog.Write(ex, "LiteDbStore.Open");
    store = new JsonStore();
}
```

**验证：** 创建损坏的 `.db` 文件后启动，验证 JSON 后备生效，数据正常加载。

---

### H1: Refresh 重载设置

**文件：** `MainWindowViewModel.cs:136-185`

在 `Refresh()` 的 entries 重载后增加：

```csharp
_settings = storeFile.Settings;
_launchWorkflow.CommandTemplates = _settings.CommandTemplates;
```

注意需要重新解析 `SelectedSortMode`、`SelectedDisplayMode`、`FilterText` 等 UI 绑定属性。

---

### M1: LiteDB Load 调用 Normalize

**文件：** `LiteDbStore.cs:42-49`

在返回前对 `LauncherStoreFile` 做 Normalize：

```csharp
return LauncherStore.Normalize(new LauncherStoreFile { ... });
```

---

### M2: LiteDB Save 事务

**文件：** `LiteDbStore.cs:65-76`

```csharp
using var trans = _db.BeginTrans();
collection.DeleteAll();
collection.Insert(new StoreDocument { ... });
trans.Commit();
_db.Checkpoint();
```

---

### M5: ExceptionFilters DEBUG 重新抛出

**文件：** `ExceptionFilters.cs:31`

```csharp
#if DEBUG
if (ex is NullReferenceException or IndexOutOfRangeException)
    throw;
#endif
```

---

### M7: 互斥体 Local\

**文件：** `Program.cs:76`

```csharp
_instanceMutex = new Mutex(false, @"Local\WTLauncher_ConfigMutex", out bool createdNew);
```

---

### M8: CI 添加发布验证

**文件：** `.github/workflows/verify.yml`

在 `build-and-test` 作业中添加：

```yaml
- name: Publish (smoke check)
  run: dotnet publish src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -r win-x64 --self-contained
```

---

### M12-M14: 测试覆盖

- `EntryDirectoryTests.cs` — 正常路径、UNC、设备路径、%注入、不存在目录
- `MigrationToolTests.cs` — JSON→LiteDB 迁移成功路径、失败回退
- `JsonConverterTests.cs` — 5 个 JsonConverter 的反序列化错误测试

---

### H2: Spec 腐烂修复

更新 `.trellis/spec/` 下 14 个文件中的过时引用：
- `LauncherConfigStore` → `LauncherStore`
- `LauncherPresetStore` → `LauncherStore`
- `LauncherSettingsStore` → `LauncherStore`
- `LauncherEntryMutationPersistence` → `LauncherEntryMutationSnapshot`
- `MainWindowDragGhostFactory` → `MainWindow.Drag.cs` (或删除引用)
- 测试计数修正：71 文件 → ~49, 556 测试 → 455

## Rollback

每个修复独立、小范围，回滚只需 `git checkout -- <file>`。

## Compatibility

- store.json → launcher.json 改名后旧 store.json 不再被读取。用户需手动合并或删除 store.json
- 其他修复不涉及数据格式变更
