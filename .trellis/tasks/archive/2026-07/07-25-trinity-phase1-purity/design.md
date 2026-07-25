# Design: Phase 1 Zone 0 纯函数化

## Architecture

### 变更全景

```
Before (混合)                    After (分离)
────────────                    ────────
Core/LauncherJson.cs            Core/LauncherJson.cs (facade ↓)
  ├── Serialize/Deserialize      Core/LauncherJsonSerializer.cs (纯函数, Zone 0)
  ├── File IO                    App/Infrastructure/ILauncherJsonIO.cs (接口, Zone 1)
  ├── Backup rotation            App/Infrastructure/FileLauncherJsonIO.cs (IO实现, Zone 1)
  └── Temp cleanup             
                                Core/LauncherJson.cs (facade: 委托给新组件, 保持兼容)

Core/LauncherStore.cs           Core/LauncherStore.cs (facade ↓)
  ├── Schema validation          Core/LauncherStoreValidator.cs (纯函数, Zone 0)
  ├── Store IO                   App/Infrastructure/IStorePersistence.cs (接口, Zone 1)
  ├── Path resolution            App/Infrastructure/FileStorePersistence.cs (IO实现, Zone 1)
  └── Legacy migration           App/Infrastructure/LegacyMigration.cs (Zone 1)

Tests/                          Tests/
  └── 现有测试不变                 新增 CsCheck PBT
```

### 接口设计

#### ILauncherJsonIO

```csharp
// App/Infrastructure/ILauncherJsonIO.cs
namespace WTLauncher.App.Infrastructure;

public interface ILauncherJsonIO
{
    T? Load<T>(string path) where T : notnull;
    T LoadRequired<T>(string path, string documentName) where T : notnull;
    T LoadWithRecovery<T>(string path, Func<string, T> loadAndValidate,
        string recoveryContext, T defaultValue,
        Action<Exception, string>? onRecovery = null);
    void Save<T>(T value, string path, bool backup);
    ErrorOr<T> LoadErrorOr<T>(string path) where T : notnull;
    ErrorOr<Success> SaveErrorOr<T>(T value, string path, bool backup);
    void CleanupStaleTempFiles(string configDir);
    void RotateBackups(string fullPath, int maxBackups = 3);
}
```

#### IStorePersistence

```csharp
// App/Infrastructure/IStorePersistence.cs
namespace WTLauncher.App.Infrastructure;

public interface IStorePersistence
{
    LauncherStoreFile Load(string storePath, Action<Exception, string>? onRecovery = null);
    void Save(LauncherStoreFile store, string storePath, bool backup = true);
    ErrorOr<LauncherStoreFile> LoadErrorOr(string storePath);
    string ResolveStorePath(string? explicitPath,
        string? currentDirectory = null, string? appBaseDirectory = null);
}
```

### Zone 0 纯函数

```csharp
// Core/LauncherJsonSerializer.cs
namespace WTLauncher.Core;

public static class LauncherJsonSerializer
{
    public static JsonSerializerOptions Options => LauncherJsonContext.Default.Options;

    public static string Serialize<T>(T value) where T : notnull { ... }
    public static T? Deserialize<T>(string json) where T : notnull { ... }
}

// Core/LauncherStoreValidator.cs
namespace WTLauncher.Core;

public static class LauncherStoreValidator
{
    public const int ExpectedSchemaVersion = 2;
    public const int MinSupportedSchemaVersion = 1;

    public static ErrorOr<Success> Validate(LauncherStoreFile store) { ... }
    public static void ThrowIfInvalid(LauncherStoreFile store) { ... }
}
```

### Facade 兼容层

原始 `LauncherJson` 和 `LauncherStore` 类保留为静态 facade，
所有方法内部委托给新组件：

```csharp
// Core/LauncherJson.cs — 改造后
public static class LauncherJson
{
    // 保留原有静态 API，内部委托
    public static T? Load<T>(string path) where T : notnull
        => JsonIO.Load<T>(path);  // JsonIO 是内部注入的 ILauncherJsonIO 实例
    
    public static void Save<T>(T value, string path, bool backup)
        => JsonIO.Save(value, path, backup);
    
    // 内部持有 IO 实现（单例）
    internal static ILauncherJsonIO JsonIO { get; set; }
        = new FileLauncherJsonIO();
}
```

这确保了所有现有调用点不需要修改任何代码。

## 测试策略

### 现有测试
- 不修改现有测试的断言逻辑
- 如果需要，仅调整 `using` 语句或文件路径
- Mock 接口测试在 Phase 2 引入（当前 VM 层直接调用静态方法）

### 新增 CsCheck PBT（6 条）

| 测试 | 属性 | 随机输入 | 迭代 |
|------|------|---------|------|
| SerializeDeserializeRoundtrip | Serialize → Deserialize 恢复原始值 | 随机 LauncherStoreFile | 1000 |
| DeserializeSerializeRoundtrip | Deserialize → Serialize 保持 JSON 结构 | 随机 JSON | 1000 |
| ValidateValidStore | 有效 store 通过验证 | 随机有效 LauncherStoreFile | 1000 |
| ValidateInvalidSchemaVersion | 无效 schema 版本被拒绝 | 随机无效版本号 | 1000 |
| SerializerOptionsIsAotSafe | Options 返回 LauncherJsonContext | N/A（单次验证） | 1 |
| NormalizeLineEndings | 输出行尾统一为 `\n` | 随机含 CRLF 字符串 | 1000 |

## 文件夹结构（变更后）

```
src/avalonia-launcher/src/WTLauncher.Core/
  ├── LauncherJson.cs              ← facade (保留, 缩减)
  ├── LauncherJsonSerializer.cs    ← 新增 (纯函数)
  ├── LauncherStore.cs             ← facade (保留, 缩减)
  └── LauncherStoreValidator.cs    ← 新增 (纯函数)

src/avalonia-launcher/src/WTLauncher.App/Infrastructure/
  ├── ILauncherJsonIO.cs           ← 新增 (接口)
  ├── FileLauncherJsonIO.cs        ← 新增 (IO 实现)
  ├── IStorePersistence.cs         ← 新增 (接口)
  ├── FileStorePersistence.cs      ← 新增 (IO 实现)
  └── LegacyMigration.cs           ← 新增 (从 LauncherStore 拆分)

src/avalonia-launcher/tests/WTLauncher.Tests/
  ├── LauncherJsonSerializerPbtTests.cs  ← 新增
  └── LauncherStoreValidatorPbtTests.cs  ← 新增
```

## 风险与缓解

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| Facade 委托引入循环引用 | 低 | 高 | 单向委托：LauncherJson → FileLauncherJsonIO，不反向 |
| CsCheck 随机生成触发极端 JSON 长度 | 低 | 低 | 设置合理的 MaxDepth/MaxLength 边界 |
| 现有调用点遗漏未更新 | 中 | 中 | 编译检查（所有方法公开 + 签名不变）|
