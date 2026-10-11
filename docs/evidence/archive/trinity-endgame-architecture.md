Status: Archived
Purpose: 记录 Trinity 终局架构提案及落地、后续被 Scheme B 取代的事实。
Original scope: 全网 deepsearch + WT Protector 全仓审计，目标从架构层消除整类 bug。

# Trinity 终局架构提案 — 剪掉一切问题的源头

## 结论

- 本提案已于 2026-07-25 落地，与本文档同提交 `beb47560 feat: implement Trinity endgame architecture — 3-zone refactoring`。
- 现已 DEPRECATED（2026-08-20）：被当前 Scheme B 架构（StoreGrain + FileStorePersistence + 5-layer）取代，仅作历史参考。

## 5 个根本性问题源头

| # | 根因 | 关键证据 |
| --- | --- | --- |
| 1 | MVVM 命令式突变 | `_isHandlingSettingsChange` 4 handler 复制；`ApplyFilter` 执行两次；Refresh Phase 1/2/3 顺序 |
| 2 | 状态投影手工维护 | `_lastAppliedCardsAvailableSize` 记忆化；Drag commit 后跳过 ApplyFilter 致 VisibleGroups stale |
| 3 | 无形式化状态机 | `_constructed` flag；异常恢复路径不完整 |
| 4 | XAML 绑定非行为契约 | 代码-behind 存在 drag hit-test 等 UI 逻辑 |
| 5 | Event Sourcing 超重 | 14 种 LauncherEvent + Grain 组合认知负载过高 |

## Trinity 三 Zone

- Zone 0 Pure Domain Core：所有业务逻辑为 `static (Input) → Output` 纯函数。
- Zone 1 State Machine + Reactive Projection：形式化状态机 + DynamicData 声明式投影，无手写事件链。
- Zone 2 Pure View：XAML + Compiled Bindings，代码-behind 零业务 `if`。

## 消除映射（根因 vs 方案）

| 根因 | 方案 | 置信度 |
| --- | --- | --- |
| MVVM 命令式突变 | Zone 0 Pure Functions | 100%（语言保证） |
| 手工状态投影 | Zone 1 DynamicData | 100%（框架保证） |
| 无形式化状态机 | Zone 1 Stateless | ~99.99% |
| XAML 非类型安全 | Zone 2 Compiled Bindings | ~99% |
| Event Sourcing 超重 | Zone 1 轻量 Reducer | ~99% |

## 落地记录（2026-08-15 核实）

- 门禁 `trinity-architecture-integrity` 当时强制六组件，实测全部命中。
- 命名差异：状态机实际为 `AppStateMachine`，枚举为 `AppUIState` / `AppUITrigger`；“AppReducer/AppState”对应 `StoreGrain.Dispatch(Func<LauncherStoreFile, LauncherStoreFile>)`。
- `EntryProjection` 实际为 `SourceCache<LauncherEntryViewModel, string>`（键 vm.Id），Filter→Sort→Bind 管道一致。

## 关键后续变更（2026-09-17）

`EntryProjection.cs`（App/Features/Entries）与 `AppStateMachine.cs`（App/ViewModels）已删除（外部零引用，从未接线）；DynamicData 随之一并移除，`Directory.Packages.props` 现仅存 Stateless 5.20.0 与 CsCheck 4.2.0；Stateless 生产使用于 `LauncherDragReorderWorkflow`，PBT 测试落地于 `tests/PropertyTest/`。

## VTK 简化落地

`beb47560` 删除 `EventJournal.cs`（142 行）、`UiStateProjection.cs`、`LauncherEvent.cs`、`LauncherStoreProjector.cs`、`EventJsonContext.cs`、`EventStore/*`；保留 Grain 线程模型（`StoreGrain : Grain<LauncherStoreFile>`）。

## 未落地项

Phase 4 提议的 `vtk-pure-functions` / `vtk-state-machine` / `vtk-no-codebehind-logic` / `vtk-compiled-bindings` 未采用，实际为 `trinity-architecture-integrity` + `vtk-architecture-integrity` + `vtk-no-appstore-residue`；Phase 3 的“全域 `x:CompileBindings=True`”未落地，`x:DataType` 见于 10 个 axaml 文件。

## 关联路径

- 落地提交：`beb47560`；状态容器：`src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs`
- PBT 测试：`src/avalonia-launcher/tests/WTLauncher.Tests/PropertyTest/`
- 历史数字：700 tests / 28 gates / 5 analyzers（git 时点测量 `beb47560`：98 测试文件、632 属性、29 Add-Check）。
