# 修复 UltraReview 全部 69 项发现问题

## Goal

修复 2026-06-24 UltraReview 发现的所有代码缺陷、安全漏洞、架构违规和代码质量问题。
**目标置信度：无限逼近 100%**。不降测试标准来迁就产品代码缺陷。

## Context

刚刚完成的 UltraReview 覆盖 ~97% 源文件（~80 个源文件），通过 16 子代理 + 主线程审查，
发现 69 个问题：7 Critical + 15 High + 22 Medium + 25 Low。

完整发现清单记录在对话历史中，此处仅列出摘要。

---

## Requirements

### P0 — Critical (7) — 必须最先修复

#### C1: 命令模板注入 — 配置投毒→任意 PowerShell 代码执行
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LaunchArgumentBuilder.cs:28`
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherCommandTemplateResolver.cs:19-20`
- **问题**: `commandTemplates` 字典值无验证注入 `pwsh -Command`
- **修复方案（已确认）**: 方案 A — 与 C2 联动，改用 `pwsh -NoExit -EncodedCommand <Base64>`。`LaunchArgumentBuilder.Build()` 将 command 做 UTF-8 → Base64 编码后传给 `-EncodedCommand`。彻底消除 PowerShell 解释器注入面。
- **验收**: 恶意模板值（含管道/子表达式）在 Base64 中被视为字面量，不再被执行

#### C2: QuoteArgument 对 PowerShell 元字符无防护
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LaunchArgumentBuilder.cs:37-73`
- **问题**: `QuoteArgument` 正确转义了 Windows 命令行，但 `pwsh -NoExit -Command "..."` 中 `$()`/backtick 仍被 PowerShell 展开
- **修复方案（已确认）**: 与 C1 统一 — 改用 `-EncodedCommand`。`ShellCommandPrefix` 从 `"pwsh -NoExit -Command"` 改为 `"pwsh -NoExit -EncodedCommand"`。`QuoteArgument` 保持不变（仍用于目录/标题参数），command 改为 Base64 编码。
- **验收**: `$(calc)` 在 Base64 编码后成为字面量，不被 PowerShell 展开

#### C3: App.axaml.cs 异常路径不调用 base.OnFrameworkInitializationCompleted()
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs:105-108`
- **问题**: catch 块吞噬异常后不调用基类方法，Avalonia 框架初始化未完成
- **修复**: `try/finally` 确保 `base.OnFrameworkInitializationCompleted()` 始终执行
- **验收**: 启动失败时 Avalonia 仍能正确清理

#### C4: UnhandledException 在 Release 构建中静默丢失
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/Program.cs:12-17`
- **问题**: `Debug.WriteLine` 在 Release 中编译移除，`Trace.WriteLine` 未配置
- **修复**: 添加 `Console.Error.WriteLine` + 文件日志回退（写入 crash.log）
- **验收**: Release 构建中未处理异常至少记录到 stderr 和文件

#### C5: 缺少 Dispatcher.UIThread.UnhandledException 处理器
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs`
- **问题**: Avalonia UI 线程异常无处理器，binding error 即可崩溃进程
- **修复**: 在 `OnFrameworkInitializationCompleted` 中注册 `Dispatcher.UIThread.UnhandledException`
- **验收**: UI 线程异常设置 `e.Handled = true` 防止崩溃，同时记录日志

#### C6: 备份轮转在原子提交前执行 — 失败导致备份链损坏
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs:44-60`
- **问题**: `RotateBackups + File.Copy` 在 `File.Move` 之前执行；若 File.Copy 失败，备份链已移位但无新备份
- **修复**: 调整顺序：先写 temp，确认写入成功 → File.Move（原子提交）→ 再轮转备份；或在备份操作失败后回滚
- **验收**: 模拟磁盘满场景，备份链完整性不受破坏

#### C7: LauncherConfigStore.Load 双重文件读取 + TOCTOU + 解析器选项不一致
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherConfigStore.cs:12-22,125-147`
- **问题**: 同一文件用不同选项解析两次，中间有 TOCTOU 窗口
- **修复**: 只读一次文件，在内存中反序列化后再做 `RequireEntrySelectionFields` 验证
- **验收**: 文件只被 open/read 一次；两个验证/解析步骤使用同一份数据

---

### P1 — High (15) — 应在 P0 之后立即修复

#### H1: OnXxxChanged 4 个方法无重入守卫 → 持久化失败时无限递归
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Layout.cs:62-100,113-138`
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Launch.cs:22-38`
- **涉及方法**: `OnFilterTextChanged`, `OnSelectedSortModeChanged`, `OnSelectedDisplayModeChanged`, `OnRequireConfirmationForLaunchesChanged`
- **修复**: 每个方法添加 `_isHandling` 可重入守卫
- **验收**: 磁盘满时设置变更不再导致 StackOverflow

#### H2: 可变 List<T>/Dictionary<K,V> 破坏 record "immutable" 契约
- **文件**: `LauncherSettings.cs:30,33,36` — `CustomEntryOrder`, `CommandTemplates`, `SearchHistory`
- **文件**: `LauncherPreset.cs:19,36` — `Presets`, `EntryIds`
- **问题**: `sealed record` + `init` 但集合内部可变
- **修复方案（已确认）**: 方案 A — 引入 `System.Collections.Immutable` NuGet 包，全面切换到 `ImmutableList<T>`/`ImmutableDictionary<K,V>`。Core 层新增 1 个依赖（.NET 官方包，非第三方框架），实现真正的不可变性。
- **验收**: 所有对集合的变异必须通过 `with { }` 创建新实例

#### H3: Restore() API 签名欺骗 — 接受参数但不使用
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsSnapshot.cs:26-30`
- **修复**: 改为无参 `Restore()` 或重命名为 `CreateRestored()`/`CloneCaptured()`
- **验收**: API 语义清晰，调用方不困惑

#### H4: RenameReferences 冲突时抛出含混异常消息
- **文件**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryReferences.cs:39-51`
- **修复**: 在循环前显式检查目标 ID 是否已存在，给出明确异常消息
- **验收**: 重命名冲突时的异常消息清晰说明"无法重命名，目标ID已存在"

#### H5: 拖拽预览双集合独立修改 — 两个"真相来源"
- **文件**: `LauncherDragReorderWorkflow.cs:95` + `MainWindowViewModel.Drag.cs:44,58-80`
- **修复方案（已确认）**: 方案 A — 拖拽期间使用轻量版 FilterAndSort（不做 card size 计算），从 `_settings.CustomEntryOrder` 统一推导 `_visibleEntries`，消除双集合独立修改。
- **验收**: 筛选激活时拖拽重排后，提交的最终顺序与预览一致

#### H6: 空原谅操作符 ! 依赖跨文件隐式不变量
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs:117,131`
- **修复**: 用显式 null 检查替代 `!`，使不变量在本地可见
- **验收**: 零 `!` 操作符依赖跨类不变量

#### H7: Presets.Helpers.cs 两处 catch 无 ExceptionFilters.IsRecoverable
- **文件**: `MainWindowViewModel.Presets.Helpers.cs:49,69`
- **修复**: `catch (Exception ex)` → `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))`
- **验收**: OOM/StackOverflow 不被吞噬

#### H8: wt.exe 裸名称 — PATH 劫持风险
- **文件**: `LaunchArgumentBuilder.cs:7` — `TerminalExecutable = "wt.exe"`
- **修复**: 解析 wt.exe 完整路径（`%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe`）
- **验收**: `Process.Start` 使用完整路径，不依赖 PATH 搜索

#### H9: Drag.cs catch(Exception) 无过滤器 + 仅 Debug.Write
- **文件**: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs:48-51`
- **修复**: 添加 `ExceptionFilters.IsRecoverable` 过滤器 + 通过 StatusText 报告
- **验收**: 拖拽失败在 Release 中仍有用户可见反馈

#### H10: 全局零持久化日志
- **文件**: 全局（`Program.cs`, `App.axaml.cs`, 所有 catch 块）
- **修复**: 实现最简文件日志（`ErrorLog.Write(ex, context)`），输出到 `%LOCALAPPDATA%/WTLauncher/errors.log`
- **验收**: 所有 catch 块在设置 StatusText 的同时调用 ErrorLog.Write

#### H11: RequiresConfirmation 在批量启动路径中被忽略
- **文件**: `LauncherLaunchWorkflow.cs` + `MainWindowViewModel.Selection.cs:111-120`
- **问题**: `LaunchPlan.RequiresConfirmation` 在批量启动决策中是死数据
- **修复**: 批量路径应检查每个计划的 `RequiresConfirmation`，或统一确认逻辑在核心层执行
- **验收**: 个别条目设置 `RequiresConfirmation = true` 时即使全局关闭也能单独确认

#### H12: 7 个对话框窗口硬编码 Brushes.White 替代 LauncherDesignTokens
- **文件**: `EntryDetailsWindow.cs`, `ConfirmEntryDeleteWindow.cs`, `ConfirmBatchLaunchWindow.cs`, `ConfirmLaunchWindow.cs`, `ConfirmPresetDeleteWindow.cs`, `PresetDetailsWindow.cs`, `LauncherDialogButtons.cs`
- **修复**: 全部替换为 `{StaticResource SurfaceBrush}` 或 `Brush.Parse(LauncherDesignTokens.SurfaceColor)`
- **验收**: 修改 `LauncherDesignTokens.SurfaceColor` 后所有对话框背景同步变化

#### H13: 唯一英文状态文本 "Ready" 与全中文 UI 不一致
- **文件**: `MainWindowViewModel.cs:191` — `public static string Ready => "Ready";`
- **修复**: 改为中文 `"就绪"`
- **验收**: 新建主窗口时显示中文"就绪"

#### H14: MainWindow.axaml 中 ~22 处硬编码圆角值 — 破坏设计令牌纪律
- **文件**: `MainWindow.axaml`
- **修复**: 全部替换为 `{StaticResource CardBorderRadius}` 或对应设计令牌资源
- **验收**: 修改 `LauncherDesignTokens.CardBorderRadius` 后所有卡片同步变化

#### H15: Validate 无上限边界 — 恶意 JSON 可导致 OOM
- **文件**: `LauncherPresetFiles.cs:11-38` + `LauncherPresetIds.cs`
- **修复**: 添加合理上限（如预设 ≤1000，每预设条目 ≤10000，名称 ≤256 字符）
- **验收**: 超出上限的 JSON 抛出明确异常而非 OOM

---

### P2 — Medium (22) — 架构改进

| # | 问题 | 文件 |
|---|------|------|
| M1 | `ExceptionFilters.IsRecoverable` 过于宽泛 — 未排除 AccessViolation/SEH | `ExceptionFilters.cs:5-7` |
| M2 | `Load` 双重文件读取（已在 C7 修复） | `LauncherConfigStore.cs` |
| M3 | `null!` 类型谎言（3处） | `LauncherSettingsCopies.cs:15,18`, `LauncherSettingsSnapshot.cs:37` |
| M4 | Preset 回滚时 `restorePresets: false` — 应尝试恢复 | `LauncherEntryMutationPersistence.cs:30-39` |
| M5 | SyncVisibleEntries 触发 N 次 CollectionChanged | `MainWindowViewModel.Layout.cs` |
| M6 | DeleteSelectedPresetAsync 中冗余快照恢复 | `MainWindowViewModel.Presets.cs:161-174` |
| M7 | 幽灵坐标无窗口边界裁剪 | `LauncherDragGhostLayoutCalculator.cs` |
| M8 | Commit() 无 finally 保护 session 清理 | `LauncherDragReorderWorkflow.cs:113-133` |
| M9 | ToRequiredSet 创建双集合 | `LauncherEntryIds.cs:57-58` |
| M10 | 异常消息泄露到 UI 状态栏 | 全局多个文件 |
| M11 | GetPresetEntries 缺条目存在性验证 | `MainWindowViewModel.Presets.Helpers.cs` |
| M12 | ViewModel 依赖 Avalonia NuGet | `MainWindowViewModel.Layout.cs:1,10` |
| M13 | RequiresConfirmation 双重真相 | `LaunchPlan.cs` + `MainWindowViewModel` |
| M14 | Preset EntryIds 用 mutable List | `LauncherPreset.cs:36` |
| M15 | 时间戳 null vs 空字符串行为不一致 | `LauncherEntryVisibility.cs:184-203` |
| M16 | App 启动异常吞噬（与 C3 联动） | `App.axaml.cs` |
| M17 | async void 事件处理器风险 | `MainWindow.Drag.cs:20` |
| M18 | RequireEntrySelectionFields 对非 Object 元素静默放行 | `LauncherConfigStore.cs:136-146` |
| M19 | RotateBackups 中 File.Exists/File.Move TOCTOU | `LauncherJson.cs:70-85` |
| M20 | 预设名称无唯一性检查 | `LauncherPresetFiles.cs:11-38` |
| M21 | RequireCommandTemplates 方法名撒谎（null 不抛异常） | `LauncherSettingsStore.cs:145` |
| M22 | TrySaveWindowBounds 捕获 InvalidDataException | `LauncherSettingsStore.cs:37-43` |

### P3 — Low (25) — 代码风格和维护性

完整列表见对话历史。重点包括：
- L1: `ShouldInsertAfter` 无迟滞
- L2: Ghost 工厂每次分配完整可视树
- L5: `_activeDragEntryId` 在两个生命周期点赋值
- L7: SyncVisibleEntries 增量位置 off-by-one 风险
- L11: `ValidateSortProjection` 纯副作用可读性
- L16: `VisibleEntries` 不必要的 private set
- L18: `Brush.Parse` 在静态初始化器中
- L20: `Normalize` Unicode (FormKC) 改变 ID 视觉外观

---

## Acceptance Criteria

### 全局验收标准
- [ ] 所有 7 Critical 问题已修复并通过验证
- [ ] 所有 15 High 问题已修复并通过验证
- [ ] 所有 22 Medium 问题已修复或明确记录为"有意不修"
- [ ] 所有 25 Low 问题至少 80% 已修复
- [ ] **零测试标准降低** — 不因代码修复而删除、放松或跳过任何现有测试
- [ ] `tools/verify.ps1` (tests + build) 全部通过
- [ ] 所有现有测试继续通过（无回归）
- [ ] 无新增编译警告
- [ ] C1/C2 修复后有安全测试覆盖
- [ ] C3-C5 修复后模拟启动失败场景验证

### P0 验收标准
- [ ] `pwsh -EncodedCommand` 或模板白名单生效，恶意模板被拒绝
- [ ] `base.OnFrameworkInitializationCompleted()` 在 finally 中执行
- [ ] Release 构建中未处理异常写入 stderr + crash.log
- [ ] UI 线程异常被 Dispatcher handler 捕获
- [ ] 备份轮转在 File.Move 成功后执行
- [ ] 文件只在 Load 中被 open/read 一次

### P1 验收标准
- [ ] 4 个 OnXxxChanged 方法有可重入守卫
- [ ] 所有可变集合迁移到不可变类型
- [ ] `Restore()` API 语义清晰
- [ ] 重命名冲突异常消息清晰
- [ ] 拖拽筛选一致性（双路径统一或测试覆盖）
- [ ] 零空原谅操作符依赖跨类不变量
- [ ] 全局文件日志写入 errors.log
- [ ] `wt.exe` 使用完整路径
- [ ] 所有对话框引用 `{StaticResource}` 而非硬编码颜色
- [ ] "Ready" → "就绪"
- [ ] MainWindow.axaml 圆角全部引用 `{StaticResource CardBorderRadius}`

---

## Out of Scope

- 不做新功能开发
- 不引入新的 NuGet 依赖（除 C1/H2 可能需要 `System.Collections.Immutable`）
- 不修改 `.trellis/` 规范文件（文档漂移 M4(设计) 另开任务）
- 不重新设计架构 — 在现有架构内修复缺陷

## Notes

- 所有修复须遵循现有代码模式（snapshot/回滚、不可变 record、Func<> 委托注入对话框）
- **关键规则**: Linus 原则 — "I don't break userspace"：零破坏现有功能
- 优先级严格 P0→P1→P2→P3，P0/P1 不能跳过
- `DESIGN.md` 和 `.trellis/spec/` 中的内容可能提供额外的约束
