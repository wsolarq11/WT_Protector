# 验证台账 — docs/architectures/trinity-endgame-architecture.md

> 验证日期：2026-08-15 ｜ 协议：.trellis/tasks/08-15-docs/verification-protocol.md
> 文档性质：带日期架构提案（2026-07-25）；该提案已随同提交落地 → HISTORICAL（提案记录）+ 落地核对

| 声明 | 判定 | 证据（命令+结果） | 处理 |
|---|---|---|---|
| 生成日期 2026-07-25 | VERIFIED | git log --follow → beb47560 2026-07-25 feat: implement Trinity endgame architecture — 3-zone refactoring（文档与实现同提交） | 日期已存在 |
| 提案组件 LauncherJsonSerializer / LauncherStoreValidator / ILauncherJsonIO / IStorePersistence / EntryProjection / AppStateMachine | VERIFIED（均按此名落地） | Select-String 'class LauncherJsonSerializer' → Core/LauncherJsonSerializer.cs:14；'class LauncherStoreValidator' → Core/LauncherStoreValidator.cs:11；'interface ILauncherJsonIO' → Core/ILauncherJsonIO.cs:10；'interface IStorePersistence' → Core/IStorePersistence.cs:10；'class EntryProjection' → App/Features/Entries/EntryProjection.cs:15；'AppStateMachine' → App/ViewModels/AppStateMachine.cs:37 | 保留；验证记录确认全部存在 |
| 门禁 trinity-architecture-integrity 强制六组件 | VERIFIED | Select-String 'Add-Check' tools/verify.ps1 → :824 trinity-architecture-integrity；组件列表 :806-813 与六组件一致 | 保留 |
| Zone 1 草稿类型名 UIState/UITrigger/AppReducer/AppState | STALE（命名与代码不同） | read AppStateMachine.cs → 实际为 AppUIState / AppUITrigger / AppStateMachine；无 AppState 类型（grep 'class AppState\b' 无命中）；Reducer 对应 StoreGrain.Dispatch(Func<LauncherStoreFile,LauncherStoreFile>)（StoreGrain.cs:74 起，状态 = LauncherStoreFile） | 验证记录注明实际命名 |
| Zone 1 草稿 EntryProjection 用 SourceCache<string, LaunchEntry> | STALE（键类型不同） | read EntryProjection.cs:17 → SourceCache<LauncherEntryViewModel, string>（键 vm.Id）；Filter→Sort→Bind 管道一致（:71-82） | 验证记录注明实际签名 |
| 草稿 StoreGrain.Dispatch(Func<LauncherStoreFile,LauncherStoreFile> reducer) | VERIFIED | read StoreGrain.cs → 真实签名 public void Dispatch(Func<LauncherStoreFile, LauncherStoreFile> reducer)，内部 Mutate 锁内 save-first-then-commit | 保留 |
| B.5：Stateless 5.20.0 已有（Directory.Packages.props 声明） | VERIFIED | read Directory.Packages.props → PackageVersion Include=Stateless Version=5.20.0 | 保留 |
| B.5：LauncherDragReorderWorkflow.cs 生产使用 Stateless | VERIFIED | Test-Path → EXISTS；Select-String 'using Stateless' → LauncherDragReorderWorkflow.cs:2 | 保留 |
| B.5：DynamicData 需新增 | STALE（现已为依赖） | read Directory.Packages.props → DynamicData 9.4.33（beb47560 提交时 +1 加入）；EntryProjection.cs 使用 | 验证记录注明已落地 |
| B.5：CsCheck 已有（CsCheckPropertyTests.cs） | VERIFIED | glob → tests/WTLauncher.Tests/PropertyTest/CsCheckPropertyTests.cs | 保留 |
| B.3：VTK 文件清单（EventJournal.cs 142 行等） | VERIFIED（时点行数） | git show --stat beb47560 → EventJournal.cs 142 删、Events/LauncherEvent.cs 123 删、EventStore/LauncherEvent.cs 30 删、EventStore.cs 78 删、EventJsonContext.cs 24 删、UiStateProjection.cs 52 删、LauncherStoreProjector.cs 72 删 | 保留（时点记录；B.3 的 ~140/~80 等为近似） |
| B.3：删除 EventJournal/Event 溯源，保留 Grain 线程模型 | VERIFIED（已落地） | 上述 stat 删除清单；当前 grep 'class EventJournal|UiStateProjection|LauncherEvent|LauncherStoreProjector|EventJsonContext' → 0 命中；StoreGrain.cs / Grain.cs 仍存在 | 保留；验证记录注明已删除 |
| Phase 4：新增门禁 vtk-pure-functions / vtk-state-machine / vtk-no-codebehind-logic / vtk-compiled-bindings | STALE（未按此命名落地） | Select-String 'Add-Check' tools/verify.ps1 → 无这四个名字；实际为 trinity-architecture-integrity（:824）、vtk-architecture-integrity（:799）、vtk-no-appstore-residue（:849）、no-imperative-view-ui（:700） | 验证记录注明实际落地门禁 |
| Phase 3：开启全域 x:CompileBindings="True" | STALE（未按此落地） | Select-String 'x:CompileBindings' Views/*.axaml → 0 命中；x:DataType 命中 10 个 axaml 文件 | 验证记录注明现状 |
| Phase 1：PBT 覆盖（EntryFilter/EntrySorter/LayoutCalculator 等） | VERIFIED（已落地） | glob tests → PropertyTest/LauncherEntryOrderPbtTests.cs、LauncherJsonSerializerPbtTests.cs、LauncherLayoutPbtTests.cs、LauncherStoreValidatorPbtTests.cs（+ CsCheckPropertyTests.cs） | 保留 |
| 根因分析："700 tests, 28 gate checks, 5 Roslyn analyzers" | UNVERIFIABLE（数字口径） | git 时点测量 beb47560：测试文件 98、[Fact]/[Theory] 632、Add-Check 29；"700/28/5" 为当时本地运行口径 | 保留（时点记录）；验证记录注明实测值 |
| 附录关键决策：Stateless "5.20.1" | STALE（版本号错） | read Directory.Packages.props → Stateless 5.20.0（与 B.5 一致） | 已原地改为 5.20.0 |
| 与当前架构对比表（"当前架构 = Event Sourcing + Grain + 14 Events"） | HISTORICAL | 对比的是 2026-07-25 时点（VTK 已落地、Trinity 未落地时） | 保留（时点对比） |
| 结构数字（53/53/15/15/24/20/7/121/728/38） | 不适用 | 本文无结构数字（"700/28/5" 为时点记录） | — |

## 处理总结
- 判定：VERIFIED 10 项 / STALE 5 项 / UNVERIFIABLE 1 项 / HISTORICAL 1 项
- 重写：① 新增 2026-08-15 验证+落地核对记录块（置于生成日期引用块之后）；② 附录“关键决策记录”Stateless 版本 5.20.1 → 5.20.0（证据：Directory.Packages.props）
- 提案正文（迁移路径/置信度分析/消除映射）保留原文；未触碰 AGENTS.md TRELLIS 块、config/launcher.json、源码