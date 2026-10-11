# Persistence — Core Layer

> 单文件 JSON 存储，原子写 + 备份轮转 + LF 行尾。`LauncherJson.Save` 是唯一原子写路径。

## Wiring Contract（2026-08-15）

- `StoreMutationSession` = 多步 VM mutation 提交原语：`Commit` 保存一次，`Dispose` 在未提交时恢复内存状态。
- `StoreGrain.ReloadFromDisk` 触发 `Reloaded`；`MainWindowViewModel` 做一次 `SynchronizeProjections(ProjectionSyncMode.FullReload)`，抑制中间 `EntriesChanged` 同步。
- 规范写走 `StoreGrain` → 原子 `LauncherJson.Save`；导入/导出读写包 `IConfigExchangeService`。
- 启动历史保存用 `LaunchSaveResult`，区分"启动前保存失败"与"启动后历史保存失败"。

## 原子写（`LauncherJson.cs`）

temp 文件 → 可选备份轮转 → 原子替换。规则：
1. 先写 temp，绝不直接覆盖真实文件。
2. temp 名含 `Guid.NewGuid():N`。
3. 目标存在 → `File.Replace`（同 NTFS 卷原子，`MoveFileEx MOVEFILE_REPLACE_EXISTING`）；首次保存（目标不存在）→ `File.Move`。`File.Move(overwrite:true)` 非原子，禁用。
4. 崩溃遗留 temp 由 `CleanupStaleTempFiles` 启动时清理。

备份轮转（最多 3 代 `.bak → .bak.1 → .bak.2 → 删`）：copy-then-rotate（先 copy 到 `bak-temp-{Guid}` 再 rotate，最后 move 到 `.bak`），避免旋转中 `.bak` 缺失窗口。

行尾：持久化 JSON 一律 LF（`NormalizeLineEndings` → `ReplaceLineEndings`）。

## AOT source-gen 序列化（2026-07-12）

`LauncherJsonContext`：`[JsonSourceGenerationOptions(WriteIndented=false, ReadCommentHandling=Skip, AllowTrailingCommas=true, MaxDepth=64)]` + 每个类型 `[JsonSerializable]`。规则：
1. 选项写在 context 类 attribute，非 `JsonSerializerOptions`（运行时 source-gen 只读 attribute）。
2. 泛型（如 `List<LaunchEntry>`）需显式 `[JsonSerializable]`。
3. 新 `Load<T>` / `Save<T>` 类型必须注册，否则 `GetTypeInfo(typeof(T))` 返回 null 运行期抛。

读写用 `JsonTypeInfo<T>` 重载：`JsonSerializer.Deserialize(stream, jsonTypeInfo)` / `Serialize(value, jsonTypeInfo)`（AOT 安全，禁 `JsonSerializerOptions` 重载）。

Loose Read / Strict Write：读静默跳过未知键（前向兼容）；写只写 C# record 声明的属性。

## 单文件存储（2026-07-08 起）

`config/launcher.json` 五段合一：

| 段 | 类型 | 所有者 |
|----|------|--------|
| `schemaVersion` | `int`（=2） | `LauncherStore.ExpectedStoreSchemaVersion` |
| `settings` | `LauncherSettings` | 窗口态 / 排序过滤 / 命令模板 / 搜索历史 |
| `entries` | `ImmutableList<LaunchEntry>` | 启动按钮 |
| `presets` | `ImmutableList<LauncherPreset>` | 保存选择组 |
| `defaultPresetId` | `string` | 启动激活预设 |

schemaVersion 单一源：只在 `LauncherStoreFile` 顶层；`LauncherSettings` 无 `SchemaVersion`（历史双版本造成漂移，pitfalls §6.1）。前向兼容：未知键跳过，旧 `settings.schemaVersion` 静默丢弃并在下次保存移除（pin：`LauncherStoreLoadFailureTests.LauncherSettings_LegacySchemaVersionInJson_LooseReadSkipsUnknownKey`）。Schema-First（Corvus.JsonSchema）否决：C# record 即内部持久化 schema，双源增漂移风险。

`SchemaMigrationRegistry` 延后：迁移面仅 1 条（`MigrateFromLegacyFiles` 一次性三文件→单文件），无 v3 bump。≥2 条迁移路径或 v3 结构性变更时重审。

备份恢复链：`LoadWithRecovery` 按 `.bak → .bak.1 → .bak.2` 搜索首个有效备份（≤ `DefaultMaxBackups`）。不变量：`RotateBackups` 不超 `maxBackups`；`CleanupStaleTempFiles` 只删匹配 `\.(tmp|stage|bak-tmp|recovery)-[0-9a-f]{32}$`，且获取 `SaveLock` 防删进行中保存的 temp；静态 `SaveLock` 序列化所有实例保存。

## 写路径编译期强制（WT1003）

`DispatchMustBeOnlyWritePathAnalyzer`：`LauncherStore.Save` 仅允许在 `StoreGrain.Dispatch` / `DispatchErrorOr` 调用。反例：`TrySaveWindowBounds` 直写绕过 grain 造成状态 desync；测试 fixture 经 `NoWarn` 豁免。

## 读侧单一源（2026-07-24）

`CurrentSettings => _storeGrain.Select(s => s.Settings)`，消除 `_settings` 漂移。过渡态：构造期与 DragWorkflow 同步保留 `_settings` 工作副本；彻底移除需 DragWorkflow 直接读写 StoreGrain。

`BuildStoreFile()` 是唯一手工组装 `LauncherStoreFile` 的方法；所有保存路径必经它。门禁 `settings-single-source-buildstorefile` 断言该签名恰好 1 处。

协作类工作态不是双源：`LauncherDragReorderWorkflow._settings` 在 Preview 前变异；StoreGrain 是单一源，保存失败内存不变；VM 经 5 处 `Drag.cs` 同步 + `_storeGrain.Select` 读取；7 个等价测试钉死。

## 为何 settings 嵌套不拆

`LauncherSettings` 只含 UI/展示态；业务态在顶层。单文件原子性优于多文件（拆开需两阶段提交，单用户启动器过度）。仅当独立高频写入（如每次 resize 写 window bounds）出现才重审。

## API 与路径解析

`LauncherStore.Load/Save/ResolveStorePath/MigrateFromLegacyFiles`；常量 `ExpectedStoreSchemaVersion=2`、`StoreFileName="launcher.json"`、`ConfigDirectoryName="config"`。

`ResolveStorePath` 顺序：显式 CLI 路径 → 自 cwd 向上找 `config/launcher.json` + `README.md` → 自 app base 向上 → 首个非空候选根。`README.md` 是 git 根标记。

加载管线：`ThrowIfStorePathMissing → MigrateFromLegacyFiles（旧三文件存在且 launcher.json 缺）→ LoadWithRecovery(LoadRequired<LauncherStoreFile>) → 文件 <50MB → 反序列化 → SchemaVersion > Expected 抛 InvalidDataException("from a newer version")`。值对象校验在用时，不在 Store 层。

首次运行：文件缺失返回 `new LauncherStoreFile()`（空 entries/默认 settings），正常路径非错误（抛错会静默退出）。

备份恢复两路径：文件存在但损坏（Json/InvalidData/IO 异常）→ 试 `.bak` 原子恢复 + 重试（重试在原 try/catch 外，防备份也损坏时死循环）+ `OnRecoveryFromBackup` 钩子；仍失败重抛原异常。文件缺失 → 试 `.bak` `File.Move`；无备份返回默认。

Schema 版本：`> Expected` 拒绝（升级提示，防降级静默丢数据）；`<= Expected` 接受。bump 前加旧→新迁移。

Legacy 迁移（一次性 F06）：`MigrateFromLegacyFiles`：读旧 `config.json`（flat entries）/ `settings.json`（best-effort）/ `presets.json`（best-effort）→ 建 `LauncherStoreFile` 存 `launcher.json`（带备份）→ 旧文件改 `.bak`（不再读）。

## 并发与清理

`LauncherJson.Save` 用 `private static SemaphoreSlim(1,1)` 序列化写（防御跨实例/直接调用）。`CleanupStaleTempFiles` 启动时删匹配命名的孤儿 temp，失败静默忽略。

## Forbidden Patterns

- ❌ 直接覆盖文件；用 `LauncherJson.Save()`。
- ❌ `File.Move(overwrite:true)`（非原子）；目标存在用 `File.Replace`。
- ❌ 持久化 JSON CRLF；必须 LF。
- ❌ 直接 `JsonSerializer.Serialize`；经 `LauncherJson.Save()`。
- ❌ Load 对 FileNotFound 抛；首次运行返回默认。
- ❌ 序列化字段未考虑 `[JsonIgnore]`；新增字段改 JSON 形状会破契约测试。
- ❌ rotate-then-copy 备份轮转；必须 copy-then-rotate。

## 双层上限集合（如 SearchHistory ≤20）

上限在两层执行：Store 加载时截断（防手改 JSON 绕过）+ ViewModel 写入时截断（防运行时 bug）。测试必须两层都验证（VM 态 + round-trip `LauncherStore.Load()`）。

## 已移除抽象（防重引入）

| 移除 | 原因 | 替代 |
|------|------|------|
| `IStore` | 单一实现 | 直接 `LauncherStore.Load/Save` |
| `JsonStore` | 薄包装 | 直接调用 |
| `LiteDbStore` | LiteDB BSON 与 init-only 不可变 record 不兼容 | `LauncherJson` |
| `MigrationTool` | LiteDB 无限期延后后删除 | 删除 |
| `NormalizeEntries` / `ValidateEntrySelectionFields` | ponytail 移除 | 值对象用时 `Parse` |
| 多文件 stage-all-then-commit | 三文件并入 `launcher.json` | 单文件原子保存 |
| `LauncherPresetMutationSnapshot` | 单一调用方内联 | 并入 mutation snapshot / MWVM |
| `LauncherEntryMutationSnapshot` / `LauncherSelectionMutationSnapshot` | 2026-07-21 删除 | `AppStore.Dispatch` 内部 revert + `RestoreVmStateFromStore()` |
| `AppStore.Mutate` / `MutateAsync` / `MutateErrorOr` | 2026-07-23 删除 | `Dispatch` / `DispatchAsync` / `DispatchErrorOr` |
| `LooseOptions` / `StrictOptions` | 2026-07-23 删除 | `JsonTypeInfo<T>` AOT 重载 |
| `ExceptionFilters.UnwrapAggregates` | 2026-07-23 删除 | DEBUG guard 内联解包；Core `IsRecoverable` 处理生产解包 |
