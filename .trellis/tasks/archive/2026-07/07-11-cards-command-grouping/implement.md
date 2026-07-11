# Execution Plan — Cards Command Grouping

## Pre-flight

- [ ] `task.py current` 指向 `07-11-cards-command-grouping`,status=in_progress。
- [ ] 基线: `tools/verify.ps1` 当前 PASS(544 测试)。
- [ ] Child 3 + 4 已归档。

## Phase A: Core helper

1. **A1** — 新建 `src/avalonia-launcher/src/WTLauncher.Core/LauncherCardsGroupKeys.cs`:
   - `LauncherCardsGroupKeys` 静态类含 None/Command/Directory 常量
   - `CardGroup` record(Key, Count, Entries)
2. **A2** — 新建 `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryGrouper.cs`:
   - 静态类 `Group(IEnumerable<LaunchEntry>, string groupKey) → IReadOnlyList<CardGroup>`
   - groupKey=none → 单组(key="", count=entries.Count, entries=all)
   - groupKey=command → 按 e.Command OrdinalIgnoreCase 分组
   - groupKey=directory → 按 e.Directory OrdinalIgnoreCase 分组
   - 未知 groupKey → 视为 none
   - 排序: count desc, then key ordinal asc
3. **A3** — 单元测试 `LauncherEntryGrouperTests`: 空 / none / command / directory / 未知 groupKey。
4. **A4** — `LauncherSettings.CardsGroupKey` 字段(optional,默认 "none")。
5. **A5** — 编译。

## Phase B: ViewModel

6. **B1** — 新建 `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherCardGroupViewModel.cs`:
   - ObservableObject + Key/DisplayName/Count/Entries/IsExpanded/ShowHeader
   - LaunchAllCommand / SaveAsPresetCommand / ToggleExpandedCommand
   - 构造接收 owner callbacks (Func<IEnumerable<LauncherEntryViewModel>,Task> launchAll, Func<IEnumerable<string>,Task> saveAsPreset)
7. **B2** — 新建 `MainWindowViewModel.Grouping.cs` partial:
   - `_groupKey` 字段 + `CardsGroupKey` 属性(setter: snapshot/save/rebuild)
   - `VisibleGroups: ObservableCollection<LauncherCardGroupViewModel>`
   - `RebuildVisibleGroups()` 调 LauncherEntryGrouper.Group + 转 VM
   - `ApplyFilter` 末尾 + `Refresh` 末尾调 RebuildVisibleGroups
   - `LaunchGroupAsync(IEnumerable<LauncherEntryViewModel>)` 走现有 batch launch
   - `SaveGroupAsPresetAsync(IEnumerable<string> entryIds)` 走现有 preset save
8. **B3** — 编译。

## Phase C: XAML + View

9. **C1** — `MainWindow.axaml` Cards 区域:
   - CardsEntriesItemsControl.ItemsSource → `{Binding VisibleGroups}` (替代 VisibleEntries)
   - ItemTemplate 改为 group template: header (Grid) + nested ItemsControl (cards)
   - header: ▼/▶ toggle + Key (mono) + count + ▶ 启所有 + 💾 存为启动组
   - nested ItemsControl: ItemsSource = `{Binding Entries}` + ItemTemplate = 现有 card template
   - IsExpanded binding controls nested ItemsControl IsVisible
   - ShowHeader=false 时只渲染 nested ItemsControl,无 header
10. **C2** — Cards 工具栏加 ComboBox "分组" + 绑 CardsGroupKey:
    - 类似 DisplayModeComboBox 的风格
    - 选项: 无 / 按 command / 按 directory
11. **C3** — 编译 + 手动 smoke (drag / 键盘导航 / 折叠展开)。
12. **C4** — 调整 drag/键盘导航 code-behind,如需(组层不应阻止 card 层交互)。

## Phase D: 测试

13. **D1** — `MainWindowViewModelTests.GroupingTests.cs`:
    - CardsGroupKey=none → VisibleGroups 含 1 组无 header
    - CardsGroupKey=command → VisibleGroups 多组,按 count 排序
    - CardsGroupKey 持久化到 settings.cardsGroupKey
    - 改 CardsGroupKey → VisibleGroups 重建
    - filter 变更 → VisibleGroups 同步刷新
14. **D2** — 现有测试保持通过(544 + 6 = 550)。
15. **D3** — `dotnet test` 全 PASS。

## Phase E: Gate + 收尾

16. **E1** — `tools/verify.ps1` 全 PASS。
17. **E2** — Grep `catch \(Exception\)` 在新文件中 = 0 命中。
18. **E3** — 手动 smoke: Cards 视图 → 选"按 command" → 14 个 codex 折叠 → ▶ 启所有 → 弹 batch launch 确认 → 启动 → 💾 存为启动组 → 弹 PresetDetailsWindow → prefill 14 个 codex entry IDs。
19. **E4** — Commit。
20. **E5** — `task.py archive 07-11-cards-command-grouping`。

## Rollback Points

- Phase A 出问题 → 删 CardsGroupKeys + LauncherEntryGrouper,无下游依赖。
- Phase B 出问题 → 删 LauncherCardGroupViewModel + Grouping partial。
- Phase C 出问题 → revert MainWindow.axaml Cards 区域 + ComboBox。
- 整体出问题 → `git revert`。

## Validation Commands

```powershell
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~LauncherEntryGrouper|FullyQualifiedName~GroupingTests"
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
```
