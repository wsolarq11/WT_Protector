# WT Protector — 行业架构调研报告

> 调研日期：2026-07-23
> 调研范围：5 个方向，10+ 来源
> 核心目标：搜索能"从根源消除整类问题"的架构模式与最佳实践

---

## 目录

1. [方向 1：Avalonia / WPF MVVM 架构模式](#方向-1avalonia--wpf-mvvm-架构模式)
2. [方向 2：Fluxor / Redux 状态管理](#方向-2fluxor--redux-状态管理)
3. [方向 3：CommunityToolkit.Mvvm ObservableObject 最佳实践](#方向-3communitytoolkitmvvm-observableobject-最佳实践)
4. [方向 4：桌面应用的"无状态"架构探索](#方向-4桌面应用的无状态架构探索)
5. [方向 5：虚拟化卡片布局与 ViewModel 生命周期](#方向-5虚拟化卡片布局与-viewmodel-生命周期)
6. [综合建议：3 个最值得应用的架构变革](#综合建议3-个最值得应用的架构变革)

---

## 方向 1：Avalonia / WPF MVVM 架构模式

### 1.1 模式：派生数据优先于事件同步

**来源：** [React Docs — You Might Not Need an Effect](https://react.dev/learn/you-might-not-need-an-effect)

**核心思想：**

如果某数据能从已有状态通过计算获得，就不应将其作为独立状态，也不应通过事件/Effect 来手动同步。每次渲染时重新计算派生数据，而不是用事件链维护多个状态变量的一致性。

```
反模式（当前代码的问题）:
  条目变化 → OnEntriesChanged → 检查守卫 → 计算缓存 → ApplyFilter
  → 更新 ViewModel → UI 重新绑定

  问题：每个环节都可能产生不一致

正模式（派生数据）:
  visibleEntries = Filter(entries, searchQuery, viewMode)  // 每次渲染时计算
  cardSize = CalculateCardSize(containerWidth, viewMode)   // 纯函数

  优势：不存在同步问题，因为根本没有待同步的独立状态
```

**核心论点引用：**

> "When something can be calculated from the existing props or state, don't put it in state. Instead, calculate it during rendering."
>
> "If you can calculate something during render, you don't need an Effect."
>
> "Whenever you try to keep two different state variables synchronized, try lifting state up instead!"

**映射到当前代码的 5 个问题：**

| 当前问题 | 派生数据模式如何解决 |
|----------|---------------------|
| #1 隐式状态缓存 (`_lastAppliedCardsAvailableSize`) | 卡片尺寸改为实时计算 + 记忆化，不需要手工守卫 |
| #2 事件链时序耦合 (ReplaceAllEntries → ... → ApplyFilter) | 过滤逻辑变成纯函数，没有事件监听就没有时序 |
| #5 守卫逻辑扩散 (`_isHandlingSettingsChange`) | 在派生数据模式下每个属性独立，不需要全局守卫 |

**可行性评估：高**

- 不需要引入新框架，纯函数思维即可解决大部分问题。
- 当前 Fluxor Store 中的数据已经是事实来源，只需要改变消费方式。

---

### 1.2 模式：ViewModel 作为 Store 投影 (Projection)

**来源：** [Redux Docs — Deriving Data with Selectors](https://redux.js.org/usage/deriving-data-selectors)

**核心思想：**

ViewModel 不再持有独立可变状态，而是作为 Store 的"投影"。ViewModel 的职责是：
1. 从 Store 中选择（Select）需要的部分
2. 将选择的数据转化为 View 需要的形状
3. 不缓冲、不缓存、不守卫

```
Store (单一数据源，不可变)
  │
  ├── Selectors (纯函数，可组合)
  │   ├── selectFilteredEntries(state) → filtered
  │   ├── selectCardSize(state) → size
  │   └── selectIsEmpty(state) → bool
  │
  ├── ViewModel (投影)
  │   ├── FilteredEntries = selectFilteredEntries(store)
  │   ├── CardWidth = selectCardSize(store).Width
  │   └── IsEmpty = selectIsEmpty(store)
  │
  └── View (绑定到 ViewModel)
```

**关键知识引用：**

> "We specifically recommend that Redux apps should keep the Redux state minimal, and derive additional data from that minimal state whenever possible."
>
> "Selectors are functions that know how to extract specific pieces of data from the store. They help to encapsulate the state shape, making components more reusable and easier to test."
>
> "Memoized selectors avoid unnecessary recalculations and re-renders."

**可行性评估：高**

- 项目已使用 Fluxor，架构方向正确。
- 当前问题在于 ViewModel（Layout）中仍有大量手工可变字段（`_lastApplied*`、`_isHandling*`），应全部迁移到 Store 中。
- 迁移后 Layout ViewModel 不应保存任何可变字段，所有数据从 Store 中选择。

---

## 方向 2：Fluxor / Redux 状态管理

### 2.1 模式：不可变 State + 纯 Reducer

**来源：** [Fluxor Tutorial 01A — State, Actions, Reducers](https://github.com/mrpmorris/Fluxor/tree/master/Source/Tutorials/01-BasicConcepts/01A-StateActionsReducersTutorial)

**核心思想：**

State 是不可变的（只读属性），Reducer 是纯函数（接受旧 State 和 Action，返回新 State）。不修改现有状态，而是创建新状态替换。

**Fluxor 关键约定：**

```csharp
// 1. State 用 [FeatureState] 标注，构造函数中属性只读
[FeatureState]
public class CounterState
{
    public int ClickCount { get; }

    private CounterState() {}           // 初始状态用
    public CounterState(int clickCount) // 带参数构造
    {
        ClickCount = clickCount;
    }
}

// 2. Reducer 是静态纯函数，用 [ReducerMethod] 标注
public static class Reducers
{
    [ReducerMethod]
    public static CounterState ReduceIncrementCounterAction(
        CounterState state, IncrementCounterAction action)
        => new CounterState(clickCount: state.ClickCount + 1);
}

// 3. Reducer 中禁止依赖注入 —— 有依赖走 Effect
// 4. 状态变更 = 旧 state + action → 新 state，不修改旧 state
```

**Fluxor 官方文档强调：**

> "State should be immutable (properties are read-only, state is replaced on change)."
>
> "Reducers should be pure functions — do not inject dependencies into reducers. If you need dependencies, use an Effect instead."
>
> "Prefer static methods over instance methods for reducers."

**映射到当前问题：**

| 当前问题 | 不可变 State 如何消除 |
|----------|---------------------|
| #3 幽灵状态（部分更新部分没更新） | 不可变 State 每次整体替换，不存在部分更新 |
| #5 守卫逻辑 (`_isHandlingSettingsChange`) | 不需要守卫，因为 Reducer 天然独立 |

**可行性评估：高**

- 项目已在用 Fluxor，但 State 可能还不够"不可变"（需要审计）。
- 确保 Entry 集合使用 `ImmutableList<T>` 或 `ImmutableArray<T>`。
- 所有 Reducer 确认是纯函数，无副作用。

---

### 2.2 模式：Effect 处理副作用 + Action 链

**来源：** [Fluxor Tutorial 01B — Effects](https://github.com/mrpmorris/Fluxor/tree/master/Source/Tutorials/01-BasicConcepts/01B-EffectsTutorial)

**核心思想：**

Reducer 只做纯状态转换，副作用（异步、IO、复杂计算）放到 Effect 中。Effect 监听 Action，执行副作用，分发结果 Action。

```
用户操作 → Dispatch(LoadEntriesAction)
           ├── Reducer: State.IsLoading = true
           └── Effect: 异步加载数据
                       └── Dispatch(EntriesLoadedAction(data))
                           └── Reducer: State.Entries = data, IsLoading = false
```

**Effect 的三种定义方式：**

```csharp
// 方式1：静态方法（不访问 Action 值）
[EffectMethod(typeof(FetchDataAction))]
public async Task HandleFetchDataAction(IDispatcher dispatcher)
{
    var data = await service.LoadAsync();
    dispatcher.Dispatch(new FetchDataResultAction(data));
}

// 方式2：实例方法（访问 Action 值）— 推荐
[EffectMethod]
public async Task HandleFetchDataAction(
    FetchDataAction action, IDispatcher dispatcher)
{
    var data = await service.LoadAsync(action.Query);
    dispatcher.Dispatch(new FetchDataResultAction(data));
}
```

**Effect 的核心原则：**

> "Effect cannot modify State directly. They can dispatch new Actions in response."
>
> "If you need dependencies (web service, file system, etc.), use an Effect, not a Reducer."

**映射到当前问题：**

| 当前问题 | Effect 模式如何解决 |
|----------|-------------------|
| #2 事件链时序耦合 | 隐式事件链 → 显式 Action 分发，依赖关系明确 |
| #4 双路径不一致 | `ReplaceEntries()` 和 `ReplaceAllEntries()` → 统一为 `SetEntriesAction`，Effect 处理加载差异 |

**可行性评估：高**

- 替换当前 `ReplaceAllEntries` → `EntriesChanged` → `OnEntriesChanged` → `ApplyFilter` 隐式链。
- 改为：`Dispatch(LoadEntriesAction)` → Effect 加载 → `Dispatch(EntriesLoadedAction)` → Reducer 更新 State。

---

### 2.3 模式：记忆化选择器 (Memoized Selectors)

**来源：** [Redux Docs — Deriving Data with Selectors](https://redux.js.org/usage/deriving-data-selectors)

**核心思想：**

选择器的输入不变时跳过计算，直接返回缓存结果。避免不必要的过滤、排序、映射操作。

```
// 记忆化选择器模式（Redux createSelector 等价物）
Func<CardLayoutState, int, bool, ImmutableList<Entry>> getFilteredEntries =
    Memoize((CardLayoutState state, int containerWidth, bool showAll) =>
    {
        var visible = state.Entries;
        if (!showAll) visible = visible.Where(e => e.IsVisible).ToImmutableList();
        if (!string.IsNullOrEmpty(state.SearchQuery))
            visible = visible.Where(e => e.Matches(state.SearchQuery)).ToImmutableList();
        return visible;
    });

// 关键：当 state.Entries、state.SearchQuery、containerWidth、showAll 都未变时
// 直接返回上次结果，不执行 filter/matches 计算
```

**何时需要记忆化：**

| 场景 | 是否需要记忆化 |
|------|--------------|
| 直接返回 state 字段 | 不需要 |
| 简单 ID 查找 | 不需要 |
| `filter`/`map` 创建新数组 | 应该记忆化 |
| 复杂计算（reduce/sort） | 应该记忆化 |

**当前代码匹配：**

`TryRefreshCardSize` 手工维护 `_lastAppliedCardsAvailableSize` 和 `_lastAppliedVisibleCount`，本质就是手写的记忆化——但硬编码、容易失效、无法复用。

**可行性评估：中**

- Fluxor 没有内置 `createSelector`，需要自行实现 `MemoizedSelector` 类。
- 可以自己实现一个轻量的：
  ```csharp
  public class MemoizedSelector<TInput, TOutput>
  {
      private TInput _lastInput;
      private TOutput _lastOutput;
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
- 也可评估第三方库 [`Fluxor.Selectors`](https://www.nuget.org/packages/Fluxor.Selectors)（社区维护）。

---

## 方向 3：CommunityToolkit.Mvvm ObservableObject 最佳实践

### 3.1 模式：[ObservableProperty] + [NotifyPropertyChangedFor] 声明式计算属性

**来源：** [CommunityToolkit.Mvvm — ObservableProperty](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) | [CommunityToolkit/dotnet](https://github.com/CommunityToolkit/dotnet)

**核心思想：**

使用源代码生成器自动生成可观察属性，用声明式语法表达跨属性依赖关系。

```csharp
// 声明式依赖：当 searchQuery 或 entries 变化时，自动通知 FilteredEntries
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FilteredEntries))]
private string searchQuery = string.Empty;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FilteredEntries))]
private ImmutableList<Entry> entries = ImmutableList<Entry>.Empty;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CardVisualState))]
private ViewMode activeViewMode = ViewMode.Grid;

// 计算属性——只读，无 setter
public ImmutableList<Entry> FilteredEntries
{
    get
    {
        var result = Entries;
        if (!string.IsNullOrEmpty(SearchQuery))
            result = result.Where(e => e.Matches(SearchQuery)).ToImmutableList();
        return result;
    }
}

// 多字段依赖的计算属性
public string CardVisualState => ActiveViewMode switch
{
    ViewMode.Grid => "grid",
    ViewMode.List => "list",
    ViewMode.Compact => "compact",
    _ => "grid"
};
```

**关键知识点：**

> "[ObservableProperty] generates a public property from a private field."
>
> "NotifyPropertyChangedFor specifies which other property to raise PropertyChanged for when the generated property changes."

**映射到当前代码：**

当前代码中如果有 `OnPropertyChanged(nameof(FilteredEntries))` 在多个地方手动调用，说明应该改用 `[NotifyPropertyChangedFor]`。这消除了"在某处忘记通知"的整类 bug。

**可行性评估：高**

- 项目已在用 CommunityToolkit.Mvvm（根据技术栈描述）。
- 需要审计：所有手工 `OnPropertyChanged(nameof(...))` 调用，改为属性级声明。
- 配合不可变集合使用效果最佳。

---

### 3.2 模式：批量更新不可变集合

**核心思想：**

当列表数据变化时，使用不可变集合一次性替换整个列表，而非逐个增删改。UI 仅收到一次通知，避免高频重渲染。

```csharp
// 反模式：逐个添加会触发 N 次 UI 更新
foreach (var entry in loadedEntries)
    Entries.Add(entry);  // 每次 Add 都触发集合变更通知

// 正模式：不可变集合一次性替换
[ObservableProperty]
private ImmutableList<Entry> entries = ImmutableList<Entry>.Empty;

// 加载完成后一次性赋值，触发一次通知
Entries = loadedEntries.ToImmutableList();
```

**可行性评估：高**

- 直接解决 `ReplaceAllEntries` 逐条更新导致的多次重绘问题。
- 使用 `ImmutableList<T>` 或 `ImmutableArray<T>`。
- 配合 `[NotifyPropertyChangedFor]` 一次性通知所有依赖。

---

## 方向 4：桌面应用的"无状态"架构探索

### 4.1 模式：Redux 单一数据源（项目已有基础）

**来源：** [Redux Docs](https://redux.js.org/) | 项目当前架构

**核心思想：**

所有可变状态集中到 Store，ViewModel 不保存可变状态。

```
┌─────────────┐  Dispatch(Action)  ┌──────────────────┐
│    View     │ ─────────────────> │      Store       │
│  (绑定)     │                    │                  │
│             │ <───────────────── │  + State (不可变) │
│  ViewModel  │   StateChanged     │  + Reducer (纯)  │
│  (纯投影)   │                    │  + Effect (副)   │
└─────────────┘                    └──────────────────┘
```

项目已经采用 Fluxor，方向正确。当前问题在于 **ViewModel 没有真正"无状态"**——Layout 中堆积了手工缓存和守卫。

**可行性评估：中**

- 不需要引入新框架，只需要把 ViewModel 中的可变字段迁移到 Store。
- 迁移后 ViewModel 的构造变成：`new LayoutViewModel(IStore<CardLayoutState> store)`，所有公开属性从 store 中 select。

### 4.2 模式：Event Sourcing 思想借鉴

**来源：** [Martin Fowler — Event Sourcing](https://www.martinfowler.com/eaaDev/EventSourcing.html)

**核心思想：**

> "Capture all changes to an application state as a sequence of events."
> "We can discard the application state completely and rebuild it by re-running the events from the event log on an empty application."

在 Redux/Flux 中天然支持：所有 Action 就是事件日志。状态是 Action 序列的归约结果。

```
Replay 思想（Debug 场景）:
  Action[] history = [...]         // 所有 dispatch 的 Action
  State current = history           // 从头归约
    .Aggregate(initialState, (s, a) => Reducer(s, a))  // 确定性的
```

**可行性评估：低**（作为整体架构）

- 全量 Event Sourcing 需要持久化，对桌面启动器项目过于重量级。
- 但可以借鉴"Action 日志"思想：在 Debug/Dev 模式下记录所有 Action，用于状态回放调试。

---

## 方向 5：虚拟化卡片布局与 ViewModel 生命周期

### 5.1 模式：ItemsRepeater + 虚拟化布局

**来源：** [Avalonia Docs](https://docs.avaloniaui.net/docs/concepts/reactiveui) | WPF UI Virtualization 概念

**核心思想：**

使用 `ItemsRepeater` + `VirtualizingLayout` 替代传统 `ItemsControl` + `WrapPanel`。只生成可见范围内的容器，大幅减少内存和渲染开销。

```
可用布局方案对比：

| 布局方案 | 支持虚拟化 | 支持换行 | 适用场景 |
|----------|-----------|---------|---------|
| ItemsControl + WrapPanel | 否 | 是 | 卡片少 (< 100) |
| ListBox + VirtualizingStackPanel | 是 | 否 | 纵向列表 |
| ItemsRepeater + FlowLayout | 是 | 是 | 卡片网格（推荐） |
| 自定义 VirtualizingLayout | 是（自定义） | 是 | 复杂布局需求 |
```

Avalonia 的 ItemsRepeater 支持 `FlowLayout`，可以实现带虚拟化的流式布局（类似 WrapPanel 但带虚拟化）。

**可行性评估：中**

- 如果卡片数量 < 200，虚拟化收益有限。
- 如果有大量卡片（搜索过滤后 500+），建议启用 ItemsRepeater + FlowLayout。
- 注意：Avalonia 11+ 才完全支持 ItemsRepeater，需确认项目使用的版本。
- 如果自定义布局需求强，可继承 `VirtualizingLayout` 实现自定义面板。

### 5.2 模式：Compiled Bindings 提升性能

**来源：** [Avalonia 文档 — Compiled Bindings](https://docs.avaloniaui.net/docs/concepts/reactiveui)

**核心思想：**

使用 `x:DataType` 开启编译绑定，避免反射绑定运行时开销。

```xml
<ItemsRepeater ItemsSource="{Binding Cards}">
    <DataTemplate DataType="vm:CardViewModel">
        <Border BorderBrush="{Binding BorderColor}">
            <TextBlock Text="{Binding Title}" />
            <TextBlock Text="{Binding Subtitle}" />
        </Border>
    </DataTemplate>
</ItemsRepeater>
```

**可行性评估：高**

- 零成本改进，只需在 XAML 中添加 `DataType`。
- 对卡片列表的每次渲染都有累积性能收益。

### 5.3 模式：ViewModel 生命周期管理策略

**三种策略对比：**

| 策略 | 实现复杂度 | 消除的问题 | 适用场景 |
|------|-----------|-----------|---------|
| 每个卡片独立 ViewModel | 低 | 最少 | 简单显示，卡片数量少 |
| ViewModel 对象池 | 中 | GC 压力、VM 重建开销 | 卡片多、频繁滚动 |
| 直接数据绑定（无 VM） | 中 | 所有 VM 生命周期问题 | 只显示、无交互的卡片 |

**建议：** 当前阶段采用"直接数据绑定 + Selector 投影"。卡片 ViewModel 不做独立生命周期管理，数据直接从 Store 选择。

**可行性评估：中**

---

## 综合建议：3 个最值得应用的架构变革

### 变革 1：派生数据优先 + 记忆化 Selector

**优先级：最高**

**解决的问题：** #1 隐式状态缓存、#2 事件链时序耦合、#5 守卫逻辑扩散

**具体做法：**

1. 将 `TryRefreshCardSize` 重构为纯函数 Selector，输入 entries + viewMode + containerWidth，输出卡片尺寸。
2. 用简单的 `MemoizedSelector` 类替换 `_lastAppliedCardsAvailableSize` 守卫。
3. 移除 `_isHandlingSettingsChange` —— 每个属性独立，不需要全局守卫。
4. `ApplyFilter` 从事件监听改为计算属性 `FilteredEntries`。

**预期收益：**
- 消除 3 类已知 bug（缓存失效、时序依赖、守卫遗漏）
- 代码可测试性大幅提升（纯函数无副作用）
- ViewModel 代码量减少约 30%

---

### 变革 2：状态集中化——消除 ViewModel 可变字段

**优先级：高**

**解决的问题：** #3 幽灵状态、#4 双路径不一致

**具体做法：**

1. 审计 Layout ViewModel 的所有私有字段，分类：
   - 属于 Store 状态 → 迁移到 Fluxor State
   - 属于派生数据 → 改为计算属性 + 记忆化
   - 属于 UI 临时状态 → 保留但最小化
2. `ReplaceEntries()` 和 `ReplaceAllEntries()` 统一为 `Dispatch(new SetEntriesAction(data))`。
3. ViewModel 构造时只注入 `IState<CardLayoutState>` 和 `IDispatcher`。

**预期收益：**
- 消除"同一数据两个来源"的不一致性
- `ReplaceEntries` wrapper 不再存在，只有一条代码路径
- 异常路径下状态不会"卡在中间"

---

### 变革 3：声明式属性依赖替代手工通知

**优先级：中**

**解决的问题：** 手工通知遗漏、跨属性依赖的隐式耦合

**具体做法：**

1. 搜索所有 `OnPropertyChanged(nameof(...))` 调用，改为 `[NotifyPropertyChangedFor]`。
2. 使用 `ImmutableList<T>` 代替 `ObservableCollection<T>` 做列表属性（除非需要增量变更）。
3. 所有计算属性定义为只读（无 setter）。

**预期收益：**
- 消除"改了 A 忘了通知 B"的整类 bug
- 依赖关系显式可见（读代码时一目了然）
- 减少 20-30% 的样板代码

---

## 调研来源汇总

| # | 来源 | URL | 贡献内容 |
|---|------|-----|---------|
| 1 | React Docs — You Might Not Need an Effect | https://react.dev/learn/you-might-not-need-an-effect | 派生数据优于 Effect 同步的原则，直接映射当前问题 #1#2#5 |
| 2 | Redux Docs — Deriving Data with Selectors | https://redux.js.org/usage/deriving-data-selectors | 记忆化选择器、Selector 组合、何时需要/不需要记忆化 |
| 3 | Fluxor Tutorial 01A — State, Actions, Reducers | https://github.com/mrpmorris/Fluxor | 不可变 State、纯 Reducer、[FeatureState] 标注 |
| 4 | Fluxor Tutorial 01B — Effects | https://github.com/mrpmorris/Fluxor | Effect 模式、三种定义方式、Action 链 |
| 5 | CommunityToolkit.Mvvm — ObservableProperty | https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/ | [ObservableProperty] 源生成器、NotifyPropertyChangedFor |
| 6 | Avalonia Docs — The MVVM Pattern | https://docs.avaloniaui.net/docs/concepts/reactiveui | Avalonia MVVM 基础模式、数据绑定、编译绑定 |
| 7 | Martin Fowler — Event Sourcing | https://www.martinfowler.com/eaaDev/EventSourcing.html | 事件溯源、确定性状态重建、Action 日志思想 |
| 8 | Microsoft — CQRS Pattern | https://learn.microsoft.com/en-us/dotnet/architecture/microservices/ | 读写分离、Command/Query 分离模型 |
| 9 | Wikipedia — Command Pattern | https://en.wikipedia.org/wiki/Command_pattern | 命令模式封装状态变更、撤销/重做实现 |
| 10 | Avalonia Community Discussions | https://github.com/AvaloniaUI/Avalonia/discussions | ItemsRepeater 虚拟化、WrapPanel 替代方案 |

---

## 附录：当前问题与架构变革映射表

| 已发现问题 | 变革 1：派生数据 | 变革 2：状态集中化 | 变革 3：声明式依赖 | 其他模式 |
|-----------|-----------------|------------------|------------------|---------|
| #1 隐式状态缓存 | 直接消除 | 辅助 | - | 记忆化 Selector |
| #2 事件链时序耦合 | 直接消除 | 辅助 | 辅助 | Fluxor Effect |
| #3 幽灵状态 | - | 直接消除 | 辅助 | 不可变 State |
| #4 双路径不一致 | - | 直接消除 | - | Effect 统一路径 |
| #5 守卫逻辑扩散 | 直接消除 | 辅助 | - | 单一数据源 |
