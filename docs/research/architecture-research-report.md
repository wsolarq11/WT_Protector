Status: Research
Purpose: 2026-07-23 行业架构调研报告，非行动方案。
Update when: 不更新（研究快照）。

# 行业架构调研报告（2026-07-23）

> 时点说明：文中"当前 Fluxor Store / 项目已用 Fluxor"是 2026-07-23 时点事实；Fluxor 已移除，当前状态管理 =
> `StoreGrain`（Dispatch/DispatchErrorOr）。文中引用的 LayoutManager.cs / Layout.cs / ReplaceEntries /
> TryRefreshCardSize / `_lastApplied*` 等是当时代码，已被后续重构替换。

## 调研方向

1. Avalonia / WPF MVVM 架构模式
2. Fluxor / Redux 状态管理
3. CommunityToolkit.Mvvm ObservableObject 最佳实践
4. 桌面应用"无状态"架构
5. 虚拟化卡片布局与 ViewModel 生命周期

## 关键模式

### 派生数据优先于事件同步

能从已有状态算出的数据不应作独立状态，也不应靠事件/Effect 手动同步；每次渲染重算。
反模式：条目变化 → OnEntriesChanged → 守卫 → 缓存 → ApplyFilter → 更新 VM。
正模式：`visibleEntries = Filter(entries, searchQuery, viewMode)`；`cardSize = CalculateCardSize(width, viewMode)`。

### ViewModel 作为 Store 投影

VM 不持独立可变状态，只从 Store select 所需部分并转成 View 形状，不缓冲、不缓存、不守卫。

### 不可变 State + 纯 Reducer

State 只读，Reducer 是纯函数，返回新 State；Reducer 禁止依赖注入，有依赖走 Effect。

### Effect 处理副作用 + Action 链

Reducer 只做纯转换；异步/IO 副作用放 Effect，Effect 执行后 dispatch 结果 Action。

### 记忆化选择器

输入不变时跳过重算，直接返回缓存；filter/map/sort 等创建新集合的场景应记忆化，直接返回字段与简单
ID 查找不需要。

### [ObservableProperty] + [NotifyPropertyChangedFor] 声明式计算属性

用源生成器声明跨属性依赖，替代手工 `OnPropertyChanged`；批量更新用不可变集合一次替换，替代逐个 Add。

### ItemsRepeater 虚拟化与 Compiled Bindings

`ItemsRepeater` + `FlowLayout` 支持带虚拟化的流式布局；卡片量小（< 200）收益有限。`x:DataType` 开启编译
绑定，避免反射绑定开销。

## 综合建议（当时 3 项）

1. 派生数据优先 + 记忆化 Selector：`TryRefreshCardSize` 改为纯函数 Selector，`_lastApplied*` 守卫改
   MemoizedSelector，移除 `_isHandlingSettingsChange`。
2. 状态集中化：Layout VM 私有字段分类迁移到 Store，`ReplaceEntries`/`ReplaceAllEntries` 统一为单一 dispatch。
3. 声明式属性依赖：`OnPropertyChanged` 改 `[NotifyPropertyChangedFor]`，列表属性用 `ImmutableList<T>`。

## 来源

| 来源 | URL |
| --- | --- |
| React — You Might Not Need an Effect | <https://react.dev/learn/you-might-not-need-an-effect> |
| Redux — Deriving Data with Selectors | <https://redux.js.org/usage/deriving-data-selectors> |
| Fluxor Tutorial 01A/01B | <https://github.com/mrpmorris/Fluxor> |
| CommunityToolkit.Mvvm | <https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/> |
| Avalonia Docs | <https://docs.avaloniaui.net/docs/concepts/reactiveui> |
| Martin Fowler — Event Sourcing | <https://www.martinfowler.com/eaaDev/EventSourcing.html> |
| Microsoft — CQRS Pattern | <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/> |
| Wikipedia — Command Pattern | <https://en.wikipedia.org/wiki/Command_pattern> |
| Avalonia Community Discussions | <https://github.com/AvaloniaUI/Avalonia/discussions> |
