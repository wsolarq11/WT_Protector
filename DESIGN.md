# Design

## Source of truth
- Status: Active
- Last refreshed: 2026-05-19
- Primary product surfaces: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` + code-behind + `src/avalonia-launcher/src/WTLauncher.App/ViewModels/*`
- Evidence reviewed: `README.md`, `src/avalonia-launcher/README.md`, current Avalonia mainline implementation, `tools/verify.ps1`, `tools/build-release.ps1`

## Brand
- Personality: 清晰、克制、可信、偏工具型，强调“快速找到、快速勾选、快速启动”
- Trust signals: 可见的配置路径、可验证的发布物、危险命令确认、可回滚的本地配置文件
- Avoid: 花哨装饰、不可读的缩放、按钮堆叠导致的功能隐藏、拖拽时无反馈的黑盒感

## Product goals
- Goals:
  - 让用户在 Windows 桌面上快速启动保存的工作空间/命令
  - 让按钮卡片在不同窗口尺寸下仍尽量直接展示全部常用动作
  - 让拖拽排序有明确跟随感、插入感和结果预演
- Non-goals:
  - 不做复杂的跨平台设计系统
  - 不把辅助状态迁移到 AppData/registry
  - 不引入额外依赖来替代当前 native gate
- Success signals:
  - 卡片可读、动作可达、拖拽可理解
  - `tools/verify.ps1` / `tools/build-release.ps1` 持续通过
  - 用户能在不依赖滚轮的情况下完成大多数操作，滚轮只作为兜底

## Personas and jobs
- Primary personas: 频繁切换本地工作区/命令的 Windows 用户
- User jobs:
  - 搜索、勾选并批量启动
  - 新增/编辑/删除启动按钮
  - 自定义排序并快速切换启动组
  - 在窗口缩放时保持可读性和可操作性
- Key contexts of use: 桌面窗口、鼠标操作优先、短时高频、需要视觉回执

## Information architecture
- Primary navigation: 顶部搜索 + 启动组 + 排序模式 + 批量操作；中部卡片网格；底部状态栏
- Core routes/screens:
  - 主窗口列表
  - 新增/编辑按钮对话框
  - 删除确认对话框
  - 启动确认 / 批量启动确认
- Content hierarchy:
  - 第一层：名称 + 启动动作
  - 第二层：目录/命令/更新时间/确认语义
  - 第三层：编辑/删除/预演等次级动作

## Design principles
- Principle 1: 先保可读，再保密度；信息收敛优先于硬塞全部字段
- Principle 2: 交互要有即时回执；拖拽必须“跟得上手、看得见结果”
- Principle 3: 轮廓要稳定，变化要可预期；布局变化尽量通过预演和轻动画弱化突变
- Tradeoffs: 在极窄窗口下允许启用滚轮兜底，而不是继续缩小到不可读

## Visual language
- Color:
  - 主按钮使用蓝色强调
  - 危险/确认使用暖黄色提示
  - 拖拽高亮使用青蓝色提示线/边框
- Typography:
  - 标题使用 SemiBold，正文使用更小字号并保持单行截断
  - 路径和命令使用等宽字体，减少视觉抖动
- Spacing/layout rhythm:
  - 采用 8 / 13 / 21 的收敛节奏，靠近黄金分割的视觉节拍
  - 卡片 padding / 角半径 / 间距尽量统一收敛，避免碎散
- Shape/radius/elevation:
  - 卡片圆角更克制，强调稳重与可扫视
  - 拖拽幽灵卡片略提升阴影与透明度，提示“浮在上层”
- Motion:
  - 轻量过渡用于边框、透明度、粗细变化
  - 不做喧宾夺主的复杂动画；只做降低重排突兀感的短过渡
- Imagery/iconography:
  - 以文字和状态标签为主
  - 次级动作可收进 “⋯” 或上下文菜单，减少主界面噪音

## Components
- Existing components to reuse:
  - `Border.card`
  - 各类确认对话框
  - `ContextMenu`
  - 右键/更多菜单承载次级操作
- New/changed components:
  - 拖拽 ghost（覆盖层上的半透明卡片）
  - 拖拽插入指示（卡片左/右高亮线）
  - 三档密度卡片布局（Normal / Compact / Micro）
  - Cards / 详细列表双视图：卡片保留高密度扫视，详细列表用于更密集的字段扫描与快速操作
- Variants and states:
  - Normal：展示名称、目录、命令摘要、启动
  - Compact：压缩次级信息，只保留关键可读信息与启动
  - Micro：仅保留最核心的名称和启动
  - Details：展开目录、命令、更新时间与右侧快捷操作，优先保证密集扫描时的可读性
- Token/component ownership:
  - ViewModel 负责密度判断与拖拽状态
  - View 负责拖拽 ghost、插入指示、轻动画

## Accessibility
- Target standard: 保持桌面工具可读、可点、可理解，避免过度依赖颜色
- Keyboard/focus behavior: 主操作仍应可通过按钮和菜单完成；拖拽是加速路径，不是唯一入口
- Contrast/readability: 路径/命令截断前保证最小可读性；小卡片不再强塞所有字段
- Screen-reader semantics: 次级动作仍以标准菜单/按钮暴露，不把唯一入口藏在拖拽手势里
- Reduced motion and sensory considerations: 仅使用短过渡，避免明显眩晕或噪声式动画

## Responsive behavior
- Supported breakpoints/devices: 桌面窗口宽高变化、宽扁/窄高/小窗口都要能工作
- Layout adaptations:
  - 先收敛信息，再压缩字号/间距
  - 最后才允许滚动条作为兜底
  - 卡片尺寸变化时自动切换 Normal / Compact / Micro
- Touch/hover differences: 当前以鼠标为主；拖拽 ghost 和插入提示需要在指针悬停/拖拽时才出现

## Interaction states
- Loading: 以现有加载状态/状态栏文本呈现
- Empty: 没有可见条目时保持空列表可解释
- Error: 使用状态栏和对话框反馈，不让错误吞掉主界面
- Success: 成功操作后状态栏给出明确中文回执
- Disabled: 不满足条件的按钮应保持语义清晰
- Offline/slow network, if applicable: 目前不属于主问题域

## Content voice
- Tone: 简洁、明确、少废话
- Terminology:
  - “按钮”即启动项卡片
  - “拖拽排序”只表示自定义顺序
  - “需确认”表示危险命令或显式确认要求
- Microcopy rules:
  - 状态文案短句化
  - 保留可验证信息（路径、配置、状态）而不是空泛描述

## Implementation constraints
- Framework/styling system: Avalonia 12 + existing code-behind / MVVM / XAML
- Design-token constraints: 不新增独立设计系统；沿用现有样式和可控的少量局部 token
- Performance constraints: DragOver 预演要避免无差别重排；仅在目标或方向变化时更新
- Compatibility constraints: 保持 Windows 主路径与当前发布门禁一致
- Test/screenshot expectations: UI 变更必须继续通过 repo gate；拖拽和密度逻辑优先补单测，截图/体验回执作为后续校验

## Open questions
- [ ] 是否要把“⋯”更多菜单也提供键盘快捷键入口？
