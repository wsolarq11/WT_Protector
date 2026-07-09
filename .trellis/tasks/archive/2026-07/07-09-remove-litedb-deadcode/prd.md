# 删 LiteDB 完整死链路

## Goal

彻底删除 LiteDB 持久化抽象的全部代码、依赖、测试、spec 引用。该链路是已删除的 IStore 抽象的遗留,生产路径(Program.cs:54)已被注释,永不触发。

## Background

审查发现(P0 #3.1):
- `WTLauncher.Core.csproj:8` 引用 LiteDB NuGet 包(增加发布物体积)
- `LiteDbStore.cs`(93 行)+ `MigrationTool.cs`(30 行)实现 JSON→BSON 迁移
- `Program.cs:54` `// MigrateJsonToLiteDb();  // LiteDB Phase 3 deferred` —— 调用点已注释
- `MigrationToolTests.cs`(3 个测试)仍在 CI 跑,测试一个永不执行的方法
- `quality-guidelines.md:69` 的 ponytail 注释用"LiteDB 是 IStore 抽象的故意例外"作为存在理由,但 IStore 已在 2026-07-06 删除

LiteDbStore.cs:9 自承"Phase 3 implementation — replaces hand-written JSON crash recovery",但 Phase 3 已废弃。README Non-goals 明确写"不引入额外依赖来替代当前 native gate"。

## Requirements

### R1: 删除代码文件
- `src/WTLauncher.Core/LiteDbStore.cs`(93 行)
- `src/WTLauncher.Core/MigrationTool.cs`(30 行)
- `tests/WTLauncher.Tests/MigrationToolTests.cs`(50 行)

### R2: 删除 Program.cs 中的死代码
- 删除 `Program.cs:54` 的注释 `// MigrateJsonToLiteDb();`
- 删除 `Program.cs:138-162` 的 `MigrateJsonToLiteDb()` 私有方法(24 行)

### R3: 删除 NuGet 依赖
- 删 `src/WTLauncher.Core/WTLauncher.Core.csproj:8` 的 `<PackageReference Include="LiteDB" />`

### R4: 删除 spec 死注释
- 删 `quality-guidelines.md:69` 的 LiteDB ponytail 注释,保留"Adding a NuGet reference to Core"这条 forbidden pattern 本身

### R5: 修复 LauncherStore.MigrateFromLegacyFiles 的潜在 bug
- `LauncherStore.cs:123` `var storePath = Path.Combine(configDirectory, "store.json");` 改为 `StoreFileName`
- 虽然 MigrationTool 删除后该路径不再被触发,但 LauncherStore.MigrateFromLegacyFiles 仍存在,文件名 bug 应一并修
- 注意:legacy 三文件迁移的触发条件是 config.json 仍存在,当前 repo 已无,但用户从老版本升级会触发

## Acceptance Criteria

- [ ] `find src -name "LiteDb*" -o -name "MigrationTool*"` 返回空(代码文件已删)
- [ ] `grep -r "LiteDb\|MigrationTool" src/` 返回空(无残留引用)
- [ ] `grep -r "LiteDb" .trellis/spec/` 仅返回历史性 pitfalls 引用(非活跃规则)
- [ ] `WTLauncher.Core.csproj` 无 LiteDB 依赖
- [ ] `dotnet build -c Release` 通过
- [ ] `dotnet test -c Release` 全部通过(原 62 文件,减 1 后应为 61 文件)
- [ ] `tools/verify.ps1` PASS

## Notes

- 不需要数据迁移代码:生产路径从未触发,用户磁盘上无 store.db
- 不需要 deprecation period:代码从未启用
- 单次 commit 完成
