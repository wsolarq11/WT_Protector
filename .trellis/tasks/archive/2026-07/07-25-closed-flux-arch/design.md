# Design: Closed Flux Architecture (CFA)

## 架构全景

```
┌────────────────────────────────────────────────────────────────────┐
│                        Closed Flux Architecture                     │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  Zone 0 (Domain)                   Zone 1 (Effects)                │
│  ┌─────────────────────┐          ┌─────────────────────┐          │
│  │ AppState (sealed    │          │ AppAction → Effect   │          │
│  │   record)           │─────────▶│ union               │          │
│  │ Reducer(state,action│          │ (compile-time        │          │
│  │   ) → state         │          │   exhaustive match)  │          │
│  │ Selector(state)→proj│          └──────────┬──────────┘          │
│  └─────────────────────┘                     │                      │
│        ▲                                     ▼                      │
│        │                           ┌─────────────────────┐          │
│        │                           │ EffectRunner         │          │
│        │    Zone 2 (Shell)          │ ┌─────────────────┐ │          │
│        │    ┌───────────────────┐   │ │ FileLauncherJson │ │          │
│        └────┤ StoreGrain        │   │ │   IO             │ │          │
│             │  .Dispatch(action)│──▶│ │ Process.Start    │ │          │
│             │  .Select(selector)│   │ │ ErrorLog         │ │          │
│             │  [Grain.Mutate    │   │ └─────────────────┘ │          │
│             │   锁内原子执行]    │   └─────────────────────┘          │
│             └────────┬──────────┘                                    │
│                      │                                               │
│             ┌────────▼──────────┐                                    │
│             │ ViewModel         │                                    │
│             │  [AutoSelector]   │ ← CommunityToolkit.Mvvm source gen │
│             └────────┬──────────┘                                    │
│                      │                                               │
│             ┌────────▼──────────┐                                    │
│             │ View (Avalonia     │                                    │
│             │   axaml)           │ ← Compiled bindings                │
│             └───────────────────┘                                    │
└────────────────────────────────────────────────────────────────────┘
```

## Step 1 设计详解: AppState 统一 + Grain 锁内原子化

### AppState 定义

```csharp
// Core/AppState.cs
namespace WTLauncher.Core;

public sealed record AppState
{
    public LauncherStoreFile Store { get; init; } = new();
    public string FilterText { get; init; } = "";
    public string SortModeId { get; init; } = LauncherEntryQuery.CustomSortModeId;
    public string ViewModeId { get; init; } = LauncherEntryViewModes.CardsId;
    public string CardsGroupKey { get; init; } = LauncherCardsGroupKeys.None;
    public bool RequireConfirmationForLaunches { get; init; } = true;
    public DragState DragState { get; init; } = DragState.Idle;
    public string? KeyboardFocusedEntryId { get; init; }
}

// 现有的 LauncherStoreFile 保持不变作为序列化形状
// AppState 是运行时状态 = StoreFile + UI 状态 + 临时状态
```

### StoreGrain 改造

```csharp
// App/Infrastructure/StoreGrain.cs
public sealed class StoreGrain : Grain<AppState>, IDisposable
{
    public void Dispatch(Func<AppState, AppState> reducer)
    {
        // ❌ 当前: 锁外 Save + 锁外 SetState → TOCTOU
        // ✅ 新: Mutate 在锁内完成整个原子块
        _grain.Mutate(state => {
            var newState = reducer(state);
            _persistence.Save(newState.Store, _storePath, backup: true);
            return newState;
        });
    }

    public T Select<T>(Func<AppState, T> selector) => ReadState(selector);
}
```

### Grain.Mutate 实现

```csharp
// App/Infrastructure/Grain.cs
public abstract class Grain<TState> where TState : class
{
    private readonly object _lock = new();
    private TState _state = null!;

    protected TState Mutate(Func<TState, TState> mutator)
    {
        lock (_lock)
        {
            _state = mutator(_state);  // 锁内原子执行全部操作
            return _state;
        }
    }
}
```

### MainWindowViewModel 消除 _settings

```csharp
// ❌ 当前: _settings 字段 + CurrentSettings 双源
// ✅ 新: 所有状态通过 StoreGrain.Select

// MainWindowViewModel.cs
public sealed partial class MainWindowViewModel
{
    // _settings 字段被删除！
    
    // 编译器生成的 AutoSelector
    [AutoSelector]
    public LauncherSettings Settings => _storeGrain.Select(s => s.Store.Settings);
    
    [AutoSelector]
    public string FilterText => _storeGrain.Select(s => s.FilterText);
    
    // 所有状态的唯一写入路径
    private void CommitSettings(Func<AppState, AppState> mutation)
        => _storeGrain.Dispatch(mutation);
}
```

## Step 2 设计详解: DI 化 + union 错误处理

### DI 容器配置

```csharp
// App/Program.cs
// ❌ 当前: 静态赋值
// LauncherJson.DefaultIO = new FileLauncherJsonIO();
// LauncherStore.DefaultPersistence = new FileStorePersistence();

// ✅ 新: DI 容器
var services = new ServiceCollection();
services.AddSingleton<ILauncherJsonIO, FileLauncherJsonIO>();
services.AddSingleton<IStorePersistence, FileStorePersistence>();
services.AddSingleton<StoreGrain>();
services.AddTransient<MainWindowViewModel>();

// LauncherJson.cs / LauncherStore.cs 的静态 DefaultIO 被删除
// 所有消费者通过构造器注入获取 ILauncherJsonIO
```

### union 错误处理（.NET 11 就绪后 / 渐进式不阻塞）

```csharp
// 预备：使用 ErrorOr 或 OneOf 过渡，.NET 11 后迁移到 union
// Core/LauncherResult.cs

public abstract record LauncherResult<T>;  // 当前 ErrorOr 立即保留

// .NET 11 迁移目标:
// public union LoadResult
// {
//     Success(LauncherStoreFile File),
//     NotFound,
//     Corrupted(string Error, string? BackupRestored),
//     IoError(string Message)
// }
```

### [MustHandleErrors] Analyzer

```csharp
// Analyzer 规则: 如果方法返回 ErrorOr<T> 且标注 [MustHandleErrors],
// 调用方必须:
//   1. switch 检查 .IsError / .Value, 或
//   2. 将 ErrorOr 传播到自己的返回类型
// 否则 → 编译错误 WT2004
```

## Step 3 设计详解: ViewModelBase + CompositeDisposable

### ViewModelBase 基类

```csharp
// App/ViewModels/ViewModelBase.cs
namespace WTLauncher.App.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CompositeDisposable _subscriptions = new();

    // 唯一合法的订阅方式
    protected IDisposable AutoSubscribe<T>(
        IObservable<T> source,
        Action<T> handler,
        [CallerMemberName] string? caller = null)
    {
        var sub = source.Subscribe(value => {
            try { handler(value); }
            catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
            {
                ErrorLog.Write(ex, $"AutoSubscribe.{caller}");
            }
        });
        return sub.DisposeWith(_subscriptions);
    }

    // 基类 Dispose → 一次性清理所有订阅
    public virtual void Dispose()
    {
        _subscriptions.Dispose();
    }
}
```

### LauncherEntryViewModel 事件泄漏修复

```csharp
// LauncherEntryViewModel.cs
public sealed partial class LauncherEntryViewModel : ViewModelBase
{
    // ❌ 当前: 构造函数订阅永不取消
    // _owner.CardLayout.PropertyChanged += OnCardLayoutPropertyChanged;

    // ✅ 新: 响应式选择器，零手动事件
    [AutoSelector]
    public double CardWidth => _owner.CardLayout.CardWidth;  // source gen 自动追踪
    
    // 不再需要 SubscribeToLayoutProvider() 方法
    // 不再需要 RefreshLayoutPresentation() 空方法
}
```

### 现有 VM 的 Dispose 集成

```csharp
// EntryListViewModel.ReplaceEntries()
public void ReplaceEntries(IEnumerable<LaunchEntry> entries)
{
    // Dispose 旧的 ViewModel 实例
    foreach (var vm in _entries)
        vm.Dispose();            // ← 新增：取消所有订阅
    _entries.Clear();
    _visibleEntries.Clear();
    foreach (var entry in entries)
    {
        var vm = new LauncherEntryViewModel(entry, this);
        _entries.Add(vm);
        _visibleEntries.Add(vm);
    }
}
```

## Step 4 设计详解: 强类型值对象 + 验证内联

### EntryId 重构

```csharp
// Core/EntryId.cs — 删除 Vogen 双包装，改为纯 record

// ❌ 当前:
// VogenEntryId (ValueObjects/EntryId.cs) + Core.EntryId (EntryId.cs) 双包装

// ✅ 新: .NET 11 record — 在构造器中完成验证，编译时保证
public sealed record EntryId
{
    public string Value { get; }

    public EntryId(string raw)
    {
        // 所有验证在构造器中 —— 编译器保证每条路径都经过
        if (string.IsNullOrWhiteSpace(raw))
            throw new FormatException("ID 不能为空。");
        var trimmed = raw.Trim().Normalize(NormalizationForm.FormC);
        // ... Latin 脚本检查、长度限制、保留名检查 ...
        Value = trimmed;
    }

    public static EntryId Empty => new(string.Empty);  // 仅内部使用
}
```

### LauncherPreset 强类型化

```csharp
// LauncherPreset.cs
public sealed record LauncherPreset
{
    public PresetId Id { get; init; } = PresetId.Empty;     // ❌ 之前是 string
    public PresetName Name { get; init; } = PresetName.Empty; // ❌ 之前是 string
    public ImmutableList<EntryId> EntryIds { get; init; }     // ❌ 之前是 ImmutableList<string>
        = ImmutableList<EntryId>.Empty;
    public bool Locked { get; init; }
}
```

## Step 5 设计详解: 新增 Roslyn Analyzer

### WT1007: View 层禁止业务逻辑

```csharp
// 命名空间 WTLauncher.App.Views 中的类型禁止出现:
// - if / switch / for / while 语句 (允许 foreach 和 using)
// - 直接赋值属性值 (允许绑定和 DataContext)
// - 调用 System.IO 方法
// 允许: 事件转发、ShowDialog、Drag hit-test、焦点管理
```

### WT1008: ViewModel 禁止手动 setter

```csharp
// ViewModel 中禁止:
// - 公共 setter（[ObservableProperty] 生成的除外）
// - public void SetXxx(T value) 模式的方法
// 允许: [RelayCommand] 方法、private 方法
```

### WT1009: Selector 属性不可赋值

```csharp
// 被 [AutoSelector] 标记的属性禁止出现在赋值左侧
// viewModel.Settings = newSettings;  // 编译错误！
// 必须通过: storeGrain.Dispatch(s => s with { Settings = newSettings });
```

### WT1010: 订阅必须有生命周期管理

```csharp
// IObservable<T>.Subscribe() 的返回值必须:
// 1. 赋给 IDisposable 局部变量, 或
// 2. 通过 DisposeWith(IDisposable container) 加入容器, 或
// 3. 赋给类的 IDisposable 字段
// 否则 → 编译警告
```

### WT2001-WT2003: Zone 0 纯函数守卫

```csharp
// WT2001: Core 命名空间禁止 using System.IO;
// WT2002: 禁止非 readonly 静态字段赋值
// WT2003: 如果存在 static Validate() 方法 → 警告提示内联到构造器
```
