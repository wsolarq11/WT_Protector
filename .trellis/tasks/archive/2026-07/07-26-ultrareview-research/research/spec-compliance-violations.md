# Research: Spec Compliance Violations

- **Query**: Compare code against project documentation to verify spec compliance
- **Scope**: Mixed - all 13 spec/docs files vs code across all layers
- **Date**: 2026-07-26

## 问题

对照 13 个 spec/docs 文件与全层代码，验证 spec 合规性。

## 结论

1 critical + 7 medium + 6 low；已核验为 CLEAN 的规则 15 条。

## 关键证据

### Critical

**C1 `ExceptionFilters.IsRecoverable` 无限循环（违反 max-iteration guard spec）**

`src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs:19-20`：

```csharp
var inner = ex;
while (inner is TypeInitializationException or AggregateException)
    inner = inner.InnerException ?? inner;
```

`inner.InnerException` 为 null 时 `?? inner` 返回自身不变，while 条件恒真，死循环。spec 要求 `for i<16` 上限守卫；Core 层 `LauncherErrors.IsRecoverable` 正确用了 16 次上限 `for` 循环，但 `ExceptionFilters.IsRecoverable` 在委托给 Core 前自行解包且缺守卫。**复现**：传入 `TypeInitializationException`/`AggregateException` 且 `InnerException=null` 时，调用即永久挂起调用线程。

### Medium

- **M1 `DispatchAsync` 仅存在于文档**：CLAUDE.md 与 persistence.md 引用 `Dispatch/DispatchAsync/DispatchErrorOr`，代码 `StoreGrain.cs` 只有 `Dispatch`/`DispatchErrorOr`/`ReloadFromDisk`，无异步派发路径。
- **M2 `AppStore` 仅存在于文档**：persistence.md、mvvm-pattern.md 描述 `AppStore.Mutate`/自动回滚，代码已用 `StoreGrain`（继承 `Grain<LauncherStoreFile>`）替代，文档未更新。
- **M3 `ExpectedStoreSchemaVersion` 位置漂移**：persistence.md 写在 `LauncherStore`，实际常量 `ExpectedSchemaVersion=2` 在 `LauncherStoreValidator.cs:13`，另有未文档化的 `MinimumSupportedSchemaVersion=1`。
- **M4 `LauncherStore` API 面与 spec 不符**：`ExpectedStoreSchemaVersion` 在 Validator；`RepositoryMarkerFileName`、`DefaultPersistence` 注入、`LoadErrorOr()` 未入 spec；`MigrateFromLegacyFiles()` 已不在 `LauncherStore`（移到 `LegacyMigration.cs`）；`Load` 多出 `onRecovery` 参数。
- **M5 mvvm-pattern 仍描述旧 `_settings` 模式**：spec 称 `_settings` 为“过渡工作副本”，代码 `MainWindowViewModel.cs:193` 已完全消除，全部经 `CurrentSettings`（`StoreGrain.Select`）读取。
- **M6 `ReloadFromDisk` 锁外读盘且不通知**：`StoreGrain.cs:52-56` 锁外 `_persistence.Load` 再锁内 `SetState`，且无 `StateChanged` 通知；并发 `Dispatch` 可能被覆盖，订阅方收不到刷新。
- **M7 `StateChanged` 在锁外触发**：`StoreGrain.cs:58-71` `Mutate` 返回后 `StateChanged?.Invoke(after)`；并发 `Dispatch` 可在回调前再次变更，订阅方 `Select()` 可能读到下一状态而非事件载荷状态。

### Low

- **L1 README 未记录 `launchCount`**：`LaunchEntry.cs:50-51` 有 `[JsonPropertyName("launchCount")]` 且序列化，README Fields 未列。
- **L2 README 未记录 `backgroundMode`**：`LauncherSettings.cs:46-47` 有 `[JsonPropertyName("backgroundMode")]`，README 未列。
- **L3 设计 token 重导出第二引用点**：`LauncherEntryLayoutCalculator.cs:21,25` 把 `NameTileGap`/`NameTileRowGap` 再声明为 `const`，可随 `LauncherDesignTokens` 漂移。
- **L4 源生成注册类型不精确**：`LauncherJsonContext.cs:15` 注册 `List<LaunchEntry>`，实际 `LauncherStoreFile.Entries` 为 `ImmutableList<LaunchEntry>`（STJ 经 `IList<T>` 处理，测试通过，但 spec 要求精确类型）。
- **L5 `cardsGroupKey` 缺 `[JsonRequired]`**：`LauncherSettings.cs:28` 有默认值但 LayoutManager 依赖有效分组值，手改 JSON 的无效值不会 fail fast。
- **L6 `StartBatch(entries)` 缺 CancellationToken**：`LauncherLaunchWorkflow.cs:43-46` 的 entries 重载无取消支持，仅 `StartBatch(preflight)` 重载有。

## Rules Verified as Clean

| 规则 | 状态 |
|------|--------|
| 无裸 `catch (Exception)` 无 `when` | CLEAN -- 54+ catch 块均有过滤器 |
| 无 per-entry `confirm` | CLEAN -- 仅全局 `requireConfirmationForLaunches` |
| 无重复投影字段 | CLEAN -- 全部由单一可写 owner 派生 |
| 无硬编码十六进制色 | CLEAN -- 全用 `{StaticResource}` |
| `File.WriteAllText` 仅临时文件 | CLEAN -- 仅 `FileLauncherJsonIO.cs:122` 与 `App.axaml.cs:134` |
| `_settings` 已消除 | CLEAN -- 全经 `CurrentSettings` |
| Refresh 前丢弃拖拽状态 | CLEAN -- `DiscardDragReorderPreview()` 先执行 |
| 四框控制行同 Grid 行 | CLEAN -- `LauncherControlBar.axaml` 单 Grid row |
| `SchemaVersion` 无 `[JsonIgnore]` | CLEAN |
| 无 `TranslateTransform` | CLEAN |
| 32767-char 命令行校验 | CLEAN -- `LaunchArgumentBuilder.cs:74-78` |
| finally 内 catch 自带 try/catch | CLEAN |
| 异常消息脱敏 `ExceptionMessageFormatter.ForUser` | CLEAN |
| 设计 token 均有消费者 | CLEAN |
| 无不透明内部标记注释 | CLEAN -- 均带描述性注释 |

## 关联路径

- `.trellis/spec/core/quality-guidelines.md`、`.trellis/spec/core/persistence.md`
- `.trellis/spec/views/mvvm-pattern.md`
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs`
- `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs`、`LauncherLaunchWorkflow.cs`
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreValidator.cs`、`LauncherJsonContext.cs`、`LaunchEntry.cs`、`LauncherSettings.cs`
- `README.md`（schema 字段缺口）
