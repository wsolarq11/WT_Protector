# Research: Serialization & Concurrency Audit

- **Query**: Ultrareview of all serialization, persistence, I/O, and concurrency code
- **Scope**: Internal (all specified source files)
- **Date**: 2026-07-26
- **Files reviewed**: 16 files across WTLauncher.Core and WTLauncher.App.Infrastructure

## 问题

审查所有序列化、持久化、I/O 与并发代码。

## 结论

1 个确认数据竞争的 medium（`ReloadFromDisk` TOCTOU）+ 3 个 medium（`Dispose` 未清理事件、`RotateBackups` 可无锁外调、嵌套锁阻塞 I/O）+ 4 个 low + 2 个 informational。序列化路径 AOT 安全、源生成注册完整、UTF-8 无 BOM 一致。

## 关键证据

| ID | 严重度 | 文件 | 行 | 问题 |
|---|---|---|---|---|
| S1 | LOW | EntryId.cs | 192-196 | `EntryIdJsonConverter.Read` 不处理 JSON null；`EntryId.Parse(null)` 抛 `FormatException` 而非返回 `EntryId.Empty`（与 `DirectoryPathJsonConverter` 不一致） |
| S2 | LOW | EntryName.cs | 74-78 | `EntryNameJsonConverter.Read` 的 null-to-empty 守卫经 converter 不可达；总是抛错 |
| S3 | LOW | FileLauncherJsonIO.cs | 124-149 | 备份轮换发生在 `File.Replace` 前；崩溃中段只移位备份链，旧内容保留，安全 |
| S4 | LOW | FileLauncherJsonIO.cs | 64-70, 90-93 | 恢复临时文件不内联清理，延迟到启动期 `CleanupStaleTempFiles` |
| S5 | INFO | LauncherJsonContext.cs | 1-24 | 源生成注册完整，所有传递类型可达 |
| S6 | INFO | Various | -- | 所有入口用 `JsonTypeInfo<T>`，无 `JsonSerializerOptions` 重载，AOT 安全 |
| S7 | INFO | FileLauncherJsonIO.cs | 16, 21-23, 122 | UTF-8 无 BOM，LF 行尾，与 `.gitattributes` 一致 |
| C1 | **MEDIUM** | StoreGrain.cs | 52-56 | **`ReloadFromDisk` TOCTOU 数据竞争**：锁外读盘，锁内 `SetState`，并发 `Dispatch` 可被静默覆盖 |
| C2 | **MEDIUM** | StoreGrain.cs | 88-91 | `Dispose` 不置 `StateChanged = null`；订阅引用泄漏；dispose 后 `Dispatch` 可触发已释放 grain |
| C3 | **MEDIUM** | FileLauncherJsonIO.cs, LauncherJson.cs | 204-220, 54-55 | `RotateBackups` 可无锁外调；与 `Save` 并发时备份链竞争 |
| C4 | MEDIUM | StoreGrain.cs, Grain.cs | 58-71, 44-49 | 嵌套锁：整个文件 I/O 期间持有 `Grain._lock`，阻塞所有 `Select` |
| C5 | LOW | FileLauncherJsonIO.cs | 111, 182 | `_saveLock.Wait()` 阻塞 UI 线程（同 App/VM ultrareview 的 M4） |
| C6 | LOW | StoreGrain.cs | 58-71 | `StateChanged` 在 `Grain._lock` 外触发；订阅方回调内 `Select()` 可能读到更新状态 |
| C7 | LOW | ErrorLog.cs | 27, 50, 79 | `Interlocked.Exchange` 提供完整 acquire/release 屏障，无需 `volatile`，正确 |

### Medium 详情

**C1 `ReloadFromDisk` TOCTOU（确认数据竞争）**

```csharp
public void ReloadFromDisk()
{
    var fresh = _persistence.Load(_storePath);  // (1) 锁外读盘
    SetState(fresh);                              // (2) 锁内设置
}
```

并发 `Dispatch` 在 (1)(2) 之间完成 `Save(Y)` 并通知订阅者 Y 后，`ReloadFromDisk` 又把内存覆写回旧值 X，与磁盘 Y 不一致，直到下次 `Dispatch`。当前代码库中两者都在 Avalonia UI 线程执行故不触发，但 `Dispatch`/`Select` 文档声明线程安全而 `ReloadFromDisk` 破坏该契约；调用模式一旦改为后台线程（`Task.Run`/定时器）即显现为静默状态损坏。

**C2 `Dispose` 未清理订阅者**

```csharp
public void Dispose()
{
    if (_persistence is IDisposable disposable) disposable.Dispose();
}
```

未置 `StateChanged = null`，也未阻止 dispose 后再 `Dispatch`。事件与订阅者互持引用导致泄漏；dispose 后 `Dispatch` 可能 `ObjectDisposedException` 或对已释放 grain 触发 `StateChanged`。

**C3 `RotateBackups` 可无锁外调**

```csharp
public static void RotateBackups(string fullPath, int maxBackups = 3)
    => IO.RotateBackups(fullPath, maxBackups);
```

`Save` 内调用持 `_saveLock`，但经 `LauncherJson.RotateBackups()` 静态门面无锁；并发时两个线程同时操作 `.bak/.bak.1/.bak.2`（`File.Delete` 可能删掉对方正在 `File.Move` 的文件）。当前无外部调用方，风险仅在未来开发。

**C4 嵌套锁**：调用链 `Dispatch → Mutate(获取 Grain._lock) → Save(获取 _saveLock) → 序列化/写盘/备份/Replace`。锁序恒为 `Grain._lock → _saveLock`；反向获取会死锁（当前 `CleanupStaleTempFiles` 只取 `_saveLock`，无反向）。

### Design observations (not bugs)

- `CleanupStaleTempFiles` 持 `_saveLock` 遍历临时文件期间阻塞所有 `Save`（进而因 `Grain._lock` 阻塞所有 `Select`）。
- `Grain._lock` 为 `Monitor` 可重入；mutator 内嵌套 `ReadState` 读到的是旧状态（技术上正确，现有 mutator 均用 lambda 参数计算）。

## 关联路径

- `.trellis/spec/core/serialization-guidelines.md`（如存在）— AOT 安全源生成、Loose Read + Strict Write
- `.trellis/spec/core/quality-guidelines.md` — 单真相源、窄化 catch、无裸 Exception
- `.trellis/spec/guides/pitfalls.md` — 关键词 `serialization` / `concurrency`

Files reviewed：`LauncherJson.cs`、`LauncherJsonSerializer.cs`、`LauncherJsonContext.cs`、`LauncherStoreFile.cs`、`ILauncherJsonIO.cs`、`IStorePersistence.cs`、`LauncherStore.cs`、`LauncherStoreValidator.cs`、`LauncherSettings.cs`、`LauncherPreset.cs`、`LegacyPresetDocument.cs`、`DirectoryPath.cs`、`EntryId.cs`、`EntryName.cs`、`PresetId.cs`、`PresetName.cs`、`LaunchEntry.cs`（Core）；`FileStorePersistence.cs`、`FileLauncherJsonIO.cs`、`StoreGrain.cs`、`Grain.cs`、`ErrorLog.cs`、`LegacyMigration.cs`（App/Infrastructure）。

## Caveats

- C1 当前不触发：`ReloadFromDisk` 与 `Dispatch` 都在 Avalonia UI 线程；bug 是潜在的多线程契约缺口。
- C4 嵌套锁是设计选择（消除读写 TOCTOU 窗口），非当前死锁；风险在未来的脆弱性。
- S1 的 null 行为可视为有意严格；因与 `DirectoryPathJsonConverter` 不一致而记录。
