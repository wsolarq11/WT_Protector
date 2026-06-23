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

### M10. 收集数据但不消费 = 纯浪费（searchHistory 教训）

| 来源 | 2026-06-23 ultrareview E1/Q6 |
|---|---|
| **问题** | `searchHistory` 被完整收集、验证、持久化 — 但没有任何 UI 读取它。每次按键触发完整 JSON 序列化写入。 |
| **修复** | 删除收集逻辑。保留数据模型和验证（向后兼容），但不再写入空数据。 |
| **适用** | 每添加一个数据收集路径，必须同时有对应的消费路径。没有消费端的数据收集是 I/O 浪费。 |

---

## 维护规则

1. **新坑入口**: 踩坑 → 在本文对应章节追加 → 确保有 session-id/证据 → PR/commit 里引用本文。
2. **不写硬数字**: 禁止"本文档含 31 条坑"。说"持续更新"。
3. **过期清理**: 如果某坑的根因已从代码层面消除（如新增 hook 拦截），标记 `[已修复]` 但不删除 — 保留根因分析供参考。
4. **关联 spec**: 如果某坑需要修改编码规范 → 同步更新 `core/quality-guidelines.md` 或 `views/quality-guidelines.md`。
