# Design: Phase 2 DynamicData + 状态机

## Architecture

### 声明式投影管道

```
用户输入 / 数据变更
       │
       ▼
SourceCache<string, LauncherEntryViewModel>   ← 唯一数据源
       │
       ▼
.Connect()
       │
       ├── .Filter(entry => MatchSearch(entry, searchText))   ← 自动响应 searchText 变化
       │
       ├── .Sort(SortExpressionComparer<T>)                   ← 自动响应 sortMode 变化
       │
       ├── .Group(groupKey)                                   ← 卡片视图自动分组
       │
       └── .Bind(out _visibleEntries)                         ← 自动同步到 ObservableCollection
              │
              ▼
        ReadOnlyObservableCollection<LauncherEntryViewModel>  ← View 层绑定
```

### EntryProjection 设计

```csharp
public sealed class EntryProjection : IDisposable
{
    private readonly SourceCache<string, LauncherEntryViewModel> _cache = new(static vm => vm.Id);
    private readonly BehaviorSubject<string> _filterSubject = new("");
    private readonly BehaviorSubject<SortMode> _sortSubject = new(SortMode.Custom);
    private readonly CompositeDisposable _cleanup = new();

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    public EntryProjection()
    {
        var disposable = _cache.Connect()
            .Filter(FilterBySearchText)
            .Sort(SortByMode)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var visibleEntries)
            .Subscribe();
        VisibleEntries = visibleEntries;
        _cleanup.Add(disposable);
    }

    public void LoadEntries(IEnumerable<LaunchEntry> entries) { ... }
    public void SetFilter(string text) => _filterSubject.OnNext(text);
    public void SetSortMode(string mode) => _sortSubject.OnNext(ParseMode(mode));
    public void Dispose() => _cleanup.Dispose();
}
```

### UI 状态机

```csharp
public enum AppUIState { Normal, Searching, Dragging, DialogOpen, Saving }
public enum AppUITrigger { StartSearch, EndSearch, StartDrag, Drop, Cancel, OpenDialog, CloseDialog, StartSave, SaveComplete }

public sealed class AppStateMachine
{
    private readonly StateMachine<AppUIState, AppUITrigger> _machine;

    public AppStateMachine()
    {
        _machine = new StateMachine<AppUIState, AppUITrigger>(AppUIState.Normal);
        _machine.Configure(AppUIState.Normal)
            .Permit(AppUITrigger.StartSearch, AppUIState.Searching)
            .Permit(AppUITrigger.StartDrag, AppUIState.Dragging)
            .Permit(AppUITrigger.OpenDialog, AppUIState.DialogOpen)
            .Permit(AppUITrigger.StartSave, AppUIState.Saving);
        _machine.Configure(AppUIState.Searching)
            .Permit(AppUITrigger.EndSearch, AppUIState.Normal)
            .Permit(AppUITrigger.StartDrag, AppUIState.Dragging)
            .Permit(AppUITrigger.OpenDialog, AppUIState.DialogOpen);
        // ... 更多转换
    }

    public bool CanFire(AppUITrigger trigger) => _machine.CanFire(trigger);
    public void Fire(AppUITrigger trigger) => _machine.Fire(trigger);
}
```

### VTuber-Killer 简化

删除 `EventJournal.cs`（142 行）和相关事件类型。StoreGrain 保留线程安全模型，
但状态变更改为直接纯 Reducer，不再经过 Event Sourcing。

### 文件变更

```
删除:
  Core/Events/EventJournal.cs
  Core/Events/EventJsonContext.cs
  Core/Events/LauncherEvent.cs (14 种事件类型)
  App/Infrastructure/UiStateProjection.cs

新增:
  App/Features/Entries/EntryProjection.cs    ← DynamicData 投影
  App/ViewModels/AppStateMachine.cs           ← UI 状态机

修改:
  App/Features/Entries/EntryListViewModel.cs  ← 用 SourceCache 替代双 ObservableCollection
  App/Infrastructure/StoreGrain.cs            ← 删除 Event Sourcing
  Directory.Packages.props                    ← 添加 DynamicData
  App/WTLauncher.App.csproj                   ← 引用 DynamicData
```
