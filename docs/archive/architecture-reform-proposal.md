# WT Protector 架构改造提案

> **⚠️ 本文档已过期 (2026-07-25)**
> 
> 本提案提出的 Fluxor 状态集中化路线 **从未被实施**。2026-07-24 落地了 VTuber-Killer 架构
> (Event Sourcing + Grain + CQRS)，后于 2026-07-25 进一步迁移至 **Closed Flux Architecture (CFA)** 
> (AppState 单源 + StoreGrain Dispatch + ViewModelBase)，以更彻底的方式实现了所有 5 类脆弱点的消除。
>
> | 原提案变革 | 原计划方案 | 实际落地方案 | 状态 |
> |---|---|---|---|
> | 变革一: 派生数据优先 | MemoizedSelector | StoreGrain.Select + ViewModelBase | ✅ CFA 完成 |
> | 变革二: 状态集中化 | Fluxor + 不可变 State | AppState 单源 + Grain.Mutate 锁内原子化 | ✅ CFA 完成 |
> | 变革三: 声明式属性 | [NotifyPropertyChangedFor] | [ObservableProperty] + ViewModelBase | ✅ CFA 完成 |
>
> 当前架构参考:
> - `.trellis/tasks/07-25-closed-flux-arch/` — CFA 迁移完整设计文档 (PRD + design + implement)
> - `.trellis/spec/views/mvvm-pattern.md` — ViewModelBase 模式规范
> - `src/avalonia-launcher/src/WTLauncher.Core/AppState.cs` — 单源状态树
> - `src/avalonia-launcher/src/WTLauncher.App/ViewModels/ViewModelBase.cs` — 生命周期管理
>
> **验证记录 (2026-08-15)** — `.trellis/tasks/08-15-docs` 协议逐项核实：
> - ✅ 时间线：VTK 落地 `e11183cc` (2026-07-24, EventJournal + Grain + StoreGrain)；CFA 落地 `9df5ceec`/`4534cddd` (2026-07-25)；Fluxor 骨架 `86cce496` (2026-07-23) → Scheme B `0c0b8771` (2026-07-24) 移除。
> - ✅ 上表“实际落地方案”中的 **Grain.Mutate 锁内原子化仍成立**：`Grain.Mutate`（App/Infrastructure/Grain.cs:43）由 `StoreGrain.Dispatch` 在锁内调用（StoreGrain.cs:60）；save-first-then-commit 不变。
> - ⚠️ “AppState 单源”仅为 2026-07-25→07-26 的中间状态：`WTLauncher.Core/AppState.cs` 由 `9df5ceec` 创建、`4e35fabc` (2026-07-26) 删除。当前无 AppState 类型，单源 = `StoreGrain`（持有 `LauncherStoreFile`）；`App/ViewModels/AppStateMachine.cs` 为 Trinity 引入的 UI 状态机（**已于 2026-09-17 删除**：外部零引用，从未接线）。
> - ⚠️ `.trellis/tasks/07-25-closed-flux-arch/` 已归档 → `.trellis/tasks/archive/2026-07/07-25-closed-flux-arch/`（`1e44ba36` 2026-07-25）。
> - ✅ `.trellis/spec/views/mvvm-pattern.md`、`App/ViewModels/ViewModelBase.cs` 均存在。
> - 正文（2026-07-23 提案）为历史记录保留原文；其中“673 测试”为当时本地运行口径（git 时点测量 `a441f56f`：[Fact]/[Theory] 属性 601 个）。


> 基于深度代码调研 + 行业架构调研的综合方案
> 目标：从根源消除整类问题，而非逐个修复 bug

---

## 一、现状：5 类系统性脆弱点

通过全量代码审计（12 个关键文件），当前架构存在以下 5 类共 18 个脆弱点：

### 类型 1：隐式状态链 (4 处)

| 脆弱点 | 位置 | 说明 |
|--------|------|------|
| `_lastAppliedCardsAvailableSize` / `_lastAppliedVisibleCount` guard | LayoutManager.cs:158-169 | 手写记忆化，新 VM 替换时缓存未失效 |
| `_constructed` flag | MainWindowViewModel.cs:212 | 构造期间抑制事件，依赖后续手动 ApplyFilter |
| `_syncSortedSet` / `_syncPositionByEntry` 复用 | Layout.cs:223-281 | Clear() 复用集合，增量更新算法自身标注 fragile |
| `_cardsAvailableSize` 归一化比较 | LayoutManager.cs:177-183 | 相等性决定是否跳过通知，边界行为隐式 |

### 类型 2：幽灵状态 (4 处)

| 脆弱点 | 位置 | 说明 |
|--------|------|------|
| `_settings` 不从磁盘重载 | MainWindowViewModel.cs Refresh() | 只重载 entries，settings 保持内存旧值 |
| `_presetFile` 异常时不同步 | MainWindowViewModel.cs:262-276 | catch 块中 `_presets`/`SelectedPreset` 可能不一致 |
| `RefreshSelectionState` 异常恢复 | Multiple catch blocks | 部分路径跳过了完整状态恢复 |
| Entry 替换触发链中的中间状态 | EntryListViewModel.cs:69-75 | ReplaceAllEntries → EntriesChanged 链中 state 处于过渡态 |

### 类型 3：双路径不一致 (4 处)

| 脆弱点 | 位置 | 说明 |
|--------|------|------|
| `ReplaceEntries()` wrapper vs `ReplaceAllEntries()` | MWVM.cs:242 vs MWVM.cs:445-450 | 一个调 InvalidateCardSizeCache，一个不调 |
| `_list.ReplaceEntries()` vs `_list.ReplaceAllEntries()` | EntryListViewModel.cs:79 vs 69 | 一个不触发事件，一个触发 |
| 持久化双路径：`BuildStoreFile→SaveStoreFile` vs `_appStore.Mutate` | MWVM.cs vs AppStore.cs | 后处理逻辑不一致 |
| `Refresh()` 中 `ApplyFilter` 执行两次 | MWVM.cs 通过事件链 + 显式调用 | 第一次用旧 settings，第二次用新 settings |

### 类型 4：时序耦合 (2 处)

| 脆弱点 | 位置 | 说明 |
|--------|------|------|
| Refresh Phase 1/2/3 顺序依赖 | MWVM.cs:236-283 | 任何阶段添加新步骤都可能破坏顺序 |
| `ReplaceAllEntries → EntriesChanged → ApplyFilter` 隐式链 | EntryListViewModel.cs:69-75 → MWVM.cs:210-214 | 调用者必须了解整个事件链 |

### 类型 5：守卫逻辑扩散 (1 处)

| 脆弱点 | 位置 | 说明 |
|--------|------|------|
| `_isHandlingSettingsChange` 在 4 个 handler 中完整复制 | Layout.cs:30-138 | OnLayoutFilterChanged、OnLayoutSortChanged、OnLayoutViewModeChanged、OnLayoutGroupKeyChanged 各自复制粘贴 |

---

## 二、目标架构：派生数据优先 + 状态集中化

### 核心原则

```
Store (单一不可变数据源)
  │
  ├── Selectors (纯函数，可组合，可记忆化)
  │   ├── selectFilteredEntries(state) → VisibleEntries
  │   ├── selectCardSize(state, containerWidth) → CardWidth/Height
  │   └── selectVisibleGroups(state) → Groups
  │
  ├── ViewModel (纯投影，无可变状态)
  │   ├── 所有属性 = selector(store)
  │   └── 所有命令 = Dispatch(action)
  │
  └── View (绑定到 ViewModel)
```

### 架构变革一：派生数据优先 + 记忆化 Selector

**优先级：最高** | **消除：隐式状态链、时序耦合、守卫扩散**

当前问题：
```csharp
// 隐式状态链：需要手工调用 InvalidateCardSizeCache
_applyFilter → TryRefreshCardSize
  → if (_lastAppliedCardsAvailableSize == current && _lastAppliedVisibleCount == current)
      return false; // 跳过！
```

改造后：
```csharp
// 纯函数 Selector：不需要守卫，输入不变时由 MemoizedSelector 自动跳过
public double CardWidth => 
    _cardSizeSelector.Get(
        (w, c) => LauncherEntryLayoutCalculator.Calculate(w, c).CardWidth,
        (_containerWidth, VisibleEntries.Count));

// 记忆化选择器工具类
public class MemoizedSelector<TInput, TOutput>
{
    private TInput? _lastInput;
    private TOutput? _lastOutput;
    private bool _hasCached;
    
    public TOutput Get(Func<TInput, TOutput> compute, TInput input)
    {
        if (_hasCached && EqualityComparer<TInput>.Default.Equals(_lastInput, input))
            return _lastOutput;
        _lastInput = input;
        _lastOutput = compute(input);
        _hasCached = true;
        return _lastOutput;
    }
}
```

**消除的问题：**
- `#1 隐式状态缓存` — MemoizedSelector 替代手写 guard，输入变化自动重算
- `#2 事件链时序耦合` — ApplyFilter 从事件处理函数改为计算属性，没有"调用时机"问题
- `#5 守卫逻辑扩散` — 每个属性独立使用 MemoizedSelector，不需要全局 `_isHandlingSettingsChange`

### 架构变革二：状态集中化 + 不可变 State

**优先级：高** | **消除：幽灵状态、双路径不一致**

当前问题：
```csharp
// 两个代码路径，行为不同
_list.ReplaceAllEntries(entries);  // 不调 InvalidateCardSizeCache
ReplaceEntries(entries);            // 调 InvalidateCardSizeCache
```

改造后：
```csharp
// 统一路径：Dispatch Action → Reducer → 不可变 State
// 在 VM 侧：
_dispatcher.Dispatch(new SetEntriesAction(loadedEntries));

// Reducer：
[ReducerMethod]
public static LayoutState ReduceSetEntries(LayoutState state, SetEntriesAction action)
    => state with { Entries = action.Entries.ToImmutableList() };
```

**关键变化：**
1. `_list.ReplaceAllEntries` 和 `ReplaceEntries` 统一为 `Dispatch(new SetEntriesAction(...))`
2. Layout ViewModel 的所有可变字段迁移到 Fluxor State
3. ViewModel 构造时只注入 `IState<LayoutState>` 和 `IDispatcher`
4. 异常路径下 State 保证一致（不可变 State + 单一数据源）

**消除的问题：**
- `#3 幽灵状态` — 不可变 State 每次整体替换，不存在部分更新
- `#4 双路径不一致` — 只有一条代码路径

### 架构变革三：声明式属性依赖

**优先级：中** | **消除：手工通知遗漏、跨属性隐式耦合**

当前问题：
```csharp
// 手工通知，容易遗漏
OnPropertyChanged(nameof(ShowDirectory));
OnPropertyChanged(nameof(ShowCommand));
OnPropertyChanged(nameof(TitleFontSize));
```

改造后：
```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsCompactLayout))]
[NotifyPropertyChangedFor(nameof(IsMicroLayout))]
[NotifyPropertyChangedFor(nameof(ShowDirectory))]
[NotifyPropertyChangedFor(nameof(ShowCommand))]
[NotifyPropertyChangedFor(nameof(TitleFontSize))]
private double _cardWidth;
```

**消除的问题：** 改了 CardWidth 忘了通知 ShowDirectory 的整类 bug

---

## 三、问题 vs 变革映射表

| 已发现问题 | 变革一：派生数据 | 变革二：状态集中化 | 变革三：声明式依赖 | 补充模式 |
|-----------|-----------------|------------------|------------------|---------|
| #1 隐式状态缓存 | 直接消除 | 辅助 | - | 记忆化 Selector |
| #2 事件链时序耦合 | 直接消除 | 辅助 | 辅助 | Fluxor Effect |
| #3 幽灵状态 | - | 直接消除 | 辅助 | 不可变 State |
| #4 双路径不一致 | - | 直接消除 | - | Effect 统一路径 |
| #5 守卫逻辑扩散 | 直接消除 | 辅助 | - | 单一数据源 |

---

## 四、迁移路径 — 分三步走

### 第一步：派生数据优先（1-2 天）

**范围：** LayoutManager + MainWindowViewModel.Layout

1. 实现 `MemoizedSelector<TInput, TOutput>` 工具类
2. 将 `TryRefreshCardSize` 重构为 `CardSizeSelector`（纯函数计算 + 记忆化）
3. 删除 `_lastAppliedCardsAvailableSize` 和 `_lastAppliedVisibleCount`
4. 将 `ApplyFilter` 从"事件处理函数"改为"计算属性"
5. 删除 `InvalidateCardSizeCache`（不再需要）
6. 验证：673 测试全部通过

**预期效果：** 已修复的 bug 不再重现，`_isHandlingSettingsChange` 不再需要

### 第二步：状态集中化（3-5 天）

**范围：** MainWindowViewModel + LayoutManager + 新 LayoutState

1. 审计所有私有字段，分类迁移到 Fluxor State
2. 定义 `LayoutState`（不可变，`[FeatureState]`）
3. 定义 `SetEntriesAction`、`SetFilterAction`、`SetViewModeAction`
4. 迁移 `ReplaceEntries` / `ReplaceAllEntries` 为 `Dispatch(SetEntriesAction)`
5. 迁移 `_isHandlingSettingsChange` 守卫为纯 Reducer
6. 清理 `_constructed` flag
7. 验证：673 测试全部通过

**预期效果：** 消除双路径和幽灵状态，代码路径唯一

### 第三步：声明式属性依赖（1 天）

**范围：** LauncherEntryViewModel + LauncherEntryViewModel.Layout

1. 寻找所有手工 `OnPropertyChanged(nameof(...))` 调用
2. 替换为 `[NotifyPropertyChangedFor]` 声明
3. 验证：673 测试全部通过

---

## 五、不做的事情（负责任的范围界定）

| 方案 | 不做的原因 |
|------|-----------|
| ItemsRepeater + FlowLayout 虚拟化 | 卡片 < 200，收益有限，Avalonia 版本兼容需确认 |
| Event Sourcing 全量实现 | 桌面启动器项目过于重量级 |
| 第三方 Fluxor.Selectors 库 | 社区维护，稳定性不保证，自己实现 MemoizedSelector 更可控 |
| ViewModel 对象池 | 当前卡片数量级不需要，引入复杂度 > 收益 |

---

## 六、参考来源

| # | 来源 | 核心贡献 |
|---|------|---------|
| 1 | [React Docs — You Might Not Need an Effect](https://react.dev/learn/you-might-not-need-an-effect) | 派生数据优于 Effect 同步的原则 |
| 2 | [Redux Docs — Deriving Data with Selectors](https://redux.js.org/usage/deriving-data-selectors) | 记忆化选择器、Selector 组合 |
| 3 | [Fluxor Tutorial 01A — State, Actions, Reducers](https://github.com/mrpmorris/Fluxor) | 不可变 State、纯 Reducer |
| 4 | [Fluxor Tutorial 01B — Effects](https://github.com/mrpmorris/Fluxor) | Effect 模式、Action 链 |
| 5 | [CommunityToolkit.Mvvm — ObservableProperty](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) | 源生成器、声明式计算属性 |
| 6 | [Avalonia Docs — MVVM Pattern](https://docs.avaloniaui.net/docs/concepts/reactiveui) | Avalonia MVVM 基础、编译绑定 |

---

*文档生成日期：2026-07-23*
*基于 arch-inspector（代码审计）+ pattern-researcher（行业调研）的联合研究成果*
