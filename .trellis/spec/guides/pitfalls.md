# 跨会话踩坑实录 — WT_Protector

> **来源**: 本仓库及关联项目的 50+ 个真实 session，经 `trellis mem` 取证，非臆测。
> **原则**: 每条坑有 session-id 可追溯、有根因、有预防规则。不做"谨慎一点"这种废话约束。

---

## 目录

1. [工具层坑 — Claude Code / Codex 通用](#1-工具层坑)
2. [WT_Protector 项目坑](#2-wt_protector-项目坑)
3. [关联项目可迁移教训](#3-关联项目可迁移教训)
4. [元教训 — 跨项目反复出现的模式](#4-元教训)

---

## 1. 工具层坑

**直接影响开发效率的坑，每次 WT_Protector 开发都会遇到。**

### 1.1 Windows CRLF — Edit 失败的根源

| 属性 | 值 |
|---|---|
| **频次** | 9+ 次 `String to replace not found` |
| **证据** | phoenix-crawler session `0c49eee5-6fa`, `645a9dfe-21f` |
| **根因** | Windows 文件实际存储 CRLF (`\r\n`)，但 `Read` 工具展示 LF (`\n`)，Edit 匹配时换行符不一致 |
| **预防** | **Edit 失败一次就立即切 Write 重写整个文件**，不重试。不调试换行符。 |

```text
# 铁律
Edit "String to replace not found" → 不修参数，直接 Write 全文件。
原因：99% 是 CRLF vs LF，重试浪费 token 且引入中间态。
```

### 1.2 `Read.pages: ""` 空字符串报错

| 属性 | 值 |
|---|---|
| **频次** | 15+ 次 `Invalid pages parameter` |
| **证据** | phoenix-crawler session `45380b66-51b`, `ee372f15-393` |
| **根因** | `pages` 参数仅用于 PDF 页码范围，传空字符串 `""` 在 schema 层就死，hook 拦不住 |
| **预防** | **非 PDF 不传 `pages`**。传了任何值就必死。 |

### 1.3 并行工具调用超过 ~5-6 个

| 属性 | 值 |
|---|---|
| **频次** | 多次 rate-limit / 部分失败 |
| **根因** | 单轮并行工具调用有上限，超限触发限流或部分返回 null |
| **预防** | 大扇出用 `Workflow` 的 `parallel()`，不用裸并行 `Agent`/`Bash` 调用。Workflow 内部自带并发控制。 |

### 1.4 `codebase-memory-mcp` — project 名写错导致全部超时

| 属性 | 值 |
|---|---|
| **频次** | 3+ 次 |
| **证据** | temp_codex session `019eab3f-2d8` |
| **根因** | 真实索引名是 `D-_AI_-_projects-xxx` 格式（路径中特殊字符转义），不是人类友好的短名 |
| **预防** | **永远先用 `list_projects` 取精确 project 名**，不猜。 |
| **错误提示识别** | 如果 3 个 MCP 工具全部 120s 超时 → 第一个怀疑 project 名，不是 DB 坏、不是索引坏、不是网络。 |

### 1.5 PowerShell 串联用 `;` 不是 `&&`

| 属性 | 值 |
|---|---|
| **频次** | 多次 |
| **证据** | poollink session `019e3c29-0e0` |
| **根因** | Windows PowerShell 5.x 不支持 `&&`（那是 Bash 语法） |
| **预防** | PowerShell 命令串联用 `;`。跨平台脚本用 `pwsh`，不是 `powershell`。 |

### 1.6 Deferred tools 直接调用报 `InputValidationError`

| 属性 | 值 |
|---|---|
| **根因** | Deferred tool 的 schema 未加载，必须先 `ToolSearch` 加载再调用 |
| **预防** | 凡是 `<system-reminder>` 提示 deferred tools 可用 → 用之前先 `ToolSearch(query="select:ToolName")` |

### 1.7 Trellis Codex `SessionStart` 脚本存在不等于已接线

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-13 任务 `07-13-codex-trellis-setup`；Trellis 0.6.6 生成 `.codex/hooks/session-start.py`，但默认 `.codex/hooks.json` 仅注册 `UserPromptSubmit` |
| **症状** | `trellis update --dry-run` 显示最新、脚本手工运行正常，但新 Codex 会话没有收到 Trellis SessionStart 概览 |
| **根因** | 生成文件存在性被误当成平台 settings 已注册；Codex 只执行 `hooks.json` 或同层 `config.toml` 中实际接线的事件 |
| **预防** | Codex/Trellis 安装验收必须同时检查：(1) CLI/模板版本；(2) 项目 trusted；(3) 用户级 `features.hooks=true`；(4) `.codex/hooks.json` 同时注册 `SessionStart` 与 `UserPromptSubmit`；(5) 两个脚本手工返回匹配的 `hookEventName`；(6) 新 hook 通过交互 `/hooks` 信任 |
| **更新注意** | `.codex/hooks.json` 是 intentional local customization。未来运行 `trellis update` 时审查并合并，不用 `--force` 覆盖 |
| **详细契约** | `docs/trellis-codex-setup-audit.md` |

---

## 2. WT_Protector 项目坑

### 2.1 Entry ID / Preset ID 绕过 Store 直接写入

| 属性 | 值 |
|---|---|
| **证据** | Codex session `019e79ae-773` (2026-05-30) |
| **根因** | ViewModel / Workflow 中散落了绕过 `LauncherStore` / `LauncherStore` / `LauncherStore` 的写入路径 |
| **后果** | 绕过 Store 的写入不经过验证/原子化/Normalize，数据一致性被破坏 |
| **预防** | 所有 JSON 写入必须走 Store 层。不可以 ViewModel 里直接 `File.WriteAllText`。 |

### 2.2 dirty JSON 规范化不一致

| 属性 | 值 |
|---|---|
| **证据** | 同 2.1，session `019e79ae-773` |
| **根因** | 多个写入点对 JSON 序列化/规范化处理不一致（缩进、key 排序、`null` vs 缺失字段、`""` vs `null`） |
| **预防** | JSON 规范化逻辑集中在 `LauncherJson` 一个地方，不在各 ViewModel 里各写一套。 |

### 2.3 KISS 合并后 catch 过宽

| 属性 | 值 |
|---|---|
| **证据** | commit `a8624d2`, `86d75c2` |
| **根因** | `TrySaveWindowBounds` 原先 catch `Exception`，裸 catch 吞掉了不应出现的异常 |
| **修复** | 窄化为 `IOException | UnauthorizedAccessException | SecurityException` |
| **预防** | **永远不写 `catch (Exception)`** — catch 你预期到的、能处理的类型。未知异常应该炸出来，不是被吞。 |

### 2.4 硬编码常量与 DesignTokens 漂移

| 属性 | 值 |
|---|---|
| **证据** | `guides/index.md` 已有记录 |
| **根因** | XAML 中写死 `#2563EB`，当 `LauncherDesignTokens.PrimaryColor` 变更时不会同步 |
| **预防** | XAML 只用 `{StaticResource PrimaryBrush}`。`grep -r` 搜索硬编码颜色/数值后再新增常量。 |

### 2.5 Scheme B 实施踩坑 (2026-07-23)

#### 2.5.1 `DirectoryNotFoundException` 在 `or` 模式中冗余于 `IOException`

| 属性 | 值 |
|---|---|
| **证据** | Phase 2 实施，`.trellis/spec/guides/index.md §5` |
| **根因** | `DirectoryNotFoundException` 继承自 `IOException`。`catch when (ex is IOException or ... or DirectoryNotFoundException)` 中 `DirectoryNotFoundException` 永远无法匹配 |
| **预防** | `catch when` 里 `IOException` 是最后一层就够了。不需要 `IOException or ... or DirectoryNotFoundException` — 去掉 `or DirectoryNotFoundException`。 |

#### 2.5.2 删除 AppStore 外观方法时测试代码是最大风险

| 属性 | 值 |
|---|---|
| **证据** | Phase 2, `AppStore.Mutate` → `Dispatch` 迁移 |
| **根因** | 删除 `AppStore.Mutate` 后，8 处测试代码和 4 处生产代码同时报错。测试代码常被 grep 漏掉 |
| **预防** | 删除非私有方法 → 先 `grep -rn "\.MethodName\("` 全仓库（含 tests）确认所有调用点。一起修改，一起验证。 |

#### 2.5.3 CancellationToken 参数传播需要全链修改

| 属性 | 值 |
|---|---|
| **证据** | Phase 2, `AgentDiscoveryScanner.Scan` + `CancellationToken` 参数 |
| **根因** | 加 `CancellationToken ct = default` 可选参数 → xUnit1051 分析器要求测试传递 `TestContext.Current.CancellationToken` → 委托签名变化需要同时改调用方和所有测试工厂方法 |
| **预防** | 给 public 方法加可选参数时做三件事：(1) 检查 xUnit 分析器效果；(2) 更新所有 `Func<>` 委托签名；(3) 测试工厂方法 + 所有调用点一起改，一起验证。 |

#### 2.5.4 AOT 安全的 JsonSerializer 重载选择

| 属性 | 值 |
|---|---|
| **证据** | Phase 1, `LauncherJson` 序列化修复, IL2026/IL3050 |
| **根因** | `JsonSerializer.Deserialize<T>(stream, options)` 和 `Serialize(value, typeof(T), options)` 都有 `[RequiresUnreferencedCode]` 和 `[RequiresDynamicCode]` |
| **正确做法** | AOT 安全：`JsonSerializer.Deserialize(stream, jsonTypeInfo)` 和 `Serialize(value, jsonTypeInfo)`。需要从 `LauncherJsonContext.Default.GetTypeInfo(typeof(T))` 获取 `JsonTypeInfo<T>` |
| **预防** | 任何新 `LauncherJson.Load<T>` / `Save<T>` 必须使用 `JsonTypeInfo<T>` 重载。不传 `JsonSerializerOptions`。新类型必须加 `[JsonSerializable]` 到 `LauncherJsonContext`。 |

### 2.5.5 StoreGrain Dispatch 后必须 SetState

| 属性 | 值 |
|---|---|
| **证据** | 07-24-vtuner-killer-impl session, AppStore→StoreGrain 迁移 (2026-07-24) |
| **根因** | `StoreGrain.Dispatch` 将 reducer 结果写入磁盘（`LauncherStore.Save`）但未写入 `Grain<T>` 基类的私有 `_state` 字段。后续 `Select` 读到的是旧状态，造成 35 个测试失败。 |
| **正确做法** | `Dispatch` 中 `SetState(after)` 必须在 `LauncherStore.Save` 之后调用，确保内存和磁盘一致。 |
| **预防** | 所有 Grain 子类的 `Dispatch`/`DispatchErrorOr` legacy-compat 方法末尾必须调用 `SetState(after)`。 |

### 2.5.6 `async void` 事件处理必须用 ExceptionFilters 保护

| 属性 | 值 |
|---|---|
| **证据** | 07-24-vtuner-killer-impl session, 4 个 dialog 文件共 11 处 (2026-07-24) |
| **根因** | Avalonia 对话框使用 `async void` 作为事件签名（Click、DoubleTapped 等），抛出未处理异常会崩溃进程。依赖 `Dispatcher.UIThread.UnhandledException` 不够可靠。 |
| **模板** | 
```csharp
private async void OnXxxClicked(object? sender, RoutedEventArgs e)
{
    try { await DoSomethingAsync(); }
    catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
    {
        ErrorLog.Write(ex, "XxxContext");
    }
}
``` |
| **预防** | 所有 `async void` 事件处理必须包裹 `try-catch(ExceptionFilters.IsRecoverable)` 并记录 ErrorLog。 |

### 2.6 OLE DragDrop 嵌套消息循环是性能杀手

| 属性 | 值 |
|---|---|
| **证据** | session `5d6ef926-604a` (2026-06-26), 6 轮优化追踪 |
| **根因** | `DragDrop.DoDragDropAsync` 启动 Win32 OLE 嵌套消息循环，优先处理输入消息（`WM_MOUSEMOVE`），渲染回调（`WM_TIMER`/`WM_PAINT`）被降级，累积为可见拖拽卡顿。隐藏全部 450+ 个可视元素后性能零改善——**证明了瓶颈不在应用层渲染，在消息循环**。 |
| **修复** | 用 `Pointer.Capture` + `PointerMoved/Released/CaptureLost` 替代 OLE drag-drop。Pointer 事件在 Avalonia 正常渲染循环中触发，无嵌套消息循环。 |
| **预防** | 应用内 reorder/拖拽 → 永远用 Pointer capture，不用 OLE。OLE 仅用于跨应用数据传输。Pointer capture 必须配 `ForceCleanup()` 模式——`PointerReleased` 和 `PointerCaptureLost` 双重路径释放 capture，所有 handler 加 try-catch 兜底。 |

### 2.6 性能排查方法论：先消除，后优化

| 属性 | 值 |
|---|---|
| **证据** | 同上 session，4 轮"优化"（eliminate layout/alloc/walk）无效后才做"消除测试"（隐藏 CardsArea） |
| **根因** | 认知偏差——默认瓶颈在"可见的代码开销"（layout、分配、树遍历），而非"不可见的系统行为"（消息循环） |
| **预防** | 遇到性能问题，**第 1 轮就做消除测试**：（1）隐藏/移除目标组件 → 测性能变化 → 确定组件是否是瓶颈 → 再深入优化。不做"看起来很慢所以优化它"。 |
| **诊断顺序** | 消除测试 → 定位瓶颈层 → 分析该层特有开销 → 针对性修复。跳过消除测试直接优化 = 在错误的维度上做正确的事。 |

### 2.7 [已反转] LiteDbStore 非原子写入=Crash 即丢数据

| 属性 | 值 |
|---|---|
| **状态** | LiteDB 已于 2026-07-09 移除（归档任务 `07-09-remove-litedb-deadcode`）。此坑保留供历史参考。 |
| **证据** | UltraReview 2026-07-05，LiteDbStore.cs:62-63 |
| **根因** | `DeleteAll()` + `Insert()` 是两个独立操作，中间无事务保护。进程在两者间崩溃 = 数据永久丢失。 |
| **当时修复** | 用 `collection.Upsert(new StoreDocument { Id = 1, ... })` 替代 DeleteAll+Insert。LiteDB 支持事务（`BeginTrans`/`Commit`）。 |
| **预防** | 所有数据库/文件写入必须原子化：JSON 用 `File.Replace`（NTFS 原子）。绝不假设两步操作不会中断。当前架构无 LiteDB，此规则适用于所有未来引入的持久化路径。 |

### 2.8 AutoCompleteBox Enter 键检测—e.Source 指向内部 TextBox

| 属性 | 值 |
|---|---|
| **证据** | UltraReview 2026-07-05，MainWindow.axaml.cs:178 |
| **根因** | `ReferenceEquals(e.Source, SearchTextBox)` — AutoCompleteBox 获得焦点时，实际接收键盘事件的元素是其内部的 TextBox，`e.Source` 是 TextBox 不是 AutoCompleteBox，`ReferenceEquals` 永远 false。 |
| **修复** | 移除 `ReferenceEquals` 检查，仅保留 `!IsDropDownOpen` 防护。 |
| **预防** | Avalonia 复合控件（AutoCompleteBox、ComboBox 等）的键盘事件 `e.Source` 指向内部焦点元素，不是控件本身。不要用 `ReferenceEquals` 匹配复合组件。 |

### 2.9 架构变更必须包含迁移计划

| 属性 | 值 |
|---|---|
| **证据** | UltraReview 2026-07-05，F06 — 三文件→统一 store.json 无迁移 |
| **根因** | 从三文件分离架构（config.json+presets.json+schema.json）切换到统一 LauncherStoreFile 时，没有任何代码处理现有用户的旧配置。启动后丢弃旧数据。 |
| **修复** | 在 `LauncherStore.Load` 中添加 `MigrateFromLegacyFiles`：检测旧文件 → 合并 → 写 store.json → 旧文件 .bak。 |
| **预防** | 任何存储格式变更的 PRD 必须包含迁移步骤。迁移是功能的一部分，不是可选项。`implement.md` 中迁移必须排在第一批。 |

### 2.10 文档同步是代码变更的必要组成部分

| 属性 | 值 |
|---|---|
| **证据** | UltraReview 2026-07-05，D01-D10 — 10 项文档与代码脱节 |
| **根因** | 代码从三文件→统一 store、从旧 OLE→Pointer capture、从无 searchHistory→有 AutoCompleteBox，文档（README、spec、审计文件）全部停留在旧状态。 |
| **修复** | Batch 0 同步了 README、3 份 spec、2 份审计、design-benchmark。 |
| **预防** | **文档修改是代码变更的 AC 的一部分**。implement.md 的每个 batch 必须包含"是否需要更新文档"检查。gate 脚本应检测 README/spec 的关键条目。 |

### 2.11 IStore 接口实现必须验证参数

| 属性 | 值 |
|---|---|
| **证据** | UltraReview 2026-07-05，F01 — LiteDbStore 忽略 storePath |
| **根因** | `IStore` 接口要求 `Load(string storePath)` 使用传入路径，但 `LiteDbStore` 在构造时固定了数据库连接，Load/Save 方法丢弃 storePath 参数静默读写固定数据库。 |
| **修复** | 添加 `_dbPath` 字段，`Load`/`Save` 中验证 `Path.GetFullPath(storePath) == _dbPath`。 |
| **预防** | 实现接口时，必须调用/验证所有参数。接口契约不是建议，是实现者的义务。特别留意"构造函数固定了实现参数而接口参数被丢弃"的模式。 |

### 2.12 单元测试必须覆盖加载失败路径

| 属性 | 值 |
|---|---|
| **证据** | UltraReview 2026-07-05，F13 — 60+ 测试文件无加载失败测试 |
| **根因** | 整个测试套件假设 JSON 文件始终有效。损坏 JSON、空文件、文件锁定、50MB 限制等加载失败场景无任何测试覆盖。 |
| **修复** | 新增 `LauncherStoreLoadFailureTests.cs`（6 个测试：corrupt JSON、empty file、backup recovery、file locked、50MB limit）。 |
| **预防** | 持久化组件的测试必须覆盖**成功+失败**两个路径。只测 happy path = 没有测试防御能力。 |

### 2.13 跳过 ApplyFilter 必须验证所有派生投影已重建

| 属性 | 值 |
|---|---|
| **证据** | FMA session 2026-07-12, `MainWindowViewModel.Drag.cs` — 卡片视图拖拽修复 |
| **根因** | `CommitDragReorder()` 在保存成功后返回 `RefreshEntries=false`，跳过 `ApplyFilter()`。卡片视图绑定 `VisibleGroups`（由 `ApplyFilter()` → `RebuildVisibleGroups()` 构建），从未被重建。详情/名称视图直接绑定 `VisibleEntries`（拖拽预览时已通过 `ReorderVisibleEntryForDrag` 更新），没受影响。 |
| **修复** | `CommitDragReorder()` 无条件调用 `ApplyFilter()`，移除 `if (result.RefreshEntries)` 条件。 |
| **预防** | 任何"跳过 ApplyFilter"的优化（`RefreshEntries=false`、`isDragging` 早退等）必须验证所有派生投影：`VisibleGroups`、`VisibleCount`、`SelectVisibleCommand`、`IsVisibleEmpty`。只检查 `_visibleEntries` 是不够的——卡片视图还依赖 `VisibleGroups`。 |
| **适用** | 任何 `ApplyFilter()` 的优化/条件跳过。 |

### 2.14 `[ObservableProperty]` 不能有后备字段访问的转发方法

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-21 MWVM Stage 5 会话，`MVVMTK0034` 编译错误 |
| **根因** | CommunityToolkit.Mvvm 的 `[ObservableProperty]` 分析器禁止在包含类内直接访问 `_field` 后备字段（MVVMTK0034），因为源代码生成器接管了字段的所有权。任何"绕过"生成属性直接设置后备字段的方法都会触发此错误。 |
| **修复** | 删除转发方法，改为在构造器中将桥接委托 `SettingsPersister` 赋值前设置生成属性，利用 `if (SettingsPersister is null) return;` 守卫跳过持久化。 |
| **预防** | 需要初始值但不触发副作用的正确顺序：(1) new 协作类 → (2) 设置初始属性值（此时桥接委托为 null，`OnXxxChanged` 中的 null 守卫返回）→ (3) 赋值桥接委托 → (4) 订阅 PropertyChanged 事件。切勿创建 `SetXxxDirect(bool value) { _field = value; }` 方法。 |
| **适用** | 所有使用 `[ObservableProperty]` 的 ObservableObject 类。 |

---

### 3.1 不要在文档里硬编码"N 条/N 类"

| 属性 | 值 |
|---|---|
| **证据** | poolswitch session `019e7521-316` |
| **问题** | `AGENTS.md` 写"13 条踩坑经验"，`project-mastery-coverage.md` 写"55 类漂移" |
| **后果** | 每次增删都要手动改数字，必漂移 |
| **修复** | 改为"持续更新的真实踩坑经验"，新增 `check_mastery_map.py` 防漂移 gate (`PITFALL_COUNT_PATTERN` 正则检测) |
| **WT_Protector 适用** | `.trellis/spec/` 和 `AGENTS.md` 里的条目数量绝对不能硬编码。写"持续更新"不写数字。 |

### 3.2 认证错误不是网络参数错误

| 属性 | 值 |
|---|---|
| **证据** | clashverge session `019eaba8-f28` |
| **问题** | `codex exec` 失败 → 调 retry/cooling/affinity 参数 → 仍然失败 |
| **根因** | 两个系统的 `OPENAI_API_KEY` 不一致。retry 99 次也只是更快制造垃圾流量 |
| **教训** | **先验证认证层**（直接打 API 测 `401`/`200`）→ 再调网络参数。倒过来就是在正确的链路上调错误的问题。 |
| **WT_Protector 适用** | 任何涉及外部 API/服务调用的调试，第一步永远是认证验证（HTTP status check），不是参数调优。 |

### 3.3 同一个坑三次 = 不是技术问题，是流程/目标缺失

| 属性 | 值 |
|---|---|
| **证据** | poollink session `019e7c6c-c69` |
| **问题** | 三次不同 session 都在 `push`，每次本地 commit 完成、gate 全绿，然后发现没有 `remote/upstream` |
| **教训** | **该标 blocked 就标 blocked**。无远端目标时不要假装能推进。Blocked 是正确状态，不是失败。 |
| **WT_Protector 适用** | 当某个条件（外部依赖、用户授权、远端）反复阻挡进度时，不要绕过，不要自己编。标 blocked，等输入。 |

### 3.4 大规模机械 edit 容易引入连锁编译错误

| 属性 | 值 |
|---|---|
| **证据** | phoenix-crawler session `2ec9bb38-226` |
| **问题** | `db_conn` → `tokio::sync::Mutex` 的大规模 edit，引入多个编译错误 |
| **教训** | 机械替换后必须立即 `cargo check` / `dotnet build`，不积累多步再检查 |
| **WT_Protector 适用** | 批量 rename/重构 → 立即 `dotnet build`。不准攒 10 个改动再一次性修。 |

### 3.5 编辑联动失误 — 删除函数体+引用分两步

| 属性 | 值 |
|---|---|
| **证据** | phoenix-crawler session `7b98e04d-a30` |
| **问题** | 删了函数体，忘了删 exports 引用，中间态 `missing: missing` 引用已删除变量 |
| **教训** | 涉及函数定义+引用点的联动删除 → **一次 Edit 同时完成**。不准分两次。 |
| **WT_Protector 适用** | 删除 C# 类/方法 + 更新所有 `using` / 引用 → 一枪打。分步删必留僵尸引用。 |

### 3.6 Feature-gate stub 签名不匹配

| 属性 | 值 |
|---|---|
| **证据** | phoenix-crawler session `30f0127e-83b` |
| **问题** | Feature-gate 的 stub 每个 public method 签名必须和原模块一致 |
| **教训** | `dotnet build` 是最低门禁。feature-gate 切换不是只改一个 `mod.rs`/`using`。 |
| **WT_Protector 适用** | 任何条件编译/DI 切换，compile check 立刻跑，不准"应该没问题"。 |

### 3.7 `scripts/` 或 `tools/` 目录被第三方依赖污染

| 属性 | 值 |
|---|---|
| **证据** | zero_to_first_revenue session `019e5e0b-c89` |
| **问题** | `Get-ChildItem -Recurse -Include *.js` 把 `node_modules` 下全部第三方 JS 也复制进 `scripts/session_tools/` |
| **教训** | 目录迁移/复制前先 `--exclude` `node_modules`/`.git`/`__pycache__`。事后清理比事前过滤贵 10 倍。 |
| **WT_Protector 适用** | `tools/`、`scripts/` 目录永远只放自写工具。验证命令：`git ls-files tools/ scripts/` 看着不对就清。 |

### 3.8 UTF-8 BOM 被意外去掉

| 属性 | 值 |
|---|---|
| **证据** | poollink session `019e6ad8-80e` |
| **问题** | `tools/release_verify_core.ps1` 首行 BOM 被去掉：`-﻿param(` → `+param(` |
| **教训** | PowerShell 脚本需要 UTF-8 BOM（Windows 兼容）。`git diff` 看到首行字符级变化时要警惕。 |
| **WT_Protector 适用** | `verify.ps1` 和相关 PowerShell 脚本，commit 前检查 BOM 完整性。 |

### 3.9 `grep -c "Add-Check"` 把函数定义也算进去 — 计数 off-by-one

| 属性 | 值 |
|---|---|
| **证据** | wt-protector session `c38ce17d-7e8`（2026-07-09 reorg-audit-pass） |
| **问题** | 审计用 `grep -c "Add-Check" tools/verify.ps1` 得到 19，实际是 18 —— 第 12 行 `function Add-Check([string]$Name, [object]$Detail) {` 也被匹配，多算 1。审计报告把这个错误数字写进 D1 第 4 项，落地阶段又把它写进 `AGENTS.md:39` 和 `README.md:138`，引入新的文档漂移。 |
| **后果** | 文档与 `verify.ps1` receipt 实际发出 18 项检查矛盾；`AGENTS.md:39` 的 "19 项" 与同行的 18-item 括号枚举内部不一致。第一轮对抗性验证抓出，订正为 18。 |
| **预防** | 计数函数调用时，**永远用锚定正则排除函数定义**：`grep -cE "^\s*Add-Check\s+['\"]"`（要求调用形式以引号开头）。更稳健的做法：**直接解析 receipt 的 `checks` 数组长度**，不依赖 grep。 |
| **WT_Protector 适用** | 任何"统计 N 个调用/N 项检查/N 条规则"的审计或文档动作，先用最严格的正则，或直接读权威 receipt。审计方法学本身也要被独立对抗性验证。 |

### 3.10 PRD 引用 spec 文件前必须验证存在性 — phantom reference

| 属性 | 值 |
|---|---|
| **证据** | wt-protector session `c38ce17d-7e8`（2026-07-09 reorg-org） |
| **问题** | 8 处 PRD/spec 引用 `.trellis/spec/guides/feedback_audit_driven_proposals_verify_each_source.md` 作为 audit-driven-verify 准则的来源，但**该文件从未存在**（Glob 全仓零匹配）。引用传播到父任务 PRD、子任务 PRD/design/implement、4 份归档 PRD。 |
| **后果** | 未来读者按引用路径找不到文件，浪费排查时间；audit-driven-verify 准则本身被"幻影引用"反噬——一个要求"验证每条提案"的准则，自己的引用都没验证。 |
| **修复** | 删除 8 处 phantom 引用，改为通用表述"严守审计准则：每条提案都是假设，必须用当前代码/文档验证后才决定动作"。**不补建文件**——补建等于为了 8 处引用造一份新文档，反而增加维护面。 |
| **预防** | 写"严守某文件：某准则"之前，**先用 Glob 或 `ls` 验证文件存在**。更稳健的做法：**把准则内容直接内联**在 PRD 里，不引用外部文件——单一权威源，不会漂移。 |
| **WT_Protector 适用** | PRD / spec / docs 之间的引用必须可解析。引用前 Glob 验证；删除文件时 grep 反向引用确认无残留。 |

### 3.11 .NET 10 NuGet audit 通过 MSBuild 属性触发，不是 `dotnet restore --audit`

| 属性 | 值 |
|---|---|
| **证据** | wt-protector session `52e06929-0c1e`（2026-07-10 modernization-wave P3） |
| **问题** | `verify.yml` 的 Audit 步骤用了 `dotnet restore --audit`（+ `continue-on-error: true`），CI 报错"unknown option `--audit`"。这是 .NET 9 早期文档遗留的假设——**.NET 10 SDK 没有 `--audit` CLI 标志**。Audit 通过 `Directory.Build.props` 的 `<NuGetAudit>true</NuGetAudit>` + `<NuGetAuditMode>all</NuGetAuditMode>` MSBuild 属性在 restore/build 时透明运行，警告作为 NU30xx 系列。 |
| **后果** | 带有 `continue-on-error: true` 的失效步骤会**伪装成通过**——CI 绿但 audit 从未运行；移除 continue-on-error 后立刻暴露标志不存在。 |
| **修复** | 删除 `--audit` 标志，audit 由 `Directory.Build.props` 的两个属性负责；步骤改为普通 `dotnet restore`，warning-as-error 在 build 阶段捕获 NU30xx。 |
| **预防** | 升级 .NET SDK 时，**先验证 CLI 标志存在性**（`dotnet restore --help | grep audit`）再写进 CI。NuGet audit 的权威接口是 MSBuild 属性，不是 CLI。 |
| **WT_Protector 适用** | `tools/verify.ps1` 和 `.github/workflows/verify.yml` 的 audit 触发点必须是 MSBuild 属性；任何"加 `--audit` 标志"的提议都要拒绝。 |

### 3.12 xunit.v3 的 xUnit1051 = CancellationToken 传播，不是 IDisposable/IClassFixture

| 属性 | 值 |
|---|---|
| **证据** | wt-protector session `52e06929-0c1e`（2026-07-10 modernization-wave，依赖升级触发分析器） |
| **问题** | 5 处测试方法收到 xUnit1051（"Missing CancellationToken"）。第一反应假设是 IDisposable/IClassFixture 生命周期规则——**错误**。xunit.v3 把 xUnit1051 重定义为：异步测试方法应接受 `CancellationToken` 以响应 `TestContext.Current.CancellationToken`（测试取消/超时传播）。xunit.v2 没有这条规则，迁移代码或文档引用 v2 语义会走弯路。 |
| **后果** | 按错误假设改测试类签名（加 IDisposable/IClassFixture）会让 xUnit1051 继续报错，且引入不必要的测试 fixture 复杂度。 |
| **修复** | 测试方法签名加 `CancellationToken` 参数，调用传入 `TestContext.Current.CancellationToken`。xunit.v3 在测试取消时通过该 token 协作式中断。 |
| **预防** | 遇到 xUnit1051，**直接查 xunit.v3 文档**而不是套用 v2 经验。v3 的分析器规则集与 v2 不完全兼容。 |
| **WT_Protector 适用** | `WTLauncher.Tests` 全部测试方法的异步签名必须接受 `TestContext.Current.CancellationToken`；未来任何"v2 经验"都要先验证 v3 语义。 |

### 3.13 快照恢复空桩导致回滚测试"假绿"

| 属性 | 值 |
|---|---|
| **证据** | wt-protector session `289f3860`（2026-07-19 architecture-migration P5） |
| **问题** | `MainWindowViewModel.RestoreShellSnapshot` 原本是 `static` 空桩，方法体为空。测试 `AddEntryRollsBackInMemoryStateWhenConfigSaveFails` 触发存盘失败时调用此方法，但空桩什么都不做，导致 `Settings.CustomEntryOrder` 保留已添加的 entry ID 未回滚。测试在 `_settings` 行失败，但失败信息不指向空桩本身——看起来像是回滚逻辑或字段不同步的问题。 |
| **后果** | 测试"假绿"（失败的测试因其他原因被误判）→ 回滚路径没有覆盖到。如果空桩未被发现，后续所有基于 `ShellSnapshot` 的回滚都不可靠。 |
| **修复** | `RestoreShellSnapshot` 改为实例方法，正确恢复 `_settings`、`_presetFile`、`TotalCount`。 |
| **预防** | 快照恢复方法必须有回归测试证明其真实执行回滚。如果恢复方法看起来像空桩（空方法体或 `static` 无副作用签名），在 code review 中被标记为可疑。对 `Bridge` 中暴露的 `RestoreSnapshot` 回调，写一个测试在 save-failure 后验证 `_settings.CustomEntryOrder` 和 `_presetFile` 回滚到快照值。 |
| **WT_Protector 适用** | `MainWindowViewModel` 中所有 `Restore*` 方法和 ELVM `EntryListBridge` 的 `RestoreSnapshot` 回调必须有对应回归测试。空桩 stub = 未完成，不是因为"暂时不实现"就安全。 |

---

## 4. 元教训

**跨项目、跨语言、跨平台反复出现的模式。不是针对某个文件，是针对你的决策习惯。**

### M1. 基于模式的阻断会被绕过；基于能力的授权不会

| 来源 | Figma Plugin Security (phoenix-crawler session `b006771a-85c`) |
|---|---|
| **问题** | 用 regex 匹配"危险 API 调用" → 大量假阳性 → 开发者用 `// @ts-ignore` 绕过 |
| **修复** | 改为 capability manifest → 假阳性归零 |
| **适用** | 验证/检查/门禁应该检查**能力有没有**，而不是**用法像不像坏**。 |

### M2. 不要削弱 gate 换速度

| 来源 | Poollink 项目偏好 |
|---|---|
| **核心** | fresh native receipt 才能声明当前 PASS。不许用旧日志冒充当前验证。 |
| **适用** | 每次声称"通过了"，背后必须有刚刚跑过的命令输出。 |

### M3. "持久化 > 记忆"

| 来源 | Trellis 核心原则 |
|---|---|
| **核心** | Conversations get compacted, files don't。所有决策、教训、约定必须落盘。 |
| **适用** | 每次踩坑 → 立即更新 spec，不靠"下次记住"。本文档就是这条原则的产物。 |

### M4. 验证的诊断顺序：认证 → 连通 → 参数 → 性能

| 来源 | clashverge CPA 认证坑 |
|---|---|
| **错误顺序** | 连通失败 → 调 retry/cooling → 仍然失败 |
| **正确顺序** | 先 curl API 验证 `200` vs `401` → 确认认证正确 → 再调性能参数 |
| **适用** | 任何网络/服务问题调试的标准顺序。 |

### M5. "未读=先读"是反直觉但必守的规则

| 来源 | CLAUDE.md 实战规则 |
|---|---|
| **核心** | `Edit` 之前没有在当前 session 中 `Read` 过那个文件 → 必须先 Read。无例外。 |
| **适用** | 所有代码修改操作。 |

### M6. 不拿旧 PASS 冒充当前验证

| 来源 | Poollink 交接 session |
|---|---|
| **核心** | 汇报时说"当前状态 PASS"，证据必须是刚跑的命令输出。 |
| **适用** | 所有 gate/验证声明。 |

### M7. 简单 > 聪明

| 来源 | Linus Torvalds 编码哲学，全项目适用 |
|---|---|
| **核心** | `if-else` 够用就不引入设计模式。直白逻辑 > 抽象魔法。 |
| **适用** | 每个设计决策。 |

### M8. 快照必须在 await 之后 — 并发窗口期是数据丢失的温床

| 来源 | 2026-06-23 ultrareview B6/B10/B11 |
|---|---|
| **问题** | `DeleteEntryAsync` 在 `await` 对话框前捕获快照 → 对话框打开期间用户可执行其他操作（如添加条目） → 保存失败回滚时丢失中间操作的数据 |
| **修复** | 快照移到 `await` 对话框确认**之后**。只有确认要修改时才捕获。 |
| **适用** | 任何 `async` + 快照回滚模式的方法。规则：**先 await 确认，再 capture 快照**。 |

### M9. 布尔标志位参数组合爆炸 → enum 消除

| 来源 | 2026-06-23 ultrareview Q2 |
|---|---|
| **问题** | `CompareEntries(useRecent, useUpdated, useAlphabetical, useCustom)` — 4 个 bool 允许 16 种组合，其中 12 种非法。编译器不报错。 |
| **修复** | 单 enum `LauncherSortMode { Recent, Updated, Alphabetical, Custom }` — 4 种合法状态，编译器强制穷尽。 |
| **适用** | 任何 ≥2 个互斥 bool 参数的场景。 |

### M11. UX 缺陷和正确性缺陷同等严重

| 来源 | 2026-06-23 polaris F13b/E8 |
|---|---|
| **问题** | 视图切换丢失滚动位置被标记为"低优不影响功能"；预演失败跳过确认被标记为"UX 未定"。两者都是用户可感知的功能缺陷。 |
| **修复** | F13b: ViewModel `_scrollOffsets` dict 保持每视图滚动状态，切换时存取。E8: 移除 `FailedCount>0` 早期跳过，强制展示确认+失败详情。 |
| **适用** | 审计发现 UX 缺陷时，不要在严重级别上打折。"用户能忍"不是拒修理由——如果 VS Code / Docker 解决了，我们就该解决。 |

### M10. 收集数据但不消费 = 纯浪费（searchHistory 教训）

| 来源 | 2026-06-23 ultrareview E1/Q6 |
|---|---|
| **问题** | `searchHistory` 初始实现每次按键触发完整 JSON 序列化写入，且无 UI 消费端。 |
| **修复** | 将收集时机从每次按键改为仅按 Enter 键时写入，并补齐 AutoCompleteBox 下拉消费端。数据模型和验证保留（向后兼容）。 |
| **适用** | 每添加一个数据收集路径，必须同时有对应的消费路径。高频写入应降为有意义的触发点（如回车），而非每次输入事件。 |

### M12. Spec 不变量必须有 gate 强制层，否则会漂移

| 来源 | 2026-07-10 任务 `07-10-discovery-scan-narrow-catch`（提交 `266aa6f`） |
|---|---|
| **问题** | `.trellis/spec/guides/index.md` §5 "Never `catch (Exception)` — Narrow It" 不变量已存在多时，但 `4c67183 feat(discovery)` 任务合入时仍在 `DiscoverAgentsViewModel.ScanAsync:70` 引入了裸 `catch (Exception)`。spec 写了规则，gate 没有强制执行 → 新代码能违反不变量合入。 |
| **修复** | (1) 修源码（narrow 化为 `when (ExceptionFilters.IsRecoverable(ex))` + `ErrorLog.Write`）；(2) 在 `tools/verify.ps1` 加 `no-bare-catch-exception` 检查，扫 `src/avalonia-launcher/src/**/*.cs` 排除 obj/bin，任何 `catch (Exception)` 或 `catch (Exception ex)` 且同行无 `when` 都让 gate 失败；(3) 更新 spec 引用 gate 强制层。 |
| **适用** | **每条 spec 不变量都问：gate 在哪里强制？** 如果答案"没有"，这条不变量只是建议。所有可静态检测的不变量（裸 catch、硬编码颜色、retired 路径回归、schema 字段名等）都应在 `tools/verify.ps1` 有对应检查。**spec/guides + gate check = 完整不变量**。 |
| **元模式** | 这与 M2 互补：M2 说"不要削弱 gate 换速度"，M12 说"不要让不变量没 gate"。两者一起 = gate 既是底线也是天花板。 |

---

## 维护规则

1. **新坑入口**: 踩坑 → 在本文对应章节追加 → 确保有 session-id/证据 → PR/commit 里引用本文。
2. **不写硬数字**: 禁止"本文档含 31 条坑"。说"持续更新"。
3. **过期清理**: 如果某坑的根因已从代码层面消除（如新增 hook 拦截），标记 `[已修复]` 但不删除 — 保留根因分析供参考。
4. **关联 spec**: 如果某坑需要修改编码规范 → 同步更新 `core/quality-guidelines.md` 或 `views/quality-guidelines.md`。

## 5. 新增教训 — UltraReview 全量修复 (2026-06-24)

### 5.1 可变API→不可变API 的变更传播

| 属性 | 值 |
|---|---|
| **证据** | UltraReview fix session, H2 ImmutableList 迁移 |
| **问题** | `LauncherEntryOrder.SyncWithEntries` 和 `MoveRelative` 从 `void`（就地变异 `List<T>`）改为返回 `ImmutableList<T>`（新实例）。3 个调用方中有 1 个（`Refresh()`）丢弃了返回值。 |
| **后果** | `Refresh()` 中 `CustomEntryOrder` 永远不被更新，导致 `ValidateCustomOrder` 测试全部挂掉。 |
| **预防** | 变更返回类型从 `void` → `T` 时，**每个调用方必须确认返回值被使用**。用 `dotnet build` 检查：丢弃返回值不产生编译器警告（C# 默认允许），必须人工审计所有调用方。更好的方案：如果返回值必须被消费，用 `[MustUseReturnValue]` 或返回 `out` 参数。 |
| **适用** | 任何 API 从 mutating → immutable 的重构。 |

### 5.2 Frozen snapshot 破坏 `with { }` 的引用一致性

| 属性 | 值 |
|---|---|
| **证据** | UltraReview fix session, `DiscardDragReorderPreview` bug |
| **问题** | `LauncherDragReorderWorkflow` 在构造时持有 `_settings` 引用。`Refresh()` 通过 `_settings = _settings with { ... }` 创建新 record 实例后，workflow 持有的是**旧实例**。`DiscardDragReorderPreview` 无条件 `_settings = _dragReorderWorkflow.Settings`，覆盖了所有 `Refresh()` 的变更。 |
| **根因** | Immutable record 的 `with { }` 创建**新实例**。之前 `List<T>.Clear()/AddRange()` 是**原地变异**，workflow 持有的引用自动同步。 |
| **预防** | 任何组件持有另一个组件的 record 引用时，如果引用方可能通过 `with { }` 替换实例，持有方必须在每次变更后同步。首选方案：不持有引用，每次通过 getter 访问。 |
| **适用** | 任何 `sealed record` + `with { }` 的快照模式。 |

### 5.3 `?` 可空标记的级联效应

| 属性 | 值 |
|---|---|
| **证据** | UltraReview fix session, M3 fix → 28 测试失败 |
| **问题** | 给 `CustomEntryOrder: ImmutableList<string>?` 加 `?` 导致所有消费方需要 `!` / `??` / null 检查。28 个测试失败，8 处空合并死代码。 |
| **教训** | 消除 `null!` 的正确方向是**消除 null 源**（保证 Normalize 后永不为 null），而不是给类型加 `?`。类型放宽 → 消费方爆炸。类型收窄 → 根源修复。 |
| **适用** | 任何 nullable reference type 的修改。 |

### 5.4 `.bak` 测试契约与实现语义耦合

| 属性 | 值 |
|---|---|
| **证据** | UltraReview fix session, C6 备份重排序 |
| **问题** | 测试用 `Directory.CreateDirectory(config + ".bak")` 模拟备份失败来触发 save failure。C6 将备份移到 try/catch 内后，这个技巧不再有效——save 不会因备份失败而失败。 |
| **教训** | 测试如果依赖实现的副作用（而非契约行为），实现变更时测试会无辜挂掉。模拟 I/O 失败的正确方式是**阻塞核心路径**（如 `File.Delete + Directory.CreateDirectory(mainPath)`），而不是依赖非关键路径。 |
| **适用** | 所有 I/O 故障注入测试。 |

### 5.5 修改序列化 record 字段 → 合约测试必挂

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-24 ultrareview 全量修复, L33 LastUsedPresetId / M11 window X/Y/WindowState |
| **问题** | 给 `LauncherSettings` record 加 `LastUsedPresetId` 字段（带 `[JsonPropertyName]`）→ 93 个测试因 JSON shape 变化而失败。给 `LauncherWindowSettings` 加 `X`/`Y`/`WindowState` → 57 个测试失败。 |
| **根因** | 合约测试断言精确的 JSON 字段集合。任何新字段改变序列化输出 → 合约测试全部挂。 |
| **预防** | 给序列化 record 加字段时，默认 `[JsonIgnore]`，仅显式 opt-in 序列化。运行时状态字段一律 `[JsonIgnore]`。必须序列化的新字段需同步更新所有合约测试。 |
| **适用** | 任何 `[JsonPropertyName]` record 的字段新增。 |

### 5.6 异常类型是公共 API — 变更前审计全部调用方

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-24 M24 RequireNonEmpty 统一异常类型 → 153 测试失败 |
| **问题** | 将 6 个文件中各自抛出的 `InvalidOperationException`/`InvalidDataException` 统一为 `ArgumentException`。测试用 `Assert.Throws<InvalidOperationException>` 精确匹配 → 全部挂。 |
| **教训** | 异常类型是抛给调用方的**公共合约**。统一异常类型之前，必须先审计所有 `Assert.Throws<T>` 测试。如果统一的价值小于测试修改的成本，不做统一。 |
| **适用** | 任何 `throw new TException` 的类型变更。 |

### 5.7 `CanXxx` 和 `DoXxx` 的守卫逻辑必须一致

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-24 C2 删除唯一预设崩溃 |
| **问题** | `CanDeletePreset` 允许单预设删除（`Count <= 1` 返回 `true`），但 `DeletePreset` 的 `RequireFallbackPresetIdAfterDelete` 在无回退时抛异常。UI 按钮可用但点击即崩溃。 |
| **预防** | `CanXxx` 不仅要检查权限/锁定，还要检查**操作执行所需的全部前置条件**。如果 `DoXxx` 的方法体里有 `FirstOrDefault(...) ?? throw`，`CanXxx` 必须验证 `FirstOrDefault` 的非 null 条件。 |
| **适用** | 所有 `CanExecute` / `CanXxx` + `DoXxx` 的配对方法。 |

### 5.8 `File.Replace` 要求目标已存在 — 首次保存不可用

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-24 L1 File.Replace → 163 测试失败 |
| **问题** | 将 `File.Move(temp, target, overwrite: true)` 改为 `File.Replace(temp, target, ...)` 追求原子性。但 `File.Replace` 要求目标文件必须存在。首次保存到空目录时 → `IOException`。 |
| **教训** | `File.Replace` 只适用于**已知目标文件已存在**的替换场景。首次创建用 `File.Move`。对于单进程桌面应用，`File.Move` + temp file 足够安全。 |
| **适用** | 所有文件 I/O 的原子化尝试。 |

### 5.9 校验延迟到持久化层 = 崩溃风险

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-24 H5 重复预设名 + M8 catch 过窄 |
| **问题** | 预设名的唯一性校验在 `Save()` → `Validate()` 中执行，不在 `AddPreset()`/`UpdatePreset()` 中。持久化层抛出的 `InvalidDataException` 逃逸了仅捕获 `IOException | UnauthorizedAccessException | SecurityException` 的窄 catch 过滤器 → 应用崩溃。 |
| **根因** | 校验离突变点太远。用户在对话框中输入 → 对话框关闭（以为成功）→ Save 校验失败 → 异常逃逸 → 崩溃。用户看不到任何错误反馈。 |
| **预防** | **校验在突变点执行，不在持久化层执行**。持久化层的校验是最后兜底（防御手动编辑 JSON 绕过 UI 校验），不是主校验路径。突变点之前的不变量检查必须覆盖可预见的重复/无效/越界场景。 |
| **适用** | 所有 CRUD 操作的校验逻辑。 |

---

## 6. 新增教训 — UltraReview 二次清零 (2026-06-25)

### 6.1 `[JsonIgnore]` + SchemaVersion = 死代码版本检查

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R1 |
| **问题** | `LauncherSettings.SchemaVersion` 标记 `[JsonIgnore]`，反序列化时永远是默认值 `1`。`LauncherStore.Load` 中的 `if (settings.SchemaVersion != Expected)` 是**死代码**——永远通过。 |
| **根因** | `[JsonIgnore]` 阻止字段写入 JSON，也阻止从 JSON 读取。版本号无法持久化→无法检测旧格式。 |
| **预防** | **SchemaVersion 绝不加 `[JsonIgnore]`**。如果需要向后兼容（旧文件无此字段），不加 `[JsonRequired]`，靠默认值兜底。 |
| **适用** | 所有带 SchemaVersion 的序列化 record。 |

### 6.2 Store.Load 损坏文件不触发备份恢复

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R3, 三个 Store.Load 全部存在此问题 |
| **问题** | 备份恢复仅在 `!File.Exists(path)` 时触发。文件**存在但内容损坏**（`JsonException`/`InvalidDataException`）→ 不尝试 `.bak` → 直接抛异常。用户数据丢失。 |
| **修复** | 用 `try { LoadAndValidate(path); } catch (JsonException | InvalidDataException) { if (File.Exists(bak)) { File.Copy(bak, path, overwrite: true); retry; } else throw; }` |
| **预防** | 任何 `Load` 方法的异常处理必须覆盖"文件存在但反序列化失败"的场景。`.bak` 的存在就是为了这个场景。 |
| **适用** | 所有文件读写 Store 的 Load 方法。 |

### 6.3 跨文件 ID 规范化契约必须一致

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R10 |
| **问题** | `LauncherEntryIds.Normalize` 超长静默截断 256 字符。`LauncherPresetIds.Normalize` 超长抛 `InvalidDataException`。同一概念，不同行为。 |
| **预防** | **同一概念的规范化函数必须行为一致**。规范化函数不做验证（将任意输入转为安全形式），验证由调用方完成。统一原则：截断、trim、消控字符 → 不抛异常。 |
| **适用** | 所有 `Normalize` / `Sanitize` 系列函数。 |

### 6.4 `SaveConfig()` 返回值必须检查 — 静默数据丢失

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R6 |
| **问题** | `LaunchAsync` 中 `SaveConfig()` 返回值被丢弃。保存失败（磁盘满/权限）→ `launchedAtUtc` 已写入内存但未写入磁盘 → 用户看到"已请求启动"，下次启动排序错乱。 |
| **预防** | **任何 `Save*()` 方法返回值必须检查**。失败时不得覆盖已有的错误状态文本。保存失败的成功假象是最隐蔽的数据丢失。 |
| **适用** | 所有 ViewModel 命令中的 `SaveConfig()` / `SavePresets()` / `SaveSettings()` 调用。 |

### 6.5 静态可变 `List<T>` — 线程不安全的定时炸弹

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R14 |
| **问题** | `LauncherStartupArguments._unknownArgs` 是 `static List<string>`，`ReadConfigPath()` 中 `Clear()` + `Add()` 就地变异。并发读取 `UnknownArgs` → 不一致状态。 |
| **修复** | 改为返回 `(string? configPath, IReadOnlyList<string> unknownArgs)` 元组。消除静态可变状态，数据由调用方拥有。 |
| **预防** | **永不使用静态可变集合**。static + mutable = 全局可变状态。如果需要缓存，用 `ImmutableList<T>` 或 `FrozenSet<T>`。如果需要返回值，用元组或 record。 |
| **适用** | 所有 `static` 字段。 |

### 6.6 批处理失败策略必须一致 — `break` vs `continue`

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R9 |
| **问题** | `StartBatch` 用 `break`（一次失败停止全部），`PreflightBatch` 用 `continue`（继续处理）。同一工作流，两个阶段，两种策略。 |
| **修复** | 统一为 `continue`。一个条目的启动失败不应阻止后续条目。`FailureIndex` 从 `??=`（首胜）改为 `=`（末胜），记录完整失败链。 |
| **预防** | **同一 workflow 的多个阶段必须有相同的失败处理策略**。如果设计要求不同，必须在代码注释中明确说明原因。 |
| **适用** | 所有批量/批处理操作方法。 |

### 6.7 内部标记注释 (`// H4`) — 对维护者无意义

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R30, ~30 处 `// H4`/`// L32`/`// M11` 等标记 |
| **问题** | 标记仅在 issue tracker 中有含义，代码独立时不可读。新维护者无法理解 `// H4` 是什么。 |
| **修复** | 全部替换为描述性注释。`// H4` → `// Entry IDs are ordinal (case-sensitive) — matches LauncherEntryIds.Comparer`。保留描述内容，丢掉标记。 |
| **预防** | 代码注释描述**做什么**和**为什么**。不引用外部系统中可能过期的 ID。如果必须引用 issue → 写完整 URL。 |
| **适用** | 所有注释和文档。 |

### 6.8 Unicode 正规化缺位 → 搜索排序不一致

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R26 |
| **问题** | `LauncherEntryText.Normalize()` 不做 Unicode 正规化。全角字母 "Ｃ" (U+FF23) ≠ 半角 "C" (U+0043)，组合变音 "é" (e + ́) ≠ 预组合 "é" (U+00E9)。搜索时用户输入半角 "cafe" 匹配不到全角 "Ｃａｆé"。 |
| **修复** | 在 `Normalize()` 末尾添加 `string.Normalize(NormalizationForm.FormKC)`。FormKC 将全角→半角、组合→预组合。 |
| **预防** | 任何处理用户输入文本的 Normalize/Sanitize 函数必须包含 Unicode 正规化。FormKC 兼容性最好。 |
| **适用** | 所有文本规范化/搜索匹配/排序比较路径。 |

### 6.9 命令行长度无上限 → Windows CreateProcess 静默失败

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-25 UltraReview R27 |
| **问题** | `LaunchArgumentBuilder.Build()` 不检查总命令行长度。超过 32767 字符（Windows `CreateProcessW` API 限制）→ 启动失败，用户无清晰错误。 |
| **修复** | `arguments.Length > 32767` → `throw new InvalidDataException(...)`。preflight 阶段就暴露，不等到实际启动。 |
| **预防** | 任何构造系统调用参数的方法，必须检查目标 API 的长度/格式限制。Windows 命令行的 32767 是硬限制。 |
| **适用** | 所有 `Process.Start` / shell 调用路径。 |

### 6.10 XAML TimeSpan 解析隐式假设 → 已发布 exe 冒烟崩溃

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview `verify.ps1` smoke 冒烟 |
| **问题** | `MainWindow.axaml` 中 `MinimumPopulateDelay="150"` — Avalonia XAML 将裸数字 `150` 解析为 `TimeSpan.Parse("150")` = 150 **天**，而非 150 毫秒。在开发环境（`dotnet run`）因 Avalonia 内部容错可能不报错，但 `dotnet publish` 后的 self-contained exe 在执行 `TimeSpan.FromMilliseconds(150天对应的毫秒数)` 时溢出 → `ArgumentOutOfRangeException: TimeSpan period must be less than or equal to Int32.MaxValue`。 |
| **修复** | 改为 `MinimumPopulateDelay="0:0:0.150"`（显式 h:m:s.ms 格式）。 |
| **预防** | XAML 中任何 TimeSpan 属性值必须使用 `h:m:s.ms` 显式格式，禁止裸整数。`dotnet run` 通过≠已发布 exe 通过 — 始终跑 `verify.ps1` GUI 冒烟。 |
| **适用** | 所有 Avalonia XAML 文件中的 TimeSpan 属性。 |

### 6.11 `SaveConfig()` 返回值被丢弃 → 数据静默丢失

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview H4, `MainWindowViewModel.Selection.cs:142` |
| **问题** | 批量启动 `LaunchBatchAsync` 中 `if (startedCount > 0) SaveConfig();` — `SaveConfig()` 返回 `bool` 指示保存是否成功，但返回值被丢弃。若保存失败，`launchedAtUtc` 时间戳已在内存中写入但未持久化。下次重载配置时时间戳丢失。单条启动（`LaunchAsync`）正确处理了返回值。 |
| **修复** | `if (startedCount > 0 && !SaveConfig()) return;` — 保存失败时提前返回，不覆盖成功状态文本。 |
| **预防** | 所有返回 `bool` 的 `Save*()` / `Try*()` 方法，调用者必须检查返回值。Linter/analyzer 无法捕获：靠 code review 和测试覆盖（每个 Save 路径都有失败测试）。 |
| **适用** | 所有调用 `SaveConfig()` / `SaveSettingsChange()` / `TrySaveWindowBounds()` 的路径。 |

### 6.12 `LoadWithRecovery` catch filter 遗漏异常 → 备份恢复被跳过

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview H1, `LauncherJson.cs:63` |
| **问题** | `LoadWithRecovery` 的 catch filter 仅捕获 `JsonException | InvalidDataException | FileNotFoundException`，但 `File.OpenRead()` 在目录被删除时抛出 `DirectoryNotFoundException`（继承自 `IOException`）。此异常绕过 filter → 备份恢复被跳过 → 用户看到启动失败而非自动恢复。 |
| **修复** | 添加 `DirectoryNotFoundException` 到 catch filter。同时为备份恢复路径（`File.Copy` / `File.Move`）添加 try/catch 保护。 |
| **预防** | 任何 catch filter 枚举异常类型时，必须覆盖被调用 API 的所有可能异常，包括继承链。`File.OpenRead` 可能抛 `DirectoryNotFoundException`、`PathTooLongException` 等。用 `catch (IOException)` 更安全但需评估粒度。 |
| **适用** | 所有文件 I/O try/catch 块。 |

### 6.13 默认实例绕过验证管道 → 半初始化状态对象逃逸

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview H3, `LauncherStore.cs:32`, `LauncherStore.cs:34` |
| **问题** | 当配置文件不存在且无 `.bak` 备份时，Store.Load 返回 `new LauncherPresetFile()` / `new LauncherSettings()`。这些默认实例跳过了 `Validate()` / `Normalize()` 管道。虽然当前默认值恰好合法（`ImmutableList.Empty`），但若未来有人修改默认值（如改 `DefaultPresetId` 为非空），将导致半初始化状态对象逃逸到下游消费代码。 |
| **修复** | 将默认值传递通过同一个 Validate/Normalize 方法：`LauncherPresetFiles.Validate(new LauncherPresetFile())` / `Normalize(new LauncherSettings())`。 |
| **预防** | 任何返回 fallback/default 值的路径，fallback 必须经过与正常路径相同的验证管道。在 Store.Load 的 LoadWithRecovery 调用处添加注释标注此约束。 |
| **适用** | 所有 `LoadWithRecovery` 调用的 `defaultValue` 参数。 |

### 6.14 `ForUser()` 异常消息 fallback 泄漏敏感信息

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview H8, `MainWindowViewModel.cs:329` |
| **问题** | `ExceptionMessageFormatter.ForUser()` 对未知异常类型使用 `exception.Message` 作为 fallback。`FileNotFoundException` 等已知类型被安全替换，但未映射的类型（如来自第三方库的异常）的消息可能包含文件路径、连接字符串等敏感信息，直接展示在 UI 状态栏。 |
| **修复** | 明确列出所有"消息安全"的异常类型（`InvalidDataException`、`ArgumentException`、`InvalidOperationException` — 这些由应用代码构造，消息是精心编写的）；对所有其他类型返回固定的通用消息。 |
| **预防** | 异常→用户消息映射必须使用白名单模式（已知安全 → 透传，其他 → 通用消息），而非黑名单模式（已知危险 → 替换，其他 → 透传）。 |
| **适用** | 所有面向用户展示异常消息的方法。 |

### 6.15 Smoke test 配置缺少必需字段 → 门禁虚假绿色

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview, `verify.ps1:47` smoke settings.json |
| **问题** | `verify.ps1` 的 GUI 冒烟测试创建的 `settings.json` 缺少 `schemaVersion` 字段。`LauncherSettings.SchemaVersion` 标记了 `[JsonRequired]`，STJ 在缺少时抛出 `JsonException`。此前门禁可能因 `$isCi=true` 跳过 GUI 冒烟而未被发现。 |
| **修复** | 为 smoke test 的 settings.json 添加 `"schemaVersion":1`。同时修复 `MinimumPopulateDelay` TimeSpan 解析问题（见 6.10）。 |
| **预防** | 任何 JSON schema 变更（添加 `[JsonRequired]` 字段）后，必须搜索并更新所有生成该 JSON 的测试 fixture 和脚本（`verify.ps1`、测试工厂方法、README 示例）。 |
| **适用** | 所有 JSON schema 变更 + 所有 JSON fixture/smoke 配置。 |

### 6.16 诊断洋葱 — 第一个 Bug 掩盖第二个 Bug

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview smoke test 双重修复 |
| **问题** | `verify.ps1` GUI 冒烟失败。第一次修复：smoke settings.json 加 `schemaVersion` → 冒烟仍然失败，但报的是另一个错（`TimeSpan period must be ≤ Int32.MaxValue`）。两个 Bug 叠在一起：`schemaVersion` 缺失被第一个 catch，修复后 `MinimumPopulateDelay="150"` 解析为 150 天的问题才暴露。 |
| **根因** | 第一个 Bug 在启动早期就抛异常阻止了后续代码执行，掩盖了第二个 Bug。修复第一个 Bug 使得代码执行到更深处，才触发第二个 Bug。 |
| **预防** | 修复一个 gate-blocking bug 后，必须完整重跑 gate（不是只验证修复的那一项）。"修复后第一次是 green"的假设是危险的——堆叠的 Bug 需要多次迭代。每次 gate failure 都可能是解锁下一个隐藏 Bug 的钥匙。 |
| **适用** | 所有 gate/CI/smoke 调试场景。尤其是启动路径、初始化管线中的堆叠 Bug。 |

### 6.17 不对称操作路径 — 成对代码未同步

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-26 UltraReview H4, batch vs single launch `SaveConfig()` |
| **问题** | 单条启动（`LaunchAsync`）检查了 `SaveConfig()` 返回值，批量启动（`LaunchBatchAsync`）没有。两条路径是自然成对的——它们做同一件事的不同规模版本——但防御措施不对称。 |
| **根因** | 批量路径是后来添加的（功能扩展），但没有从单条路径复制所有的守卫逻辑。这是"成对代码漂移"：两个语义上等价的实现因不同时间编写而逐渐分歧。 |
| **预防** | 当添加"X 的批量版本"时，必须逐行对比原版的错误处理和返回值检查。理想情况下，批量版本应复用原版的核心逻辑（而非重写），避免歧义。代码审查时，成对路径必须做 diff 对比。 |
| **适用** | 所有 single-vs-batch、sync-vs-async、local-vs-remote 成对实现。 |

### 6.18 跨层验证不一致 — 对话框层和持久化层独立实现同一检查

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-28 UltraReview #2 H6, C14 修复引入的回归 |
| **问题** | `LauncherEntryDrafts.GetRequiredFieldError` 和 `LauncherEntryIds.RequireEntryId` 都检查 Windows 保留名（CON/PRN/NUL/COM1-9/LPT1-9），但前者的 pattern match 是 case-sensitive，后者的 HashSet 是 case-insensitive (OrdinalIgnoreCase)。用户输入 `"con"` 通过对话框验证，但在 `RequireEntryId` 处被拒绝——导致静默回滚或崩溃。另外 `LauncherPresetFiles.Validate` 的 preset count 用 `> 1000`，而 `LauncherPresetWorkflow.AddPreset` 用 `>= 1000`——同样的"两层独立实现"不一致。 |
| **根因** | 当验证逻辑跨越两个层（对话框 UI 层 → 持久化 Store 层）时，每一层独立实现自己的检查，没有共享权威来源。开发者在修复一层时复制了检查代码但遗漏了细微差异（如比较器、边界条件）。 |
| **预防** | (1) 跨层验证必须有单一权威来源——要么 Store 层暴露 `IsValid*` 方法供对话框调用，要么共享常量/集合（如 `WindowsReservedNames` 必须至少是 `internal`）。(2) 添加跨层一致性测试——用非规范输入（小写保留名、边界 count）同时测试对话框和 Store 层。(3) 任何 `> N` vs `>= N` 的不一致都应视为 bug，代码审查时必须逐字对比成对条件。 |
| **适用** | 所有在多个层重复验证相同约束的场景：ID 验证、名称长度、count 限制、路径格式。 |

### 6.19 应用层事务协议 vs 文件系统原子性

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-28 单文件架构重构：CommitJournal 165 行 + 多文件提交协议被 File.Replace 取代 |
| **问题** | 应用有三个独立 JSON 文件需要跨文件一致性。解决方案是实现了 WAL 风格的 commit journal（写日志→提交→崩溃恢复），165 行代码。但 NTFS 本身提供了单文件原子 rename（MoveFileEx with MOVEFILE_REPLACE_EXISTING）。将三个文件合并为一个，单次 `File.Replace` 就覆盖了全部数据——不需要 journal、不需要预提交验证、不需要多文件提交协议。 |
| **根因** | "多文件 → 需要事务"是数据库思维。但文件系统已经提供了单文件原子性。在文件数量可控时（3个），合并文件比实现事务协议更简单、更可靠。 |
| **预防** | 当发现自己在实现"跨文件事务"时，先问：能不能把这些文件合并成一个？如果能，事务由文件系统提供，不需要自己实现。如果不能（文件太大、不同进程写入不同文件、安全隔离要求），再考虑应用层协议。 |
| **适用** | 所有"多个配置文件需要一致性"的场景。单文件 + 原子 rename 几乎总是优于多文件 + 应用层事务。 |

### 6.20 不可变记录拷贝必须传播所有字段

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-28 UltraReview C1: `LauncherEntryDrafts.CreateEntry` 从 `previousEntry` 只传播了 `UpdatedAtUtc`，遗漏了 `LaunchedAtUtc` |
| **问题** | `CreateEntry` 构造新的 `LaunchEntry` 时显式设置了 `UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty`，但遗漏了 `LaunchedAtUtc`。编辑条目后 `launchedAtUtc` 静默变为空字符串——最近启动排序丢失该条目的历史。 |
| **根因** | 不可变记录的 `with { }` 克隆只显式声明要复制的字段。新增字段后，所有手动构造 `new()` 而非 `with { }` 的拷贝点必须同步更新。没有编译器强制执行——这是"变异点审计"问题。 |
| **预防** | (1) 记录定义旁边的注释列出所有手动构造点。新增字段后，grep 所有 `= new RecordType` 和 `= new()` 确保新字段被传播。(2) 工厂方法 (`CreateEntry`) 应接受 `previousEntry` 并在注释中声明传播行为。(3) 对带时间的记录考虑在测试中断言 `LaunchedAtUtc` 在编辑后不为空。 |
| **适用** | 所有使用不可变记录 + 手动构造函数拷贝的场景。 |

### 6.21 Schema 版本迁移：不接受旧版本 = 测试崩溃

| 属性 | 值 |
|---|---|
| **证据** | 2026-06-28 Ponytail-audit 执行后: `LauncherStore.ExpectedPresetSchemaVersion` 保持为 1，但 `LauncherPresetFiles.Validate()` 强制写入 `SchemaVersion = 2`。Load 时 `if (2 > 1)` 抛出异常，89 个测试失败。 |
| **问题** | schema 版本号在两个位置定义（Store 和 Validate），但版本升级时只更新了一处。`Validate()` 写入 v2，`Load()` 只能读 v1——测试中写 v1 数据再读回时被拒绝。 |
| **根因** | Schema 版本号不是单一来源。两个常量 (`ExpectedPresetSchemaVersion` vs `ExpectedStoreSchemaVersion`) 没有编译时关联。当一个更新时，另一个保持旧值，形成"生产者写新版、消费者只读旧版"的不一致。 |
| **预防** | (1) Schema 版本必须是单一常量。(2) 升级 schema 时，必须接受旧版本（至少 1 个主版本回退）或提供显式迁移路径。(3) `schemaVersion < expected` 应该触发迁移（不是拒绝），除非数据格式不兼容。(4) 升级验证清单：grep 所有 `ExpectedSchemaVersion` 常量 → 确认全部同步 → 运行全部测试 → 预期 0 失败。 |
| **适用** | 所有 JSON schema 版本管理的场景。 |

## 7. 类型驱动架构重构教训 (2026-06-29/30)

> 来源：UltraReview 完整扫描（28 子代理 + 直接审查 130 文件）→ 架构调研 → 全阶段实现

### 7.1 多层验证不一致 → 保存-崩溃循环

| 属性 | 值 |
|---|---|
| **证据** | `LauncherPresetWorkflow.NormalizeRequiredName` 不检查 256 字符（仅 `LauncherPresetFiles.Validate` 检查）。超长名称通过工作流创建→保存成功→下次加载时 `NormalizeStorePresetName` 抛出→应用无法启动。 |
| **根因** | 验证逻辑分散在"工作流层"和"持久层"两个独立路径中，各自独立实现同一检查。不一致时运行正常，重启崩溃。 |
| **预防** | (1) 使用 `Parse-Don't-Validate` 模式——类型构造即验证，非法状态无法创建。(2) 验证只在单一入口完成，下游客代码信任类型。(3) 新增 `PresetName.TryParse()` 在构造时强制 256 字符限制，`NormalizeRequiredName` 委托给它。 |
| **适用** | 所有存在多层验证的场景。见 `PresetName.cs`、`EntryId.cs` 实现。 |

### 7.2 规范化在错误的地方做 → 注入检测失效

| 属性 | 值 |
|---|---|
| **证据** | `LaunchArgumentBuilder.Build()` 在 FormKC 规范化**之后**检测 `%` 注入，但 `LauncherEntryDrafts.NormalizeDraft` 在**创建 `LaunchEntry` 之前**已经做了 FormKC。全角 `％` (U+FF05) 在 `Build` 拿到数据时已被转为 `%`，两个比较值都已规范化→检测失效。 |
| **根因** | 规范化不在类型边界执行。数据被提前规范化后，注入检测比较的是两个"已规范化"的值——始终相等。 |
| **预防** | (1) 注入检测必须在**原始输入**上执行。(2) 类型构造时检测：`EntryDirectory.TryParse` 比较原始输入 `raw` 和规范化后 `normalized` 的 `%` 差异。(3) 类型内部为"已验证"状态，不再执行二次安全检测。 |
| **适用** | 所有需要防注入的字段。见 `EntryDirectory.cs:TryParse()` 实现。 |

### 7.3 手写崩溃恢复不可靠 → [已反转] LiteDB 路径已回退

| 属性 | 值 |
|---|---|
| **证据** | `CommitJournal.Replay` 在目标文件缺失时 `File.Replace` 抛异常，journal 文件不删除→下次启动重放→永久循环。`File.Replace` 失败不删除 journal→无限重试。null/空 `Target` 字段崩溃未处理。非原子部分回放留下不一致状态。 |
| **根因** | 手写 journal 协议实现 5 个 CRITICAL 级缺陷。temp-file + backup-rotation + journal-replay 三个机制各不完整。 |
| **原方案（已废弃）** | 当时用 LiteDB 嵌入式数据库替代手写文件 I/O。**该路径已于 2026-07-09 回退**——见归档任务 `07-09-remove-litedb-deadcode`。LiteDB 引入了进程文件锁（见 §2.14）且对桌面单用户场景过度工程，单文件 JSON + temp-file + `File.Replace` + `.bak` rotation 足够安全。 |
| **当前方案** | `LauncherJson.Save` 用 temp-file + `File.Replace`（NTFS 原子）+ `.bak` rotation。`LauncherStore.Load` 在损坏时自动回退到 `.bak`。零应用层恢复代码。 |
| **适用** | 所有需要事务性持久化的场景。优先单文件 + 文件系统原子性，仅在单文件不足时（多进程并发写、超大数据库）才考虑嵌入式 DB。 |

### 7.4 硬编码环境假设 → 静默故障

| 属性 | 值 |
|---|---|
| **证据** | `LaunchArgumentBuilder.ShellCommandPrefix` 硬编码 `pwsh`。未安装 PowerShell Core 的系统上终端标签页静默失败。`wt.exe` 报"找不到 pwsh"无提示。 |
| **根因** | 环境依赖硬编码为常量，无配置化路径。 |
| **预防** | (1) 创建 `ShellCommand.Resolve(shellName)` 配置驱动。(2) 默认 `pwsh`，允许用户配置 `powershell` 或其他。(3) 解析时验证 shell 存在于已知位置，失败时提供明确错误消息。(4) JSON 序列化透明保存 shell 名称。 |
| **适用** | 所有依赖外部可执行文件的场景。见 `ShellCommand.cs`。 |

### 7.5 异常用于控制流 → 编译器无法保证完整性

| 属性 | 值 |
|---|---|
| **证据** | `ExceptionFilters.IsRecoverable(ex)` 黑名单式过滤：仅排除 4 种异常（OOM/StackOverflow/AV/SEH），所有其他异常被视为"可恢复"。`MainWindow.Drag.cs` 裸 `catch (Exception)` 4 处捕获致命异常后继续运行。保存失败后 `StatusText` 不更新——用户看到成功提示但数据未持久化。 |
| **根因** | 异常语义模糊——"可恢复"无编译期定义，靠运行时测试覆盖。 |
| **预防** | (1) 创建 `Result<T, E>` 类型——成功/失败的定义由类型系统保证。(2) `LauncherStore.TryLoad/TrySave` 返回 `Result`，消除 5 种异常 catch。(3) `IStore` 接口统一 `Result` 返回。(4) 真实异常仅用于不可恢复场景（OOM 等），不再用于业务逻辑控制流。 |
| **适用** | 所有 try-catch 处理"可恢复"错误的场景。见 `Result.cs`、`StoreErrors.cs`。

### 7.6 [Obsolete] + 委托模式 = 渐近迁移安全网

| 属性 | 值 |
|---|---|
| **证据** | Phase 1 引入 6 个 value object，142 个调用方引用旧静态类。直接删除→巨大 diff 高风险；保留不标记→新旧代码混用。 |
| **根因** | 类型系统升级需要所有调用方同步变更，无法原子操作。 |
| **预防** | (1) 旧方法标记 `[Obsolete]` 属性——编译器警告所有未迁移调用方。(2) 旧方法内部委托给新类型——行为保证一致。(3) 抑制 CS0618 直到迁移完成。(4) Phase 3 删除旧方法。 |
| **适用** | 所有 API 升级场景。见 `LauncherEntryIds.cs`、`LauncherPresetIds.cs`、`LauncherEntryText.cs` 实现。 |

---

## 8. 架构演进教训 (2026-07)

> 来源：MainWindowViewModel 巨类拆分（2587 行 → 8 partial + 1 状态文本文件）

### 8.1 Partial 类拆分是机械操作，不要以"共享状态纠缠"为由推迟

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-09 Stage 2 拆分，单次会话交付 6 个 partial 提取，451/451 测试全程通过 |
| **问题** | 此前以"partial 类共享可变状态会形成依赖纠缠，组合优于继续拆 partial"为由推迟 Stage 2。这个论断基于未经验证的隐含假设：partial 类间共享可变状态会让提取变得危险。 |
| **根因** | 把**未来理想态**当成了**当前阻塞理由**。partial 类在编译期合并为单一类型，共享状态是语言特性而非风险点——提取是纯机械操作：编写新文件（带正确 using）→ 从主文件 head/tail 删除 → `dotnet build` → `dotnet test`。 |
| **预防** | 推延清理工作的决策必须用**具体阻塞点**论证，不能用**架构审美**论证。当前价值（文件可维护性、diff 噪音降低）是立即兑现的，未来理想态（组合优于 partial）可以未来做，但不是今天不做拆分的理由。 |
| **适用** | 所有 C# partial 类拆分决策。同样的逻辑适用于：函数提取（共享局部状态不是阻塞理由）、文件拆分（跨文件 using 不是阻塞理由）。 |

### 8.2 FindUpward 幽灵配置文件陷阱

| 属性 | 值 |
|---|---|
| **频次** | 10+ 次（2026-07 会话反复出现） |
| **根因** | 项目子目录（如 `src/avalonia-launcher/`）有自己的 `README.md`。当 `FindUpward` 在该目录找到 `README.md` + `config/launcher.json`（来自之前误操作保存的幽灵文件）时，误判为仓库根目录停止向上搜索，导致真正的 `config/launcher.json` 从未被加载 |
| **修复** | 删除幽灵文件 `src/avalonia-launcher/config/launcher.json`。未来如果再有子目录配置干扰 `FindUpward`，考虑添加 `.git` 目录检查作为更可靠的仓库根标记 |
| **预防** | 运行 `.exe` 时不加 `--config` 参数，依赖 auto-discovery 从当前目录向上搜索。运行 `dotnet run` 始终从仓库根目录执行。发现视图为空时首先检查 `FindUpward` 搜索路径 |

### 8.3 [已反转] 旧 LiteDB 进程文件锁死循环

| 属性 | 值 |
|---|---|
| **状态** | LiteDB 已于 2026-07-09 移除（归档任务 `07-09-remove-litedb-deadcode`）。此坑保留供历史参考。 |
| **频次** | 8+ 次（LiteDB 时代） |
| **根因** | 旧 WTLauncher App 进程（通过 Explorer 双击启动的）持有 Debug 构建输出的文件锁，`dotnet build` 无法覆盖。`dotnet run` 被迫运行旧 exe，而旧 exe 使用 LiteDB 路径加载 0 条目后覆盖 `config/launcher.json`，形成"旧 exe 加载空数据→覆盖配置文件→新 exe 启动读空数据→旧 exe 仍在运行→锁死"的循环 |
| **当时修复** | `wmic process where "name='WTLauncher.App.exe'" delete` + `Stop-Process -Force` + 删除旧构建输出 |
| **教训** | 嵌入式数据库引入进程级文件锁，对桌面单用户应用是净负担。单文件 JSON 无锁、可随时覆盖、原子性由文件系统保证。见 §7.3。 |

### 8.4 Schema 合并遗留死字段 → 双源漂移

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-09 任务 `07-09-schema-first-persistence` (T7) |
| **问题** | 2026-07 三文件（config.json/presets.json/settings.json）合并为单文件 `launcher.json` 时，`LauncherSettings.SchemaVersion` 字段保留为死代码（默认 1，从未被 `ExpectedStoreSchemaVersion` 校验）。4 个 mutation 站点（`LauncherEntryMutationSnapshot.Capture/CloneSettings`、`LauncherDragReorderWorkflow.Begin`、`MainWindowViewModel.CaptureSettingsMutationSnapshot`）持续强制 `with { SchemaVersion = 1 }`，而顶层 `LauncherStoreFile.SchemaVersion = 2` 是真正校验来源。settings 级 schema version 字段从未被消费 → 与 §6.1 / §6.19 同性质的双源漂移。 |
| **根因** | (1) 文件合并后未做 dead-field 审计；(2) mutation 站点的 `with { SchemaVersion = 1 }` 是迁移过渡代码，未在合并完成后清理；(3) 缺少 pin 测试钉死"哪些 record 应该有/没有 schemaVersion"。 |
| **修复** | (1) 移除 `LauncherSettings.SchemaVersion` 属性（含 `[JsonPropertyName]` / `[JsonRequired]`）；(2) 清理 4 处 mutation 死代码；(3) 加 pin 测试 `LauncherSettings_NoSchemaVersionField_SingleSourceOfTruth`；(4) 加向前兼容测试 `LauncherSettings_LegacySchemaVersionInJson_IsIgnoredForwardCompatible`（`UnmappedMemberHandling=Skip` 保证旧文件仍可加载）。 |
| **预防** | (1) **Schema 合并/迁移完成时**：grep 所有 `[JsonPropertyName("schemaVersion")]` 或其他"version-like"字段，标_dead_并清理。(2) **每个 schema-bearing record 必须有 pin 测试**：序列化后 JSON 必须包含/不包含特定字段。(3) **Single source of truth**：如果一个版本号字段在 N 个 record 中存在，N-1 个是 dead，必须移除。 |
| **Bayesian 应用** | 初始假设：可能是 `LoadWithRecovery` 误恢复（H1, 30%）/ 可能是 mutation snapshot 漏抓字段（H2, 30%）/ 可能是死字段漂移（H3, 40%）。证据：`git diff` 显示 `settings.schemaVersion` 从 2 变为 1 after mutation → 强烈支持 H3，否定 H1/H2。修复后 pin 测试一次性通过，无重试 → 高置信度（95%+）。 |
| **适用** | 所有经历 schema 合并/迁移的序列化 record 树。本次审计确认 `LaunchEntry`、`LauncherPreset`、`LauncherPresetFile`、`LauncherWindowSettings` 均无类似 dead field。 |
| **二次发现 (2026-07-09 重执行)** | T7 修复后，`tools/verify.ps1:367-368` 仍断言 `settings.schemaVersion = 2`，导致根门禁失败。**根因扩展**：原预防规则只覆盖 C# 代码 + pin 测试，遗漏了 **PowerShell 门禁脚本**也是 schema 消费者。**新预防规则**：schema 字段移除时，必须 grep 三处：(1) `src/` C# 代码；(2) `tests/` 单元测试；(3) `tools/*.ps1` 门禁脚本（`verify.ps1`、`build-release.ps1`）。修复方式：将 `Assert-OnlyJsonKeys` 的 `schemaVersion` 移除 + 添加负向门禁 `if ($settings.PSObject.Properties['schemaVersion']) { throw ... }` —— 这提升了测试标准（任何未来重引入死字段都会被根门禁拦截）。 |


---

## 9. 无障碍生产审计教训 (2026-07-20)

> 来源：任务 `07-13-v04-accessibility-production-audit`

### 9.1 Avalonia 12.1.0 无 ReducedMotion API — 预备 XAML 样式但无法激活

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-20 V-04 实现会话，尝试访问 `PlatformSettings.ReducedMotion`、`ReducedMotionState`、`IPlatformSettings.ReducedMotion` 均无法解析。`strings` 搜索 Avalonia 12.1.0 程序集确认无 `ReducedMotion` 字符串 |
| **根因** | Avalonia 12.1.0 尚未暴露 OS level reduced-motion 偏好 API。`IPlatformSettings` 和 `TopLevel.PlatformSettings` 均缺少相关属性。该 API 可能在后续版本（12.2+）加入 |
| **当前状态** | (1) XAML 样式 `.reduced-motion Border.card` 已准备为 stub，`transition` 归零；(2) `MainWindow.axaml.cs` 中有 `ApplyReducedMotion()` 空方法体（`static` 避免 CA1822）；(3) 动态检测 deferred 到 Avalonia 升级 |
| **AC 标记** | PRD AC2 标记为 `[~]`（部分完成 — 静态样式 stub 就位但动态 API 不可用） |
| **预防** | (1) 引入 Avalonia 版本检查到工具链（`dotnet list package | findstr Avalonia`）；(2) 未来升级时 grep `PlatformSettings` 确认 `ReducedMotion` 是否暴露；(3) 先从 XAML 侧准备样式（反向绑定 `classes="reduced-motion"`），API 就位后只需在 `ApplyReducedMotion()` 中添加平台检测逻辑 |
| **适用** | 所有依赖 OS-level 可访问性偏好 API 的功能（reduced motion、high contrast、color scheme detection） |

---

## 10. Unified Store 架构迁移教训 (2026-07-21)

> 来源：任务 `07-21-unified-store-parent`

### 10.1 RestoreVmStateFromStore 事件链断裂

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-21 Unified Store Phase 3 实现会话。`RestoreVmStateFromStore()` 调用 `_list.ReplaceAllEntries()` → 触发 `EntriesChanged` → `OnEntriesChanged` → `RefreshAllProjections` → `ApplyFilter` → `FilterAndSort` → `ValidateCustomOrder` 因 `CustomEntryOrder` 尚未同步而抛出 `missing entry id for projection`。7 个 save-failure 测试一致失败。 |
| **根因** | `ReplaceAllEntries` 释放 `EntriesChanged` 事件，该事件被 `OnEntriesChanged` 订阅并调用 `RefreshAllProjections()`，而 `RefreshAllProjections()` → `ApplyFilter` → `ValidateCustomOrder` 需要 `_settings.CustomEntryOrder` 包含所有 entry ID。但 `RestoreVmStateFromStore` 在设置时才从 AppStore 读取 `_settings` — 事件在 `_settings` 更新 `CustomEntryOrder` 之前就被触发了。 |
| **修复** | 替换 `_list.ReplaceAllEntries()` 为 `_list.ReplaceEntries()`（不触发事件），然后显式调用 `EnsureCustomOrderContainsEntries()` 再 `RefreshAllProjections()`。 |
| **预防** | (1) **`ReplaceAllEntries` vs `ReplaceEntries` 的语义差异必须标注**：前者释放事件（适用于"我已在事件处理前准备好所有状态"），后者不释放（适用于"我还有状态需同步后再刷新"）。(2) **新建 restore/revert 方法时**：先追踪哪些状态依赖关系必须先就位，再触发刷新操作。事件订阅形成隐式 DAG，调用者必须确保 DAG 的上游依赖在触发事件前已满足。(3) **save-failure 回归测试的价值**：7 个依赖 rollback 的测试一次性捕获了这个 bug，否则代码带着逻辑缺陷提交而门禁全绿。 |
| **适用** | 所有操作 `EntryListViewModel` 且涉及事件订阅的 mutation。 |

### 10.2 Inline 重构丢失副作用（DiscardDragReorderPreview）

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-21 Unified Store Phase 3。`SelectionChangeDuringDragPreviewDiscardsPreviewOrder` 测试失败：设置 `IsSelected = true` 应丢弃拖拽预览，但 `Settings.CustomEntryOrder` 仍保持拖拽顺序。 |
| **根因** | 旧 `CaptureSelectionMutationSnapshot(LauncherEntryViewModel, bool)` 重载内部调用了 `DiscardDragReorderPreview()`。inline 后 `UpdateSelection()` 仅创建 `List<(LauncherEntryViewModel, bool)>` 但没有调用 `DiscardDragReorderPreview()`。另一个重载 `CaptureSelectionMutationSnapshot(IEnumerable<>)` 被 `SetSelection` 调用，inline 时保留了 `DiscardDragReorderPreview()` — 双入口不一致。 |
| **修复** | `UpdateSelection()` 入口加 `DiscardDragReorderPreview()`。 |
| **预防** | (1) **方法 inline 时必须审计所有副作用**：不只是返回值，还有内部调用的状态变异方法。(2) **删除一个方法前**：列出其所有调用的外部方法，确认每个调用在新的 inline 站点都有对应。(3) **Inline 后留意双入口不一致**：同一 old 方法的多个重载如果被不同站点调用且行为不同，inline 后必须保持该差异。 |
| **适用** | 所有 `CaptureXxx` → inline 重构场景。 |

### 10.3 Avalonia TransformParser 不接收 translateY 无单位非零值

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-24 Mode 2 acrylic 调试会话。`translateY(-1.5)` 和 `translateY(-2)` 均抛出 FormatException |
| **根因** | Avalonia TransformParser 不接受不带 px 后缀的非零值。0 可无单位，但非零值必须带 px。 |
| **修复** | `translateY(-1.5)` → `translateY(-2px)` |
| **预防** | Avalonia RenderTransform CSS 式字符串值，非零值必须带 px 后缀。编译检查不捕获此问题，必须运行时验证。 |
| **适用** | 所有 Avalonia RenderTransform CSS 字符串赋值。 |

### 10.4 ExperimentalAcrylicMaterial 颜色不匹配

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-24 Mode 2 acrylic 实现。FallbackColor 渲染为灰色而非预期颜色。 |
| **根因** | Win10 <19628 不支持 AcrylicBlur，MaterialOpacity/TintOpacity 在无亚克力时仍然参与颜色计算，FallbackColor 不是纯色兜底而是叠加层。 |
| **修复** | 回退到纯色 BackgroundBrush。 |
| **预防** | Win10 亚克力优先用 SetWindowCompositionAttribute，ExperimentalAcrylicBorder 仅作为兜底。 |
| **适用** | 所有 Avalonia 窗口背景特效。 |

### 10.5 Ponytail 决策必须正式归档 — 不应留下"planning"死任务

| 属性 | 值 |
|---|---|
| **证据** | 2026-07-24 arch-depth-landing 归档。fluxor-migration 和 declarative-props 两个子任务在 parent 的 3/5 子项完成后停留在 "planning" 状态，导致父任务无法完成。原始结果文档已标记 ponytail（完整 VM 接线 YAGNI/零边际收益，声明式替换纯抛光无功能增益），但未执行归档操作。 |
| **根因** | 结果文档写"不做了"和任务状态设为"planning"是矛盾的——planning 暗示未来会做，ponytail 是"已评估、决定不做"。不归档的 ponytail 任务会无限期占用任务列表、在 `task list --mine` 中持续显示、被自动继续系统反复拉回。同时 dirty parent 也无法完成。 |
| **修复** | (1) 更新 parent task.json `status: completed` + `completedAt`；(2) 归档两个子任务；(3) 更新 parent result.md 记录完整的 ponytail 决策理由、恢复条件；(4) 归档 parent；(5) 更新 pitfalls.md 记录此坑。 |
| **预防** | (1) **Ponytail 是"已评估、决定不做"，不是"planning"**。写 `result.md` 时同步归档子任务——不要留 planning 死任务。(2) 归档前确认 result.md 包含完整 ponytail 文档（决策理由 + 恢复条件 + 当前状态），方便未来重新评估时不丢上下文。(3) `task list --mine` 显示 `in_progress` + `planning` 的任务，定期清理让列表真实反映活跃工作。 |
| **适用** | 所有 ponytail/跳过决策。 |
