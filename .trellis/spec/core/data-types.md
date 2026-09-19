# Data Types — Core Layer

> record / enum / design token / 值对象：系统不可变数据形状。

## Record vs Class

规则：不可变形状 → `sealed record`（全 `{ get; }` 或 `{ get; init; }`）；可变实体 → `sealed class`（任一 `{ get; set; }`）。

- record：值相等、`with` 克隆、自动 `ToString()`。
- class：引用相等、显式 `Clone()` 深拷贝。

### Record（不可变持久实体）

`LaunchEntry`（`LaunchEntry.cs`）字段全部 `[JsonPropertyName]`，非可选加 `[JsonRequired]`，字符串默认 `""`：`Id` / `Name` / `Directory` / `Command`（JsonRequired）+ `Selected`、`UpdatedAtUtc`、`LaunchedAtUtc`。变更用 `with { }` 产生新实例。

### Class（可变持久实体）

`{ get; set; }` + 显式 `Clone()` 深拷贝，供 snapshot/rollback。

### Rules

1. 不可变 → `sealed record`；可变 → `sealed class`。
2. 所有非可选 JSON 字段 `[JsonRequired]`。
3. 每个属性 `[JsonPropertyName]`。
4. 字符串默认 `""`，禁 `null`。
5. 数据载体无行为方法；逻辑放静态 helper。
6. 每个可变 class 提供 `Clone()`。

## Enum Pattern

enum 是代码契约；配对静态 helper 拥有序列化。规则：
1. Enum + static helper 配对。
2. 字符串 ID 用 `const`，禁魔数（如 `"cards"`）。
3. `NormalizeId()` 反序列化：校验、未知值抛 `InvalidDataException`。
4. `ToId()` 序列化：穷尽 switch，未知成员抛 `ArgumentOutOfRangeException`。

## Design Tokens

`LauncherDesignTokens`（Core）是所有设计常量唯一源。规则：
1. 全部颜色/间距/尺寸常量在此。
2. token 为 `const`。
3. 经 `App.axaml.cs` 注入，不硬编码 XAML。
4. 禁止在 XAML/code-behind 复制 token 值。
5. 只加有生产消费者的稳定语义 token（`FontSizeSmall` / `FontSizeBody` / `FontSizeEmphasis` 可；一次性 margin/width/height/grid ratio 不可）。
6. 框架形状（`Brush` / `CornerRadius` / `Thickness` / `BoxShadows`）留在 App `InjectDesignTokens()`；Core 只存 string/double。

## Cloning

- record：`with { }` 浅克隆。
- class：显式 `Clone()` 深拷贝（`sourceEntries.Select(e => e.Clone()).ToList()`、`LauncherPresetFiles.Clone(...)`）。
- 历史：`LauncherEntryMutationSnapshot.cs` 已于 2026-07-21 删除，克隆语义并入 `AppStore.Mutate()` 内部回滚 + `RestoreVmStateFromStore()`。

## Forbidden Patterns

- ❌ 无 `[JsonPropertyName]` 的可变属性。
- ❌ JSON 字段 `null` 默认；用 `""` / `[]` / `false`。
- ❌ record 上放逻辑方法；放静态 helper。
- ❌ 标识符魔数字符串；用配对 helper 的 `const`。
- ❌ Core 引用 Avalonia / CommunityToolkit 类型（Layer 0 零 UI 依赖）。
- ❌ `Dictionary<string, object>` / `dynamic`；定义类型化 record/class。
- ❌ 无代理对边界检查的截断（`Substring(0,N)` / `[..N]`）；先 `char.IsHighSurrogate(span[^1])` 守卫。
- ❌ `char.IsControl()` 不同时查 `UnicodeCategory.Format`；必须 `Control or Format` 才能剥掉 BiDi/零宽/LTR-RTL。
- ❌ ID / 跨引用键缺失 NFC 规范化；比较/存储前 `Normalize(NormalizationForm.FormC)`。

## String Safety（2026-06-25）

- 截断前检查代理对边界（防截断高位代理产生非法 UTF-16）。适用 `LauncherEntryIds.Normalize()`、`LauncherPresetIds.Normalize()`。
- 剥离不安全字符必须同时剥 `UnicodeCategory.Control` 和 `Format`（BiDi U+202A–U+202E、零宽 U+200B、LTR/RTL U+200E/F）。
- ID 必须 FormC 规范化；`LauncherPresetIds.Normalize()` 已用 FormKC（FormC 超集）。
- 百分比注入：FormKC 会把全角 `％`(U+FF05) 转为 `%`(U+0025)。若 `Environment.ExpandEnvironmentVariables()` 在 FormKC 之后运行，可被注入。检测 `normalized.Contains('%') && !original.Contains('%')` 则抛。适用 `LaunchArgumentBuilder.Build()`。

## Accepted Design Decisions（有意为之）

- 时间戳 `string` 而非 `DateTimeOffset`：配置文件人可读，避免破坏用户配置/迁移。`ParseLaunchedAt` / `ParseUpdatedAt`（`LauncherEntryVisibility`）是唯一校验点。
- 颜色 token `const string` hex：XAML `StaticResource` 直接可用，避免 Core 依赖 Avalonia.Media；启动时一次性 string→Brush。
- Enum + string ID 双轨：JSON 用字符串 ID 前向兼容；新增成员旧文件仍可反序列化（未知串 → 清晰异常）。序列化 ordinal 会静默漂移。
- 集合用 `ImmutableList<T>` / `ImmutableDictionary<K,V>`：record init-only 只保护引用，不保护集合内容。迁移 mutating→immutable 时每个调用方必须消费返回值（C# 不警告丢弃返回值；审计全部调用方或用 `[MustUseReturnValue]`）。范式：`LauncherEntryOrder.SyncWithEntries` / `MoveRelative`。
- record 引用共享：组件 A 持有组件 B 的 record，B 用 `with { }` 替换后 A 副本变 stale。修法：始终走 getter 读最新实例，或每次 mutation 后同步引用。坑：`LauncherDragReorderWorkflow._settings`。

## Value Object — Parse, Don't Validate

`readonly record struct` + 私有构造 + `TryParse` + `Result<EntryId,IdError>` + 每类型 `JsonConverter` + 隐式转 `string`。规则：
1. `readonly record struct`：零堆分配、值语义。
2. 私有构造：仅 `TryParse` 可创建。
3. Result 返回：成败编译期强制。
4. 每类型 `JsonConverter`：透明 JSON 序列化。
5. 隐式转 `string`。
6. 所有规范化在构造内；构造后不再规范化。
7. 安全检测在原始输入上（raw vs normalized 比对，如 % 注入）。

值对象表：

| 类型 | 校验 | 取代 |
|------|------|------|
| `EntryId` | FormC、控字符剥离、256 截断、保留名、路径分隔符 | `LauncherEntryIds` |
| `EntryName` | FormKC、未配对代理剥离 | `LauncherEntryText` |
| `EntryDirectory` | FormKC、raw % 注入检测、UNC/DOS 拒绝 | `LaunchArgumentBuilder` 目录逻辑 |
| `PresetId` | FormC、小写、字母数字过滤 | `LauncherPresetIds` |
| `PresetName` | FormKC、256 限制 | `LauncherPresetFiles` 名称校验 |
| `ShellCommand` | shell 路径解析、fallback `pwsh` | 硬编码 `pwsh` 前缀 |

迁移步骤：1) 建值对象 `TryParse`；2) 旧静态方法标 `[Obsolete]` 委托值对象；3) 加 `JsonConverter`；4) `.csproj` 抑制 CS0618 注明理由；5) 逐步迁移调用方；6) 清理阶段删旧方法。
