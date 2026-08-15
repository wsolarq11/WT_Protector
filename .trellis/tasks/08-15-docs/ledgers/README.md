# 验证台账 — README.md（仓库根）

> 依据 `.trellis/tasks/08-15-docs/verification-protocol.md`（2026-08-15）逐项查证。
> 判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL。仅只读 pwsh 证据，未运行构建/门禁。

## What this project contains

| 声明（原文摘录） | 判定 | 证据（命令 + 结果，截断） | 处理 |
|---|---|---|---|
| `src/avalonia-launcher/` 为活动主线 | VERIFIED | `Test-Path 'src/avalonia-launcher'` → True | kept |
| `config/launcher.example.json` — 机器无关模板（零条目、locked blank-manual preset） | VERIFIED | Test-Path True；读文件：`entries: []`、`presets: [{id:"blank-manual",locked:true}]`、`defaultPresetId:"blank-manual"` | kept |
| 活动 `config/launcher.json` 是 git-ignored 运行时状态（2026-08-02 脱敏：含本地绝对路径） | UNVERIFIABLE（日期部分） | `.gitignore` 含 `config/launcher.json`（VERIFIED）；`git log --all -- config/launcher.json` 无历史、`git log --grep='desensit' -i` 无记录 | rewritten：保留 git-ignored 事实，删除无法证实的 2026-08-02 脱敏日期断言 |
| `docs/使用说明.txt` — 朋友向快速上手（三步、零术语）；打进朋友 zip | VERIFIED（经 glob 机制；无字面量引用） | Test-Path True；`tools/package-friend.ps1` L22-24 用 `docs/*.txt` glob（要求恰好一个）取文件并 L49 拷入 bundle；文件含「一、第一次使用」三步与「更多信息→仍要运行」。主代理交叉核查：`Select-String 使用说明` 于 tools/*.ps1、.github/workflows/*.yml、src/avalonia-launcher/*.ps1 → 0 命中——符合脚本设计：L20-21 注释明示「Windows PowerShell 5.1 以 ANSI 读取 no-BOM UTF-8 脚本会损坏非 ASCII 字面量」，故故意用 glob 而非中文字面量。docs/ 当前恰有一个 .txt（= 使用说明.txt），故 zip 中文件名成立；仅当 docs/ 出现第二个 .txt 时脚本会抛错 | rewritten（README 改为描述 glob 机制，不再暗示字面量引用） |
| `retired/` — HTA 快照 2026-07-06（commit `825ecb5b`）移除，config 与 WinForms 设计教训保留 | VERIFIED | `Test-Path 'retired/WT Launcher.hta'` → False；`git log -1 --format='%h %cs %s' 825ecb5b` → `825ecb5b 2026-07-06 refactor: ponytail-audit execution …`；`retired/config/config.json` True、`retired/design/winforms-launcher.md` True | kept |
| `retired/config/config.json` 为退役 HTA 本地配置（读取它的 HTA 已消失） | VERIFIED | Test-Path True；src 中无 retired 引用（`Select-String 'retired' src/**/*.cs` 仅 `AgentCatalog.cs:65` 显示名 "Gemini CLI (retired)"） | kept |
| `retired/design/winforms-launcher.md` 退役 WinForms 设计教训 | VERIFIED | Test-Path True；`tools/verify.ps1` `Add-Check 'winforms-retired-design' @{ path = 'retired\\design\\winforms-launcher.md' }` | kept |
| `docs/reference/shortcut-backup-manifest.json` — 已删除 `WT_Shortcuts_Backup/` 二叉树的可读替代 | VERIFIED | Test-Path True；清单头 `"sourceDirectory": "WT_Shortcuts_Backup"`；`verify.ps1` `Add-Check 'shortcut-backup-retirement'`（`sourceDirectoryRemoved = -not (Test-Path … WT_Shortcuts_Backup)`）；Test-Path `WT_Shortcuts_Backup` → False | kept |
| `docs/feature-reality-north-star-audit.md`、`docs/feature-first-class-upgrade-audit.md` 存在 | VERIFIED | 两者 Test-Path True | kept |
| `.snow/` — 忽略的本地运行时/索引状态；Git 与验证「不追踪、不校验」 | STALE（后半句） | 本地 Test-Path False；`.gitignore` 含 `^\.snow/$`；`verify.ps1` L46-59 有 `snow-ignored-local-state` 检查（断言被忽略、`git ls-files -- .snow` 为空）——门禁确实校验忽略策略 | rewritten：改为「门禁仅断言其保持被忽略，从不校验其内容」 |
| `.gitattributes` — LF 行尾策略 | VERIFIED | 文件含 `* text=auto eol=lf`；`verify.ps1` `Add-Check 'line-ending-policy' @{ text = 'lf' }` | kept |
| `.wt-protector.dsc.yaml` — WinGet Configuration（Windows Terminal + PowerShell 7） | VERIFIED | Test-Path True；文件含 `configurationVersion: 0.2.0`、`id: Microsoft.WindowsTerminal`、`id: Microsoft.PowerShell`、OsVersion `MinVersion: "10.0.17763"`；`verify.ps1` `winget-config-schema-valid` 要求同四个 marker | kept |
| `tools/verify.ps1` 无交互本地验证门禁 | VERIFIED | Test-Path True；`Select-String "Add-Check '" tools/verify.ps1` → 38 项 | kept |
| `tools/config-lib.ps1` 共享函数库 | VERIFIED | Test-Path True；被 verify/build-release/avalonia verify 等 dot-source（`src/avalonia-launcher/verify.ps1` L7） | kept |
| `tools/apply-machine-config.ps1` 薄包装（`-DryRun` 仅 validate） | VERIFIED | Test-Path True；README 用法与脚本一致（仅存在性验证） | kept |
| `docs/reference/github-similar-projects-snapshot.json` 当前市场证据 | VERIFIED | Test-Path True；`verify.ps1` L253-258：schemaVersion=3、capturedAtUtc 必需、30 天过期拒绝 | kept |
| `tools/update-github-similar-projects-snapshot.ps1` 通过 `gh repo view` 刷新；门禁拒绝过期快照 | VERIFIED | 脚本 L101 `& gh repo view $fullName --json $fields`；`verify.ps1` L258 `$githubSnapshotMaxAgeDays = 30` | kept |
| `docs/giants-shoulders/` — 外部项目教训与来源链接 | VERIFIED | 目录存在，含 `launcher-first-class-patterns.md`、`README.md`、`codexswitch-avalonia-native-aot.md` | kept |
| 旧根 `WT Launcher.exe`、旧 `src/winforms-launcher/`、旧 `spikes/` 已退役；原 Avalonia spike 即现主线 | VERIFIED | 三者 Test-Path 均 False；`src/avalonia-launcher` True；`verify.ps1` `retired-paths-removed` 检查同名路径 | kept |

## Architecture 表与关键决策

| 声明 | 判定 | 证据（截断） | 处理 |
|---|---|---|---|
| 5 层架构，Core = `WTLauncher.Core`（数据记录、`LaunchArgumentBuilder`、`LauncherJson`、`LauncherStore` schema 校验、`AgentDiscoveryScanner`、设计 token） | VERIFIED | `Select-String` 于 src：`record LaunchEntry`(LaunchEntry.cs:8)、`record LauncherStoreFile`、`class LaunchArgumentBuilder`、`class LauncherJson`(LauncherJson.cs:18)、`class LauncherStoreValidator`、`class AgentDiscoveryScanner`(Core:17)、`class LauncherDesignTokens`、`class LauncherStore`（facade） | kept |
| Commands = `MainWindowViewModel` + partials 经 `StoreGrain` 处理增删改启动 | VERIFIED | `class MainWindowViewModel` 存在；`StoreGrain.cs` 存在；partial 文件（CommandManagement/DirectoryManagement/Entries/Presets 等）存在 | kept |
| Persistence = `StoreGrain` 单一状态容器、SemaphoreSlim 线程安全、保存失败回滚 | STALE（SemaphoreSlim） | `StoreGrain : Grain<LauncherStoreFile>`；`Grain.cs` 用 `lock (_lock)`（非 SemaphoreSlim）串行化；`Dispatch` 先持久化后提交 → 回滚语义成立 | rewritten：改为「Grain 锁串行化」 |
| 状态管理：`Dispatch`/`DispatchAsync`/`DispatchErrorOr` 变更 + `Select`/`SelectAsync` 读取 | STALE | `Select-String 'DispatchAsync' src` → 0 命中；`SelectAsync` → 0 命中；`StoreGrain.cs` 实有 `Dispatch`(L58)、`DispatchErrorOr`(L74)、`Select<T>`(L44) | rewritten：删 DispatchAsync/SelectAsync |
| CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` | VERIFIED | `CommunityToolkit.Mvvm` 引用 37 处（`using CommunityToolkit.Mvvm.ComponentModel`/`.Input`），LayoutManager 等使用 | kept |
| Fluxor 于 2026-07-23 移除 | STALE（日期） | Scheme B 实现提交 `0c0b8771 2026-07-24 feat: implement Scheme B architecture across all 3 phases` 删除 LayoutActions/LayoutReducers/LayoutState/Store\\Launcher{Effects,Reducers,State}.cs + `ExceptionBoundary.cs`、`LauncherIcons.cs`（git show --stat）；src 中 Fluxor 引用 0 | rewritten：日期改 2026-07-24（commit 0c0b8771） |
| 单一 `config/launcher.json`；temp-file + `File.Replace` 原子写 | VERIFIED | `FileLauncherJsonIO.Save`（L109-155）：`.tmp-{guid}` → `File.Replace`/`File.Move` | kept |
| Loose Read（未知键静默跳过）+ Strict Write（源生成仅写已知字段）、AOT 安全 `LauncherJsonContext` | VERIFIED | `LauncherJsonContext.cs` `[JsonSourceGenerationOptions(WriteIndented=false, ReadCommentHandling=Skip, AllowTrailingCommas=true, MaxDepth=64)]`（无 UnmappedMemberHandling.Disallow → 默认跳过未知键）；`FileLauncherJsonIO` 用 `JsonSerializer.Deserialize(stream, (JsonTypeInfo<T>)…)` | kept |
| 异常处理：`catch when (ExceptionFilters.IsRecoverable(ex))`；`ErrorOr<T>`；无裸 catch | VERIFIED | `ExceptionFilters.IsRecoverable`（ViewModels\\ExceptionFilters.cs:12，委托 Core `LauncherErrors.IsRecoverable`）；StoreGrain/FileStorePersistence 用 `ErrorOr`；`verify.ps1` `no-bare-catch-exception` | kept |
| 并发：`FileLauncherJsonIO` Save 实例级 SemaphoreSlim；`MemoizedSelector._hasCached` volatile；`AgentDiscoveryScanner.Scan()` 接受 CancellationToken | VERIFIED | `FileLauncherJsonIO.cs:17 _saveLock = new SemaphoreSlim(1,1)`；`MemoizedSelector.cs:11 private volatile bool _hasCached`；`AgentDiscoveryScanner.cs:55-58 Scan(…, CancellationToken ct = default)` | kept（StoreGrain 部分同前改写） |

## Configuration schema

| 声明 | 判定 | 证据（截断） | 处理 |
|---|---|---|---|
| `LauncherStoreFile` schema，`schemaVersion: 2`；顶层 entries/presets/settings/defaultPresetId | VERIFIED | `LauncherStoreFile.cs`：`schemaVersion`=2、`settings`、`entries`、`presets`、`defaultPresetId`（均 JsonRequired）；另有 `runHistory`（WhenWritingDefault 忽略） | kept + 补充 runHistory |
| 条目字段 id/name/directory/command/selected/updatedAtUtc/launchedAtUtc | VERIFIED | `LaunchEntry.cs` 全部 JsonPropertyName 命中（另含可选 `launchCount`） | kept |
| `command` 经 `pwsh -NoExit -EncodedCommand`（base64 防注入） | VERIFIED | `LaunchArgumentBuilder.cs`：`ShellCommandPrefix = "-- pwsh -NoExit -EncodedCommand"`、UTF-16LE base64（L70） | kept |
| `directory` 作为 `wt.exe -d` 工作目录 | VERIFIED | `LaunchArgumentBuilder.WorkingDirectoryOption = "-d"` | kept |
| `updatedAtUtc` 用于「更新时间」排序、`launchedAtUtc` 用于「最近启动」排序 | VERIFIED | `LaunchEntry` 注释 + `LauncherEntryQuery` Recent/Updated 分支；`RecordLaunchFromOwner` 写启动时间戳 | kept |
| 无逐条目 `confirm` 键；确认唯一权威 `settings.requireConfirmationForLaunches` | VERIFIED | `LauncherSettings.RequireConfirmationForLaunches`（JsonRequired）；LaunchEntry 无 confirm | kept |
| 未知 JSON 键加载时静默跳过（前向兼容）；源生成只写已知字段 | VERIFIED | 见上 LauncherJsonContext | kept |
| `settings.window.width/height` 必须为正；门禁拒绝非正几何；运行时 Math.Max 钳制 | VERIFIED | `verify.ps1` L425-426 `Assert-PositiveJsonNumber $settings.window.width/height`；`App.axaml.cs` L174-175 `Math.Max(windowSettings.Width, LauncherDesignTokens.WindowMinWidth)` | kept |
| 示例 JSON 中 settings 含 `schemaVersion` | STALE | `LauncherSettings` 无 schemaVersion 属性（注释明确「reintroducing settings.schemaVersion fails loudly」）；实际 settings 键：lastFilterText/sortMode/entryViewMode/cardsGroupKey/requireConfirmationForLaunches/customEntryOrder/commandTemplates/searchHistory/backgroundMode/window | rewritten：示例块对齐实际 example 文件键 |
| 示例 settings 显示 quickLaunchHotkey/runInTray/updateManifestUrl 具体值 | STALE（值非 example 文件内容） | example 文件 settings 不含这三键（可选键，见 `LauncherSettings` L49-58）；README 示例块为虚构值 | rewritten：示例块用 example 文件实值，三键移入「额外字段」清单 |
| presets 字段 id/name/entryIds/locked | VERIFIED | `LauncherPreset.cs` 四属性均 JsonPropertyName；example 中 blank-manual `locked:true`；`verify.ps1` L397 强制 blank-manual 保持 locked | kept |
| `locked` — 系统组（如 blank-manual）UI 防护，可应用不可更新/删除 | VERIFIED | `PresetManager.CanUpdatePreset/CanDeletePreset`（locked 排除）；gate L397 | kept |
| `defaultPresetId`（顶层）启动 UI 选择 | VERIFIED | `LauncherStoreFile.DefaultPresetId` JsonRequired；gate L386/L414 必填且须匹配某 preset | kept |
| `sortMode` ∈ {custom, recent, updated, alphabetical} | STALE | `LauncherEntryQuery.cs` 另有 `FrequentSortModeId = "frequent"`；`LayoutManager.cs` L30 `new(…FrequentSortModeId, "使用频率")`（UI 第 5 项） | rewritten：补 frequent |
| `entryViewMode` ∈ {cards, details, names} | VERIFIED | `LauncherEntryViewMode.cs` CardsId/DetailsId/NamesId | kept |
| `customEntryOrder` — 自定义拖拽持久化 | VERIFIED | `LauncherSettings.CustomEntryOrder`；gate L427-434 引用校验 | kept |
| `commandTemplates` — `$key` 解析，缺失键=启动校验错误 | VERIFIED | `LauncherCommandTemplateResolver`（缺失抛 `InvalidDataException "Command template 'key' is not defined."`）；`ValidateTemplateReferences` | kept |
| `searchHistory` — 去重过滤词列表；「验证数组不得为 null」 | UNVERIFIABLE（后半句） | `LauncherSearchHistory`（去重、`MaximumTerms = 20`）；AutoCompleteBox（`LauncherControlBar.axaml:51` `ItemsSource="{Binding SearchHistoryItems}"`）；未发现显式 null 校验点 | rewritten：保留去重/上限/AutoCompleteBox，删「不得为 null」断言 |
| `quickLaunchHotkey` — Ctrl/Alt/Shift/Win + Space/A-Z/0-9 | VERIFIED | `LauncherHotkey.cs` Parse：ModControl/ModAlt/ModShift/ModWin + SPACE/A-Z/0-9（L47-50）；DefaultText "Ctrl+Alt+Space" | kept |
| `runInTray` — 关闭主窗口藏入托盘 | VERIFIED | `LauncherSettings.RunInTray`（默认 true）；`Win32TrayIcon` + App.axaml.cs WireQuickLaunchAndTray | kept |
| `updateManifestUrl` — 检查更新读取，绝不改写 launcher.json | VERIFIED | `UpdateCheckService`；`build-release.ps1` L79-80「never rewrites config/launcher.json」；LauncherStoreFile 无更新清单字段 | kept |

## Friend handoff / Run the mainline app / Fresh-machine setup

| 声明 | 判定 | 证据（截断） | 处理 |
|---|---|---|---|
| 朋友 zip 由 build-release.ps1 + package-friend.ps1 两步构建 → `artifacts/friend/WTLauncher-<version>.zip` | VERIFIED | 两脚本存在；package-friend 输出 `src\\avalonia-launcher\\artifacts\\friend\\WTLauncher-$version.zip`（L52） | kept |
| zip 含 AOT exe、`使用说明.txt`、`config/launcher.example.json` | VERIFIED | package-friend L46-50：拷 publish 目录文件（除 receipt.json）+ docs 唯一 txt + example 到 `config\\` | kept |
| build-release.ps1 为 Native AOT 发布 | VERIFIED | L50 `dotnet publish … -p:EnableNativeAot=true … -o $publishDir` | kept |
| 解压后双击 exe 以空 store 启动，首启欢迎面板引导添加首个工作区 | VERIFIED | `StoreGrain.LoadInitialState` 文件缺失 → `new LauncherStoreFile()`；`LauncherEntriesView.axaml` L611/L620 空状态文本 + `添加第一个工作区` 按钮 | kept |
| exe 未签名；快速上手文档记载「更多信息 → 仍要运行」 | VERIFIED | `docs/使用说明.txt` L14-15 原文命中 | kept |
| 仅接受一个可选 `--config <path>`；未知 `--` 参数静默忽略；重复/缺值/位置参数被拒 | VERIFIED | `LauncherStartupArguments.ReadConfigPath`：`--config` 与 `--config=` 支持；重复抛「at most once」；缺值抛「required」；非 `--` 抛「Unknown startup argument」；未知 `--` 收集为 unknownArgs（App.axaml.cs:123 未进一步使用） | kept（措辞微调：未知参数收集为警告） |
| 应用读写 `config/launcher.json` 统一存储，不用 AppData/注册表 | VERIFIED | `FileStorePersistence.ResolveStorePath`：显式路径→向上找 README.md marker→cwd `config\\launcher.json`；无 AppData/registry | kept |
| `.wt-protector.dsc.yaml`：OsVersion 断言 Win10 1809+；WinGetPackage 装 WindowsTerminal + PowerShell；opencode/aider 注释掉 | VERIFIED | 文件 L18-23（`MinVersion: "10.0.17763"`）、L25-41、L48-66 注释块 | kept |
| irm\|iex 分发的 agent CLI（claude/codex/cursor/antigravity/gemini/qwen-code）非 winget 包，经「发现」导入 | VERIFIED | DSC 文件 L68-72 注释原文一致 | kept |

## Build and verification / Testing

| 声明 | 判定 | 证据（截断） | 处理 |
|---|---|---|---|
| 根门禁 `tools/verify.ps1` | VERIFIED | Test-Path True；38 项 `Add-Check` | kept |
| 检查类别逐项（repo hygiene、mainline layout、documentation、market evidence、launcher.json contract、schema invariants、retirement hygiene、machine setup、Avalonia mainline） | VERIFIED | `Add-Check` 清单含 line-ending-policy、snow-ignored-local-state、retired-paths-removed、mainline-layout、readme、feature-objective-boundary（22/22 + not-complete-as-written）、claim-boundary-ledger、github-similar-projects-snapshot、json/config-schema/presets-schema/settings-json、settings-single-source-buildstorefile、no-stale-three-file-refs、shortcut-backup-retirement、winforms-retired-design、hta-retired-config、winget-config-schema-valid、avalonia-mainline-verify 等 | kept |
| README 检查 = 6 个章节标题 + 4 条客观边界短语 | VERIFIED | `verify.ps1` L158-170（章节数组与 4 短语逐字校验） | kept（保护内容原样保留） |
| 门禁末行 JSON 回执含 schemaVersion/kind/status/repoHead/worktreeDirty/worktreeStatus | VERIFIED | `verify.ps1` L573-586 | kept |
| 规范分发物 `src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe`；`verify-publish` 为隔离验证输出 | VERIFIED | `config-lib.ps1` 路径函数；`src/avalonia-launcher/verify.ps1` L13 `artifacts\\verify-publish\\win-x64` | kept |
| build-release.ps1 在 exe 已运行时停止并报告 PID | VERIFIED | L21-24 `Get-CimInstance Win32_Process … throw "…(pid(s): …)"` | kept |
| 测试：单一 `WTLauncher.Tests` 工程、物理层级、`[Trait("Category", …)]`、规范地图 `docs/test-structure.md` | VERIFIED | 目录存在；`Trait("Category"` 命中 110；`docs/test-structure.md` 8 层级表；`verify.ps1` `test-structure-layering` | kept |
| `tools/test-fast.ps1` = CoreUnit+PropertyTest+Architecture+Analyzer | VERIFIED | L8 `Category=CoreUnit|Category=PropertyTest|Category=Architecture|Category=Analyzer` | kept |
| `tools/test-coverage.ps1`、`tools/smoke-critical-journey.ps1` 存在；冒烟用 `-Exe` 参数 | VERIFIED | 两文件存在；`src/avalonia-launcher/verify.ps1` L105 `& powershell … smoke-critical-journey.ps1 -Exe $exe -ConfigPath $smokeStore -WorkingDirectory $smokeRoot` | kept |
| CI：`build-and-test` 构建+快速车道；`full-gate` 单一完整 verify/publish | VERIFIED | `.github/workflows/verify.yml`：build-and-test（restore/build/test-fast）、full-gate（`.\\tools\\verify.ps1`）、另有 nuget-audit、aot-verify(continue-on-error) | kept |

## WinForms retirement / Safety notes / Current implementation status

| 声明 | 判定 | 证据（截断） | 处理 |
|---|---|---|---|
| 旧根可执行已删；`src/winforms-launcher/Program.cs` 已删；教训存 `retired/design/winforms-launcher.md` | VERIFIED | `WT Launcher.exe`、`src/winforms-launcher` Test-Path False；design md True | kept |
| 批量启动预检全部目标后才确认/启动；「若任一计划无效则完全不启动」 | STALE | `LaunchOrchestrator.LaunchBatchAsync`（L205-217）：PreflightBatch 失败项被剔除（`PreflightBatch` catch 后不加入 items）；`ConfirmBatchLaunchWindow` 文案「仍有 X 个有效…请确认实际 wt.exe」→ 有效项仍可启动 | rewritten：无效计划被剔除且永不启动，有效项经确认后照常启动 |
| 外部启动失败后继续尝试剩余项（continue-on-failure） | VERIFIED | `LauncherLaunchWorkflow.StartBatch`（L95-100）`catch … IsRecoverable → RecordFailure; continue` | kept |
| 单/批启动写「启动回执」；「已请求启动」仅指进程启动已请求 | VERIFIED | `LauncherLaunchReceiptText.cs` 多处；`已请求启动` 命中 7；`LauncherRunHistory.cs` 注释「Requested only means the process start request reached」 | kept |
| 批回执显示 requested/failed/skipped | VERIFIED | `LauncherRunStatus` 枚举 Requested/Failed/Skipped/Canceled；`LauncherBatchLaunchReceiptText.Format(preflight, report)` | kept |
| 「预演当前组」运行当前组预检并显示 wt.exe 命令、不启动不重写勾选 | STALE（标签） | 实际 UI 标签为「预演」（`LauncherControlBar.axaml:87` `Content="预演"` → `PreviewSelectedPresetCommand`）；`PreviewSelectedPreset`（Presets.cs L274-299）预检 + FailedPreflight/Format 显示 | rewritten：标签改为「预演」 |
| 应用 preset 只重写勾选状态；「启动当前组」直接启动所选 preset 条目、不先重写勾选 | STALE（标签） | 实际标签「启动」（`LauncherControlBar.axaml:89-90` → `LaunchSelectedPresetCommand`）；`ApplyPreset`（Presets.cs L242-255）仅 `ReplaceSelection`；`LaunchSelectedPresetAsync`（L258-271）直接 `LaunchBatchAsync` | rewritten：标签改「启动」 |
| 应用/预演/启动 preset 前先丢弃未提交拖拽预览 | VERIFIED | `ApplyPreset` L244、`PreviewSelectedPreset` L286 `DiscardDragReorderPreview()`；批启动 `LaunchOrchestrator` L196 `ReorderDiscarder()` | kept |
| JSON 保存经临时文件、正常保存留 `.bak`；多文件条目编辑失败回滚 | STALE（「多文件」） | `FileLauncherJsonIO.Save` temp + .bak 旋转；回滚 = `StoreGrain.Dispatch` 先持久化后提交（无多文件语义，快照类已删 2026-07-21） | rewritten：改为 StoreGrain save-first-then-commit 回滚描述 |
| 勾选/preset 应用保存失败显示「配置保存失败」并回滚 | VERIFIED | `配置保存失败` 命中 1；`StoreGrain.DispatchErrorOr` → `LauncherErrors.ConfigSaveFailed`；`ApplyStatusText(saveFailure)` | kept |
| 删除当前启动组后显式选择下一个组并在状态栏报告 | VERIFIED | `LauncherStatusText.cs` L272「已删除启动组: …；当前启动组: …」；`DeleteSelectedPresetAsync` 用 `FallbackPresetId`（Presets.cs L437） | kept |
| 使用应用会弄脏「被跟踪的」config/launcher.json | STALE | `config/launcher.json` 在 `.gitignore` 中（未跟踪）；应用只写本地运行时文件 | rewritten：应用写本地 launcher.json（git-ignored），不会弄脏已跟踪文件 |
| `retired/config/config.json` 是退役 HTA 配置（HTA 2026-07-06 移除），Avalonia 主线从不读取 | VERIFIED | Test-Path；src 无 retired/config 引用（见上）；HTA 提交 825ecb5b（2026-07-06） | kept |
| 支持多词分词搜索（空格分隔关键词全匹配）+ 直接点卡片启动 | VERIFIED | `LauncherEntryQuery` L54-60 tokens 全匹配；`LaunchAsync` 卡片命令 | kept |
| `launchedAtUtc` 每次成功启动后记录供「最近启动」排序 | VERIFIED | `LaunchOrchestrator` L239 `entryVm?.RecordLaunchFromOwner(timestamp)` | kept |
| 自适应卡片尺寸（收缩适配窗口，避免第二滚动条） | VERIFIED | `LauncherEntryLayoutCalculator`（可用区域计算）；`CardLayoutProvider` | kept |
| 自定义拖拽：跟随光标 ghost + 实时重排预览 | VERIFIED | `MainWindow.Drag.cs` ShowDragGhost/MoveDragGhost/HideDragGhost；`LauncherDragReorderWorkflow`（实时预览、提交前保存） | kept |
| 右键「上移排序/下移排序」；有拖拽预览时先丢弃再按最后提交顺序解析 | VERIFIED | `LauncherEntriesView.axaml` L117-122/336-341/508-509「上移排序/下移排序」MenuItem | kept |
| 三种密度（Normal/Compact/Micro） | VERIFIED | `LauncherEntryViewModel.Layout.cs` `LauncherEntryLayoutDensity.Normal/Compact/Micro` | kept |
| Cards/Details(详细列表)/Names(名称墙) 视图切换 | VERIFIED | `LauncherEntryViewMode.cs` + `LayoutManager` L33-38（卡片/详细列表/名称墙） | kept |
| 头部动作：全选可见 / 清空已保存选择 | VERIFIED | `LauncherControlBar.axaml` L29-34 全选可见/清空选择；`SelectVisibleCommand`/`ClearSelectionCommand` | kept |
| 应用内增/删/改/克隆；右键「复制副本」深拷贝 | STALE（标签） | 上下文菜单 Header 为「复制」绑定 `CloneCommand`（`LauncherEntriesView.axaml` L125-128 等） | rewritten：标签改「复制」 |
| AI Agent 自动发现（claude/codex/cursor/antigravity/gemini/opencode/aider/qwen-code）、已添加置灰去重 | VERIFIED | `AgentCatalog.cs` 8 个 ExecName 全命中；`DiscoverAgentsViewModel` `IsAlreadyAdded` 置灰 + 导入时过滤（L142/L196） | kept |
| 每 agent 品牌徽章（近似品牌色 + 2 字母字形） | VERIFIED | `DiscoverAgentsWindow.axaml.cs` L206-223 Border+BrandGlyph+BrandColorHex；`LauncherDesignTokens.BrandBadge*` | kept |
| 命名启动组/preset 显式应用替换勾选；一等公民动作（保存为新组/更新/删除可编辑组） | VERIFIED | `LauncherControlBar.axaml` L144-153 三个 MenuItem + 命令；`PresetManager` | kept |
| 直接启动当前组、不先重写勾选 | VERIFIED | `LaunchSelectedPresetAsync`（见上） | kept |
| 应用前实时差异反馈；「已匹配时禁用 no-op apply」 | STALE（后半句） | `CanApplyPreset` 注释：「Always allow apply … even if the current selection exactly matches — re-applying also forces a config save」；按钮文案 `LauncherPresetSelectionPreview` 匹配时显示「已匹配」但不禁用 | rewritten：改为「按钮显示已匹配但仍可应用（重复应用也强制保存）」 |
| 四种持久化排序模式 | STALE | `LayoutManager` L24-31 有 5 项：自定义拖拽/最近启动/更新时间/首字母顺序/使用频率 | rewritten：五种，补「使用频率」 |
| 命令模板 + 未知键在启动计划构建期拒绝 | VERIFIED | 见 commandTemplates 行 | kept |
| 全局启动确认 | VERIFIED | `RequireConfirmationForLaunches` 开关（LauncherControlBar ToggleSwitch） | kept |
| 全局快速启动热键 + 独立覆盖窗口（输入过滤/Enter 启动/Esc 关闭） | VERIFIED | `QuickLaunchWindow.axaml.cs` L33/L37 Key.Escape/Key.Enter；`LauncherHotkey.Parse` | kept |
| 托盘图标 Open/Quit + 关闭藏托盘 | VERIFIED | `Win32TrayIcon.cs` L36-37 OpenCommandId/QuitCommandId；L139-140「打开 WT Launcher」「退出」 | kept |
| 管理菜单导出全部/导出选中/导入（路径映射 + 增/替换冲突处理） | VERIFIED | `LauncherControlBar.axaml` L113-121 导出全部/导出选中/导入配置；`ImportConfigViewModel`/`ImportMigrationWindow`（映射、重复标记） | kept |
| 持久最近运行历史（requested/failed/skipped/canceled、100 条上限、不声明终端健康） | VERIFIED | `LauncherRunHistory.DefaultMaxEntries = 100`；`LauncherRunStatus` 4 值；`RunHistoryWindow` | kept |
| 管理菜单显式检查更新（版本化 `update-manifest.json`、本地或 updateManifestUrl、不自动覆盖便携配置） | VERIFIED | `LauncherControlBar.axaml` L125 检查更新；`UpdateCheckService`（LauncherUpdateManifest 解析）；`build-release.ps1` 生成 update-manifest.json（L81-91） | kept |
| 便携安装脚本：开始菜单/可选桌面快捷方式、写卸载器、卸载保留用户 config | VERIFIED | `tools/install-portable.ps1`：Start Menu 快捷方式 L42-48、`-Desktop` L50-55、写 `uninstall.ps1` L88-92、卸载备份 config L11-19；`verify.ps1` `portable-installer-contract`（markers：WTLauncher.App.exe/uninstall.ps1/config/Start Menu） | kept |
| JSON 持久化临时文件 + `.bak`；Core/App/Tests 分离；self-contained win-x64 publish | VERIFIED | 见 FileLauncherJsonIO；目录结构；build-release `-r win-x64` | kept |

## 门禁保护内容

| 声明 | 判定 | 处理 |
|---|---|---|
| 6 个章节标题：'Configuration schema'、'Build and verification'、'Run the mainline app'、'WinForms retirement'、'Safety notes'、'Current implementation status' | VERIFIED（verify.ps1 L158 逐字校验） | 原样保留 |
| 4 条边界短语：'all 22 active feature mappings as narrow-domain north-star'、'portable Windows Terminal workspace-launcher scope only'、'full absolute objective remains not complete as written'、'unimprovable-product, stronger-than-every-GitHub-similar, and absolute zero-side-effect claims' | VERIFIED（verify.ps1 L161-166 逐字校验） | 原样保留 |
| no-stale-three-file-refs：README.md 为 activeDocs 之一（verify.ps1 L629-634） | VERIFIED | 重写全程避免 `config/(config|presets|settings).json`（仅允许 `retired/` 前缀形式） |
