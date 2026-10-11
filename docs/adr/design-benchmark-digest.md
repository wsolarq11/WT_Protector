Status: Active
Purpose: 12 项外部设计基准翻译为本地验收规则。
Update when: 基准来源、验收规则或 DESIGN.md Token 变化时。

# 设计基准摘要

本文把 12 项外部设计基准翻译为 WT Launcher 验收规则，不是依赖清单，不授权引入外部设计系统。

## 12 项基准

| # | 基准 | 本地含义 |
| --- | --- | --- |
| 1 | Microsoft Windows app design | Windows-first 行为应原生、可访问、跨输入一致 |
| 2 | Microsoft Fluent 2 | 组件基础与可访问性资源用于减少本地 Token 漂移 |
| 3 | Apple HIG: Feedback | 用户动作需及时、适度、可访问的反馈 |
| 4 | Apple HIG: Accessibility | 交互可感知、可适应、有手势之外的替代 |
| 5 | Material adaptive layout | 布局适配断点并持续伸缩 |
| 6 | WCAG 2.2 | 可访问性主张可测试：键盘/焦点/对比度/目标尺寸/拖拽替代 |
| 7 | IBM Carbon | 设计系统需代码+工具+指南+贡献纪律；本地 Token 需 owner |
| 8 | Atlassian Design System | 内聚 UI 依赖共享基础：间距/网格/颜色/排版/图标/圆角等 |
| 9 | GitHub Primer | 产品组件/品牌组件/原语/可访问性分层 |
| 10 | Shopify Polaris | 语义组件、内建键盘/焦点、Token 化色调与间距 |
| 11 | Adobe Spectrum 2 | 成熟工具应语境化、内聚、高性能、自适应、包容 |
| 12 | GOV.UK + NN/g heuristics | 复用已验证可访问模式、减少记忆、状态可见、用户语言 |

## 验收规则

- 原生优先：Avalonia/MVVM/XAML + Windows 发布门禁；外部系统只是参考，不是新依赖。
- 单一状态 owner：视图模式、排序、拖拽会话/提示、卡片密度、可用尺寸、选择变更各有一个可写 owner。
- 单一视觉 Token owner：拖拽插入提示颜色/圆角/透明度/命中测试归 `drag-target`；新 Token 需命名 owner 或复用现有样式。
- 反馈分级：状态栏处理常规保存/启动；模态确认仅用于启动/删除。
- 手势替代：拖拽是加速路径；自定义排序、右键菜单、按钮、双击、可见手柄保留非拖拽路径。
- 自适应密度：卡片只缩到可读下限，之后滚动兜底必须有测试覆盖。
- 视图分离：Cards 可读动作、详细列表 密集扫描、名称墙 总览；不强行合一。
- 少装饰：优先布局、排版、单一强调色，不加渐变/徽章/装饰层。
- Token 克制：先用 8/13/21 节奏；新 Token 必须消除重复或编码产品不变量。
- 可测试：每条规则至少对应 XAML 契约测试、VM 单测、计算器单测、门禁产物或显式缺口之一。

## 仓库证据

- `DESIGN.md` 是本地设计契约。
- `Views/MainWindow.axaml` 拥有视觉表面与 XAML Token。
- `LauncherDragGhostLayoutCalculator.cs` 拥有拖拽幽灵尺寸与跟随几何。
- `MainWindowViewModel.cs` 拥有视图状态、布局输入、选择、预设与条目变更编排。
- `tests/WTLauncher.Tests/*` 锁定拖拽预览、右键排序替代、三视图、尺寸兜底与持久化契约。
- `npm run verify` 与 `npm run release:contract` 是发布判定；截图/人工检查是补充，不替代门禁。
