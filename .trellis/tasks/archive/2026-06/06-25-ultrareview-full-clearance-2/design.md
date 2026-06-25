# Technical Design — 清零 UltraReview 全部发现项

## Architecture Decisions

### ADR-1: SchemaVersion 序列化 (R1)

**决策**: 移除 `LauncherSettings.SchemaVersion` 的 `[JsonIgnore]`，添加 `[JsonPropertyName("schemaVersion")]`。

**影响**:
- 现有 `settings.json` 文件不包含 `schemaVersion` 字段。`[JsonRequired]` 不能加（会拒绝现有文件），因此必须设置默认值 1 并容忍缺失。
- 加载时版本不匹配 → `InvalidDataException`（与 `LauncherPresetStore.Load` 一致）。
- `LauncherSettingsCopies.Clone()` 中 `SchemaVersion = 1` 的赋值变为有效代码。

**兼容性**: 向后兼容。旧文件无 `schemaVersion` → 默认 1。新文件写入 `schemaVersion: 1`。

### ADR-2: 损坏文件回退到 .bak (R3)

**决策**: 在三个 Store.Load 中包装 `try-catch (JsonException | InvalidDataException)`，失败时尝试 `.bak` 恢复。

**实现**:
```csharp
try {
    var data = LauncherJson.LoadRequired<T>(path, label);
    // validate...
} catch (Exception ex) when (ex is JsonException or InvalidDataException) {
    if (File.Exists(backupPath)) {
        File.Copy(backupPath, path, overwrite: true);
        var data = LauncherJson.LoadRequired<T>(path, label); // 重试
        // validate...
    } else throw; // 无备份可用，传播原始异常
}
```

**权衡**: 备份恢复覆盖损坏的主文件。这是数据恢复的正确行为——损坏文件已不可用，备份是最后希望。

### ADR-3: ErrorLog 大小限制 (R4)

**决策**: 保持简单。1MB 上限 + 单文件回滚（`.errors.log` → `.errors.log.old`）。

不引入按日期轮转——桌面启动器错误日志量极小，1MB 足够数年使用。只在文件超过 1MB 时移到 `.old` 再新建。

### ADR-4: StartBatch 失败策略 (R9)

**决策**: `break` → `continue`。一个条目的启动失败不应阻止后续条目。与 `PreflightBatch` 行为统一。

**风险**: `FailureIndex` 字段使用 `??=` 仅记录第一个失败索引。改为 `continue` 后多次失败会丢失后续失败索引。修复：存储最后一个失败索引而非第一个。

### ADR-5: ID 规范化统一 (R10)

**决策**: `LauncherPresetIds.Normalize` 的超长抛异常改为静默截断 256 字符（与 `LauncherEntryIds.Normalize` 一致）。

统一原则：规范化函数不应抛异常——它们将任意输入转为安全形式。验证由调用方完成。

### ADR-6: LaunchStartupArguments 重构 (R14)

**决策**: `static (string? configPath, IReadOnlyList<string> unknownArgs) ReadConfigPath(string?[]? args)` 返回元组。移除 `static List<string> _unknownArgs`。

**影响**: 调用方 `App.axaml.cs` 需更新解构语法。一处改动，风险低。

### ADR-7: Unicode 正规化 (R26)

**决策**: 在 `LauncherEntryText.Normalize()` 末尾添加 `string.Normalize(NormalizationForm.FormKC)`。

`FormKC` 兼容性最好——将全角字母转为半角，组合变音转为预组合字符。对搜索匹配和排序一致性有帮助。

## Data Flow Impacts

| 修复 | 数据流影响 |
|------|-----------|
| R1 | `settings.json` 写入 `schemaVersion` 字段 |
| R2 | `ToDictionary` → `GroupBy` 或 `TryAdd` 循环，重复 ID 给出明确错误 |
| R3 | Store.Load 增加 try-catch + bak 恢复路径 |
| R4 | ErrorLog.Write 增加文件大小检查 |
| R6 | LaunchAsync 中 `SaveConfig()` 返回值影响 `StatusText` |
| R7 | UpdatePreset 名称冲突检查使用规范化 ID |
| R9 | StartBatch 循环不因单次失败中断 |
| R14 | ReadConfigPath 返回元组，消除静态可变状态 |
| R15 | RenameEntryId 返回 bool 指示是否成功 |

## Compatibility

- 所有 JSON 格式变更向后兼容（`schemaVersion` 缺失 → 默认 1）
- 无 API 签名变更影响外部调用者
- 无设计契约变更
- Core 层零外部依赖约束保持不变
