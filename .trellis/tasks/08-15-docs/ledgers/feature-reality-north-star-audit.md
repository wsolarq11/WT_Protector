# 验证台账: docs/feature-reality-north-star-audit.md

> 验证日期: 2026-08-15（只读 pwsh，未运行 dotnet/build/verify.ps1）
> 文档日期: Reviewed on 2026-06-24（门禁保护: 22 headings / 22 verdict lines / 4 allowed + 5 forbidden / ledger 顺序 / not-complete-as-written）

## 结构基线（与 AGENTS.md 基线表逐项比对，全部 VERIFIED）

| 声明 | 判定 | 证据（命令 + 结果, 截断） | 处理 |
|------|------|---------------------------|------|
| 文档审计 22 项功能映射（22 个 `### N.` 章节） | VERIFIED | `[regex]::Matches($fa,'(?m)^### \d+\. ').Count` → 22 | kept |
| 22 行 `- Verdict level: narrow-north-star` | VERIFIED | `[regex]::Matches($fa,'(?m)^- Verdict level: narrow-north-star$').Count` → 22 | kept |
| `## Claim boundary ledger` 在 `## Final answer` 之前 | VERIFIED | IndexOf ledger=51370, final=54802, 顺序正确 | kept |
| 账本内 exactly 4 行 `- Claim status: allowed` / exactly 5 行 forbidden | VERIFIED | 正则计数 → allowed=4, forbidden=5 | kept |
| `Full objective result: not-complete-as-written` 等 6 个受保护字符串 | VERIFIED | `$fa.Contains(...)` → 全部 True | kept |
| 无 config/(config|presets|settings).json 残留引用 | VERIFIED | `[regex]::Matches($fa,'(?<!retired[/\\])config[/\\](?:config|presets|settings)\.json').Count` → 0 | kept |
| 行尾 LF | VERIFIED | CRLF 行数 → 0 | kept |
| 结构数字引用基线（Core 53 / VM 53 / partials 15 / Features 15 / Views 24cs+20axaml / Analyzers 7 / Tests 121 / 728 attrs / 38 checks） | VERIFIED | Get-ChildItem 计数 → 53/53/15/15/24/20/7/121; attrs=728(699 Fact+29 Theory); Add-Check=38 | kept（未在文档硬编码漂移计数） |

## 逐功能映射核验

| 声明（as written） | 判定 | 证据（命令 + 结果, 截断） | 处理 |
|------|------|---------------------------|------|
| S1: entry rows 含 id/name/directory/command/selected/optional updatedAtUtc | VERIFIED（子集） | config/launcher.json entries[0] 字段 = id,name,directory,command,selected,updatedAtUtc,launchedAtUtc,launchCount | rewritten（补充 launchedAtUtc/launchCount） |
| S1: `LaunchArgumentBuilder.Build` 生成 wt.exe launch plan | VERIFIED | Select-String 'LaunchArgumentBuilder' → Core/LaunchArgumentBuilder.cs; 'LaunchPlan' → Core/LaunchPlan.cs | kept |
| S2: verify.ps1 拒绝 directory 不存在的活动行 | VERIFIED | verify.ps1:371 `throw "Config item $idx directory does not exist"` | kept |
| S3: 搜索框占位称 搜索名称、目录或命令; 过滤跨三字段 | VERIFIED | LauncherControlBar.axaml:53 PlaceholderText="搜索名称、目录或命令"; LauncherEntryQuery.MatchesFilter 匹配 Name/Directory/Command | kept |
| S3: 数据 owner 为 in-memory FilterText（保存时落到 launcher.json settings） | VERIFIED | settings 键含 lastFilterText; FilterText 5 个文件引用 | kept |
| S4: 诚实 `已请求启动` 措辞 + 单次启动回执 | VERIFIED | LauncherLaunchReceiptText.cs:30 `已请求启动: {entryName}` | kept |
| S5: `配置保存失败` 状态 + 内存/文件回滚 | VERIFIED | LauncherStatusText.cs:148 `Failed("配置保存失败", exception)` | kept |
| S5/S9: `LauncherLaunchWorkflow.PreflightBatch` 预演=实际同路径 | VERIFIED | LauncherLaunchWorkflow.cs:48 public LauncherBatchLaunchPreflight PreflightBatch(...) | kept |
| S6: presets schema 由 tools/verify.ps1 校验 | VERIFIED | Add-Check 列表含 'presets-schema' | kept |
| S7: `MainWindowViewModel.Presets*.cs` CRUD + locked preset | VERIFIED | 存在 MainWindowViewModel.Presets.cs; LauncherPresetViewModel.IsLocked = preset.Locked（blank-manual locked=true） | kept |
| S8: `LauncherPresetSelectionPreview` 纯函数 Matched/Missing/Extra | VERIFIED | LauncherPresetSelectionPreview.cs: MatchedCount/MissingCount/ExtraCount/MissingConfigIds | kept |
| S9: `PreviewSelectedPresetCommand` 复用 PreflightBatch; `LauncherBatchLaunchPlanText` | VERIFIED | MainWindowViewModel.Presets.cs:196; LauncherBatchLaunchPlanText 4 文件引用 | kept |
| S10: add/edit/delete entry CRUD + dialog-boundary 失败不改文件 | VERIFIED | EntryService/EntryListViewModel/MainWindowViewModel 命令 + 测试文件（MainWindowViewModelTests.*） | kept |
| S11: `LauncherDragReorderWorkflow`; settings.customEntryOrder; 上移/下移排序 | VERIFIED | LauncherDragReorderWorkflow.cs; config settings.customEntryOrder; LauncherEntriesView.axaml:117/121 上移排序/下移排序 | kept |
| S12: 三种排序模式（custom/updated/alphabetic） | STALE | LauncherEntryQuery.cs: `enum LauncherSortMode { Recent, Updated, Alphabetical, Custom, Frequent }` + NormalizeSortModeId 白名单含 frequent; LayoutManager:30 "使用频率" → 现为 5 种模式 | rewritten（改 5 模式含 frequent frecency） |
| S13: 代码暴露 cards/details/names; XAML 三个根 | VERIFIED | LauncherEntryViewMode.cs:12-14 CardsId/DetailsId/NamesId; LauncherEntriesView.axaml IsCardsViewMode(14,18)/IsDetailedListMode(274)/IsNamesViewMode(478) | kept |
| S14: `LauncherEntryLayoutDensity` Normal/Compact/Micro | VERIFIED | LauncherEntryViewModel.Layout.cs:122-126 enum { Normal, Compact, Micro } | kept |
| S15: settings.requireConfirmationForLaunches; `LaunchPlan.RequiresConfirmation` | VERIFIED | config settings 键含 requireConfirmationForLaunches; LaunchPlan.cs:8 | kept |
| S16: 未用 theme 状态已移除 | VERIFIED | settings 键无 theme | kept |
| S17: 数据 owner "config/preset/settings stores plus mutation snapshots" | STALE | 快照类 LauncherEntryMutationSnapshot/LauncherSelectionMutationSnapshot 已删（2026-07-21）; 当前: StoreGrain.Dispatch save-first-then-commit（StoreGrain.cs:10-11）+ FileLauncherJsonIO（.bak, DefaultMaxBackups=3, File.Replace）; 全 src 无 Snapshot 命名类 | rewritten |
| S17: `.bak` 备份 + temp 写入 | VERIFIED | FileLauncherJsonIO.cs:59-66 .bak 恢复 + File.Replace; :16 DefaultMaxBackups=3 | kept（并入重写） |
| S18: 启动参数拒绝 null + 未知 + 重复 --config | STALE（部分） | LauncherStartupArguments.cs: null 拒绝 ✓; 重复 --config 拒绝 ✓; 未知 `--` 参数收集为 warning（unknownArgs 返回，非拒绝）; 非 `--` 未知参数抛异常 | rewritten（按实描述） |
| S18: config 路径解析拒绝缺失搜索根 | VERIFIED | FileStorePersistence.cs:134-144 RequireSearchRoot 抛出 "currentDirectory and appBaseDirectory are null or empty" | kept |
| S19: 数据 owner tools/verify.ps1 + avalonia verify.ps1 + publish 产物 | VERIFIED | Test-Path 两脚本 → True; Add-Check 'avalonia-mainline-verify' 存在 | kept |
| S20: root gate 拒绝旧 WinForms/spike/根 exe 复活 | VERIFIED | Add-Check: retired-paths-removed / winforms-retired-design / shortcut-backup-retirement / hta-retired-config | kept |
| S21: "tests keep the digest referenced" | STALE | tests 全树 Select-String 'design-benchmark-digest|digest|benchmark' → 0 hits（无测试引用 digest） | removed（重写） |
| S21: "verify the giants-shoulders admission rule and current entries" | UNVERIFIABLE | verify.ps1 仅 :633 activeDocs 列表引用 launcher-first-class-patterns.md，无 admission 检查 | replaced（改为 activeDocs 事实） |
| S21: 12 工具模式表 + "what not to copy" 边界 | VERIFIED | launcher-first-class-patterns.md:32 "## The 12 borrowed design patterns"; :53 "## What we intentionally did not copy" | kept |
| S21: root verify 校验快照 schema/freshness/代表项目/broad-market | VERIFIED | verify.ps1 github-similar-projects-snapshot 检查体：12 代表仓库、broad-market-stronger=2、Flow/Wox stars>10000、summary 一致性 | kept |
| S22: 数据 owner 路径 `src/WTLauncher.Core/AgentCatalog.cs` 等 | STALE | Test-Path 'src/WTLauncher.Core/...' 不存在；实际: src/avalonia-launcher/src/WTLauncher.Core/{AgentCatalog,AgentDiscoveryScanner}.cs; Views/DiscoverAgentsWindow.axaml(+.cs)（无 DiscoverAgentsWindow.cs）; ViewModels/DiscoverAgentsViewModel.cs + MainWindowViewModel.Discovery.cs | rewritten |
| S22: 8-agent CLI 市场 | VERIFIED | AgentCatalog.cs ExecName ×8: claude/codex/cursor/antigravity/gemini/opencode/aider/qwen-code | kept |
| S22: ArchUnitNET R5 豁免（AgentDiscoveryScanner 等仅 File.Exists 读） | VERIFIED | ArchitectureRules.cs:99-123 R5 豁免 LauncherJson/LaunchArgumentBuilder/LauncherStore/AgentDiscoveryScanner | kept |
| S22: "snapshot + atomic-save + rollback pipeline" | STALE | 快照类已删（2026-07-21）; 现为 StoreGrain.Dispatch save-first-then-commit | rewritten |
| 摘要: Proof count repo-battle-tested=20, market-informed=4 | STALE | 22 行 Proof type 实计: repo-battle-tested=21, market-informed=5, legacy=1, accessibility=1, source-backed=1 | rewritten（21/5/1/1/1） |
| 摘要: Gap count future-workload=8 | STALE | 22 行 Gap class 实计: future-workload=9, none-proven=5, external-process-limit=4, visual-proof=3, distribution=1, source-freshness=1 | rewritten（9） |
| 客观审计: "22 sections cover every active feature mapping documented in README" | STALE | README「Current implementation status」另文档化 quick-launch hotkey/tray/export-import/run-history/update-check/installer/clone/brand badges（不属于 22 章） | rewritten（如实标注未分类面） |
| GitHub 快照 capturedAtUtc: 2026-06-09T15:47:55Z | HISTORICAL | 文档 2026-06-24 记录当时快照；当前快照 capturedAtUtc=2026-08-14T11:53:15Z | kept（带日期）；台账注明现值 |

## 未能验证 / 边界说明

- 各章节内对测试行为的细节断言（如 "tests cover ... rejection"）未逐条跑测试复核；判定依据为对应测试文件存在（LauncherEntryQueryTests/FrecencyTests、LauncherDragReorderWorkflowTests/SessionTests、LauncherPresetSelectionPreviewTests、MainWindowViewModelTests.* 等）+ 源码类型存在。凡与源码矛盾处已改（S12/S17/S18/S21/S22）。
- 未运行 dotnet/build/verify.ps1（协议铁律 6）；门禁最终由主代理统一跑。
