# Implement: Phase 2 DynamicData + 状态机

## Batch 0: 基础设施

### 0.1 添加 DynamicData NuGet 包
- [ ] `Directory.Packages.props`: `<PackageVersion Include="DynamicData" Version="9.4.33" />`
- [ ] `WTLauncher.App.csproj`: `<PackageReference Include="DynamicData" />`
- [ ] `dotnet build -warnaserror` 验证通过

**验证**: `dotnet build -warnaserror` 通过

### 0.2 创建 EntryProjection 类
- [ ] 新增 `App/Features/Entries/EntryProjection.cs`
- [ ] SourceCache<string, LauncherEntryViewModel> 作为数据源
- [ ] Connect().Filter().Sort().Bind() 声明式管道
- [ ] BehaviorSubject 驱动搜索/排序变化

**验证**: 编译通过

## Batch 1: EntryListViewModel 改造

### 1.1 替换双 ObservableCollection 为 SourceCache
- [ ] 删除 `_entries` 和 `_visibleEntries` 字段
- [ ] 添加 `EntryProjection` 实例
- [ ] `VisibleEntries` 改为从 EntryProjection 获取
- [ ] `ReplaceEntries()` → `_projection.LoadEntries()`
- [ ] 删除 `EntiresChanged` 事件
- [ ] 删除 `CountChanged` 事件

### 1.2 搜索自动过滤
- [ ] OnSearchTextChanged → `_projection.SetFilter(value)`
- [ ] 删除手工 `ApplyFilter` 调用
- [ ] 搜索时 Filter 管道自动更新

### 1.3 排序自动切换
- [ ] OnSortModeChanged → `_projection.SetSortMode(value)`
- [ ] 删除手工 `RebuildVisibleGroups` 调用

**验证**: `dotnet test -c Release` 通过

## Batch 2: 状态机

### 2.1 创建 AppStateMachine
- [ ] 新增 `App/ViewModels/AppStateMachine.cs`
- [ ] AppUIState 枚举: Normal, Searching, Dragging, DialogOpen, Saving
- [ ] AppUITrigger 枚举: 所有合法转换
- [ ] Stateless 配置所有状态转换

**验证**: 编译通过

## Batch 3: VTuber-Killer 简化

### 3.1 删除 EventJournal
- [ ] 删除 `Core/Events/EventJournal.cs`
- [ ] 删除 `Core/Events/EventJsonContext.cs`
- [ ] 删除 `Core/Events/LauncherEvent.cs`（14 种事件类型）

### 3.2 简化 StoreGrain
- [ ] 删除 Event Sourcing 相关代码
- [ ] 保留 Dispatch 方法（纯 Reducer）
- [ ] 保留 SemaphoreSlim 线程安全

### 3.3 删除 UiStateProjection
- [ ] 删除 `App/Infrastructure/UiStateProjection.cs`

**验证**: `dotnet test -c Release` 全绿

## Batch 4: 全量验证

### 4.1 运行完整 gate
- [ ] `dotnet build -warnaserror` 零错误
- [ ] `dotnet test -c Release` 705+ tests PASS
- [ ] `tools/verify.ps1` PASS

## 执行顺序

```
0.1 → 0.2 → 1.1 → 1.2 → 1.3 → 2.1 → 3.1 → 3.2 → 3.3 → 4.1
```
