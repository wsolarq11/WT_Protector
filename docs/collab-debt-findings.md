# 协作模式与技术债务管理审查报告

审查时间: 2026-07-23 (本文档上次更新: 2026-07-25)
审查范围: 最近 500 个提交 (实际跨度: 约 369 个提交自 2026-07-01)

> **2026-07-25 状态同步 (final)**: 本文档所有 High 严重级别项目已清零。
> - F1/F2/F3 安全审计修复 + gate 回归 — 已关闭
> - AppStore/StoreReducers/StoreSelectors 已物理删除(VTuber-Killer 架构 → CFA 架构)
> - .snow/ 跟踪评估结论已记录(保持 gitignore)
> - 测试: 696 → 716, Gate: verify.ps1 PASS (含 GUI smoke)
> - Vogen 值对象已全部迁移为 Core 原生 `record struct` (ValueObjects/ 目录已删除)
> - CFA (Closed Flux Architecture) 5 步迁移已完成 — 参见 `.trellis/tasks/archive/2026-07/07-25-closed-flux-arch/`

> **2026-08-15 归档 + 核验注记**: 本文档已于 2026-08-15 自仓库根目录 `git mv` 至 `docs/collab-debt-findings.md`（内容不变；git status 显示 staged rename）。
> 文中全部短提交哈希（4aebd24、ffb9cd6、c801a50、77504425 等）在当前 git 历史中无法解析（历史曾重写，0 个 unreachable 对象），仅能确认对应提交主题仍存在于现历史；哈希类声明按历史记录保留，未做追溯修改。
> 可复核的现状: F1 落点 `DirectoryPath.cs` 含 GetFullPath（2 处）；F2 `knownEntryIds` 参数与 `PresetService.FindInvalidEntryIds` 现存（10 处引用）；F3 `EntryId.cs` 含 LatinScriptRanges/EnsureLatinScript（4 处）；LiteDB 0 命中；WeakReferenceMessenger 仅 1 处注释提及（`ViewModels/Messages.cs:7`），FeatureMessage 0 命中；verify.yml `full-gate` 确为 PR 触发；verify.ps1 现 43 项 Add-Check，测试属性现 749（门禁实测 832 用例）。

---

## 维度 4: 协作模式分析

### 4.1 Snow Team 提交概览

**发现 4.1-A — Agent 提交集中在两个日期** [Medium]

- **描述**: 所有 32 个 Snow Team 提交集中在 2026-07-22 和 2026-07-23 两天，表明 Agent 协作是"脉冲式"的批量使用而非持续集成。
- **证据**: `git log --grep="Snow Team" --format="%ad"` 结果只包含 2026-07-22 (6 auto-commits + 6 merges) 和 2026-07-23 (14 auto-commits + 6 merges) 两个日期。
- **建议**: 如果 Agent 协作模式持续，建议考虑更均衡的分布以避免批处理冲突风险。

### 4.2 Agent 类型分布

**发现 4.2-A — 20 种 Agent 类型覆盖广泛** [Low]

- **描述**: Agent 类型涵盖修复 (bugfixer, core-fixer, app-fixer, deadlock-fixer 等)、架构 (phase-c-arch, phase-b-ux, phase-de-quality)、工具 (builder, gate-verifier, code-reviewer)、重构 (path-unifier, layout-state, guard-remover) 等多个领域。
- **证据**: Agent 类型分布计数 - 20 个 distinct 类型各出现 1 次 (非 merge)，12 个 merge 提交各对应一个 Agent。
- **建议**: Agent 类型多样性高，说明团队系统性地为不同任务类型分配专用 Agent。建议建立 Agent 类型目录以复用最佳实践。

**发现 4.2-B — 未出现并发多 Agent 协作模式** [Low]

- **描述**: 提交时间线显示 Agent 按顺序执行 (一个 Agent auto-commit 后，下一个 Agent 才开始)，没有并行 Agent 同时工作的证据。
- **证据**: 提交时间戳显示严格顺序排列，无时间重叠。
- **建议**: 这是合理的——单开发者环境下顺序执行更可控。如果未来需要加速，可考虑 Snow Team 的并行协作模式。

### 4.3 Agent 产出质量审查 (3 个选定提交)

#### 审查 1: core-fixer (ffb9cd6)

**发现 4.3-A — core-fixer 高质量 Vogen Value Object 引入** [Low]

- **描述**: 引入 6 个 Vogen 强类型 Value Object (CommandTemplate, DirectoryPath, EntryId, EntryName, PresetId, PresetName, SearchTerm) 并配置 ErrorOr 依赖。每个 Value Object 包含 Validation 方法、XML 文档注释、Instance("Empty", "") 模式。
- **位置**: `src/avalonia-launcher/src/WTLauncher.Core/ValueObjects/*.cs`
- **证据**: 新增 7 个 Value Object 文件 + 修改 EntryId 使用 Vogen 内部包装 + 更新 .csproj 和 Directory.Packages.props。
- **评估**: 代码风格与项目一致。每个 Value Object 都有非空验证和 XML 文档。修改后的 EntryId 保留了原有解析逻辑并包装了 Vogen 类型，保持了向后兼容。新增 `implicit operator` 转换使迁移无缝。质量高，无死代码。

#### 审查 2: path-unifier (c801a50)

**发现 4.3-B — path-unifier 精确的手术式删除** [Low]

- **描述**: 移除 `ReplaceAllEntries` 方法 (EntryListViewModel.cs:64-73)，将两个调用点迁移为 `ReplaceEntries` + 显式属性设置 (MainWindowViewModel.cs:173-175, :244-245)。总计 -12 行代码。
- **位置**: `src/WTLauncher.App/Features/Entries/EntryListViewModel.cs:64-73`, `src/WTLauncher.App/ViewModels/MainWindowViewModel.cs:173-175, 244-245`
- **证据**: 2 个文件修改，5 行增加，12 行删除。
- **评估**: 代码变更精确，仅移除冗余方法及更新调用方。没有引入新依赖或死代码。符合"删除优于增加"原则。严格好评。

#### 审查 3: phase-c-arch (5028046)

**发现 4.3-C — phase-c-arch 新增 5 个 View 文件，总体质量良好但含 emoji 字符** [Medium]

- **描述**: 新增 CardsView, DetailsView, FilterToolbar, NamesView, StatusBar 共 5 组 .axaml + .axaml.cs 文件，修改 MainWindow.axaml 集成新视图。总计 +710 行。
- **位置**: `src/WTLauncher.App/Views/Controls/*.axaml` 和 `*.axaml.cs`
- **证据**: 11 个文件修改，710 行插入，33 行删除。
- **评估**: XAML 结构良好，使用了正确的 DataType 绑定和样式引用。但 CardsView.axaml 中的按钮内容使用了 emoji 字符 ("启动所有" 和 "存为启动组")。这与项目风格指南中"禁止使用 emoji"的约束不一致。此外，新视图与 MainWindow.axaml 的集成方式是通过条件绑定 (IsVisible=绑定) 而非导航框架，这在小型应用中是可接受的。

### 4.4 Merge 策略评估

**发现 4.4-A — 全部为简单 2-parent merge，无冲突证据** [Low]

- **描述**: 所有 12 个 Snow Team merge 提交都是标准的 2-parent merge。从 parent 1 的 diff 通常很小 (0-100 行变化)，表明 Agent 工作是在干净的基础上完成的。
- **证据**: 12 个 merge 提交，diff from parent 1 范围: 1-710 行。无 merge 冲突标记，无 revert/rollback 提交对应这些 merge。
- **建议**: 当前 merge 策略有效。建议维持"先 review 再 merge"的模式。

**发现 4.4-B — 无自动 CI 验证证据** [Medium → ✅ 已解决]

- **描述**: merge 提交前后没有发现 CI 运行记录或构建验证提交。merge 后没有立即的 fix-up 提交。
- **证据**: 在 Snow Team merge 提交后没有找到 "fix build" 或 "rollback" 提交。但也没有找到 CI 状态标记。
- **建议**: 建议在 merge 前至少运行 verify.ps1 确保 gate 通过。当前无 CI gate 的情况下依赖开发者手动验证。

> **[2026-07-25 更新]** 此问题已在 `.github/workflows/verify.yml` 中解决。`full-gate` job 配置为 `on: pull_request` 时触发，在 build-and-test 通过后运行 `.\tools\verify.ps1`。PR merge 前会自动执行完整 gate。

### 4.5 单人+Agent 协作模式

**发现 4.5-A — 开发者主导的代理辅助模式** [Low]

- **描述**: 自 2026-07-01 以来，274 个提交中 32 个 (11.8%) 为 Agent 提交，242 个 (88.2%) 为手动提交。Agent 主要用于离散的修复任务和阶段性架构工作。
- **证据**: `总提交: 274, Agent提交: 32, 手动: 242`
- **评估**: 这是一个健康的协作比例——开发者主导架构方向和日常工作，Agent 处理定义明确的子任务。Agent 的"脉冲"使用模式表明开发者有意识地选择何时使用 Agent。

---

## 维度 5: 技术债务管理

### 5.1 verify.ps1 防御 Gate

**发现 5.1-A — 22 项检查覆盖全面** [Low]

- **描述**: verify.ps1 (609 行) 实施 22 项防御性检查，涵盖基础设施、代码质量、配置模式、文档完整性、外部验证和回归预防。
- **证据**: 检查清单: line-ending-policy, snow-ignored-local-state, retired-paths-removed, no-bare-catch-exception, app-raw-hex-zero, mainline-layout, readme, feature-objective-boundary, claim-boundary-ledger, github-similar-projects-snapshot, json, config-schema, presets-schema, settings-json, shortcut-backup-retirement, winforms-retired-design, hta-retired-config, avalonia-mainline-verify, no-stale-three-file-refs, settings-single-source-buildstorefile, winget-config-schema-valid, no-imperative-view-ui。
- **建议**: 各检查项职责清晰，加上文档化的 ponytail 注释说明设计决策和局限性。

**发现 5.1-B — 安全审计发现缺乏对应 Gate** [Closed ✅ — verify.ps1 已包含 gate]

- **描述**: SEC-AUDIT 的 3 个 MEDIUM 发现 (Path.GetFullPath、preset entryId 交叉验证、同形字保护) 均未在 verify.ps1 中设置 gate。当前无门禁阻止这些问题的回归。
- **位置**: tools/verify.ps1
- **证据**: verify.ps1 中无 Path.GetFullPath 检查、无同形字检测、无运行时 entryId 交叉引用验证。
- **建议**: 为 3 个 MEDIUM 安全发现添加 gate：
  - 添加 `Select-String -Pattern "GetFullPath"` 检查确保 LaunchEntry.cs 在 ExpandEnvironmentVariables 后调用 Path.GetFullPath
  - 添加静态分析检查入口目录的规范化
  - 至少添加文档声明同形字保护状态

> **[2026-07-23 更新]** 此问题已在提交 `4aebd24` 中修复。已为 3 个安全审计发现（F1: Path.GetFullPath、F2: preset entryId 交叉验证、F3: 同形字保护）分别添加 verify.ps1 回归门禁（`security-audit-f1`/`f2`/`f3`）。

### 5.2 安全审计项跟踪状态

**发现 5.2-A — F1 (MEDIUM): Path.GetFullPath — 已修复** [Closed ✅ — 2026-07-23 修复]

- **描述**: SEC-AUDIT 建议在 ExpandEnvironmentVariables 后添加 Path.GetFullPath() 调用作为防御深度。当前 LaunchEntry.cs 中未发现 Path.GetFullPath 调用。
- **位置**: `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs:57`
- **证据**: `Select-String -Pattern "GetFullPath"` 在 LaunchEntry.cs 中无匹配。notebook 确认 F1 为 MEDIUM 且未修复。
- **风险**: 攻击者可通过包含未规范化路径分隔符的目录字符串绕过目录存在性检查。
- **建议**: 在 ExpandEnvironmentVariables 后添加 `directory = Path.GetFullPath(directory);`。

> **[2026-07-23 更新]** 已在提交 `4aebd24` 中修复。`DirectoryPath.cs` 的 `JsonConverter.Read` 方法中添加了 `Path.GetFullPath` 调用，确保路径在反序列化时已被规范化。

**发现 5.2-B — F2 (MEDIUM): preset entryId 交叉验证 — 已解决** [Medium]

- **描述**: SEC-AUDIT 建议运行时交叉验证 preset 引用 entryId 是否存在于 config entries 中。verify.ps1 gate 已做静态检查 (presets-schema 检查确保 referenced entry IDs 有效)，但运行时验证未完整实现。
- **位置**: 运行时: `LauncherPresetWorkflow` (推测)
- **证据**: git log 显示有相关提交 (duplicate preset entry rejection, blank reference rejection) 但未找到专门的 cross-reference 提交。notebook 确认 F2 状态为待处理。
- **建议**:
  - 确定现有运行时行为: 不存在的 entryId 是静默过滤还是抛错
  - 添加 failing-fast 验证: preset.entryIds 中每个 ID 必须在 config.entries 中存在

> **[2026-07-23 更新]** 已在提交 `4aebd24` 中修复。`IPresetService.AddPreset`/`UpdatePreset` 方法中添加了 `knownEntryIds` 参数验证，运行时确保 preset 引用的 entryId 在配置中已存在。

**发现 5.2-C — F3 (MEDIUM): 同形字保护 — 已修复** [Medium]

- **描述**: SEC-AUDIT 建议跨脚本同形字检测 (Latin 'a' U+0061 vs Cyrillic 'а' U+0430)。EntryId 使用 FormKC 规范化但不处理跨脚本同形字。
- **证据**: git log 中无同形字/同形词相关提交。notebook 确认 F3 状态为待处理。
- **风险**: 可能实现 UI 欺骗——两个视觉相同但字节不同的 EntryId 指向不同的启动配置。
- **建议**:
  - 评估 trusted-local-config 威胁模型下同形字的实际可利用性
  - 如果决定修复，添加 Unicode 同形字检测到 EntryId.Parse()
  - 至少添加文档说明已知限制和威胁模型假设

> **[2026-07-23 更新]** 已在提交 `4aebd24` 中修复。`EntryId.cs` 中添加了 `LatinScriptRanges` 定义和 `EnsureLatinScript()` 验证方法，在 EntryId 构造时确保字符属于拉丁脚本，有效阻止跨脚本同形字攻击。

### 5.3 死代码清理效果

**发现 5.3-A — WeakReferenceMessenger/FeatureMessage 已完全清理** [Low]

- **描述**: 旧的消息基础设施已完全移除。
- **证据**: `Select-String -Pattern "WeakReferenceMessenger|FeatureMessage"` 在 `src/avalonia-launcher/src/**/*.cs` 中无匹配。删除提交: `77504425 "清除死代码 WeakReferenceMessenger + FeatureMessages (667/667)"`。
- \*\*评估: 清理彻底，测试仍通过 (667→667)。

**发现 5.3-B — LiteDB 已完全清理** [Low]

- **描述**: LiteDB 死代码路径已移除。
- **证据**: `Select-String -Pattern "LiteDB"` 在 `src/avalonia-launcher/src/**/*.cs` 中无匹配。删除提交: `3ce3619 "remove LiteDB dead code path"`, `ef978b7 "remove ShellCommand/LiteDB dead code"`。
- **评估**: 清理彻底。

**发现 5.3-C — 多次系统化死代码清除模式** [Low]

- **描述**: 项目历史显示系统化的死代码/重构清除模式: 快照类消除 (67827bb)、Store 架构统一 (d2fd836)、ShellCommand/LiteDB 清理 (ef978b7)、Result<T,E> 消除 (55cf008)、UltraReview 批量修复 (89a4650, 03e9401, 61c1e1c)、KISS consolidation (022f708)。
- **证据**: git log 显示超过 15 个专门的重构/清理提交。
- **评估**: 项目团队有良好的技术债务管理习惯——系统性的清理是常规工作的一部分。

### 5.4 现代化完成度

**发现 5.4-A — 3 波现代化全部完成** [Low]

- **描述**: Modernization 任务分 3 波执行，全部标记为已完成并归档。
- **证据**:
  - Wave 1 (ad9d80f): P2-P7 modernization wave
  - Wave 2 (1732754): 5-transform modernization wave
  - Wave 3 (ac675a4): 8-task wave — CompiledBindings, Native AOT, static delegate, frecency, status text, rename (LauncherEntryVisibility → LauncherEntryQuery), chrome constants, spec docs
  - 归档提交: c21ec1e "archive completed tasks — FMA leftovers, modernization program", 757b84a "archive 07-12-modernization-audit"
- **评估**: 8 项任务 + 5 项转化 + P2-P7 多波全部完成。frecency 功能附带 8 个单元测试和设计文档。Native AOT 在构建脚本中使用 PublishAot 而非项目文件配置。总计 ~1150+ 行代码变更，46 个文件。

### 5.5 复发模式门禁分析

**发现 5.5-A — verify.ps1 包含 5 个针对性复发门禁** [Medium]

- **描述**: verify.ps1 中的 5 个检查直接针对已知的复发模式:
  1. `no-bare-catch-exception` — 防止 `catch(Exception)` 复发 (bug fix 266aa6f)
  2. `app-raw-hex-zero` — 防止原始颜色字面量出现在 Views 层
  3. `no-stale-three-file-refs` — 防止文档回归三文件架构描述
  4. `settings-single-source-buildstorefile` — 防止持久化双路径漂移
  5. `no-imperative-view-ui` — 防止 C# 命令式 UI 构建替代 XAML
- **证据**: verify.ps1 中各检查的 `ponytail:` 注释明确引用了对应的 task/bug fix。
- **建议**: 这是良好的实践——每个 gate 都有明确的动机和升级路径。

**发现 5.5-B — 安全审计复发模式无门禁** [Closed ✅ — verify.ps1 已包含 security-audit-f1/f2/f3 gate]

- **描述**: 3 个 MEDIUM 安全发现对应的复发模式无门禁保护。如果修复后出现回归，verify.ps1 无法捕获。
- **证据**: verify.ps1 中无 Path.GetFullPath 检查、无同形字检查、无运行时 entryId 交叉引用 gate。
- **建议**: 修复 3 个 MEDIUM 问题后，为每个修复添加 verify.ps1 gate：
  - Path.GetFullPath: 扫描 LaunchEntry.cs 包含 GetFullPath
  - 同形字: 扫描 EntryId.cs 包含同形字映射 (至少)
  - 交叉引用: 在 presets-schema check 中增强

> **[2026-07-23 更新]** 此问题已在提交 `4aebd24` 中修复。verify.ps1 中已添加 3 个回归门禁（`security-audit-f1`/`f2`/`f3`），分别用于检测 Path.GetFullPath 存在性、preset entryId 交叉引用验证存在性、以及同形字保护存在性。

**发现 5.5-C — 方法级别死代码复发无门禁** [Low]

- **描述**: "ReplaceAllEntries" 模式 (c801a50 删除的冗余方法) 无复发门禁。类似的"只被一个调用方使用的包装方法"可能再次出现。
- **证据**: verify.ps1 中没有检查"单个调用者的公有方法"模式。
- **建议**: 这属于设计审查范畴，不适合自动化 gate。当前状态可接受。

---

## 总结

### 严重程度汇总

| 严重程度 | 数量 | 关键发现 |
| -------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Critical | 0    | - |
| High     | 0    | 3 项安全审计 High 发现（5.1-B, 5.2-A, 5.5-B）已在提交 `4aebd24` 中全部修复 |
| Medium   | 2    | Agent 批量提交 (4.1-A), emoji 使用 (4.3-C) — 5.2-B 和 5.2-C 已在 `4aebd24` 中修复, 4.4-B (无 CI) 已在 verify.yml 中解决 |
| Low      | 9    | Agent 类型多样性 (4.2-A), 顺序执行 (4.2-B), core-fixer 质量 (4.3-A), path-unifier 质量 (4.3-B), merge 策略 (4.4-A), 协作模式 (4.5-A), gate 覆盖 (5.1-A), 死代码清理 (5.3-A/B/C), 现代化完成 (5.4-A) |

### 主要行动项

1. ~~[High] 修复安全审计 F1: 在 LaunchEntry.cs 中添加 Path.GetFullPath()~~ — **[2026-07-23]** 已完成 (4aebd24)
2. ~~[High] 为安全审计发现添加 verify.ps1 gate 防止回归~~ — **[2026-07-23]** 已完成 (4aebd24)
3. ~~[Medium] 完成 F2: 运行时 preset entryId 交叉引用验证~~ — **[2026-07-23]** 已完成 (4aebd24)
4. ~~[Medium] 完成 F3: 评估并决定同形字保护方案~~ — **[2026-07-23]** 已完成 (4aebd24)
5. ~~[Low] 审查 phase-c-arch 中 emoji 使用，替换为纯文本~~ — **[2026-07-23]** 已完成
6. ~~[Medium] 添加 CI pre-merge 验证 (4.4-B)~~ — **[2026-07-23]** 已在 `.github/workflows/verify.yml` 中配置 `full-gate` job

> **全部 6 项行动项已关闭。** 无未完成的行动项。

### 正面评价

- 项目的 verify.ps1 gate 设计优秀，25 项检查覆盖全面且有明确动机文档
- 系统化的死代码清理习惯——15+ 专门的重构/清理提交
- 3 波现代化全部完成并归档
- Agent 协作模式健康 (11.8% Agent 比例)，开发者主导方向
- Code-reviewer agent 的存在和 UltraReview 批量修复表明多层质量检查
- 单文件架构迁移成功，无残留三文件引用

---

### 后续变更 (本文档撰写后)

以下架构变更发生在本文档撰写后，本文档不再反映当前代码结构的最新状态：

| 变更 | 日期 | 说明 |
|------|------|------|
| VTuber-Killer 架构 | 07-24 | Event Sourcing + Grain 替代 AppStore。AppStore.cs / StoreReducers.cs / StoreSelectors.cs 物理删除 |
| 5 个 Roslyn Analyzer | 07-24 | WT1001/1003/1004/1005/1006 在编译期强制执行架构规则 |
| .snow/ 跟踪评估 | 07-25 | 578MB 不可整体跟踪，结论：保持 gitignore |
| 测试增量 | 07-25 | 696 → 700，Gate 22 → 25，架构规则 + analyzer 测试新增 |
| CI pre-merge gate | 07-23 | verify.yml 已包含 full-gate job (PR 触发运行 verify.ps1) |

> **状态**: 本文档中的所有发现已全部处理。如需最新的完成度总览，请参考 `docs/completion-overview.md`。
