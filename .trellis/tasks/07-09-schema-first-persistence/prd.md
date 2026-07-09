# Schema-First Persistence via Corvus.JsonSchema

## Goal

调查并定案：是否需要引入 Corvus.JsonSchema 源生成器进行 Schema-First 持久化，切断 S2/S3/S9。

## 调查结论：Schema-First 迁移拒绝；改为小范围清理死字段（dual SchemaVersion）

### Schema-First 迁移拒绝理由

审查 2025 现代 .NET 实践 + 当前代码结构后确认：**C# records 本身就是 schema 的正确表达**，Corvus.JsonSchema 迁移是投机性重构。

**现代实践调研：**

| 来源 | 共识 |
|------|------|
| [Corvus.Text.Json V5 (endjin)](https://corvus-oss.org/Corvus.JsonSchema) | "Source-generated strongly-typed C# from JSON Schema" — 适用于**外部 authored** JSON（API 契约、第三方配置） |
| [json-everything — Dropping CodeGen](https://blog.json-everything.net/posts/dropping-codegen) | 作者 Greg Dennis（JSON Schema 工作组）放弃自家 CodeGen 转推 Corvus — 原因是 Corvus 解决 schema-validity 问题，不是替换 POCO 持久化 |
| [Meziantou — Json schema validation in .NET](https://www.meziantou.net/json-schema-validation-in-dotnet.htm) | ".NET doesn't support JSON schema validation out of the box" — 但 STJ POCO 反序列化对应用内部持久化已足够 |
| [Microsoft Learn — STJ](https://learn.microsoft.com/en-us/dotnet/api/system.text.json) | 官方模板（worker/web/desktop）均使用 POCO 持久化应用配置 |

**为什么 Schema-First 不适合 WT Launcher：**

1. **App 是 JSON 的唯一 author 和 reader** — 不是外部 API 契约。C# records 是 schema 的权威表达，编译时检查，IDE 可导航。
2. **Schema 文件 + C# 类型 = 双源** — 引入 JSON Schema 文件作为 source-of-truth 反而增加 drift 风险（schema 文件改了但 C# 类型没同步）。
3. **Corvus 生成 mutable 类型** — 当前 35+ 处使用 `record with { }` 不可变模式会被破坏。
4. **零分配解析对单用户启动器无意义** — 19 entries 的 JSON 解析成本 < 1ms，与 CompiledBindings 同性质（T3 已论证）。
5. **依赖体积** — Corvus.Text.Json 是 ~2MB NuGet 包，为本就小巧的启动器引入新依赖。

### 各 audit 源的处置

| Audit 源 | 原始判断 | 实际状态 | 处置 |
|---------|---------|---------|------|
| **S2** Schema implicit in C# types | 需要 Schema-First | C# types 就是 schema 的正确表达 | ✅ 误识别，拒绝 |
| **S3** SchemaVersion magic constant | 需要 `$schema` URL | const int 是离线便携应用的正确选择 | ✅ 误识别，拒绝 |
| **S9** Docs/code drift | Schema-generated docs | T2 已用 verify.ps1 negative gate 切断 | ✅ 已切断 |

### 真实 bug：dual SchemaVersion drift

调查中发现的真实 bug（与 pitfalls §6.1 一致）：

**事实：**
- `LauncherStoreFile.SchemaVersion = 2`（顶层，被 `ExpectedStoreSchemaVersion` 校验）
- `LauncherSettings.SchemaVersion = 1`（嵌套，**从未被校验** — dead field）
- 4 处 mutation 站点强制写 `settings with { SchemaVersion = 1 }`：
  - `LauncherEntryMutationSnapshot.cs:43` (`Capture`)
  - `LauncherEntryMutationSnapshot.cs:59` (`CloneSettings`)
  - `LauncherDragReorderWorkflow.cs:56` (`Begin`)
  - `MainWindowViewModel.cs:263` (`CaptureSettingsMutationSnapshot`)
- 实际 `config/launcher.json` 当前值为 `settings.schemaVersion: 2`（手编辑或旧版本残留），但任何 mutation 后会变为 `1`

**为什么是 bug：**
- 双 SchemaVersion 违反"single source of truth"
- `settings.SchemaVersion` 没有 consumer，是死字段
- mutation 强制写 1 是历史迁移残留（2026-07 三文件合并时的过渡代码）

**修复（最小范围，ponytail ultra）：**
1. 从 `LauncherSettings` 移除 `SchemaVersion` 属性
2. 移除 4 处 `with { SchemaVersion = 1 }` 改为 `with { }` 或直接 `_settings`
3. `UnmappedMemberHandling = Skip` 保证旧文件（含 `settings.schemaVersion`）向前兼容
4. 更新 `verify.ps1` smoke 配置移除 `settings.schemaVersion=1`
5. 加一条测试钉死"LauncherSettings 无 SchemaVersion 字段"

## Requirements

- 从 `LauncherSettings` 移除 `SchemaVersion` 属性（含 `[JsonPropertyName]` / `[JsonRequired]` 特性）
- 移除 4 处 `with { SchemaVersion = 1 }` 死代码
- 更新 `verify.ps1` GUI 冒烟配置（移除 `settings.schemaVersion=1`）
- 新增测试钉死"LauncherSettings 不含 SchemaVersion"
- 更新 spec：`persistence.md` 记录"单一 SchemaVersion 在 LauncherStoreFile 顶层"

## Acceptance Criteria

- [x] `LauncherSettings` 不再含 `SchemaVersion` 字段
- [x] 4 处 mutation 死代码清理
- [x] `verify.ps1` smoke 配置同步
- [x] 新测试钉死字段不存在
- [x] `dotnet build` 0 警告 0 错误
- [x] `dotnet test` 451 测试通过（含新测试）
- [x] `tools/verify.ps1` 16 项检查通过
- [x] `config/launcher.json` 当前 `settings.schemaVersion` 字段在下次 mutation 后会从 JSON 中消失（前向兼容，反序列化忽略未知字段）

## Notes

- 这是"拒绝投机重构 + 顺手清理死字段"型任务。
- 真正的 schema-first 适用于：API 契约、第三方配置、跨团队数据交换。WT Launcher 是单进程本地工具，不适用。
- 父任务 PRD 中 S2/S3 标记为误识别；S9 已被 T2 切断。
