# Design

## Source of truth
- Status: Active
- Last refreshed: 2026-05-19
- Primary product surfaces: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` + code-behind + `src/avalonia-launcher/src/WTLauncher.App/ViewModels/*`
- Evidence reviewed: `README.md`, `src/avalonia-launcher/README.md`, current Avalonia mainline implementation, `tools/verify.ps1`, `tools/build-release.ps1`, Microsoft Windows controls guidance for ToggleSwitch / ListView / GridView / Win32 List-View details modes

## Brand
- Personality: 清晰、克制、可信、偏工具型，强调“快速找到、快速勾选、快速启动”
- Trust signals: 可见的配置路径、可验证的发布物、危险命令确认、可回滚的本地配置文件
- Avoid: 花哨装饰、不可读的缩放、按钮堆叠导致的功能隐藏、拖拽时无反馈的黑盒感

## Product goals
- Goals:
  - 让用户在 Windows 桌面上快速启动保存的工作空间/命令
  - 让按钮卡片在不同窗口尺寸下仍尽量直接展示全部常用动作
  - 让拖拽排序有明确跟随感、插入感和结果预演
  - 提供三种明确视图：卡片用于可读操作，详细信息用于紧凑字段扫描，名称墙用于一眼看全启动项
  - 将普通启动确认策略上提为全局二元开关，同时保持危险命令强制确认
- Non-goals:
  - 不做复杂的跨平台设计系统
  - 不把辅助状态迁移到 AppData/registry
  - 不引入额外依赖来替代当前 native gate
- Success signals:
  - 卡片可读、动作可达、拖拽可理解
  - `tools/verify.ps1` / `tools/build-release.ps1` 持续通过
  - 用户能在不依赖滚轮的情况下完成大多数操作，滚轮只作为兜底
  - 详细信息视图每行只容纳一行文字，上下仅保留约半个字母高度的呼吸空间
  - 名称墙视图能随窗口宽度自适应双列/多列，常规窗口下一眼看全更多启动项

## Personas and jobs
- Primary personas: 频繁切换本地工作区/命令的 Windows 用户
- User jobs:
  - 搜索、勾选并批量启动
  - 新增/编辑/删除启动按钮
  - 自定义排序并快速切换启动组
  - 在窗口缩放时保持可读性和可操作性
- Key contexts of use: 桌面窗口、鼠标操作优先、短时高频、需要视觉回执

## Information architecture
- Primary navigation: 顶部搜索 + 启动组 + 排序模式 + 视图模式 + 全局启动确认开关 + 批量操作；中部三视图列表/网格；底部状态栏
- Core routes/screens:
  - 主窗口列表
  - 新增/编辑按钮对话框
  - 删除确认对话框
  - 启动确认 / 批量启动确认
- Content hierarchy:
  - 第一层：名称 + 启动动作
  - 第二层：目录/命令/确认语义
  - 第三层：编辑/删除/预演等次级动作
  - 非目标层：更新时间不再出现在主列表/详细信息视图；如未来需要，应放在详情弹窗或 Tooltip，而不是占用扫描列

## Design principles
- Principle 1: 先保可读，再保密度；信息收敛优先于硬塞全部字段
- Principle 2: 交互要有即时回执；拖拽必须“跟得上手、看得见结果”
- Principle 3: 轮廓要稳定，变化要可预期；布局变化尽量通过预演和轻动画弱化突变
- Principle 4: 信息密度由视图承担，不由单个视图硬扛；字段扫描与一眼看全是两个不同任务
- Principle 5: 安全策略集中表达；普通启动确认使用全局二元开关，危险命令保留不可绕过的强制确认
- Tradeoffs: 在极窄窗口下允许启用滚轮兜底，而不是继续缩小到不可读

## Visual language
- Color:
  - 主按钮使用蓝色强调
  - 危险/确认使用暖黄色提示
  - 拖拽高亮使用青蓝色提示线/边框
- Typography:
  - 标题使用 SemiBold，正文使用更小字号并保持单行截断
  - 路径和命令使用等宽字体，减少视觉抖动
  - 详细信息视图行高以“一行字母 + 上下各半个字母呼吸”为目标：约 12-13px 字号、22-24px 行高，避免卡片化留白
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
  - Cards / 详细信息 / 名称墙三视图：卡片保留可读操作，详细信息用于紧凑字段扫描，名称墙只显示名称并自适应多列
  - 全局启动确认 ToggleSwitch：顶部或启动区附近显示“启动需确认 / 启动无需确认”，立即保存，普通启动项受其控制
- Variants and states:
  - Normal：展示名称、目录、命令摘要、启动
  - Compact：压缩次级信息，只保留关键可读信息与启动
  - Micro：仅保留最核心的名称和启动
  - Details：单行显示名称、目录、命令与右侧快捷操作；不显示更新时间；Tooltip 承载被截断的完整字段
  - Names：只显示名称，保留勾选、双击启动、右键菜单和必要的拖拽入口；自适应双列/多列，目标是常规窗口下一眼看全
  - Confirmation：`普通项是否确认` 由全局开关决定；`危险命令` 始终强制确认，不被全局开关关闭
- Token/component ownership:
  - ViewModel 负责密度判断与拖拽状态
  - ViewModel 负责三视图状态、名称墙列数/项目尺寸、全局启动确认设置与危险命令强制确认规则
  - View 负责拖拽 ghost、插入指示、轻动画

## Accessibility
- Target standard: 保持桌面工具可读、可点、可理解，避免过度依赖颜色
- Keyboard/focus behavior: 主操作仍应可通过按钮和菜单完成；拖拽是加速路径，不是唯一入口
- Contrast/readability: 路径/命令截断前保证最小可读性；小卡片不再强塞所有字段
- Screen-reader semantics: 次级动作仍以标准菜单/按钮暴露，不把唯一入口藏在拖拽手势里
- Toggle semantics: 全局确认开关必须可聚焦、可键盘切换，并用短标签说明当前状态；危险命令强制确认需通过 Tooltip/状态文案解释
- Reduced motion and sensory considerations: 仅使用短过渡，避免明显眩晕或噪声式动画

## Responsive behavior
- Supported breakpoints/devices: 桌面窗口宽高变化、宽扁/窄高/小窗口都要能工作
- Layout adaptations:
  - 先收敛信息，再压缩字号/间距
  - 最后才允许滚动条作为兜底
  - 卡片尺寸变化时自动切换 Normal / Compact / Micro
  - 详细信息视图优先压缩纵向高度，不通过显示更新时间占用扫描宽度
  - 名称墙视图按可用宽度计算列数：窄窗口至少 1-2 列，常规窗口 3+ 列；项目宽度保证名称可读，超长名称单行省略并提供 Tooltip
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
  - “启动需确认”表示普通启动项启动前统一确认
  - “启动无需确认”表示普通启动项直接启动
  - “危险”表示命令含高风险标记，始终强制确认，不受全局开关关闭
- Microcopy rules:
  - 状态文案短句化
  - 保留可验证信息（路径、配置、状态）而不是空泛描述

## Implementation constraints
- Framework/styling system: Avalonia 12 + existing code-behind / MVVM / XAML
- Design-token constraints: 不新增独立设计系统；沿用现有样式和可控的少量局部 token
- Performance constraints: DragOver 预演要避免无差别重排；仅在目标或方向变化时更新
- Compatibility constraints: 保持 Windows 主路径与当前发布门禁一致
- Test/screenshot expectations: UI 变更必须继续通过 repo gate；拖拽、三视图持久化、名称墙自适应、全局确认策略与危险命令强制确认优先补单测，截图/体验回执作为后续校验

## Open questions
- [ ] 是否要把“⋯”更多菜单也提供键盘快捷键入口？
- [ ] 名称墙是否需要显示微型危险标记，还是仅在 Tooltip/确认弹窗中表达危险语义？
