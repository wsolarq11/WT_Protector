# 架构改革第2步: Fluxor状态集中化迁移

## Goal

消除架构提案中识别的 5 类 18 个系统性脆弱点中尚未处理的剩余问题：**双路径不一致**（ReplaceEntries vs ReplaceAllEntries）、**时序耦合**（增量排序 SyncVisibleEntries 的脆弱位置更新）以及 **幽灵状态残留**（手动状态字段分散在 VMs 中）。

核心手段：将 LayoutState 连接到真实数据流，让 ViewModel 通过 Dispatch Action → Reducer 模式统一所有 entry/list/sort/filter 变更路径。

## Requirements

1. **统一 entry 替换路径**: 消除 `ReplaceEntries()` (无事件) vs `ReplaceAllEntries()` (触发事件) 两条路径的行为差异，统一通过 Dispatch 走同一路径
2. **消除增量排序算法**: 移除 `MainWindowViewModel.Layout.cs` 中的 `_syncSortedSet`/`_syncPositionByEntry` 手写增量排序，替换为简单全量重建（数据量 < 200，全量重建性能可接受）
3. **LayoutState 接入真实数据流**: LayoutState/LayoutActions/LayoutReducers 已存在但未被 VM 引用。VM 读写状态应改走 Fluxor Dispatch + IState\<LayoutState\>
4. **不破坏现有 MVVM 绑定结构**: View 层的 XAML 绑定不应变更，ViewModel 公共属性签名不变
5. **保持 696 测试全绿**: 重构期间不减少测试数量，不降低断言覆盖率

## Non-goals

- 不做 Event Sourcing 全量实现
- 不引入第三方 Fluxor.Selectors 库（已有 MemoizedSelector）
- 不改动 Core 层（WTLauncher.Core 不依赖 Fluxor）
- 不一步到位把所有 VM 字段迁移到 State（仅迁移 entries/list/sort/filter/viewMode）

## Acceptance Criteria

- [ ] `ReplaceEntries` 和 `ReplaceAllEntries` 统一为单一路径，验证无测试回归
- [ ] `_syncSortedSet`/`_syncPositionByEntry` 删除，SyncVisibleEntries 改为全量重建
- [ ] MainWindowViewModel 通过 `IState<LayoutState>` 读取 entries/sort/filter/viewMode
- [ ] `dotnet test` 696 全绿
- [ ] `tools/verify.ps1` 25/25 PASS
- [ ] XAML 绑定零变更
