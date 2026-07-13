# Design — CQ04 MainWindow XAML Modularity

## Module Boundaries

```text
MainWindow.axaml
  Window styles/resources
  LauncherControlBar (toolbar/header/preset/batch controls)
  LauncherEntriesView (Cards/Details/Names + empty states)
  authoritative status panel
```

两个 UserControl 都绑定同一个 `MainWindowViewModel`，不创建子 ViewModel。`MainWindow` 仍是窗口事件、context menu、keyboard 和 drag session owner。

## Narrow Control Contract

`LauncherControlBar` 只读暴露 `SearchBox` 与 `DisplayModeBox`。

`LauncherEntriesView` 只读暴露 Cards/Names ScrollViewer，以及 Cards/Details/Names ItemsControl。

MainWindow 的现有字段使用改为模块属性转发。禁止字符串 `FindControl` 和 public mutable fields。

## Event Routing

优先保留 MainWindow handler，通过 UserControl code-behind 转发 typed .NET events 或 owner callback。子控件不判断 launch、drag eligibility 或 persistence；它们只携带原始 Avalonia event args/row context。

## Compatibility and Rollback

无 Core、schema、service 或 persistence 变化。先抽控制栏，再抽 entries；每步立即 build，避免大规模 XAML 错误堆积。任何 pointer/scroll 回归都恢复对应模块，不禁用现有能力。
