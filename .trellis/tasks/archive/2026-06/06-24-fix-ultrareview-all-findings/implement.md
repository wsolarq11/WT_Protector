# Implement — UltraReview 全量修复执行计划

## Execution Order

严格按 P0 → P1 → P2 → P3 顺序。每级完成后运行 `tools/verify.ps1` 验证。

---

## Phase 0: 基础设施准备

- [ ] **INFRA-1**: 添加 `System.Collections.Immutable` NuGet 到 WTLauncher.Core.csproj
- [ ] **INFRA-2**: 创建 `WTLauncher.App/Infrastructure/ErrorLog.cs` — 文件日志
  ```csharp
  internal static class ErrorLog
  {
      private static readonly string LogPath = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          "WTLauncher", "errors.log");
      public static void Write(Exception ex, string context) { ... }
  }
  ```
- [ ] **INFRA-3**: 创建 ImmutableList/Dictionary 的 System.Text.Json JsonConverter（如需要）
- [ ] **INFRA-4**: 运行 `dotnet build` 确认基础设施就绪

---

## Phase 1: P0 — Critical (7)

### C1/C2: pwsh -EncodedCommand + Base64

- [ ] **P0-1a**: 改 `LaunchArgumentBuilder.ShellCommandPrefix` 从 `"pwsh -NoExit -Command"` 到 `"pwsh -NoExit -EncodedCommand"`
- [ ] **P0-1b**: 在 `Build()` 中对 command 做 `Convert.ToBase64String(Encoding.UTF8.GetBytes(command))`
- [ ] **P0-1c**: 更新 `LaunchPlan.CommandLine` 属性解码 Base64（保持 UI 可读）
- [ ] **P0-1d**: 更新所有断言 `Arguments`/`CommandLine` 的测试

### C3: App.axaml.cs finally

- [ ] **P0-2a**: `App.axaml.cs:76-109` — `try/catch` 改为 `try/catch/finally`，finally 中调用 `base.OnFrameworkInitializationCompleted()`
- [ ] **P0-2b**: 验证启动失败时 Avalonia 仍能正确清理（手动测试）

### C4: UnhandledException 日志

- [ ] **P0-3a**: `Program.cs:12-17` — 添加 `Console.Error.WriteLine` + `ErrorLog.Write`
- [ ] **P0-3b**: `Program.cs:19-23` — UnobservedTaskException 添加 ErrorLog.Write

### C5: Dispatcher.UnhandledException

- [ ] **P0-4a**: `App.axaml.cs` — 在 `OnFrameworkInitializationCompleted` 注册 `Dispatcher.UIThread.UnhandledException`
- [ ] **P0-4b**: Handler 中 `ErrorLog.Write(e.Exception, "UI")`，`e.Handled = true`

### C6: 备份操作重排序

- [ ] **P0-5a**: `LauncherJson.Save` — 调整顺序：WriteAllText → File.Move → RotateBackups + File.Copy
- [ ] **P0-5b**: 备份操作包在 try/catch 中（备份失败不影响保存成功）

### C7: 单次文件读取

- [ ] **P0-6a**: `RequireEntrySelectionFields` 改为接受 `JsonDocument` 参数
- [ ] **P0-6b**: `Load()` 中只读文件一次：`byte[] → JsonDocument 验证 → Deserialize`
- [ ] **P0-6c**: 非 Object 元素在 RequireEntrySelectionFields 中显式拒绝（M18）

### Phase 1 验证

- [ ] 运行 `dotnet test` 全部通过
- [ ] 手动测试：启动 WTLauncher，确认 Base64 启动正常
- [ ] 手动测试：模拟磁盘满 → 确认备份链完好

---

## Phase 2: P1 — High (15)

### H1: 可重入守卫

- [ ] **P1-1a**: `OnFilterTextChanged` — 添加 `_isHandlingFilterChange` 守卫
- [ ] **P1-1b**: `OnSelectedSortModeChanged` — 添加 `_isHandlingSortModeChange` 守卫
- [ ] **P1-1c**: `OnSelectedDisplayModeChanged` — 添加 `_isHandlingDisplayModeChange` 守卫
- [ ] **P1-1d**: `OnRequireConfirmationForLaunchesChanged` — 添加 `_isHandlingConfirmationChange` 守卫

### H2: ImmutableList/Dictionary 迁移

- [ ] **P1-2a**: `LauncherSettings.CustomEntryOrder` → `ImmutableList<string>`
- [ ] **P1-2b**: `LauncherSettings.CommandTemplates` → `ImmutableDictionary<string, string>`
- [ ] **P1-2c**: `LauncherSettings.SearchHistory` → `ImmutableList<string>`
- [ ] **P1-2d**: `LauncherPresetFile.Presets` → `ImmutableList<LauncherPreset>`
- [ ] **P1-2e**: `LauncherPreset.EntryIds` → `ImmutableList<string>`
- [ ] **P1-2f**: 更新所有 `.Add()`, `.Clear()`, `.AddRange()`, `[]=`, 变异点为 `with { }` 创建新实例
- [ ] **P1-2g**: 更新 `LauncherSettingsCopies.Clone()` — 简化（ImmutableList 值语义）
- [ ] **P1-2h**: 更新 `LauncherEntryReferences.ApplyUpdates` — 改用 immutable 变异
- [ ] **P1-2i**: 更新 `LauncherEntryOrder.SyncWithEntries` + `MoveRelative`
- [ ] **P1-2j**: 更新 `LauncherPresetFiles.Clone` + `Validate`
- [ ] **P1-2k**: 更新 `LauncherPresetWorkflow` 中 `Add/Update/Delete` 操作

### H3: Restore API 语义

- [ ] **P1-3a**: `Restore(LauncherSettings)` → `Restore()` (无参)
- [ ] **P1-3b**: 更新调用方（`LauncherSettingsMutationPersistence.Save`, `MainWindowViewModel`）

### H4: RenameReferences 异常消息

- [ ] **P1-4a**: `RenameReferences` — 循环前显式检查目标 ID 是否存在，给出明确异常

### H5: 拖拽双路径统一

- [ ] **P1-5a**: 新增 `SyncVisibleEntriesFromCustomOrder()` 轻量同步方法
- [ ] **P1-5b**: `ReorderVisibleEntryForDrag` 替换为调用新方法

### H6: 空原谅操作符

- [ ] **P1-6a**: `MainWindow.Drag.cs:117` — `decision.PreviewDraggedEntryId!` 改为显式 null 检查
- [ ] **P1-6b**: `MainWindow.Drag.cs:131` — `decision.DraggedEntryId!` 改为显式 null 检查

### H7: Presets.Helpers catch 过滤器

- [ ] **P1-7a**: 第 49 行 — `catch (Exception ex)` → `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))`
- [ ] **P1-7b**: 第 69 行 — 同上

### H8: wt.exe 完整路径

- [ ] **P1-8a**: `LaunchArgumentBuilder` — 添加 `ResolveTerminalExecutable()` 方法
- [ ] **P1-8b**: `Build()` 中使用解析后的完整路径

### H9: Drag.cs catch 过滤器

- [ ] **P1-9a**: 第 48 行 — 添加 `ExceptionFilters.IsRecoverable` + 通过 DataContext 设置 StatusText

### H10: 全局 ErrorLog.Write

- [ ] **P1-10a**: 审计所有 catch 块（24 个），在 StatusText 设置后添加 `ErrorLog.Write(ex, "context")`
- [ ] **P1-10b**: 重点文件：`MainWindowViewModel.Launch.cs`, `.Selection.cs`, `.Entries.cs`, `.Presets.cs`, `LauncherLaunchWorkflow.cs`

### H11: RequiresConfirmation 批量路径

- [ ] **P1-11a**: `ConfirmBatchLaunchIfRequiredAsync` — 检查 preflight items 中任何 `Plan.RequiresConfirmation = true`
- [ ] **P1-11b**: 保持 E8 覆盖（preflight report failures 总是确认）

### H12: 对话框 Brushes.White 替换

- [ ] **P1-12a**: 7 个文件 — `Brushes.White` → `Brush.Parse(LauncherDesignTokens.SurfaceColor)`
- [ ] **P1-12b**: `Brushes.LightGray` → `Brush.Parse(LauncherDesignTokens.BorderColor)`

### H13: Ready → 就绪

- [ ] **P1-13a**: `MainWindowViewModel.cs:191` — `"Ready"` → `"就绪"`

### H14: MainWindow.axaml 圆角令牌化

- [ ] **P1-14a**: 审计 MainWindow.axaml 中所有硬编码 `CornerRadius` 值
- [ ] **P1-14b**: 替换为 `{StaticResource CardBorderRadius}`

### H15: Validate 上限边界

- [ ] **P1-15a**: `LauncherPresetFiles.Validate` — preset 数量 ≤ 1000
- [ ] **P1-15b**: `NormalizeEntryIds` — 每 preset entryIds ≤ 10000
- [ ] **P1-15c**: `LauncherPresetIds.Normalize` — 名称 ≤ 256 字符

### Phase 2 验证

- [ ] P0 验收标准全部满足
- [ ] P1 验收标准全部满足
- [ ] `dotnet test` 全部通过
- [ ] `tools/verify.ps1` 通过

---

## Phase 3: P2 — Medium (22)

### M1: ExceptionFilters 加宽排除
- [ ] 添加 `AccessViolationException`, `SEHException` 到 `IsRecoverable` 排除列表

### M3: null! 类型谎言
- [ ] 3 处 `null!` → 改为可空类型 `?` 或消除 null 产生源

### M4: Preset 回滚补完
- [ ] `LauncherEntryMutationPersistence.cs:30-39` — `restorePresets: true`

### M5: CollectionChanged 批量
- [ ] `SyncVisibleEntries` — 添加批量抑制标志或 accept 现有行为

### M6: 冗余快照恢复移除
- [ ] `DeleteSelectedPresetAsync` — 移除确认后的冗余 RestorePresetMutationSnapshot

### M7: 幽灵坐标裁剪
- [ ] `CalculatePosition` 添加窗口边界参数 + 裁剪逻辑

### M8: Commit finally
- [ ] `Commit()` — try/finally 保护 `ClearPreviewState`

### M9: ToRequiredSet 去重
- [ ] 直接构建 HashSet，跳过中间 List

### M10: 异常消息脱敏
- [ ] 区分用户消息 vs 详细日志，UI 显示通用消息

### M11: GetPresetEntries 验证
- [ ] 添加 `TryGetValue` + 缺失条目异常

### M12: Avalonia 依赖
- [ ] 不修复 — DESIGN.md 明确 "不做跨平台设计系统"

### M13: RequiresConfirmation 统一
- [ ] 与 H11 联动完成

### M14: EntryIds mutable List
- [ ] 与 H2 联动完成

### M15: 时间戳一致性
- [ ] `ParseTimestamp` — null 和空字符串统一抛异常

### M16: App 启动异常
- [ ] 与 C3 联动完成

### M17: async void 风险
- [ ] 将 `OnEntryDragPointerPressed` 核心逻辑提取为 `async Task` 方法

### M18: 非 Object 元素拒绝
- [ ] 与 C7 联动完成

### M19: RotateBackups TOCTOU
- [ ] `File.Exists` → 直接 `try/catch FileNotFoundException`

### M20: 预设名称唯一性
- [ ] `Validate` 添加 name 重复检查

### M21: RequireCommandTemplates 重命名
- [ ] → `NormalizeCommandTemplates`

### M22: TrySaveWindowBounds 异常分离
- [ ] `InvalidDataException` 单独日志，不让它静默返回 false

### Phase 3 验证
- [ ] `dotnet test` 全部通过
- [ ] `tools/verify.ps1` 通过

---

## Phase 4: P3 — Low (25)

25 个 Low 优先级修复，按文件批量处理：

- [ ] **L1**: `ShouldInsertAfter` 添加 ±5% 迟滞区
- [ ] **L2**: Ghost 工厂接受可选的复用（非必需，现有性能可接受）
- [ ] **L5**: `_activeDragEntryId` 赋值添加注释说明两个生命周期点
- [ ] **L7**: `SyncVisibleEntries` 增量位置更新添加单元测试覆盖所有象限
- [ ] **L11**: `ValidateSortProjection` 添加 `out _` 或重命名为 `ThrowIfInvalidProjection`
- [ ] **L16**: `VisibleEntries` 的 `private set` → `get;` (只读)
- [ ] **L18**: `Brush.Parse` 包装在 try/catch (TypeInitializationException)
- [ ] **L20**: `Normalize` 添加注释说明 FormKC 的 ID 视觉影响
- [ ] 其余 17 项逐一修复

### Phase 4 验证
- [ ] 至少 80% Low 已修复
- [ ] `dotnet test` 全部通过
- [ ] `tools/verify.ps1` 通过

---

## Rollback Points

每个 Phase 结束后是一个回滚点。如果 Phase N+1 引入回归：
1. `git diff HEAD` 确认变更范围
2. `git stash` / `git reset --hard` 回到 Phase N 的提交点
3. 分析回归原因后重新开始

## Validation Commands

```bash
# 每阶段结束后运行
dotnet build src/avalonia-launcher/WTLauncher.sln --no-restore
dotnet test src/avalonia-launcher/WTLauncher.sln --no-build

# 最终验证
powershell -File src/avalonia-launcher/verify.ps1
```

## Key Risks

| 风险 | 影响 | 缓解 |
|------|------|------|
| ImmutableList NuGet 版本冲突 | 编译失败 | 固定版本到当前 .NET 10 兼容版本 |
| Base64 破坏测试断言 | 测试失败 | P0-1d 优先更新测试 |
| H2 重写面过大 | 引入新 bug | H2 拆分为 10 个子步骤，每步独立提交 |
| 设计令牌 XAML 替换遗漏 | 视觉不一致 | grep 验证零 `CornerRadius="[0-9]"` 残留 |
