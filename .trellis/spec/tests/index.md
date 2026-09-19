# Tests Layer Guidelines

> xUnit v3 + ArchUnitNET，覆盖全部 5 层。测试是主线门禁，提交前必须绿。文件/测试计数随代码演进：`ls tests/WTLauncher.Tests/*.cs | wc -l` + `dotnet test -c Release` 取现数。

参考：`Support/LauncherPersistenceTestFixtures.cs`（`Entry()` / `PresetFile()` 工厂）、`Support/TestRepositoryPaths.cs`（临时目录）、`Architecture/ArchitectureRules.cs`（R1-R10）。

## Guidelines Index

| Guide | 内容 |
|-------|------|
| [Test Patterns](./test-patterns.md) | fixture、共享 helper、工厂方法 |
| [Test Quality](./test-quality.md) | 测什么、命名、覆盖预期 |

## Pre-Development Checklist

- [ ] 新测试前读本层 2 份指南 + [Pitfalls](../guides/pitfalls.md) §2.15 / §2.16 / §3.12 / §3.14。
- [ ] 用临时目录（`TestRepositoryPaths`），禁写真实 `config/`。
- [ ] 时间依赖用固定时钟注入，禁 Sleep/Delay。

## Quality Check

- [ ] `dotnet build ... -c Release --no-restore -warnaserror` + `npm run test:fast` + `dotnet test ... -c Release --no-restore`。
- [ ] 声称完成前 `npm run verify`。
- [ ] 每个测试至少一个 `Assert`；无 `[Fact(Skip=...)]` 累积。
