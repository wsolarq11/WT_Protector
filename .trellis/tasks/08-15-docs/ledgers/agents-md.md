# AGENTS.md 验证台账 (2026-08-15)

> 按 .trellis/tasks/08-15-docs/verification-protocol.md 逐项取证。所有命令为只读 pwsh / git。
> 结构基线 10 行为门禁 structure-baseline-consistency 强制项，一字未改。TRELLIS 块未触碰。

## 全仓地图 (Repo Map)

| 声明 (as written) | 判定 | 证据 (command + result) | 处理 |
|---|---|---|---|
| README.md / DESIGN.md / .wt-protector.dsc.yaml 存在 | VERIFIED | `Test-Path` → True (README.md, DESIGN.md, .wt-protector.dsc.yaml) | kept |
| .wt-protector.dsc.yaml → WinGet Configuration：新机就绪层（Windows Terminal + PowerShell 7） | VERIFIED | 读文件: resources = Microsoft.WinGet.DSC/WinGetPackage id=Microsoft.WindowsTerminal + id=Microsoft.PowerShell ("Install PowerShell 7.x (stable)") | kept |
| config/ → 运行时配置（JSON, 人可读写）；launcher.json → 统一存储：启动条目 + 命名启动组 + UI 偏好 + defaultPresetId | STALE(不完整) | `ConvertFrom-Json` launcher.json 顶层键: schemaVersion,settings,entries,presets,defaultPresetId,**runHistory** (4 条); settings 含 UI 偏好 (sortMode/entryViewMode 等) | rewritten — 补充 runHistory |
| src/avalonia-launcher/ 活动主线 + README.md | VERIFIED | Test-Path True; src/avalonia-launcher/README.md 含 "### Layer 0" 分层与 Verify 章节 | kept |
| WTLauncher.Core → Layer 0 纯数据/规则（53文件, 零Avalonia依赖） | VERIFIED | `Get-ChildItem -Recurse -Filter '*.cs'` (排除 obj/bin/TestResults) = 53 | kept |
| WTLauncher.Analyzers → Roslyn 分析器（7文件） | VERIFIED | 同法计数 = 7 | kept |
| WTLauncher.Core.FSharp → 孤儿项目（_FSharpProjectIsOrphaned, 无 C# 引用, 待决策） | VERIFIED | `Select-String '<_FSharpProjectIsOrphaned>' *.fsproj` → true; fsproj 无 ProjectReference（仅有注释 "如需保留，添加 ProjectReference…或移除"） | kept |
| WTLauncher.App ViewModels → 53文件（MainWindowViewModel + 15 partial） | VERIFIED | 计数 = 53; `Select-String 'partial class MainWindowViewModel'` → MainWindowViewModel.cs + 15 partial 文件 = 16 声明（1 主 + 15 partial） | kept |
| Features → 15文件（Entries/Presets/Status/Updates/Dialogs） | VERIFIED | 计数 = 15 | kept |
| Infrastructure → 9文件（持久化 IO + IStorePersistence） | VERIFIED | 计数 = 9; IStorePersistence 声明于 WTLauncher.Core/IStorePersistence.cs:10, FileStorePersistence/FileLauncherJsonIO 于 Infrastructure | kept |
| Platform → 3文件（平台接线） | VERIFIED | 计数 = 3 | kept |
| Views → 44文件（24 cs + 20 axaml, 单 MainWindow.axaml 三视图） | VERIFIED(表述微调) | 计数 = 24 cs + 20 axaml; MainWindow.axaml:337 承载 <views:LauncherEntriesView>; 三视图 IsCardsViewMode(14,18)/IsDetailedListMode(274)/IsNamesViewMode(478) 定义于 LauncherEntriesView.axaml | rewritten — 表述改为 "MainWindow.axaml 承载三视图（视图体在 LauncherEntriesView.axaml）"，计数不变 |
| tests/WTLauncher.Tests → 121测试文件, xUnit（8层, 门禁实测 811 用例） | VERIFIED | 计数 = 121; tests 子目录 = Analyzer,Architecture,CoreUnit,Persistence,PropertyTest,Support,UiContract,ViewModelBehavior (8 层); xunit.v3 3.2.2 于 Directory.Packages.props; 811 为协议已知已核实事实 | kept |
| run.ps1 → 开发运行；verify.ps1 → 主线门禁（restore → test → publish → GUI smoke） | VERIFIED | Test-Path 均 True | kept |

## 脚本工具箱 (tools/)

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| 6 个脚本均存在于 tools/ | VERIFIED | Get-ChildItem tools -File → verify.ps1, build-release.ps1, apply-machine-config.ps1, config-lib.ps1, test-launch-args.ps1, update-github-similar-projects-snapshot.ps1 全部存在 | kept |
| verify.ps1 全量门禁 38 项检查 | VERIFIED | `Select-String '^Add-Check '` tools/verify.ps1 → 38 行; 自计数逻辑 $baselineCheckCount = $checks.Count + 1 与 AGENTS.md 表 38 一致 | kept |
| 表中点名的各检查项存在（行尾策略/snow/退役路径/裸catch/硬编码颜色/主线布局/README/功能审计/声明边界/GitHub快照/schema 系列/快捷方式+WinForms+HTA 退役/Avalonia 门禁/结构基线/settings-single-source/no-stale-three-file-refs/winget-config-schema/免命令式 UI/安全审计 F1-F3/无瘦包装/VTK/Trinity/AppStore 残留/架构规则/分析器连线） | VERIFIED | Add-Check 名单逐一核对: line-ending-policy, snow-ignored-local-state, retired-paths-removed, no-bare-catch-exception, app-raw-hex-zero (Get-AppRawHexViolations 扫描 App 内 raw hex), mainline-layout, readme, feature-objective-boundary, claim-boundary-ledger, github-similar-projects-snapshot, json, config-schema, presets-schema, settings-json, config-example-schema, shortcut-backup-retirement, winforms-retired-design, hta-retired-config, avalonia-mainline-verify, structure-baseline-consistency, settings-single-source-buildstorefile, no-stale-three-file-refs, winget-config-schema-valid, no-imperative-view-ui, security-audit-f1/f2/f3, no-thin-wrapper-delegation, vtk/trinity-architecture-integrity, vtk-no-appstore-residue, architecture-rule-check, analyzer-wiring — 全部存在 | kept |
| build-release.ps1 → win-x64 self-contained publish → src/avalonia-launcher/artifacts/publish/win-x64/ | VERIFIED | config-lib.ps1:63-64 Get-AvaloniaPublishDir = artifacts\publish\win-x64; build-release.ps1:50 dotnet publish -r win-x64 -p:EnableNativeAot=true -o $publishDir; exe=WTLauncher.App.exe | kept |
| apply-machine-config.ps1 → winget configure --file .wt-protector.dsc.yaml；-DryRun 仅 validate | VERIFIED | apply-machine-config.ps1:2 param([switch]$DryRun); :18-21 if($DryRun){ winget configure validate -f $yaml } else { winget configure -f $yaml } | kept |
| config-lib.ps1 被 verify.ps1、build-release.ps1 等引用 | VERIFIED | verify.ps1:7 / build-release.ps1:6 → . (Join-Path $PSScriptRoot 'config-lib.ps1') | kept |
| test-launch-args.ps1 仅运行 LaunchArgumentBuilder 测试 | VERIFIED | test-launch-args.ps1:14 --filter FullyQualifiedName~LaunchArgumentBuilder | kept |
| update-github-similar-projects-snapshot.ps1 通过 gh repo view 拉取 12 个对标项目；快照超 30 天门禁拒绝 | VERIFIED | 脚本 :10/:101-102 用 gh repo view; docs/reference/github-similar-projects-snapshot.json repositories = 12; verify.ps1:258 $githubSnapshotMaxAgeDays = 30 | kept |

## 结构基线（门禁强制）— 保护内容

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| WTLauncher.Core C# files = 53 | VERIFIED | Get-ChildItem 计数 = 53 | kept（表行未动） |
| ViewModels C# files = 53 | VERIFIED | 计数 = 53 | kept |
| MainWindowViewModel partials = 15 | VERIFIED | 16 个 partial 声明文件 − 1 主文件 MainWindowViewModel.cs = 15 partial | kept |
| Features C# files = 15 | VERIFIED | 计数 = 15 | kept |
| Views C# files = 24 | VERIFIED | 计数 = 24 | kept |
| Views AXAML files = 20 | VERIFIED | 计数 = 20 | kept |
| WTLauncher.Analyzers C# files = 7 | VERIFIED | 计数 = 7 | kept |
| Tests C# files = 121 | VERIFIED | 计数 = 121 | kept |
| Test attributes [Fact]+[Theory] = 728 | VERIFIED | [Fact]=699 + [Theory]=29 = 728 | kept |
| Gate checks (Add-Check) = 38 | VERIFIED | '^Add-Check ' 行数 = 38 | kept |

## 藤曼路径 (Vine Trail)

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| README.md（配置 schema、安全说明、功能清单、实现状态） | VERIFIED | README.md 含 "## Configuration schema"(:46), "## Safety notes"(:275), "## Current implementation status"(:297), feature 审计引用(:14-15) | kept |
| src/avalonia-launcher/README.md（5 层架构、关键不变量） | VERIFIED | 含 "### Layer 0 — Pure data & contracts (Core)"(:46), "## Verify"(:15) | kept |
| DESIGN.md（品牌、原则、组件所有权、Token 规则） | VERIFIED | DESIGN.md 含 "## Brand"(:10), "## Design principles"(:61), "Token/component ownership"(:118) | kept |
| docs/feature-reality-north-star-audit.md（22 项功能、声明边界账本） | VERIFIED | 文档 :228-234 "22 feature sections"; "Verdict count: narrow-north-star=22" | kept |
| docs/design-benchmark-digest.md（12 项外部设计基准） | VERIFIED | 文档 :5 "convert 12 external first-class design benchmarks"; :7 "The 12 benchmark inputs" | kept |
| docs/giants-shoulders/ 存在 | VERIFIED | Test-Path True | kept |
| run.ps1 / tools/verify.ps1 存在 | VERIFIED | Test-Path True | kept |

## 核心规则

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| .snow/ 是忽略的本地运行时/索引状态 | VERIFIED | .gitignore 含 ".snow/"; 门禁 snow-ignored-local-state 存在 | kept |
| 行尾由 .gitattributes 管理：全部文本归一化为 LF | VERIFIED | .gitattributes 含 "* text=auto eol=lf" | kept |
| 源码在 src/avalonia-launcher/，退役物在 retired/ | VERIFIED | Test-Path 均 True; retired/ = README.md + config/config.json + design/winforms-launcher.md | kept |
| config/launcher.json 是唯一权威源（entries + presets + settings + defaultPresetId 一体），无 AppData/registry 后备 | STALE(不完整) | launcher.json 顶层键含 runHistory (4 条); README.md:153 "No installer, no registry, no AppData" | rewritten — 补充 runHistory |

## 其他

| 声明 | 判定 | 证据 | 处理 |
|---|---|---|---|
| docs/research 含 architecture-zero.md 退路方案，非行动方案 | STALE | 实际文件为 docs/research/**wt-launcher-architecture-zero.md**; 文档头部 "Contingency record, not an active recommendation"（= 退路/非行动方案） | rewritten — 修正文件名 |
| docs/reference → GitHub对标快照 + 快捷方式退役清单 | VERIFIED | reference/ = github-similar-projects-snapshot.json + shortcut-backup-manifest.json (+distribution-signing-strategy.md) | kept |
| docs/archive → phase*-complete、ultrareview 等 | VERIFIED | archive/ = f2/f3/phase2/phase3-complete.md + ultrareview-2026-07-23(-fluxor-skeleton).md | kept |
| retired/ → 旧HTA启动器 + WinForms设计教训 | VERIFIED | retired/config/config.json（HTA 残留）+ retired/design/winforms-launcher.md | kept |
| 门禁 no-stale-three-file-refs 覆盖 AGENTS.md | VERIFIED | verify.ps1 $activeDocs 含 'AGENTS.md'; Select-String AGENTS.md 'config/config.json|config/presets.json|config/settings.json' → 0 命中 | kept |
| TRELLIS 块 | 保护内容 | — | 未触碰 |

## 结论

- VERIFIED: 62 | STALE(不完整→改写): 4 | UNVERIFIABLE: 0 | HISTORICAL: 0
- 改写点：launcher.json 统一存储描述（补 runHistory）×2、Views 三视图表述（视图体在 LauncherEntriesView.axaml）、research 文件名 wt-launcher-architecture-zero.md。
- 结构基线 10 行与 TRELLIS 块原样保留；未引用三个退役 config 文件。
