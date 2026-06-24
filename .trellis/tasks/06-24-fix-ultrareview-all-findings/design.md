# Design — UltraReview 全量修复

## Architecture Decisions

### ADR-1: PowerShell 命令传递改用 `-EncodedCommand` (C1/C2)

**决策**: `LaunchArgumentBuilder.Build()` 中将 command 做 UTF-8 Base64 编码，`ShellCommandPrefix` 从 `"pwsh -NoExit -Command"` 改为 `"pwsh -NoExit -EncodedCommand"`。

**数据流变更**:
```
Before:  user command → QuoteArgument() → "pwsh -NoExit -Command "...""
After:   user command → UTF-8 Base64 → "pwsh -NoExit -EncodedCommand <base64>"
```

**影响范围**: 
- `LaunchArgumentBuilder.cs` — `ShellCommandPrefix` 常量, `Build()` 方法
- `LaunchPlan` 的 `Arguments` 属性值变化（command 从明文变 Base64，其他参数不变）
- `CommandLine` 属性（仅用于 UI 显示/DryRun）需解码 Base64 为可读形式
- 所有测试中涉及 `CommandLine` / `Arguments` 断言的需更新

**QuoteArgument 作用域**: 仍用于 `workingDirectory` 和 `title` 参数（它们不是 PowerShell 代码，不需要 Base64）。

### ADR-2: 引入 System.Collections.Immutable (H2)

**决策**: Core 层新增 `System.Collections.Immutable` NuGet 依赖。将以下类型中的集合属性改为不可变类型：
- `LauncherSettings.CustomEntryOrder`: `List<string>` → `ImmutableList<string>`
- `LauncherSettings.CommandTemplates`: `Dictionary<K,V>` → `ImmutableDictionary<string, string>`
- `LauncherSettings.SearchHistory`: `List<string>` → `ImmutableList<string>`
- `LauncherPresetFile.Presets`: `List<LauncherPreset>` → `ImmutableList<LauncherPreset>`
- `LauncherPreset.EntryIds`: `List<string>` → `ImmutableList<string>`

**变异模式变更**:
```csharp
// Before (mutable)
_settings.CustomEntryOrder.Add(entryId);           // 直接变异
update.Target.Clear(); update.Target.AddRange(...); // 就地替换

// After (immutable)
_settings = _settings with { CustomEntryOrder = _settings.CustomEntryOrder.Add(entryId) };
_presetFile = _presetFile with { Presets = updatedPresets };
```

**JSON 序列化**: `System.Text.Json` 原生不支持 `ImmutableList`/`ImmutableDictionary`。需添加自定义 JsonConverter，或改用 `[..list]` 在序列化前转为数组。

**回退方案**: 如果 ImmutableList 带来过大变更量，对 `EntryIds`(LauncherPreset) 和 `Presets`(LauncherPresetFile) 可降级为方案 B（`IReadOnlyList<T>` 接口 + Clone 时拷贝），保留运行时防御力但接受 `sealed record` 的语义不够纯。

### ADR-3: LauncherConfigStore.Load 单次 IO (C7)

**决策**: 合并两次文件读取为一次。新流程：
```
1. byte[] content = File.ReadAllBytes(configPath)
2. JsonDocument doc = Parse(content)  → RequireEntrySelectionFields(doc)
3. List<LaunchEntry> entries = Deserialize<List<LaunchEntry?>>(content)
4. return NormalizeEntries(entries)
```

**影响**: `RequireEntrySelectionFields` 从接受 `string configPath` 改为接受 `JsonDocument doc`。`LauncherJson.Load<T>`/`LoadRequired<T>` 新增 `byte[]` 或 `ReadOnlySpan<byte>` 重载。

### ADR-4: 备份操作重排序 (C6)

**决策**: 调整 `LauncherJson.Save` 的操作顺序：
```
1. WriteAllText(temp)
2. File.Move(temp → target)          ← 先原子提交
3. RotateBackups(target)             ← 后轮转备份（成功提交后才移位）
4. File.Copy(target → .bak)           ← 已轮转，拷入最新
```

**影响**: 如果 File.Move 成功但后续 RotateBackups 失败，target 文件已更新但备份链不完整。需要 try/catch 包裹备份操作（备份失败不影响新数据的保存）。

### ADR-5: 拖拽预览双路径统一 (H5)

**决策**: 方案 A — 拖拽期间使用轻量版 FilterAndSort（不做 card size 计算），从 `_settings.CustomEntryOrder` 统一推导 `_visibleEntries`。

**新增方法**:
```csharp
private void SyncVisibleEntriesFromCustomOrder()
{
    var sorted = LauncherEntryVisibility.FilterAndSort(
        _entries, FilterText, ..., _settings.CustomEntryOrder, ToLaunchEntry);
    SyncVisibleEntries(sorted);
    // 不调用 ApplyCardSize
}
```

### ADR-6: 文件日志系统 (H10, C4, C5)

**决策**: 新增 `WTLauncher.App/Infrastructure/ErrorLog.cs`（internal static class），写入 `%LOCALAPPDATA%/WTLauncher/errors.log`。

**日志格式**: `ISO8601 [context] exception.ToString()`

**调用模式**: 在所有现有 catch 块中添加 `ErrorLog.Write(ex, "context")` 调用。

### ADR-7: wt.exe 完整路径 (H8)

**决策**: `TerminalExecutable` 改为运行时解析：
```csharp
internal static string ResolveTerminalExecutable()
{
    var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
    if (string.IsNullOrEmpty(localAppData))
        return "wt.exe"; // fallback
    var wtPath = Path.Combine(localAppData, @"Microsoft\WindowsApps\wt.exe");
    return File.Exists(wtPath) ? wtPath : "wt.exe";
}
```

### ADR-8: H5/H11/M12/M13 联动决策

- **H11**: 批量启动路径的 `ConfirmBatchLaunchIfRequiredAsync` 改为检查 preflight 中每个 item 的 `Plan.RequiresConfirmation`（而非仅全局设置）。当全局 `RequireConfirmationForLaunches = false` 但存在 `FailedCount > 0` 时，仍然确认。
- **M12**: 保持 ViewModel 对 Avalonia 的 NuGet 依赖（DESIGN.md Non-goal 已明确"不做跨平台设计系统"）。
- **M13**: 与 H11 联动修复，确认逻辑源统一为 `LaunchPlan.RequiresConfirmation`。

---

## Data Flow & Contracts

### Base64 Command Flow (C1/C2)

```
UserInput("echo $(calc)")
  → LauncherEntryText.Normalize()
  → LauncherCommandTemplateResolver.Resolve()   // $key → template value
  → Encoding.UTF8.GetBytes(command)
  → Convert.ToBase64String(bytes)
  → "$WorkingDirectoryOption {dir} --title {title} $ShellCommandPrefix {base64}"
  → Process.Start()
  → wt.exe 接收参数
  → pwsh -EncodedCommand <base64>
  → PowerShell 解码 Base64 → UTF-8 → 执行（字面量，不展开 $() 等）
```

### ImmutableList Mutation Flow (H2)

```
Before: _settings.CustomEntryOrder.Add(id)       [就地变异]
After:  var newOrder = _settings.CustomEntryOrder.Add(id);
        _settings = _settings with { CustomEntryOrder = newOrder };  [新实例]
```

### Snapshot/Rollback Impact

现有 snapshot 模式使用 `LauncherSettingsCopies.Clone()` 做深拷贝。切换到 ImmutableList 后：
- `Clone()` 简化为 `settings with { CustomEntryOrder = settings.CustomEntryOrder }`（ImmutableList 的值语义使拷贝变 O(1)）
- `RestoreSnapshot` 无需变更 — 快照持有的 ImmutableList 引用本身不受后续变异影响

---

## Compatibility

### 不破坏现有功能
- 所有 API 契约不变（public 方法签名不变）
- 配置文件格式不变（JSON schema 不变）
- `LaunchPlan.Arguments` 中 command 部分从明文变 Base64，但 `CommandLine` 属性解码后保持可读

### JSON 序列化兼容
- `LaunchEntry` 字段类型不变
- `LauncherSettings` 字段逻辑类型不变（接口改为 ImmutableXxx），需自定义 JsonConverter

### 测试兼容
- 断言 `Arguments` 含明文的测试需更新（改用 Base64）
- 断言 `CommandLine` 含明文的测试需更新
- 所有其他测试继续通过

---

## Trade-offs

| 决策 | 收益 | 代价 |
|------|------|------|
| Base64 | 彻底消除 PS 注入面 | DryRun 需解码显示；测试更新 |
| ImmutableList | 真正不可变，`with { }` 语义完整 | NuGet 依赖 +1；JsonConverter 开销；变异点改写 |
| 备份后移 | 数据完整性保证 | 备份失败不影响新数据的副作用（有意） |
| 单次 IO | 消除 TOCTOU | RequireEntrySelectionFields 接口变更 |
| 轻量 FilterAndSort | 统一数据源 | 拖拽期间额外 O(n log n) 排序（替代 O(1) Move） |
