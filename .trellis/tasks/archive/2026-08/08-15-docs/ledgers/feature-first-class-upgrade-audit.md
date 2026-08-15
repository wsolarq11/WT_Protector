# 验证台账: docs/feature-first-class-upgrade-audit.md

> 验证日期: 2026-08-15（只读 pwsh，未运行 dotnet/build/verify.ps1）
> 文档日期: Reviewed on 2026-06-10（自述为 2026-06-10 时点审计; 门禁约束: 仅 no-stale-three-file-refs 于 activeDocs 内扫描本文件）

## 判定原则

- 带日期的点状记录（证据块：HEAD/entries/测试数/检查数/快照年龄）→ HISTORICAL，保留、不改写为现值。
- 表格中指向具体代码事实的「关键证据」单元格（类型/路径/行号/字段数/行数/模式数）→ 按当前源码核验，STALE 改实、UNVERIFIABLE 删/标注。
- 星级与「关键变化」叙述是审计时的判断 → HISTORICAL，保留（仅修复文档自身内部矛盾：行 3 星级）。

## 核验明细

| 声明（as written） | 判定 | 证据（命令 + 结果, 截断） | 处理 |
|------|------|---------------------------|------|
| Reviewed on 2026-06-10 | HISTORICAL | 文档自带日期；git log --follow 见 1bbc4769 "audit: fresh 23-feature first-class citizen re-evaluation from HEAD 61f7716"（2026-06-10 前后） | kept |
| Scope: HEAD `22f57a9` | UNVERIFIABLE | `git cat-file -t 22f57a9` → "Not a valid object name"; `git log --all --grep` 无对象；同期 61f7716（2026-06-10）可解析 | rewritten（保留记录并标注不可解析；台账注同期 61f7716 可解析） |
| 证据块: 19 entries / 19 unique ids / 19 existing directories | HISTORICAL | 现 config/launcher.json entries=12（2026-08-15）; 2026-06-10 时点为审计记录 | kept（带日期；台账注现值 12） |
| 证据块: 2 presets（core-projects, blank-manual） | HISTORICAL | 现值仍 2 presets：core-projects（主线维护）、blank-manual（清空勾选/手动模式） | kept |
| 证据块: settings sortMode=custom, entryViewMode=cards, requireConfirmationForLaunches=false, 3 commandTemplates, 2 searchHistory | HISTORICAL | 现值一致（sortMode=custom, entryViewMode=cards, requireConfirmationForLaunches=false, templates=3, searchHistory=2）；现值 settings 另含 lastFilterText/cardsGroupKey/customEntryOrder/backgroundMode/quickLaunchHotkey/runInTray/updateManifestUrl/window | kept（带日期） |
| 证据块: dotnet test → 542 tests PASS | HISTORICAL | 现值: 基线 728 属性（699 Fact+29 Theory）、门禁 811 用例 | kept（带日期） |
| 证据块: verify.ps1 → 16/16 root checks | HISTORICAL | 现值 Add-Check=38 项 | kept（带日期） |
| 证据块: 快照 capturedAtUtc 2026-06-09T15:47:55Z 年龄 1 天 | HISTORICAL | 现值 capturedAtUtc=2026-08-14T11:53:15Z | kept（带日期；台账注现值） |
| 行 1: `LaunchEntry.cs:5-30` 7 字段、`UnmappedMemberHandling.Disallow` | STALE | LaunchEntry.cs 现 8 个 init-only 属性（id/name/directory/command/selected/updatedAtUtc/launchedAtUtc/launchCount）; CLAUDE.md Scheme B（2026-07-23）: Loose Read + Strict Write，不再使用 UnmappedMemberHandling.Disallow | rewritten |
| 行 2: `LaunchArgumentBuilder.cs:25-26` runtime check | VERIFIED（行号漂移） | LaunchArgumentBuilder.cs:20-40 wt.exe 路径探测（LOCALAPPDATA→ProgramFiles→FileNotFoundException） | rewritten（去掉脆弱行号） |
| 行 3: `LauncherEntryVisibility.cs:124-141` 多词匹配 | STALE | 类型不存在（0 文件命中）；现为 Core/LauncherEntryQuery.cs MatchesFilter 多词匹配（名称/目录/命令） | rewritten |
| 行 3 星级 ⭐⭐⭐⭐ vs 摘要表 ⭐⭐⭐ 与「关键变化 F3 降为 3 星」矛盾 | STALE（文档内部矛盾） | 摘要表 "| ⭐⭐⭐ | 1 | 3 |" + 关键变化 "F3 (Search/filter) 降为 3 星"；行 3 却写 4 星 | rewritten（对齐为 ⭐⭐⭐，证据格同步对齐时点叙述） |
| 行 3: "searchHistory 已通过 AutoCompleteBox 绑定——不再是无 UI 死数据" | STALE（与该文档自身时点叙述矛盾） | 现码: LauncherControlBar.axaml:55 ItemsSource="{Binding SearchHistoryItems}"（现值已渲染）; 但文档「关键变化」时点称未渲染死数据 | rewritten（对齐时点叙述；台账注现值已绑定 AutoCompleteBox，MinimulumPopulateDelay=150ms 已存在） |
| 行 6: `LauncherPresetFiles.Validate` 三阶段校验 | STALE | 类型不存在；现为 Core/LauncherPresetValidator.cs | rewritten |
| 行 7: `MainWindowViewModel.Presets*.cs` CRUD + locked preset + rollback | VERIFIED | MainWindowViewModel.Presets.cs 存在; LauncherPresetViewModel.IsLocked=preset.Locked; PresetService/PresetManager 存在 | kept |
| 行 8: `LauncherPresetSelectionPreview` 纯函数 Matched/Missing/Extra | VERIFIED | LauncherPresetSelectionPreview.cs: MatchedCount/MissingCount/ExtraCount/MissingConfigIds | kept |
| 行 9: 复用 `PreflightBatch`（预演=实际同路径） | VERIFIED | LauncherLaunchWorkflow.cs:48 PreflightBatch; LaunchOrchestrator.cs:205/420 | kept |
| 行 10: 7 步统一模式（DiscardDrag→Dialog→Validate→Snapshot→Mutate→SaveChain→StatusText） | VERIFIED（快照语义） | MainWindowViewModel 命令链 + LauncherEntryDeleteTarget/LauncherEntryDrafts 快照目标类型存在 | kept |
| 行 11: `customEntryOrder` 单一 owner; lazy preview; context-menu 共用模型 | VERIFIED | config settings.customEntryOrder; LauncherEntriesView.axaml 上移排序/下移排序; LauncherDragReorderWorkflow.cs | kept |
| 行 12: "4 mode 契约" | STALE | LauncherEntryQuery.cs 现 5 模式（Recent/Updated/Alphabetical/Custom/Frequent; NormalizeSortModeId 白名单含 frequent）; LayoutManager:30 "使用频率" | rewritten（5 mode） |
| 行 13: 单一 `CurrentEntryViewMode` 驱动 3 只读投影 | VERIFIED | LayoutManager.cs CurrentEntryViewMode; LauncherEntryViewMode.cs cards/details/names | kept |
| 行 14: `LauncherEntryLayoutCalculator` 纯函数; `_layoutDensity` enum; `FastWrapPanel` | VERIFIED | Core/LauncherEntryLayoutCalculator.cs; LauncherEntryViewModel.Layout.cs enum; Views/FastWrapPanel.cs | kept |
| 行 15: per-entry confirm 已拒绝; 全局开关 | VERIFIED | settings 无 per-entry confirm 键; requireConfirmationForLaunches 存在; LaunchPlan.RequiresConfirmation | kept |
| 行 16: `LauncherSettings` POCO 单源; "31 个设置测试" | VERIFIED（类型）; UNVERIFIABLE（测试数） | Core/LauncherSettings.cs 存在; 测试数未跑 dotnet 无法复核（协议铁律 6） | kept（31 为时点断言，台账标注未复核） |
| 行 17: temp-file→backup rotation→File.Move; 3 代备份; staged rollback 跨 config/preset/settings | STALE（部分） | FileLauncherJsonIO.cs: File.Replace（非 File.Move）、.bak、DefaultMaxBackups=3 ✓; 快照类 2026-07-21 删除，回滚现为 StoreGrain.Dispatch save-first-then-commit 单文件 | rewritten（File.Replace + save-first-then-commit） |
| 行 18: Core 零 Avalonia 依赖; 542 tests | VERIFIED（架构）; HISTORICAL（测试数） | ArchitectureRules.cs R5 等架构规则; 542 为时点记录 | kept |
| 行 19: `build-release.ps1` SHA256 receipt; verify.ps1 16 checks + GUI smoke | VERIFIED（SHA256）; HISTORICAL（16 checks） | tools/build-release.ps1 含 sha256; 现值 38 checks | kept |
| 行 20: retired/ 归档; root gate 阻止 4 个旧路径复活 | VERIFIED | Add-Check: retired-paths-removed/winforms-retired-design/shortcut-backup-retirement/hta-retired-config; retired/ 存在 | kept |
| 行 21: 12 基准→10 本地验收规则 | VERIFIED | design-benchmark-digest.md: 12 benchmark inputs + 10 acceptance rules（逐条点数） | kept |
| 行 21: "12 工具对标↔launcher.json presets 映射" | VERIFIED（广义翻译表） | launcher-first-class-patterns.md:32 "The 12 borrowed design patterns" 翻译表（Docker Compose Profiles↔presets 等） | rewritten（按实表述翻译表） |
| 行 21: "xUnit 锁定文档结构" | UNVERIFIABLE | tests 全树 Select-String digest/benchmark → 0 hits（无测试引用 digest 文档） | rewritten（改为 xUnit 预设套件 + 门禁 presets-schema/security-audit-f2） |
| 行 22: `LauncherCommandTemplateResolver` 纯函数; unknown key fail-fast; $key 预览 | VERIFIED | Core/LauncherCommandTemplateResolver.cs:36 throw InvalidDataException; TemplatePrefix="$" | kept |
| 行 22: "零专用单元测试"（关键变化叙述） | HISTORICAL | 现存在 LauncherCommandTemplateResolverTests.cs | kept（时点叙述；台账注现值已有测试） |
| 行 23: `LauncherDesignTokens.cs` 66 行 | STALE | 现值 93 行 | rewritten（93） |
| 行 23: `App.axaml.cs` 注入→XAML {StaticResource} | VERIFIED | App.axaml.cs:33-38 InjectDesignTokens（PrimaryBrush 等） | kept |
| 行 23: 合约测试禁止硬编码 | VERIFIED | tests/UiContract/VisualTokenContractTests.cs 存在 | kept |
| 关键变化 F11: "LauncherDragReorderWorkflow 只有构造函数测试" | HISTORICAL | 现存在 LauncherDragReorderWorkflowTests.cs + LauncherDragReorderSessionTests.cs | kept（时点叙述；台账注现值已有隔离测试） |
| 无 config/(config|presets|settings).json 残留引用 | VERIFIED | `[regex]::Matches($t,'(?<!retired[/\\])config[/\\](?:config|presets|settings)\.json').Count` → 0（改前改后均 0） | kept |
| 行尾 LF | VERIFIED | CRLF 行数 → 0 | kept |

## 未能验证 / 边界说明

- 测试数量类断言（542 tests、31 个设置测试、1 个专项测试、0 条 Clone 测试、0 条模板测试）未跑 dotnet 复核（协议铁律 6）——统一按 HISTORICAL 时点记录保留，台账标注现值证据（LauncherCommandTemplateResolverTests.cs、LauncherDragReorderWorkflowTests.cs 等已存在）。
- 星级与「关键变化」「当前结论」是 2026-06-10 审计判断，非事实声明，保留（仅修复行 3 与文档自身摘要的矛盾）。
