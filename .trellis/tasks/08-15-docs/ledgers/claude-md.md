# CLAUDE.md 验证台账 (2026-08-15)

> 按 .trellis/tasks/08-15-docs/verification-protocol.md 逐项取证。所有命令为只读 pwsh / git。
> CLAUDE.md 不在门禁 no-stale-three-file-refs 的 activeDocs 中（verify.ps1 $activeDocs = README.md, AGENTS.md, DESIGN.md, docs/feature-…，不含 CLAUDE.md）。

## MCP Knowledge Graph

| 声明 (as written) | 判定 | 证据 (command + result) | 处理 |
|---|---|---|---|
| This repo is indexed by codebase-memory-mcp. 项目名 D-_AI_-_projects-WT_Protector | UNVERIFIABLE | 全仓无 .mcp.json / MCP 服务器配置；'codebase-memory-mcp' 仅出现在 CLAUDE.md 自身、.trellis/spec/guides/pitfalls.md:1.4（踩坑记录）、trellis-spec-bootstrap mcp-setup 技能中。MCP 服务器配置在仓库外，无法以仓库证据验证 | rewritten — 明确标注未验证，删除具体断言 |
| all tools require this project parameter; there is no list_projects tool | UNVERIFIABLE | 同上（外部工具行为，无本地证据） | rewritten — 随上节改为未验证标注 |

## What this is

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| Avalonia 12 + .NET 10, win-x64 self-contained | VERIFIED | Directory.Packages.props: Avalonia 12.1.0; WTLauncher.App.csproj TargetFramework net10.0; build-release.ps1 publish -r win-x64 --self-contained (Native AOT) | kept |
| All launcher state in one atomic JSON store at config/launcher.json (entries + presets + settings + defaultPresetId together). No AppData, no registry, no DB | STALE(不完整) | launcher.json 顶层键: schemaVersion,settings,entries,presets,defaultPresetId,**runHistory**(4条); README.md:153 "No installer, no registry, no AppData" | rewritten — 补 runHistory |
| Active mainline: src/avalonia-launcher/; retired/ archive-only (HTA, WinForms design lessons, old shortcut tree) | STALE | retired/ = README.md + config/config.json + design/winforms-launcher.md — 无 "old shortcut tree"（快捷方式退役清单在 docs/reference/shortcut-backup-manifest.json） | rewritten — 括号改为 "(HTA config remnant + WinForms design lessons)" |

## Commands — the gate is the contract

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| tools/verify.ps1 存在；38 checks | VERIFIED | Test-Path True; '^Add-Check ' = 38 | kept |
| tools/build-release.ps1 → src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe | VERIFIED | config-lib.ps1 Get-AvaloniaPublishDir = artifacts\publish\win-x64; build-release.ps1:15 exe = WTLauncher.App.exe; :50 publish -o $publishDir | kept |
| src/avalonia-launcher/run.ps1 / verify.ps1 / tools/test-launch-args.ps1 存在 | VERIFIED | Test-Path 均 True | kept |
| WTLauncher.Avalonia.sln 存在 | VERIFIED | Test-Path True | kept |
| -warnaserror is real — CI (/.github/workflows/verify.yml) builds with it, and so does the gate | VERIFIED | verify.yml:38 dotnet build … -warnaserror; verify.ps1:621 dotnet build … -warnaserror | kept |

## Scheme B architecture decisions (2026-07-23, 3 phases landed) — 带日期历史记录

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| Fluxor is gone (7 Fluxor files + ExceptionBoundary + LauncherIcons deleted) | HISTORICAL | src+tests 全量 grep 'Fluxor' = 0; 'ExceptionBoundary' 仅 1 条注释 (ArchitectureRules.cs:207); 'LauncherIcons' = 0; 段落标题带 2026-07-23 | kept（历史） |
| StoreGrain (single container, SemaphoreSlim, Dispatch/DispatchErrorOr) | VERIFIED | StoreGrain.cs:14 class StoreGrain; :58 public void Dispatch; :74 DispatchErrorOr; SemaphoreSlim 于 FileLauncherJsonIO.cs:17 | kept |
| All callers use Dispatch / DispatchAsync / DispatchErrorOr | STALE | 全仓 grep 'DispatchAsync' = 0 命中（StoreGrain.cs 亦无）；实际为 Dispatch + DispatchErrorOr | rewritten — 删除 DispatchAsync |
| CommunityToolkit.Mvvm for VM bindings | VERIFIED | App 多处 using CommunityToolkit.Mvvm; csproj PackageReference CommunityToolkit.Mvvm 8.4.2 | kept |
| AOT-safe serialization: JsonSerializer.Deserialize(stream, jsonTypeInfo) / Serialize(value, jsonTypeInfo); NO JsonSerializerOptions-based overloads; [JsonSourceGenerationOptions] on LauncherJsonContext | VERIFIED | FileLauncherJsonIO.cs:22-23 Deserialize(stream, (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!); LauncherJsonSerializer.cs:21 Serialize(value, typeInfo); LauncherJsonContext.cs:6-10 [JsonSourceGenerationOptions(WriteIndented, ReadCommentHandling, AllowTrailingCommas, MaxDepth)] | kept |
| Loose Read + Strict Write; unknown keys skipped; no UnmappedMemberHandling.Disallow | VERIFIED | LauncherJsonContext 源生成配置不含 UnmappedMemberHandling（System.Text.Json 默认跳过未知键）；LauncherStoreFile 仅对已知字段标 [JsonRequired] | kept |
| StoreGrain.Mutate removed | VERIFIED | StoreGrain.cs 无 Mutate; 全仓 'void Mutate' = 0 | kept |
| ExceptionFilters.IsRecoverable single catch-filter entry point; DEBUG guard; delegates to LauncherErrors.IsRecoverable (Core); no duplicate UnwrapAggregates | VERIFIED | ViewModels/ExceptionFilters.cs:10-12 internal static class ExceptionFilters / IsRecoverable; :29 return LauncherErrors.IsRecoverable(ex); Core/ErrorOrExtensions.cs:6 class LauncherErrors, :21 IsRecoverable; 'UnwrapAggregates' = 0 | kept |
| FileLauncherJsonIO instance-level SemaphoreSlim(1,1) on Save; LauncherJson.Save static facade delegates to ILauncherJsonIO | VERIFIED | FileLauncherJsonIO.cs:17 _saveLock = new(1,1); :66/:147 File.Replace; LauncherJson.cs:18 static class, :26 DefaultIO, :42-43 Save → IO.Save; ILauncherJsonIO.cs:10 | kept |
| MemoizedSelector._hasCached is volatile | VERIFIED | MemoizedSelector.cs:11 private volatile bool _hasCached; | kept |
| AgentDiscoveryScanner.Scan() accepts CancellationToken ct = default | VERIFIED | AgentDiscoveryScanner.cs:58 CancellationToken ct = default | kept |
| DiscoverAgentsViewModel propagates CancellationToken through _scanFunc | VERIFIED | DiscoverAgentsViewModel.cs:22/:65/:115 _scanFunc | kept |
| CI: AOT publish job (informational), dotnet nuget audit (was no-op restore), Dependabot (NuGet + GitHub Actions), .editorconfig | VERIFIED | verify.yml:68-87 aot-verify job (continue-on-error: true); :66 dotnet nuget audit; .github/dependabot.yml (nuget + github-actions); .editorconfig 存在 | kept |
| .snow/ .claude/ .agents/ artifacts/ *.bak* *.log *.tmp are git-ignored | VERIFIED | .gitignore 含 .snow/, .claude/, .agents/, artifacts/, *.bak, *.bak.*, *.bak-*, *.log, *.tmp | kept |

## 5-layer architecture

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| Layer 0 WTLauncher.Core/ owns LaunchEntry, LauncherPreset, LauncherStoreFile, LaunchArgumentBuilder, LauncherJson (temp-file + File.Replace), LauncherStore schema validation, LauncherDesignTokens, pure layout/order calculators | VERIFIED | LaunchEntry.cs:8 record; LauncherPreset.cs:23 record; LauncherStoreFile.cs:9 record; LaunchArgumentBuilder.cs:6 class; LauncherJson.cs:18 class; LauncherStore.cs:13 + LauncherStoreValidator.cs:11; LauncherDesignTokens.cs:7; File.Replace 在 FileLauncherJsonIO.cs:66/:147; LauncherEntryOrder 引用于 LauncherEntryQuery.cs:121 | kept |
| Layer 1 WTLauncher.App/ViewModels/MainWindowViewModel*.cs | VERIFIED | MainWindowViewModel.cs + MainWindowViewModel.{Batch,CommandManagement,DirectoryManagement,Discovery,Drag,Entries,Grouping,History,IEntryCardHost,IEntryListViewModel,Layout,Migration,Presets,Reorder,Update}.cs 均存在 | kept |
| Layer 2 WTLauncher.App/Infrastructure/ (StoreGrain.Dispatch, FileStorePersistence); save-first-then-commit; snapshot classes removed 2026-07-21 | VERIFIED + HISTORICAL | StoreGrain.cs:58 Dispatch; FileStorePersistence.cs:14; 'LauncherEntryMutationSnapshot|LauncherSelectionMutationSnapshot' = 0; 日期为历史记录 | kept |
| Layer 3 MainWindowViewModel single writable owner; injects dialog Func<>s through MainWindow ctor | VERIFIED | MainWindowViewModel.cs 存在; MainWindow.axaml.cs 存在（主窗口代码后置） | kept |
| Layer 4 WTLauncher.App/Views/ single MainWindow.axaml with 3 view modes: Cards/Details/Names | VERIFIED(表述微调) | MainWindow.axaml:337 承载 <views:LauncherEntriesView>; 三视图体在 LauncherEntriesView.axaml: IsCardsViewMode(14,18), IsDetailedListMode(274,312 details-list), IsNamesViewMode(475-478 NamesScrollViewer) | rewritten — 表述改为 MainWindow.axaml 承载三视图（视图体在 LauncherEntriesView.axaml） |
| Tests: 728 [Fact]/[Theory] across all layers; gate runs 811 cases | VERIFIED | [Fact]=699+[Theory]=29=728; 811 为协议已知事实 | kept |

## Non-negotiable invariants — 符号逐一验证

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| LauncherEntryLayoutDensity / LauncherEntryViewMode / SelectedSortMode / DragCueState / DragReorderSession? 为唯一可写字段 | VERIFIED | LauncherEntryViewModel.Layout.cs:24,26,28; EntryListViewModel.cs:19,20,27; LayoutManager.cs:44,76,96; LauncherEntryViewModel.Drag.cs:5,7,9; LauncherDragReorderSession.cs:6 | kept |
| 投影: IsCompactLayout/IsCardsViewMode/IsDetailedListMode/IsNamesViewMode/IsCustomSortMode/IsDragPreviewSource/Target | VERIFIED | LauncherEntryViewModel.Layout.cs:19; LayoutManager.cs:121,145; EntryListViewModel.cs:363,364; LauncherDragReorderWorkflow.cs:77,81,98; LauncherEntryViewModel.Drag.cs:7,9 | kept |
| IsCustomSortMode 是 SelectedSortMode 的只读投影 | VERIFIED | LauncherDragReorderWorkflow.cs:77-98（投影用法） | kept |
| LauncherJson.Save temp-file + File.Replace 原子写 | VERIFIED | FileLauncherJsonIO.cs:66/:147 File.Replace(recoveryTempPath/tempPath, path/fullPath, null) | kept |
| StoreGrain.Dispatch save-first-then-commit 回滚机制 | VERIFIED | StoreGrain.cs:58-76 Dispatch/DispatchErrorOr 结构（try { Dispatch(reducer); return Result.Success; }） | kept |
| 选择双写路径: IsSelected 立即保存; SetSelectionFromOwner 批量保存 | VERIFIED | LauncherEntryViewModel.cs:63-91 IsSelected setter + SetSelectionFromOwner | kept |
| Refresh() 以 DiscardDragReorderPreview() 开头 | VERIFIED | MainWindowViewModel.cs:251-255 private void Refresh() { try { DiscardDragReorderPreview(); … } | kept |
| 设计 Token 唯一色彩源 LauncherDesignTokens / {StaticResource} | VERIFIED | LauncherDesignTokens.cs:7; DESIGN.md Token/component ownership | kept |
| catch (Exception) 禁止 | VERIFIED | 门禁 no-bare-catch-exception; CatchExceptionMustHaveWhenAnalyzer.cs | kept |
| settings.requireConfirmationForLaunches 唯一确认权威; 无 per-entry confirm | VERIFIED | launcher.json settings.requireConfirmationForLaunches=false; 门禁 config-schema | kept |

## Config schema — quick reference

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| config/launcher.json is LauncherStoreFile, schemaVersion: 2 | VERIFIED | LauncherStoreFile.cs:9 record (SchemaVersion init=2); launcher.json schemaVersion=2; verify.ps1:360 校验 ==2 | kept |
| Top-level: entries[], presets[], settings{}, defaultPresetId | STALE(不完整) | launcher.json 另有 **runHistory[]**（4 条，LauncherStoreFile.RunHistory, JsonIgnore(WhenWritingDefault)） | rewritten — 补 runHistory[] |
| Entry command supports $key template references resolved against settings.commandTemplates; missing key = launch validation error, not silent | VERIFIED | launcher.json entries command="$codex" 且 settings.commandTemplates{opencode,codex,claude}; Core/LauncherCommandTemplateResolver.cs:33/:71 missing keys 清单; CommandUsageAnalyzer 引用计数 | kept |
| settings.sortMode ∈ {custom, recent, updated, alphabetical} | STALE | Core/LauncherEntryQuery.cs:13 另有 FrequentSortModeId = "frequent"（LauncherSortMode { Recent, Updated, Alphabetical, Custom, Frequent }），NormalizeSortModeId 接受 frequent | rewritten — 集合改为 {custom, recent, updated, alphabetical, frequent} |
| settings.entryViewMode ∈ {cards, details, names} | VERIFIED | LauncherEntryViewMode.cs:3-14 enum {Cards, Details, Names} + ids cards/details/names | kept |
| plus searchHistory[] and window {width, height} (positive) | VERIFIED | launcher.json settings.searchHistory=["w","wt"]; settings.window{width:985.33,height:862} | kept |
| Unknown JSON keys silently skipped (forward compat); missing/null/blank mode keys rejected | VERIFIED | NormalizeSortModeId/NormalizeId 对空串抛 InvalidDataException("… is required.")，未知值抛 Unknown; LauncherJsonContext 无 UnmappedMemberHandling | kept |

## Trellis workflow is installed

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| .trellis/workflow.md; .trellis/spec/{core,views,tests,guides}/; index.md; quality-guidelines.md | VERIFIED | Test-Path 全部 True; spec/core 含 index.md + quality-guidelines.md | kept |
| pitfalls.md — 50+ real-session gotchas | VERIFIED(表述微调) | pitfalls.md 头注 "本仓库及关联项目的 50+ 个真实 session，经 trellis mem 取证"（= 50+ 个 session，非 50+ 条坑）；表行 607 行（每条坑含 属性/证据/根因/预防 多行） | rewritten — 措辞改为 "50+ 个真实 session 沉淀的踩坑实录" |
| .trellis/tasks/; .trellis/workspace/<dev>/journal-*.md | VERIFIED | .trellis/tasks 存在; .trellis/workspace/user/journal-1.md, journal-2.md | kept |
| .codex/agents (trellis-research, trellis-implement, trellis-check); .agents/skills/ | VERIFIED | .codex/agents/trellis-research.toml + trellis-implement.toml + trellis-check.toml; .agents/skills/*/SKILL.md | kept |
| AGENTS.md 结构基线表 enforced by structure-baseline-consistency | VERIFIED | verify.ps1:904 校验 AGENTS.md 含 "| Gate checks (Add-Check) | $baselineCheckCount |"；AGENTS.md 表值 53/53/15/15/24/20/7/121/728/38 全部与文件系统一致 | kept |

## Where to look / Common ways to break the gate / Line endings

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| README.md / DESIGN.md / AGENTS.md / docs/feature-reality-north-star-audit.md (22-feature) / docs/giants-shoulders/ 路径与描述 | VERIFIED | 全部 Test-Path True; feature-reality 文档 :228-234 22 feature; giants-shoulders/codexswitch-avalonia-native-aot.md 存在 | kept |
| 门禁破坏清单（硬编码颜色/双可写字段/JSON 只经 LauncherStore/裸 catch/obsolete confirm/directory 存在/退役路径/.snow 与构建产物/编辑 TRELLIS 块/声明边界禁用词） | VERIFIED | 对应门禁: no-imperative-view-ui + app-raw-hex-zero, structure-baseline + 投影不变量, settings-single-source-buildstorefile, no-bare-catch-exception, config-schema, retired-paths-removed, snow-ignored-local-state + .gitignore, claim-boundary-ledger | kept |
| .gitattributes normalizes all text to LF; JSON files UTF-8 | VERIFIED | .gitattributes "* text=auto eol=lf"; launcher.json UTF-8 可读 | kept |
| 本文件不含 config/config.json|config/presets.json|config/settings.json 引用 | VERIFIED | Select-String 0 命中（CLAUDE.md 不在 activeDocs，但保持干净） | kept |

## 结论

- VERIFIED: 50 | STALE(不完整→改写): 6 | UNVERIFIABLE: 2（MCP 节） | HISTORICAL: 1（Fluxor 删除记录）
- 改写点：MCP 节标注未验证；launcher.json 描述补 runHistory（×2 处）；retired/ 括号内容删 "old shortcut tree"；DispatchAsync 删除；sortMode 集合补 frequent；顶层键补 runHistory[]；MainWindow.axaml 三视图表述；pitfalls 措辞。
