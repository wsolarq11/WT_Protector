# Technical Design — Cards Command Grouping

## Architecture Decisions

### ADR-1: 分组是 Cards 子选项,不是新 view mode

**决策**: `settings.cardsGroupKey ∈ {none, command, directory}` 作为 Cards 视图的子选项,不加第 4 种 view mode。

**否决**:
- 新 view mode(grouped-cards): 用户切到 Details/Names 后分组消失,失去一致性。
- 全局 sort mode 扩展: 排序 ≠ 分组 — 排序不显示组头。

**理由**: 分组只在 Cards 视图有意义(Details/Names 已是列表,分组头会破坏列表节奏)。Cards 子选项最贴合用户心智。

### ADR-2: 用嵌套 ItemsControl,而非 flat list 含 header items

**决策**: `CardsEntriesItemsControl.ItemsSource` 改为 `VisibleGroups`,每个 group 内嵌一个 ItemsControl 渲染该组的 cards。

**否决**:
- Flat list(VisibleCards: ObservableCollection<object>,混排 LauncherEntryViewModel 和 LauncherCardGroupHeaderViewModel): 需要 DataTemplateSelector,且 drag/keyboard 导航跨 group 复杂。
- 自实现 ItemsControl: 重造轮子。

**理由**: 嵌套 ItemsControl 是 Avalonia 标准模式,DataTemplate 自然处理 group + card 模板。Drag/键盘导航仍作用于最内层 ItemsControl,scope 限制在单组内。

### ADR-3: 折叠状态是 session-only,不持久化

**决策**: `LauncherCardGroupViewModel.IsExpanded` 是内存状态,不存到 settings。

**否决**:
- 持久化 `Dictionary<string, bool>`: 滥用 settings 字段,用户每次启动想看到全貌,持久化反而打扰。
- 跟 settings.cardsGroupKey 一起持久化: 同上。

**理由**: 折叠是临时浏览行为,不是用户偏好。重启回到全展开。

### ADR-4: 组排序 = 引用次数倒序 + 字典序

**决策**: 组按 (count desc, key asc) 排序。14 个 codex 在前,1 个的在后。

**否决**:
- 字典序优先: `$codex` 排不到前面,失去视觉降噪价值。
- 自定义组排序: 增加 UI 复杂度,YAGNI。

**理由**: 高频组在前是用户最想看到的(查重视角)。

### ADR-5: 启所有 / 存为启动组 走现有路径

**决策**:
- 启所有 → 复用 `MainWindowViewModel.LaunchBatchAsync(IEnumerable<LauncherEntryViewModel>)`(若已存在,否则暴露公共方法)
- 存为启动组 → 复用 `RequestNewPresetAsync` 注入点,prefill 该组的 entry IDs

**否决**:
- 组头自己启动: 违反"VM 拥有 launch 状态"原则。
- 新加 preset mutation 路径: 复用现有 PresetDetailsWindow 流程。

**理由**: 现有路径已经经过测试。复用降低新 bug 风险。

### ADR-6: 当 groupKey=none 时,VisibleGroups 是单组"all"

**决策**: 始终用 VisibleGroups 渲染。当 groupKey=none,VisibleGroups 含 1 个特殊组(group key = "",entries = VisibleEntries,不显示组头)。

**否决**:
- groupKey=none 时切换 ItemsSource 到 VisibleEntries: 需要在 XAML 加 conditional binding,违反 single source 原则。
- groupKey=none 时隐藏 group header: 同上,但单组"all"实现一致。

**理由**: 单源真 — VisibleGroups 始终是渲染源。groupKey=none 时单组无 header。

## Data Flow

```
[用户改 groupKey ComboBox]
    ↓
[MainWindowViewModel.CardsGroupKey setter]
    ↓
[snapshot → _settings = with { CardsGroupKey = ... } → SaveStoreFile]
    ↓
[RebuildVisibleGroups()]
    ↓
[LauncherEntryGrouper.Group(VisibleEntries, groupKey) → IReadOnlyList<CardGroup>]
    ↓
[VisibleGroups.Clear() + Add(new LauncherCardGroupViewModel(...))]
    ↓
[XAML CardsEntriesItemsControl.ItemsSource=VisibleGroups 自动重渲染]
    ↓
[每组: header template + nested ItemsControl(cards)]
    ↓
[组头按钮点击 → MainWindowViewModel 方法(launch all / save as preset)]
```

## Contracts

### Core

```csharp
public static class LauncherCardsGroupKeys
{
    public const string None = "none";
    public const string Command = "command";
    public const string Directory = "directory";
}

public sealed record CardGroup(
    string Key,           // "$codex" or "D:\proj\foo" or "" (none mode)
    int Count,
    IReadOnlyList<LaunchEntry> Entries);

public static class LauncherEntryGrouper
{
    public static IReadOnlyList<CardGroup> Group(
        IEnumerable<LaunchEntry> entries,
        string groupKey);  // none/command/directory
}
```

### App

```csharp
public sealed partial class LauncherCardGroupViewModel : ObservableObject
{
    public string Key { get; }
    public string DisplayName { get; }      // "$codex" or "D:\proj\foo"
    public int Count { get; }
    public ObservableCollection<LauncherEntryViewModel> Entries { get; }

    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private bool _showHeader;   // false when groupKey=none

    [RelayCommand] private void ToggleExpanded() => IsExpanded = !IsExpanded;
    [RelayCommand] private async Task LaunchAllAsync() { ... }       // delegate to owner
    [RelayCommand] private async Task SaveAsPresetAsync() { ... }    // delegate to owner
}
```

## Compatibility / Rollback

- `settings.cardsGroupKey` 是 optional(默认 "none"),旧 store 文件零迁移可读
- schemaVersion 保持 2
- 不改 `entry` 字段
- 不改 view mode 枚举(不加第 4 种 view mode)
- Rollback: revert CardsGroupKey 字段 + VisibleGroups 属性 + XAML nested ItemsControl 改动

## Risks

- **风险 1**: 嵌套 ItemsControl 破坏 drag-and-drop。**缓解**: drag handler 仍作用于 card 层,组层只渲染不交互。手动 smoke 验证。
- **风险 2**: 折叠状态在 ApplyFilter / Refresh 时被重置(VisibleGroups 重建)。**缓解**: 文档化"折叠是 session-only,filter/sort 变更会重置"。
- **风险 3**: 启所有时 14 个 entry 同时启动可能资源压力。**缓解**: 走现有 batch launch(preflight + 错误报告),用户已习惯 batch launch 行为。

## Test Strategy

- `LauncherEntryGrouper` 单元测试:
  - groupKey=none → 单组"all",含所有 entries,无 header
  - groupKey=command → 按 command 分组,引用次数倒序
  - groupKey=directory → 按 directory 分组
  - 空集合 → 空组列表
- `MainWindowViewModel` 集成测试:
  - 改 CardsGroupKey → settings 持久化 + VisibleGroups 重建
  - VisibleGroups 在 filter/sort 变更时正确刷新
- 现有 544 测试保持通过(+6 新 → 550+)。
- Gate 全 PASS。
