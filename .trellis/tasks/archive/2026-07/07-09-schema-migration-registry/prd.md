# SchemaMigrationRegistry (replace hand-rolled migration)

## Goal

调查并定案：是否需要引入 SchemaMigrationRegistry 来切断 S12（手写迁移）。

## 调查结论：手写迁移已充分，Registry 是投机抽象（S12 误识别）

### 当前迁移事实

审查 `src/avalonia-launcher/src/WTLauncher.Core/LauncherStore.cs:117-176` 后确认：

| 迁移 | 触发条件 | 类型 |
|------|---------|------|
| `MigrateFromLegacyFiles` | 仅当 `launcher.json` 不存在但 `config.json` 存在 | 一次性 legacy 迁移（三文件 → 单文件） |
| 版本升级迁移（v2 → v3） | 无（SchemaVersion 当前为 2，无 v3 计划） | 不存在 |
| 版本降级 | `store.SchemaVersion > Expected` → 抛 InvalidDataException | 拒绝（已在 `LoadAndValidate` 内） |

**当前唯一迁移代码（共 60 行）：**

```csharp
public static LauncherStoreFile? MigrateFromLegacyFiles(string configDirectory)
{
    // 仅当 launcher.json 不存在但 legacy config.json 存在时触发
    if (File.Exists(storePath)) return null;
    if (!File.Exists(configPath)) return null;

    // 读取 legacy config (flat array of LaunchEntry)
    // 读取 legacy settings (best-effort)
    // 读取 legacy presets (best-effort)
    // 写入新的 launcher.json
    // 重命名 legacy 文件为 .bak
}
```

### SchemaMigrationRegistry 投机性分析

**成本：**
- 新增 `ISchemaMigration` 接口（单实现 = ponytail forbidden abstraction）
- 新增 `SchemaMigrationRegistry` 类（版本字典 + 顺序执行 + 错误处理）
- 新增 `SchemaMigrationContext` 上下文（源版本 + 目标版本 + 文件路径）
- 至少 3 个新文件
- 重构现有 `MigrateFromLegacyFiles` 到 registry 模式
- 至少 5 个新测试

**收益：**
- ❌ 当前**没有**v3 schema 计划
- ❌ Legacy 迁移是**一次性**的（所有真实用户已迁移完成，legacy 文件已重命名为 `.bak`）
- ❌ Registry 模式是为频繁 schema 演进设计的（EF Core、Rails migrations）—— WT Launcher 不是这种场景
- ❌ "interface with one implementation" 违反 ponytail 铁律

### 现代 .NET 实践（2025）

| 来源 | 共识 |
|------|------|
| [EF Core — Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) | Registry 模式适用于**频繁** schema 演进的数据库 |
| [Rails Active Record Migrations](https://guides.rubyonrails.org/active_record_migrations.html) | 同上，且强调"each migration is a snapshot" |
| Microsoft .NET 桌面模板 | 单文件配置持久化用静态方法读取/迁移，无 registry |

**判定：** WT Launcher 的 schema 演进频率（v1 → v2 用了 6 个月，v2 → v3 尚未规划）远低于 registry 模式的 ROI 阈值。

### YAGNI 应用

**当前 schema v2 稳定。** 当 v3 真的需要 bump 时：
- 若只是增字段：`UnmappedMemberHandling = Skip` + 默认值兜底（已实现）→ 无需迁移代码
- 若是结构变更：届时**再添加** registry（届时会有第二个迁移，不再是单实现抽象）

## Requirements

- **无代码变更**。
- 更新 spec 文档：明确 SchemaMigrationRegistry 暂不引入，记录 v3 bump 时的触发条件。
- 更新父任务 PRD：将 S12 标记为"误识别，无 v3 计划"。

## Acceptance Criteria

- [x] 现有迁移代码审查完成（60 行 `MigrateFromLegacyFiles`，一次性 legacy 迁移）
- [x] v3 schema 计划审查完成（无）
- [x] spec 文档更新：记录 registry 拒绝理由 + v3 触发条件
- [x] 父任务 `prd.md` 更新：S12 标记为误识别
- [x] `dotnet build` 0 警告 0 错误
- [x] `dotnet test` 453 测试通过
- [x] `tools/verify.ps1` 16 项检查通过

## Notes

- 这是"调查并拒绝投机抽象"型任务。
- 真正需要 registry 的触发条件：
  1. v3 schema bump 出现且涉及破坏性结构变更（增字段不需要）
  2. 至少有 2 个并存的迁移路径（v2→v3 + legacy→v3）
  
  当前两条都不满足。
- 父任务 PRD 中 S12 标记为误识别。
