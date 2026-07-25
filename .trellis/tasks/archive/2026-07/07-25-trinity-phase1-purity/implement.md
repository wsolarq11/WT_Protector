# Implementation Plan: Phase 1 Zone 0 纯函数化

## Batch 0: 基础设施

### 0.1 创建新文件骨架
- [ ] `src/WTLauncher.Core/LauncherJsonSerializer.cs` — 纯序列化函数
- [ ] `src/WTLauncher.App/Infrastructure/ILauncherJsonIO.cs` — IO 接口
- [ ] `src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs` — IO 实现
- [ ] `src/WTLauncher.Core/LauncherStoreValidator.cs` — 纯验证函数
- [ ] `src/WTLauncher.App/Infrastructure/IStorePersistence.cs` — 存储接口
- [ ] `src/WTLauncher.App/Infrastructure/FileStorePersistence.cs` — 存储实现
- [ ] `src/WTLauncher.App/Infrastructure/LegacyMigration.cs` — 旧格式迁移

**验证**: `dotnet build -warnaserror` 通过

### 0.2 LauncherJson.cs 拆分
- [ ] 将 `Serialize`/`Deserialize`/`NormalizeLineEndings` 移到 `LauncherJsonSerializer`
- [ ] 定义 `ILauncherJsonIO` 接口（Load/Save/Cleanup/Rotate 方法签名）
- [ ] 将 IO 代码移到 `FileLauncherJsonIO`（含 SaveLock、备份、恢复逻辑）
- [ ] 改造 `LauncherJson.cs` 为 facade，内部持有 `ILauncherJsonIO` 实例并委托
- [ ] `[assembly: InternalsVisibleTo]` 调整为新文件的归属

**验证**: `dotnet build -warnaserror` + `dotnet test -c Release --filter "FullyQualifiedName~LauncherJson"` 全绿

### 0.3 LauncherStore.cs 拆分
- [ ] 将 schema 验证逻辑移到 `LauncherStoreValidator`
- [ ] 定义 `IStorePersistence` 接口
- [ ] 将路径解析 + 文件 IO 移到 `FileStorePersistence`
- [ ] 将 `MigrateFromLegacyFiles` + `TryMoveToBak` 移到 `LegacyMigration`
- [ ] 改造 `LauncherStore.cs` 为 facade，内部持有 `IStorePersistence` 实例并委托

**验证**: `dotnet build -warnaserror` + `dotnet test -c Release --filter "FullyQualifiedName~LauncherStore"` 全绿

## Batch 1: 全量验证

### 1.1 修复编译错误
- [ ] 检查所有引用 `LauncherJson` 的调用点（grep 确认无遗漏）
- [ ] 检查所有引用 `LauncherStore` 的调用点
- [ ] 修复 `[assembly: InternalsVisibleTo]` 在拆分后的归属

**验证**: `dotnet build -warnaserror` 零错误

### 1.2 运行全量测试
- [ ] `dotnet test -c Release` 全量运行，确认 700+ tests 全绿

**验证**: 700+ tests PASS

## Batch 2: CsCheck 属性基测试

### 2.1 添加 Fixture 生成器
- [ ] 在 `tests/WTLauncher.Tests/` 下创建 `LauncherJsonPbtFixtures.cs`
  - `Gen<LauncherStoreFile>` 生成器（随机 entries/presets/settings）
  - `Gen<string>` 含 CRLF 的随机 JSON 字符串

### 2.2 添加 LauncherJsonSerializer PBT
- [ ] `LauncherJsonSerializerPbtTests.cs`:
  - `SerializeDeserializeRoundtrip`: 任意 LauncherStoreFile → Serialize → Deserialize → 相等
  - `DeserializeSerializeRoundtrip`: 任意 JSON → Deserialize → Serialize → 结构保持
  - `NormalizeLineEndings`: 任意含 CRLF 字符串 → 输出含 `\n` 不含 `\r\n`

### 2.3 添加 LauncherStoreValidator PBT
- [ ] `LauncherStoreValidatorPbtTests.cs`:
  - `ValidStorePasses`: 随机有效 store → Validate 返回 Success
  - `InvalidSchemaVersionRejected`: 随机无效 schema 版本 → Validate 返回 Error

### 2.4 运行 PBT
- [ ] `dotnet test -c Release --filter "FullyQualifiedName~Pbt"` 确认 6+ 条 PBT 通过

**验证**: 6+ PBT 全绿，每条 >= 1000 次迭代

## Batch 3: 门禁

### 3.1 运行完整 gate
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` 全绿

### 3.2 代码审查
- [ ] 确认 Zone 0 新文件无文件 IO 引用
- [ ] 确认 Zone 1 新文件无业务逻辑
- [ ] 确认 facade 方法签名与原始 API 完全一致

## 回滚计划

如果 gate 失败：

1. `git diff` 查看改动
2. 如果 facade 委托有问题 → 回退到直接调用新组件
3. 如果接口设计有问题 → 简化接口，合并方法
4. 如果不可修复 → `git checkout -- .` 回退所有改动，分析失败原因

## 执行顺序

```
Batch 0.1 → 0.2 → 0.3 → 1.1 → 1.2 → 2.1 → 2.2 → 2.3 → 2.4 → 3.1 → 3.2
```
