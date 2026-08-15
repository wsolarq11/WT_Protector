# 验证台账 — docs/research/architecture-patterns-survey.md

> 验证日期：2026-08-15 ｜ 协议：.trellis/tasks/08-15-docs/verification-protocol.md
> 文档性质：带日期调研报告（2026-07-21），其推荐方案在同提交已实现 → HISTORICAL

| 声明 | 判定 | 证据（命令+结果） | 处理 |
|---|---|---|---|
| 调研日期 2026-07-21 | VERIFIED | git log --follow → bfc7ed90 2026-07-21 refactor: Unified Store Architecture — AppStore + EntryManager 消除 + 消息简化 | 日期已存在 |
| 推荐方案 Unified Store（AppStore + Selector）被实现 | VERIFIED | git show --stat bfc7ed90 → AppStore.cs +102 新增、EntryManager.cs -70 删除、MainWindowViewModel.Messaging.cs -46 删除、ShellSnapshot.cs / LauncherMutationTransaction.cs 删除 | 保留；验证记录注明已实现 |
| 删除文件清单含 LauncherEntryMutationSnapshot.cs / LauncherSelectionMutationSnapshot.cs | VERIFIED | 协议已知事实（2026-07-21 删除）；当前 grep 'class LauncherEntryMutationSnapshot|LauncherSelectionMutationSnapshot' → 0 命中；回滚 = StoreGrain.Dispatch save-first-then-commit（CLAUDE.md/协议一致） | 保留 |
| 删除文件清单含 LauncherDragReorderWorkflow.cs | STALE（未删除，至今存在） | Test-Path → EXISTS（App/ViewModels/LauncherDragReorderWorkflow.cs，生产使用 Stateless） | 验证记录注明未删除 |
| R1：共享可变 ObservableCollection（EntryListViewModel._entries 与 MainWindowViewModel 同一集合） | HISTORICAL | 2026-07-21 时点代码诊断；当前 EntryListViewModel 已改用 EntryProjection + SourceCache（read EntryProjection.cs:17） | 保留（时点诊断） |
| R2：EntryManager 委托聚合（10+ Func<> 注入：SettingsGetter/StatusApplier/SnapshotCapturer/ChangesApplier） | HISTORICAL | 2026-07-21 时点诊断；EntryManager.cs 在 bfc7ed90 删除；当前 grep 'EntryManager' → 仅 App/ViewModels/MainWindowViewModel.IEntryCardHost.cs:14 注释提及 | 保留（时点诊断） |
| R3：WeakReferenceMessenger 全局消息 | HISTORICAL（仍在使用） | 当前 read Messages.cs → 仍用 CommunityToolkit.Mvvm Messaging / WeakReferenceMessenger | 保留；验证记录注明现状 |
| 保留不变：IEntryService / IEntryCardHost 接口 | VERIFIED | grep 'interface IEntryCardHost' → App/Features/Entries/IEntryCardHost.cs:8 | 保留 |
| 保留不变：Core 层 0 改动、Views 层 0 改动 | UNVERIFIABLE（时点断言） | 无法精确核实当时 diff 范围；beb47560 之后 Core/Views 均有变动 | 验证记录注明该承诺为时点（bfc7ed90 当次）范围 |
| "670 测试继续通过" | UNVERIFIABLE（数字口径） | git 时点测量 bfc7ed90：测试文件 94、[Fact]/[Theory] 607；"670" 为当时本地运行口径 | 保留（时点）；验证记录注明实测值 |
| 方案 1-8 比较、增量成本估计、不做的方案 | HISTORICAL | 2026-07-21 调研结论（Redux No-Go 等）；后续实际演进：VTK (e11183cc 07-24) → CFA (9df5ceec 07-25) → Trinity (beb47560 07-25) | 保留（时点调研） |
| AppStore 后续命运（本文未述） | — | git 证据：AppStore.cs 于 bfc7ed90 创建、e11183cc（VTK, 07-24）迁移删除；门禁 vtk-no-appstore-residue（verify.ps1:849）强制零残留；当前 grep 'class AppStore' → 0 命中 | 验证记录补充后续演进 |
| 外部来源（bool.dev/Redux/Akka/Signals 等） | UNVERIFIABLE（外部研究） | 仓库无证据可查；带日期调研记录 | 保留 |
| 结构数字（53/53/15/15/24/20/7/121/728/38） | 不适用 | 本文无结构数字 | — |

## 处理总结
- 判定：VERIFIED 4 项 / STALE 2 项 / UNVERIFIABLE 3 项 / HISTORICAL 4 项
- 重写：仅新增 2026-08-15 验证记录块（置于头部引用块之后）；正文原文保留（带日期历史研究记录）
- 无删除；未触碰 AGENTS.md TRELLIS 块、config/launcher.json、源码