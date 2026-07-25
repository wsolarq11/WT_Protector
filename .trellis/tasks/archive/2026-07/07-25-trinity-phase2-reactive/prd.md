# Phase 2: DynamicData 声明式投影 + 状态机

## Goal

在 Zone 1 引入 DynamicData 声明式投影管道和 Stateless 形式化状态机，
消除根因 #2（手工投影漂移）和根因 #3（无形式化状态机）。

## Background

当前 EntryListViewModel 使用两个 `ObservableCollection` + 手动 `EntiresChanged` 事件 +
8 个 UI 桥接委托来管理投影。这导致了：

- `ReplaceEntries` vs `ReplaceAllEntries` 双路径不一致
- `ApplyFilter` → `RebuildVisibleGroups` 必须手工调用
- `_isHandlingSettingsChange` 守卫在 4 个 handler 中复制粘贴
- Drag commit 后如果跳过 ApplyFilter，VisibleGroups 不重建

DynamicData 的 `SourceCache<TKey, TValue>` 提供了声明式管道：
`.Connect().Filter().Sort().Group().Bind()` — 数据变化自动传播。

Stateless 已经在项目中使用（LauncherDragReorderWorkflow），需要扩展到整个 UI 状态。

## Requirements

### REQ-1: 引入 DynamicData NuGet 包
- 添加到 `Directory.Packages.props`
- 添加到 `WTLauncher.App.csproj` 引用
- 版本兼容 .NET 10

### REQ-2: EntryProjection 声明式管道
- `SourceCache<string, LauncherEntryViewModel>` 作为唯一数据源
- 声明式 Filter 管道（响应 SearchText 变化自动重过滤）
- 声明式 Sort 管道（响应 SortMode 变化自动重排序）
- 声明式 Group 管道（卡片视图通过 GroupOn 自动分组）
- `ReadOnlyObservableCollection` 绑定到 View 层

### REQ-3: 删除手工 ApplyFilter/RebuildVisibleGroups
- 删除 `_entries` 和 `_visibleEntries` 两个集合 → 统一 SourceCache
- 删除 `EntiresChanged` 事件（管道自动传播）
- 删除 `_isHandlingSettingsChange` 守卫
- 删除 `InvalidateCardSizeCache`（不需要）

### REQ-4: UI 状态机扩展
- 定义全局 `UIState` 枚举 + `UITrigger` 枚举
- Stateless 配置所有合法 UI 状态转换
- 状态转换（Normal / Searching / Dragging / DialogOpen / Saving）

### REQ-5: VTuber-Killer 简化
- 删除 `EventJournal.cs`
- 保留 `StoreGrain` 线程安全保证
- 保留 `ErrorSupervisor` 熔断器

## Non-goals

- 不改动 XAML 层（Phase 3）
- 不改动 LauncherStore/LauncherJson（Phase 1 已拆分）
- 不引入新的事件类型（删除而不是新增）
- 不改动 CsCheck PBT 测试

## Acceptance Criteria

- [ ] `DynamicData` NuGet 包已添加，构建通过
- [ ] `EntryProjection` 类存在，使用 SourceCache 作为数据源
- [ ] 搜索时 Filter 管道自动更新，不需要手工 ApplyFilter
- [ ] 切换排序模式时 Sort 管道自动更新
- [ ] 卡片视图分组自动传播
- [ ] `_entries` 和 `_visibleEntries` 双集合已删除
- [ ] `EntiresChanged` 事件已删除
- [ ] `_isHandlingSettingsChange` 已删除
- [ ] `EventJournal.cs` 已删除
- [ ] 所有 705 测试仍通过
- [ ] `dotnet build -warnaserror` 通过
- [ ] `tools/verify.ps1` PASS
