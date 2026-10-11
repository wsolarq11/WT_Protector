Status: Archived
Purpose: 记录 2026-07-23 的 Fluxor 状态集中化架构改造提案及其后续被替代的事实。
Original scope: 深度代码调研 + 行业架构调研，目标从根源消除整类 bug。

# WT Protector 架构改造提案

## 结论

- 本提案的 Fluxor 路线从未实施。2026-07-24 落地 VTuber-Killer，2026-07-25 进一步迁移至 Closed Flux Architecture（CFA），以更彻底方式消除原 5 类脆弱点。
- 提案正文（根因分析、三变革、迁移路径）为历史记录，仅保留结论与映射。

| 原提案变革 | 原计划方案 | 实际落地方案 | 状态 |
| --- | --- | --- | --- |
| 变革一：派生数据优先 | MemoizedSelector | StoreGrain.Select + ViewModelBase | CFA 完成 |
| 变革二：状态集中化 | Fluxor + 不可变 State | AppState 单源 + Grain.Mutate 锁内原子化 | CFA 完成 |
| 变革三：声明式属性 | `[NotifyPropertyChangedFor]` | `[ObservableProperty]` + ViewModelBase | CFA 完成 |

## 关键证据

### 5 类系统性脆弱点（全量审计，共 18 处）

| 类型 | 数量 | 代表位置 |
| --- | --- | --- |
| 隐式状态链 | 4 | LayoutManager.cs 手写记忆化、`_constructed` flag |
| 幽灵状态 | 4 | Refresh() 不重载 `_settings`、异常恢复不完整 |
| 双路径不一致 | 4 | `ReplaceEntries` vs `ReplaceAllEntries`、持久化双路径 |
| 时序耦合 | 2 | Refresh Phase 1/2/3 顺序、事件隐式链 |
| 守卫逻辑扩散 | 1 | `_isHandlingSettingsChange` 在 4 个 handler 复制粘贴 |

### 落地时间线（2026-08-15 核实）

- VTK 落地 `e11183cc`（2026-07-24，EventJournal + Grain + StoreGrain）。
- CFA 落地 `9df5ceec` / `4534cddd`（2026-07-25）。
- Fluxor 骨架 `86cce496`（2026-07-23）→ Scheme B `0c0b8771`（2026-07-24）移除。

### 落地差异修正

- `Grain.Mutate` 锁内原子化仍成立：`Grain.Mutate`（App/Infrastructure/Grain.cs:43）由 `StoreGrain.Dispatch` 锁内调用，save-first-then-commit 不变。
- “AppState 单源”仅为 2026-07-25→07-26 中间状态：`WTLauncher.Core/AppState.cs` 由 `9df5ceec` 创建、`4e35fabc`（2026-07-26）删除。当前单源 = `StoreGrain`（持有 `LauncherStoreFile`）。
- `App/ViewModels/AppStateMachine.cs` 为 Trinity 引入的 UI 状态机，已于 2026-09-17 删除（外部零引用，从未接线）。
- `.trellis/tasks/07-25-closed-flux-arch/` 已归档至 `.trellis/tasks/archive/2026-07/07-25-closed-flux-arch/`（`1e44ba36`）。

## 关联路径

- CFA 迁移设计：`.trellis/tasks/archive/2026-07/07-25-closed-flux-arch/`
- MVVM 模式规范：`.trellis/spec/views/mvvm-pattern.md`
- 状态容器：`src/avalonia-launcher/src/WTLauncher.Core/AppState.cs`（已删除）
- 生命周期管理：`src/avalonia-launcher/src/WTLauncher.App/ViewModels/ViewModelBase.cs`
- 历史数字：673 tests / 601 个 `[Fact]`+`[Theory]` 属性（git 时点测量 `a441f56f`）。
