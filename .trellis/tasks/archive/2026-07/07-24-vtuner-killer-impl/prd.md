# Architecture: VTuber-Killer Implementation

## Goal

把 WT Launcher 的状态管理从 monolithic `AppStore` 迁移到事件溯源 + Actor(Grain)架构,使状态变更可回放、写入路径单线程化、故障可隔离。

## Background

triple-cut 架构(2026-07-24 Session #53)解决了状态漂移与持久化绕过问题,但 `AppStore` 仍是单一可变状态容器,所有 reducer/selector 集中在一个文件。VTuber-Killer 架构在此基础上进一步:把"状态"换成"事件流",当前状态只是事件流的投影,从根上消除多源状态漂移的可能。

## Requirements

父任务统筹 6 个子任务的执行顺序与集成:

1. **Phase 0 — Infrastructure Setup**: OneOf + Immutype NuGet, Roslyn analyzer 骨架, `AppError` ADT, `ErrorSupervisor` 断路器。
2. **Phase 1 — Compile-time Moat + Error Kernel**: 清零 bare catch,完成 ResourceKeyAnalyzer,OneOf 化错误处理,保护 11 个 async void handler。
3. **Phase 2 — Event Sourcing**: `EventJournal` (crash-safe append-only) + 14 个 `LauncherEvent` + `LauncherStoreProjector` 纯投影。
4. **Phase 3 — CQRS + Grain**: `Grain<T>` actor 基类 + `StoreGrain` 替代 `AppStore` + `UiStateProjection` 自动 UI 同步。
5. **Migration tail**: 扫除 `AppStore` 残留引用,更新 DI 配置,同步测试桩。
6. **Finalize store**: 物理删除 `AppStore.cs`,新增 WT1005/WT1006 analyzer,更新 pitfalls spec。

## Acceptance Criteria

- [x] `AppStore.cs` / `StoreReducers.cs` / `StoreSelectors.cs` 从仓库删除。
- [x] `StoreGrain` 接管全部写入路径,`WT1003` analyzer 编译期强制。
- [x] `EventJournal` 使用 `FileOptions.WriteThrough`,崩溃前已写入事件不丢。
- [x] 14 个 `LauncherEvent` 类型覆盖所有状态变更。
- [x] 11 个 async void handler 全部加 `ExceptionFilters.IsRecoverable` 保护。
- [x] 4 处 bare catch 全部修复,`WT1004` 同时检测 `catch {}` 与 `catch (Exception)`。
- [x] WT1005 (ResourceKey) + WT1006 (JsonRegistration) analyzer 落地。
- [x] `dotnet build -warnaserror` 通过。
- [x] `tools/verify.ps1` 全 PASS,700 tests 通过(2026-07-25 验证)。
- [x] `pitfalls.md` §2.5.5 + §2.5.6 写入迁移踩坑。

## Sub-tasks

| 子任务 | 状态 | 归档位置 |
|---|---|---|
| `07-24-phase0-infra` | completed | `archive/2026-07/` |
| `07-24-phase1-moat` | completed | `archive/2026-07/` |
| `07-24-phase2-eventsourcing` | completed | `archive/2026-07/` |
| `07-24-phase3-cqrs` | completed | `archive/2026-07/` |
| `07-24-migration-tail` | completed | `archive/2026-07/` |
| `07-24-finalize-store` | completed | `archive/2026-07/` |

## Implementation Evidence

- Session #54 完成全部实现,4 个提交:`d9fa243` / `075682b` / `1b6dc51` / `fd9a1bd`。
- 6 个子任务于 2026-07-24 一次性归档(commits `40e8d9e` ~ `f5e70ff`)。
- 2026-07-25 verify gate 复跑确认:`status: PASS`, 700 tests, 25 checks 全绿。

## Notes

- 父任务 PRD 在归档时回填(2026-07-25),原始 PRD 是 TBD 模板;真实需求与验收据 6 个子任务 PRD + Session #54 journal 还原。
- 4 个子任务(phase2/3/migration-tail/finalize-store)的 PRD 同时在 2026-07-25 回填。
