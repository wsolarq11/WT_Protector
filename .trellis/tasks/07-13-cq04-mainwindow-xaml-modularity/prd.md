# CQ04 MainWindow XAML 模块化

## Goal

沿已经被代码行为证明的边界，把 1027 行 `MainWindow.axaml` 拆成控制栏、条目三视图区域和主窗口壳，同时保持拖拽、滚动恢复、键盘导航、compiled binding 与视觉行为完全兼容。

## Confirmed Facts

- `MainWindow.axaml` 当前 1027 行、约 52 KB，包含窗口级 styles、控制栏、Cards/Details/Names 三套视图与状态区。
- code-behind/drag 直接使用 `SearchTextBox`、`DisplayModeComboBox`、三套 ItemsControl 和两个 ScrollViewer；拆分必须提供窄的公开控件契约。
- 三视图共享 selection、drag classes、context menu、keyboard accelerators 与 empty state，但各自布局确实独立。
- CQ-03 已证明 compiled XAML + typed DataTemplate + modal code-behind routing 能通过 publish/GUI smoke。

## Requirements

- R1：主窗口保留 Window 壳、窗口级 styles/resources、顶层布局和状态区；不得把所有内容重新塞进一个同等大小 UserControl。
- R2：抽取控制栏 UserControl，拥有搜索、排序、显示模式、预设选择和批量/管理操作的稳定布局。
- R3：抽取条目区域 UserControl，拥有 Cards/Details/Names 三视图、empty states 与 DataTemplates。
- R4：UserControl 使用 `MainWindowViewModel` 作为 typed DataContext；不得复制或缓存 ViewModel 状态。
- R5：为现有 code-behind/drag 提供最小只读控件访问器（search、display mode、cards/details/names items、cards/names scroll），不暴露整个 visual tree。
- R6：context menu、double tap、drag pointer 事件继续路由到 MainWindow owner；可通过 UserControl 事件转发或明确 owner callback，不把窗口业务注入子 ViewModel。
- R7：保持当前视觉 token、automation names、keyboard gestures、view visibility、scroll retention 和 drag insertion behavior。
- R8：删除迁移后的重复 XAML，MainWindow 本体显著缩减；拆分不得增加新依赖或通用组件框架。

## Acceptance Criteria

- [ ] AC1：新增控制栏和条目区域两个 typed UserControl，MainWindow 壳通过声明式组合使用它们。
- [ ] AC2：MainWindow 本体不再包含三套大型 DataTemplate 或控制栏完整视觉树，行数/职责显著下降。
- [ ] AC3：MainWindow code-behind 和 drag 通过窄访问器继续访问所需控件，无跨组件 `FindControl` 字符串查找。
- [ ] AC4：Cards/Details/Names 的切换、空状态、selection、context menu、double tap、drag preview 与滚动恢复行为保持。
- [ ] AC5：compiled binding、automation names 与 token resource 引用保持，子控件不引入硬编码颜色。
- [ ] AC6：新增结构契约证明模块边界、typed binding、关键事件和命名控件所有权。
- [ ] AC7：Release tests、`git diff --check`、`tools/verify.ps1`、publish 与 GUI smoke 全绿。

## Out of Scope

- 改变三种视图的视觉设计或交互。
- 抽取新的 ViewModel/service/event bus。
- 把每个小按钮或单行模板拆成独立控件。
- 同时执行 V-01/V-02 的视觉 token 或降噪改造。
