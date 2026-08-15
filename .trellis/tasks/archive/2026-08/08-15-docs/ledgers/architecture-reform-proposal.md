# 验证台账 — docs/architecture-reform-proposal.md

> 验证日期：2026-08-15 ｜ 协议：.trellis/tasks/08-15-docs/verification-protocol.md
> 文档性质：带日期提案（正文 2026-07-23）+ 过期标记横幅（2026-07-25）→ 判定为 HISTORICAL

| 声明 | 判定 | 证据（命令+结果） | 处理 |
|---|---|---|---|
| 横幅：2026-07-24 落地 VTuber-Killer（Event Sourcing + Grain + CQRS） | VERIFIED | git log --all → `e11183cc 2026-07-24 feat: implement VTuber-Killer architecture (EventJournal + Grain + StoreGrain)` | 保留 |
| 横幅：2026-07-25 迁移至 CFA（AppState 单源 + StoreGrain Dispatch + ViewModelBase） | VERIFIED | git log --all → `9df5ceec/4534cddd 2026-07-25 feat: implement Closed Flux Architecture`；StoreGrain.Dispatch 存在于 App/Infrastructure/StoreGrain.cs:74（DispatchErrorOr）/ Dispatch | 保留 |
| 横幅：实际落地方案 "AppState 单源" | STALE（时点有效，现已被删） | git log --follow -- Core/AppState.cs → 9df5ceec 创建、`4e35fabc 2026-07-26 feat: reconnect analyzers, add build cache, delete dead code` 删除；当前 grep 'class AppState' 无命中（仅有 AppStateMachine） | 验证记录注明：AppState 仅存在于 2026-07-25→07-26；当前单源 = StoreGrain（LauncherStoreFile） |
| 横幅：参考路径 `src/.../WTLauncher.Core/AppState.cs` | STALE（路径已不存在） | Test-Path → MISSING（2026-08-15） | 验证记录注明正确现状 |
| 横幅："Grain.Mutate 锁内原子化" | VERIFIED | Select-String '`bMutate`b' src → Grain.cs:43 `protected TState Mutate(Func<TState,TState> mutator)`，StoreGrain.cs:60 `var after = Mutate(...)` | 保留 |
| 横幅：参考路径 `.trellis/tasks/07-25-closed-flux-arch/` | STALE（已归档） | Test-Path → MISSING；Get-ChildItem .trellis/tasks/archive/2026-07 → `07-25-closed-flux-arch` 存在；git log --diff-filter=D → `1e44ba36 2026-07-25 chore(task): archive 07-25-closed-flux-arch` | 验证记录注明归档路径 |
| 横幅：参考 `.trellis/spec/views/mvvm-pattern.md` | VERIFIED | Test-Path → EXISTS（该文件于 9df5ceec 创建） | 保留 |
| 横幅：参考 `App/ViewModels/ViewModelBase.cs — 生命周期管理` | VERIFIED | Test-Path → EXISTS；read → ViewModelBase : ObservableObject, IDisposable，AddDisposable/Dispose | 保留 |
| 横幅："Fluxor 状态集中化路线从未被实施" | VERIFIED（时点） | Fluxor 骨架 `86cce496 2026-07-23` 提交后于 `0c0b8771 2026-07-24 Scheme B` 移除；未成为最终架构 | 保留（时点判断成立） |
| 正文：5 类 18 个脆弱点（LayoutManager.cs / Layout.cs / EntryListViewModel.cs / AppStore.cs 等旧代码引用） | HISTORICAL | 正文日期 2026-07-23（git a441f56f 同日创建）；相关类型已被后续重构替换 | 保留原文，不加改动 |
| 正文：迁移步骤 "验证：673 测试全部通过" ×3 | UNVERIFIABLE（数字口径） | git 时点测量 a441f56f（2026-07-23）：测试文件 92、[Fact]/[Theory] 属性 601；"673" 应为当时本地 dotnet test 运行数，无法从仓库核实 | 验证记录注明：时点 git 实测 601 个属性 |
| 正文：参考来源 1-6（React/Redux/Fluxor/CommunityToolkit/Avalonia URL） | UNVERIFIABLE（外部研究） | 仓库无证据可查；属带日期调研记录 | 保留（研究记录） |
| 文档日期 | VERIFIED | git log --follow → a441f56f 2026-07-23 / 9df5ceec+ac8dcd60 2026-07-25 | 日期已存在 |
| 结构数字（53/53/15/15/24/20/7/121/728/38） | 不适用 | 本文无结构数字 | — |

## 处理总结
- 判定：VERIFIED 9 项 / STALE 3 项（均已注明现状，不改动历史文本）/ UNVERIFIABLE 2 项 / HISTORICAL 1 项
- 重写：仅新增 2026-08-15 验证记录块（置于横幅之后）；横幅与正文原文保留（带日期历史记录）
- 无删除；未触碰 AGENTS.md TRELLIS 块、config/launcher.json、源码
