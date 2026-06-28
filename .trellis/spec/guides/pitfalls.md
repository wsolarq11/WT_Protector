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

---

## 2. WT_Protector 项目坑

### 2.1 Entry ID / Preset ID 绕过 Store 直接写入

| 属性 | 值 |
|---|---|
| **证据** | Codex session `019e79ae-773` (2026-05-30) |
| **根因** | ViewModel / Workflow 中散落了绕过 `LauncherConfigStore` / `LauncherPresetStore` / `LauncherSettingsStore` 的写入路径 |
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

### 2.5 OLE DragDrop 嵌套消息循环是性能杀手

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

---

## 3. 关联项目可迁移教训

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
| **问题** | `LauncherSettings.SchemaVersion` 标记 `[JsonIgnore]`，反序列化时永远是默认值 `1`。`LauncherSettingsStore.Load` 中的 `if (settings.SchemaVersion != Expected)` 是**死代码**——永远通过。 |
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
| **证据** | 2026-06-26 UltraReview H3, `LauncherPresetStore.cs:32`, `LauncherSettingsStore.cs:34` |
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
| **证据** | 2026-06-28 Ponytail-audit 执行后: `LauncherPresetStore.ExpectedPresetSchemaVersion` 保持为 1，但 `LauncherPresetFiles.Validate()` 强制写入 `SchemaVersion = 2`。Load 时 `if (2 > 1)` 抛出异常，89 个测试失败。 |
| **问题** | schema 版本号在两个位置定义（Store 和 Validate），但版本升级时只更新了一处。`Validate()` 写入 v2，`Load()` 只能读 v1——测试中写 v1 数据再读回时被拒绝。 |
| **根因** | Schema 版本号不是单一来源。两个常量 (`ExpectedPresetSchemaVersion` vs `ExpectedStoreSchemaVersion`) 没有编译时关联。当一个更新时，另一个保持旧值，形成"生产者写新版、消费者只读旧版"的不一致。 |
| **预防** | (1) Schema 版本必须是单一常量。(2) 升级 schema 时，必须接受旧版本（至少 1 个主版本回退）或提供显式迁移路径。(3) `schemaVersion < expected` 应该触发迁移（不是拒绝），除非数据格式不兼容。(4) 升级验证清单：grep 所有 `ExpectedSchemaVersion` 常量 → 确认全部同步 → 运行全部测试 → 预期 0 失败。 |
| **适用** | 所有 JSON schema 版本管理的场景。 |

