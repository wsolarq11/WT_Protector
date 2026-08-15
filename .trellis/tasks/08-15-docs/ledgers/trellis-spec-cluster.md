# Trellis 规范簇验证台账 (2026-08-15)

> 接管自卡住的 J 代理（e5c25585 中断）。主代理按 verification-protocol.md 亲自逐项验证与修复。

## 修复的 STALE 声明（证据化改写）

| 文件 | 声明（原文） | 判定 | 证据 | 处理 |
|------|------|------|------|------|
| views/mvvm-pattern.md L421-488 | "Scenario: AppStore mutation with automatic rollback" 整节（AppStore.Mutate 捕获 before→revert） | STALE | AppStore 已删（VTK 迁移 2026-07-24）；StoreGrain.cs 实测 save-first-then-commit：reducer → _persistence.Save → Mutate 提交内存 | 整节重写为 StoreGrain.Dispatch 语义 |
| views/mvvm-pattern.md L710/714/720/722/753 | 状态读写经 AppStore.Select/Dispatch | STALE | 同上 | 全部改 StoreGrain |
| core/persistence.md L200 | Rule WT1003: Save 仅限 AppStore.Dispatch/DispatchAsync/DispatchErrorOr | STALE | DispatchAsync 全仓 0 命中；StoreGrain.cs 仅有 Dispatch/DispatchErrorOr | 改 StoreGrain.Dispatch/DispatchErrorOr |
| core/persistence.md L202/208/211/214/224/346 | AppStore._state/Select/SemaphoreSlim 描述 | STALE | StoreGrain 用 Grain<TState> 锁（Grain.cs:43 Mutate）；无 AppStore | 全部改 StoreGrain |
| core/error-handling.md L132 | ViewModels call AppStore.Mutate ... AppStore 自动 revert | STALE | 同上 | 改 StoreGrain.Dispatch save-first-then-commit |
| guides/index.md L90 | 自 266aa6f 后 no-bare-catch 门禁 | STALE | git log: 266aa6f 不存在（unknown revision）；真实提交 aae2454e（引入）+ b3556262（消除裸 catch） | 改 aae2454e/b3556262 |
| tests/index.md L12-14 | 引用文件路径缺子目录（LauncherPersistenceTestFixtures.cs 等在 tests 根） | STALE | 实测在 Support/（2 个）与 Architecture/（1 个） | 补全子目录路径 |

## 核实无误的声明（VERIFIED 抽样）

| 文件 | 声明 | 证据 |
|------|------|------|
| views/drag-drop.md | LauncherDragReorderSession、LauncherDragGhostLayoutCalculator 存在 | Test-Path ViewModels/LauncherDragReorderSession.cs；Core/LauncherDragGhostLayoutCalculator.cs |
| views/quality-guidelines.md L164 | LauncherDragGhostLayoutCalculatorTests.cs 存在 | tests/WTLauncher.Tests/CoreUnit/ 下存在 |
| views/mvvm-pattern.md | Stateless 状态机建模拖拽工作流 | LauncherDragReorderWorkflow.cs L2 using Stateless |
| views/drag-drop.md L52 | IsDragInsertBefore/After 为 DragCueState 只读投影 | DragCueState 定义于 LauncherEntryViewModel.Drag.cs |
| tests/test-quality.md L56 | AppStoreTests.cs 引用 | 文件存在（内为 StoreGrainTests 类，文件名遗留） |
| core/persistence.md L408-410 | 快照类/AppStore.Mutate 删除记录 | HISTORICAL（2026-07-21/2026-07-23 已删，保留为历史） |
| guides/pitfalls.md | 50+ 踩坑（887 行 / 121 编号标题） | 实测计数 |

## 未验证/保留

- 全部历史教训条目（pitfalls.md AppStore→StoreGrain 迁移、data-types.md 快照类删除记录）按 HISTORICAL 保留原文
- core/directory-structure.md 与 views/directory-structure.md 已于 2026-08-15 由主代理重写并核实行数表（437/166/172/... 实测一致）
