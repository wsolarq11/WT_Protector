# 清理 LauncherPresetFile 死字段 + 改名 LauncherPresetFiles

## Goal

`LauncherPresetFile` 在三文件时代(presets.json)是独立反序列化目标,带 `SchemaVersion` 字段;commit 3c394b3 合并到单文件 launcher.json 后,该类型降级为 VM 内部状态容器(承载 `Presets + DefaultPresetId` 配对)。`SchemaVersion` 字段从此再也不被 JSON 反序列化读写,但代码、JSON 注解、`LauncherPresetFiles.Validate()` 还在维护它——死字段,误导。

同时 `LauncherPresetFiles` 静态类只有 `Validate()` 一个方法,命名复数(`Files`)暗示"文件管理",实际职责是"验证器",改名 `LauncherPresetValidator` 反映实际。

## Scope Decision (ponytail ultra)

原 PRD 提议"删整个 `LauncherPresetFile` 类型"。调研后**否决**:
- VM 维护 `_settings`/`_presetFile`/`_entries` 三套独立状态
- `_presetFile` 是 `Presets + DefaultPresetId` 的命名配对容器,删除后两者必须分离存储
- 8 个 Core 函数(`LauncherPresetWorkflow` × 10、`LauncherEntryReferences` × 4)签名都要从 `LauncherPresetFile` 改成 `(ImmutableList<LauncherPreset>, string)` 元组——API 复杂度上升
- 净效果:**删一个类型 + 加一坨元组解构 = 不是减肥,是搬运**

保留 `LauncherPresetFile`,只做真死代码清理 + 命名矫正。

## Requirements

### R1: 移除 `LauncherPresetFile.SchemaVersion` 字段
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherPreset.cs:10-12` — 删除 `[JsonPropertyName("schemaVersion")]` + `[JsonRequired]` + `public int SchemaVersion { get; init; } = 1;`
- 该字段从不写入 launcher.json(顶层 `LauncherStoreFile.SchemaVersion` 是唯一 schema 门)
- 该字段从不从 launcher.json 读取(`LauncherStoreFile` 直接反序列化 `presets` 数组)

### R2: 改名 `LauncherPresetFiles` → `LauncherPresetValidator`
- `src/avalonia-launcher/src/WTLauncher.Core/LauncherPresetFiles.cs` 重命名为 `LauncherPresetValidator.cs`
- 类名同步改:`public static class LauncherPresetFiles` → `public static class LauncherPresetValidator`
- 所有引用同步:
  - `LauncherEntryMutationSnapshot.cs:42,54` (`LauncherPresetFiles.Validate(...)` → `LauncherPresetValidator.Validate(...)`)
  - `LauncherPresetWorkflowTests.cs:49` (测试)
- `Validate` 方法本身简化:删除 `if (presets.SchemaVersion != 1) throw ...` 检查(因为字段已删)

### R3: 不破坏现有测试
- `LauncherPresetFile` 测试 fixture(`LauncherPersistenceTestFixtures.cs:19,26`)仍能工作
- 只删 SchemaVersion 字段引用(如果 fixture 设置了 `SchemaVersion = 1`,直接删这行)

### R4: 不动 LauncherPresetFile 类型本身
- 保留为 `sealed record LauncherPresetFile { DefaultPresetId, Presets }`(只剩两字段)
- 仍是 VM 的状态容器
- JSON 反序列化路径不经过它(经过 `LauncherStoreFile`)

## Acceptance Criteria

- [ ] `LauncherPresetFile.SchemaVersion` 字段不存在(grep 验证)
- [ ] `LauncherPresetFiles` 类不存在(grep 验证)
- [ ] `LauncherPresetValidator` 类存在,有 `Validate(LauncherPresetFile)` 方法
- [ ] `dotnet test -c Release` 仍 451 passing
- [ ] `tools/verify.ps1` 仍 PASS
- [ ] launcher.json 不变(顶层 schemaVersion=2 仍是唯一)

## Notes

- 单次 commit 完成
- 不动 LauncherPresetWorkflow / LauncherEntryReferences 的签名(它们仍接受 LauncherPresetFile)
- 不动 LauncherStoreFile(已经正确,SchemaVersion=2)
- 历史性 ponytail 注释保留(`LauncherStore.MigrateFromLegacyFiles` 中的 LegacyPresetDocument 仍有 SchemaVersion=1,因为它是兼容旧 presets.json 的迁移代码)
