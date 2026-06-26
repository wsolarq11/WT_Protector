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
