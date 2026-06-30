# Design — 类型驱动架构重构

> 技术设计：Phase 1-3 的架构边界、数据流、兼容性、权衡和回滚方案。

---

## 架构概览

```
┌──────────────────────────────────────────────────────┐
│  Views (Avalonia)         — 不变                      │
├──────────────────────────────────────────────────────┤
│  ViewModels (MVVM)        — Phase 2 改为 Result 返回  │
├──────────────────────────────────────────────────────┤
│  IStore 接口               — Phase 3 抽象持久化       │
│  ├── JsonStore (Phase 1-2) — 现有实现                 │
│  └── LiteDbStore (Phase 3) — 新实现                   │
├──────────────────────────────────────────────────────┤
│  Domain Value Objects      — Phase 1 新增             │
│  EntryId, EntryName, EntryDirectory,                  │
│  PresetId, PresetName, ShellCommand                   │
├──────────────────────────────────────────────────────┤
│  Result<T, E>              — Phase 2 新增             │
│  替代 try-catch 异常控制流                            │
└──────────────────────────────────────────────────────┘
```

---

## Phase 1: 类型加固

### 1.1 Value Object 设计

所有 value object 使用 `readonly record struct`，遵循统一模式：

```csharp
// 统一模式
public readonly record struct EntryId
{
    public string Value { get; }

    // 构造私有——只能通过 TryParse 创建
    private EntryId(string value) => Value = value;

    // 唯一的公开构造入口——构造成功 = 验证通过
    public static Result<EntryId, IdError> TryParse(string? raw);

    // JSON 转换
    public static EntryId FromJson(string jsonValue);
    public string ToJson() => Value;

    // 比较器（单一定义，不再分散在各静态类中）
    public static readonly StringComparer Comparer = StringComparer.Ordinal;

    // 隐式转换（只出不进——可以当 string 用，但不能从 string 隐式创建）
    public override string ToString() => Value;
}
```

**关键设计约束：**
- **构造器必须私有**——`TryParse` 是唯一创建路径。编译器强制调用方处理验证结果。
- **隐式转换只出不进**——`EntryId` 可以隐式转为 `string`（用于 JSON/绑定），但不能从 `string` 隐式创建（强制调用 `TryParse`）。
- **`Result<T, E>` 在 Phase 1 中初步引入**——仅用于 value object 的错误返回，Phase 2 才推广到全代码库。

### 1.2 六个 Value Object

| 类型 | 构造验证 | 消除的重复代码 |
|------|---------|---------------|
| `EntryId` | FormC 规范化 + 空值 + Windows 保留名 + 路径分隔符 + 256 长度 | `LauncherEntryIds.Normalize`, `.RequireEntryId`, `.IsUsable` |
| `EntryName` | FormKC 规范化 + 非空 + unpaired surrogate 清理 | `LauncherEntryText.Normalize`, `.RequireNonEmpty` |
| `EntryDirectory` | FormKC + **原始输入 `%` 检测** + `ExpandEnvironmentVariables` + `Path.GetFullPath` + UNC 拒绝 + DOS 拒绝 | `LaunchArgumentBuilder.StripUnsafeCharacters`, `Build` 前半部分 |
| `PresetId` | FormC + 小写 + 字母数字过滤 + 256 长度 | `LauncherPresetIds.Normalize`, `.NormalizeReference` |
| `PresetName` | FormKC + 非空 + 256 限制 | `LauncherPresetFiles.NormalizePresetName` (仅 256 检查) |
| `ShellCommand` | 从配置解析 shell 路径，默认 `pwsh` | 消除 `LaunchArgumentBuilder.ShellCommandPrefix` 硬编码 |

### 1.3 JSON 序列化兼容

**目标：`store.json` 格式不变，value object 透明序列化。**

方案：为每个 value object 注册 `JsonConverter`。

```csharp
// EntryId 在 JSON 中仍然是 "id": "my-entry"
// 通过 JsonConverter 透明转换
public class EntryIdJsonConverter : JsonConverter<EntryId>
{
    public override EntryId Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        var result = EntryId.TryParse(raw);
        return result.Match(
            ok: id => id,
            err: error => throw new InvalidDataException(error.Message));
    }

    public override void Write(Utf8JsonWriter writer, EntryId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
```

**`LaunchEntry` 的 JSON 属性类型变更：**

```csharp
// Before
public string Id { get; init; } = "";

// After — JSON 格式不变，内存类型加固
[JsonConverter(typeof(EntryIdJsonConverter))]
public EntryId Id { get; init; }
```

### 1.4 旧代码委托模式

```csharp
// LauncherEntryIds.cs — 旧方法委托给新类型
public static class LauncherEntryIds
{
    [Obsolete("Use EntryId.TryParse() instead.")]
    public static string Normalize(string? id)
    {
        var result = EntryId.TryParse(id);
        return result.Match(ok: e => e.Value, err: _ => string.Empty);
    }

    [Obsolete("Use EntryId.TryParse() instead.")]
    public static string RequireEntryId(string? id, string paramName)
    {
        var result = EntryId.TryParse(id);
        return result.Match(
            ok: e => e.Value,
            err: error => throw new ArgumentException(error.Message, paramName));
    }
    // ...其余方法同样模式
}
```

### 1.5 Import Map — 哪些文件需要改动

```
Phase 1 涉及的文件 (~30-40 个):

Core 层新增 (6 文件):
  src/WTLauncher.Core/EntryId.cs
  src/WTLauncher.Core/EntryName.cs
  src/WTLauncher.Core/EntryDirectory.cs
  src/WTLauncher.Core/PresetId.cs
  src/WTLauncher.Core/PresetName.cs
  src/WTLauncher.Core/ShellCommand.cs

Core 层修改 (5 文件):
  src/WTLauncher.Core/LaunchEntry.cs          — string→强类型属性
  src/WTLauncher.Core/LauncherPreset.cs        — string→强类型属性
  src/WTLauncher.Core/LaunchPlan.cs            — 使用 EntryDirectory
  src/WTLauncher.Core/LaunchArgumentBuilder.cs — 删除重复验证逻辑
  src/WTLauncher.Core/LauncherStore.cs         — JSON converter

Core 层标记 [Obsolete] (5 文件):
  src/WTLauncher.Core/LauncherEntryIds.cs
  src/WTLauncher.Core/LauncherEntryText.cs
  src/WTLauncher.Core/LauncherPresetIds.cs
  src/WTLauncher.Core/LauncherPresetFiles.cs   — 仅移除 256 检查
  src/WTLauncher.Core/LauncherCommandTemplateResolver.cs

App/ViewModels 层修改 (~15 文件):
  所有使用 LauncherEntryIds/LauncherEntryText/LauncherPresetIds 的文件
  编译器警告引导逐个迁移

App/Views 层修改 (~3 文件):
  EntryDetailsWindow.cs
  PresetDetailsWindow.cs
  MainWindowEntryCardResolver.cs

Tests 层修改 (~15-20 文件):
  所有构造 LaunchEntry/LauncherPreset 的测试
```

---

## Phase 2: Result 类型

### 2.1 Result<T, E> 设计

```csharp
// Core/Result.cs (~25 行)
namespace WTLauncher.Core;

public readonly record struct Result<T, E> where E : notnull
{
    private readonly T? _value;
    private readonly E? _error;
    private readonly bool _ok;

    private Result(T value) { _value = value; _ok = true; _error = default; }
    private Result(E error) { _error = error; _ok = false; _value = default; }

    public static Result<T, E> Ok(T value) => new(value);
    public static Result<T, E> Err(E error) => new(error);

    public bool IsOk => _ok;
    public bool IsErr => !_ok;

    public TResult Match<TResult>(Func<T, TResult> onOk, Func<E, TResult> onErr)
        => _ok ? onOk(_value!) : onErr(_error!);

    // 隐式转换——方便与现有代码互操作
    public static implicit operator Result<T, E>(T value) => Ok(value);
}

// Unit type for Result<Unit, E> (void equivalent)
public readonly record struct Unit { public static readonly Unit Value = default; }
```

### 2.2 API 改造模式

```csharp
// Before: 异常控制流
public static LauncherStoreFile Load(string storePath)
{
    try { ... }
    catch (Exception ex) when (...) { ... }
}

// After: Result 返回
public static Result<LauncherStoreFile, LoadError> Load(string storePath)
{
    if (!File.Exists(storePath))
        return LoadError.FileNotFound(storePath);
    // ...
}

// ViewModel 调用侧
var result = store.Load(path);
result.Match(
    onOk: file => { /* 正常流程 */ },
    onErr: error => { StatusText = error.ToUserMessage(); }
);
```

### 2.3 错误类型定义

```csharp
// 集中定义所有错误类型，每个都有 .ToUserMessage()
public abstract record LoadError
{
    public abstract string ToUserMessage();

    public record FileNotFound(string Path) : LoadError
    {
        public override string ToUserMessage() => $"找不到文件";
    }
    public record SchemaTooNew(int Version) : LoadError
    {
        public override string ToUserMessage() => $"配置文件版本过新，请升级 WT Launcher";
    }
    // ...
}
```

### 2.4 ExceptionFilters 简化

Phase 2 完成后，`IsRecoverable` 仅在基础设施层使用（Avalonia 事件、后台线程）。ViewModel 层不再需要它。

---

## Phase 3: 持久化替换

### 3.1 IStore 接口

```csharp
// Core/IStore.cs
public interface IStore<T>
{
    Result<T, LoadError> Load(string path);
    Result<Unit, SaveError> Save(T data, string path);
}
```

### 3.2 LiteDbStore 实现

```
LiteDbStore : IStore<LauncherStoreFile>
  - 单文件 .db（替代 store.json）
  - BSON 序列化，Collection 映射到 entries/presets/settings
  - 事务内读写，自动 WAL
  - 零手写崩溃恢复代码
```

### 3.3 迁移流程

```
Program.cs 启动:
  1. 检测 store.json 存在 + store.db 不存在
  2. 读取 store.json → 写入 LiteDB
  3. File.Move(store.json → store.json.bak)
  4. 后续启动直接用 LiteDB
```

### 3.4 删除列表

Phase 3 完成后删除：
- `CommitJournal.cs` — 不再需要手写 journal
- `LauncherJson.Save` 中的 temp-file/backup-rotation 逻辑 — 被 LiteDB 事务替代
- `LauncherJson.LoadWithRecovery` — 被 LiteDB WAL 替代
- `LauncherJson.CleanupStaleTempFiles` — 不再产生 temp 文件
- `Program.cs` 中的 `RecoverStaleCommitJournals()` — 不再需要

**保留：**
- `LauncherJson.Load<T>` — Phase 3 的 JSON→LiteDB 迁移工具需要它
- `LauncherJson.Options` — 迁移时需要 JSON 反序列化配置

---

## 回滚计划

### Phase 1 回滚
- 新增的 value object 类型与旧 string 类型共存（`[Obsolete]` 模式）
- 回滚：`git revert`，无数据迁移风险（store.json 格式不变）
- 风险：★☆☆☆☆（低）

### Phase 2 回滚
- Result 类型与 try-catch 模式共存（逐步替换）
- 回滚：`git revert`，每个 API 改造独立可逆
- 风险：★★☆☆☆（中低）

### Phase 3 回滚
- 迁移前自动备份 store.json → store.json.bak
- 回滚：删除 store.db，恢复 store.json.bak，git revert
- 不会自动回滚——需要用户手动操作
- 风险：★★★★☆（中高）— 需要充分测试迁移工具

---

## 兼容性

| 维度 | Phase 1 | Phase 2 | Phase 3 |
|------|---------|---------|---------|
| store.json 格式 | 不变 | 不变 | 格式变更（JSON→LiteDB） |
| 公开 API | [Obsolete] 兼容 | 部分 API 签名变更 | IStore 接口替代静态方法 |
| 测试 | 全部通过 | 全部通过 | 新增迁移测试 |
| 向前兼容 | 新版写的 JSON 旧版可读 | 同 Phase 1 | 不再兼容 |
