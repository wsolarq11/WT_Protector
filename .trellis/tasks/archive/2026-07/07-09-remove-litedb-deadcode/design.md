# Design — 删 LiteDB 完整死链路

## 边界

本任务只做**删除**,不改任何业务逻辑。LiteDbStore/MigrationTool 是独立模块,被引用点极少且明确。

## 删除清单(完整图)

```
src/WTLauncher.Core/LiteDbStore.cs            (整文件删,93 行)
src/WTLauncher.Core/MigrationTool.cs          (整文件删,30 行)
src/WTLauncher.Core/WTLauncher.Core.csproj    (删 L8 LiteDB PackageReference)
src/WTLauncher.App/Program.cs                 (删 L54 注释 + L138-162 MigrateJsonToLiteDb 方法)
tests/WTLauncher.Tests/MigrationToolTests.cs  (整文件删,50 行)
.trellis/spec/core/quality-guidelines.md      (删 L69 ponytail 注释)
src/WTLauncher.Core/LauncherStore.cs          (L123 store.json → StoreFileName 修复)
```

## 引用图(确认安全)

`grep -r "LiteDbStore|MigrationTool" src/` 结果:
- `MigrationTool.cs:19` 调用 `LiteDbStore.Open` —— 自删
- `MigrationToolTests.cs:24/31/44` 调用 `MigrationTool.MigrateIfNeeded` —— 自删
- `Program.cs:151` 调用 `MigrationTool.MigrateIfNeeded` —— 在被注释的 `MigrateJsonToLiteDb` 方法内,自删

无其他调用点。LiteDB 包通过 `LiteDbStore.cs:1 using LiteDB;` 引入,删 LiteDbStore 即可删依赖。

## 风险评估

| 风险 | 等级 | 缓解 |
|------|------|------|
| 漏删引用导致编译失败 | 极低 | grep 已确认引用点全部在被删文件内 |
| MigrationToolTests 删除后 test count 变化 | 极低 | verify.ps1 不硬编码测试数,quality-guidelines.md L40 的"457 tests"已是过时数字 |
| 历史性 pitfalls.md 引用 LiteDB | 无 | pitfalls.md 是踩坑实录,引用历史事件合理,保留 |
| LauncherStore.MigrateFromLegacyFiles 文件名修复 | 极低 | 该方法当前不被触发,但修复后未来用户从 legacy 升级时不会丢数据 |

## 验证策略

1. `dotnet build -c Release` — 编译通过证明无残留引用
2. `dotnet test -c Release` — 全部测试通过
3. `tools/verify.ps1` — 完整门禁通过
4. `grep -r "LiteDb\|MigrationTool" src/ tests/` — 返回空

## 不做

- 不重写 LauncherJson(已有 temp+rotate+replace,稳健)
- 不动 LauncherStore.MigrateFromLegacyFiles 的整体逻辑(只修文件名常量)
- 不删 pitfalls.md 中历史性 LiteDB 引用(那是真实踩坑记录)
