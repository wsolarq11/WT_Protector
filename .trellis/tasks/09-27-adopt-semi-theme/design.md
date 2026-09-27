# Design — 换 Semi.Avalonia 主题

## 边界（最小行为差）

现状：`FluentTheme` 底座 + `App.axaml` 手写控件样式 + `InjectDesignTokens()` 运行时注入 token。
目标：`SemiTheme` 底座 + 项目 token 覆盖 Semi token + 删除手写控件样式，让 Semi 的成熟模板接管。

## 对接机制（关键设计，已落地验证）

Semi 的控件模板与官方 FluentTheme 结构兼容——按钮模板同样暴露 `PART_ContentPresenter`，因此项目既有的 `Button.accent /template/ ContentPresenter#PART_ContentPresenter` 覆盖样式在 Semi 下继续生效，accent 五档热刷新机制无需改动。

token 覆盖无需手动加：项目 `InjectDesignTokens()` 早已注入 `ButtonCornerRadius`（值 10），Application.Resources 优先级高于 Semi 主题内部同名资源，Semi 按钮自动读 10，保持项目圆角。

accent/destructive 不映射到 Semi 色档（Primary/Danger），而是保留项目语义类 + `/template/` 覆盖，理由：
- `secondary` 类本无样式定义（默认按钮），无需处理。
- `accent`/`destructive` 是项目语义（强调/危险），颜色走 `AccentColorRamp`/`DestructiveBrush` 单一源，映射到 Semi 色档会引入第二套颜色并破坏热刷新。
- 50 处 `Classes="accent|destructive|secondary"` 零改动，diff 最小。

## 类名映射（已取消）

原计划 `accent`→`Primary`、`destructive`→`Danger`、`secondary`→`Secondary`。落地时发现保留项目语义类更优（见上），故不改 50 处 XAML 类名。`Button.more`、`Button.tab.active` 等局部布局类不受影响。

## 改动文件（实际）

- `Directory.Packages.props`：加 `Semi.Avalonia` 12.1.0.1。
- `WTLauncher.App.csproj`：加 `<PackageReference Include="Semi.Avalonia" />`。
- `App.axaml`：`<FluentTheme />` → `<semi:SemiTheme Locale="zh-CN" />`；删 TextBox/ComboBox/AutoCompleteBox/ListBoxItem/ContextMenu/MenuItem/ToolTip 手写样式（Semi 接管）；删 Button 全局圆角/padding/字重/上浮动效（Semi 模板 + 项目 token 接管）；保留并强化 `accent`/`destructive` 的 `/template/` 覆盖。
- `packages.lock.json` × 4：Semi 依赖锁定（用 `-r win-x64 -p:EnableNativeAot=true` restore 生成，勿用裸 `dotnet build` 污染）。

## 风险与验证

- AOT：Semi 是否 AOT 友好未知 → 阶段 1 先证 `release:build` 编过，再谈全量替换。
- 视觉回归：Semi 模板与项目 token 组合后的观感需 GUI smoke 目检。
- 依赖冲突：Semi 依赖 `Avalonia 12.1.0`，项目是 `12.1.2`，需确认 NuGet 能解析（向后兼容应无碍）。

## 卡片内操作元素成熟化（第二轮：图标按钮与拖拽手柄）

用户反馈卡片内「更多（⋯）」「折叠/展开」图标按钮和「拖拽手柄（⠿）」有婴儿感。根因：它们不是真正的图标按钮，而是「普通按钮塞图标」或「恒定浅灰色块」。

落地：
- `Button.more` 改 Semi `BorderlessButton`（透明底 + 悬停浅灰），图标从蓝色改中性灰 `TextMutedBrush`，悬停加深为 `TextPrimaryBrush`。
- 折叠/展开按钮（分组头）复用 `more` 类，同样幽灵化。
- 拖拽手柄去恒定浅灰块，改透明底 + `TextMutedBrush` 图标，悬停才给浅灰底。三种视图模式图标色统一。
- 卡片模式把拖拽手柄从孤立的 Row 3 移到操作行 Row 2，跟「启动 / 更多」并排，Grid 4 行改 3 行，删 `entry-footer` 死样式。
- 清理孤儿 token：`SecondaryButton*` 从未被样式使用，删除；`DragHandleTextBrush` 资源无 XAML 引用，删除注入（`DragHandleTextColor` 常量仍被 LauncherDialogText 使用，保留）。

分析器适配：自研门禁 WT1005 校验「所有 StaticResource key 必须在项目内定义」，Semi 的按钮形态 key 在 NuGet 包内、分析器扫不到。给 `ResourceKeyAnalyzer` 加 `ExternalThemeResourceKeys` 白名单（三个 Semi 按钮 ControlTheme key），并补测试。

## 全仓反推（第三轮：把卡片内的成熟化推到底）

把「幽灵图标按钮 + 圆角单一出口」从卡片推到全仓：

- 控制栏 overflow「⋯」原是文本字符 `Content="⋯"`，与卡片 more 的 `IconMoreVertical` 图标不一致；改为同一图标 + `Button.more` 幽灵化，全仓「更多」操作统一。
- 新增 `BadgeCornerRadius=14` token，所有状态徽章（控制栏选中数/stale/总数、分组计数、卡片 stale、详情 stale、快速启动 stale）统一胶囊圆角，消除 14/8/6/4 四种漂移。
- 裸圆角换 token：`CornerRadius="10"` → `ControlCornerRadius`（6 处）、`CornerRadius="16"` → `DialogCornerRadius`（状态栏）。
- 保留的非徽章局部语义值：色板 `8`（AccentColor）、预览块 `8`（DiscoverAgents）、stale 圆点 `4`（8×8 圆）。过度 token 化违背「少即是多」，故不为它们单独造 token。
- 补 `EntryDetailsWindow` 浏览按钮的 `secondary` 类（对齐其他次要按钮）。
- `secondary` 类仍无样式、走 Semi 默认按钮，作为「次要操作」语义标记保留；tab 按钮（CommandManagement）用按钮模拟分段控件，现状可接受，未改。

## 外部库深挖（第四轮：认知外扩）

对整个 Avalonia 生态（awesome-avalonia 全清单）做「活跃 + Avalonia 12 + AOT + MVVM-first + 不破坏五层架构」筛选，深挖了候选库源码与 NuGet 元数据。

结论：**当前不引任何新库**。项目已用生态最优（CommunityToolkit.Mvvm + Stateless + ErrorOr + OneOf + Semi 主题），卡片/排版/图标/对话框手写是「少即是多」正解。与「不做」第三条一致。

登记未来牌（条件触发 → 动作，避免下次重复深挖）：

- 跨预设拖拽 / 批量排序动画 / 长列表自动滚动 → `Sortable.Avalonia`（唯一候选。深挖源码后确认：代码质量不优于我们的状态机 + 快照回滚，但它有跨列表 Move、FLIP 动画、自动滚动三样我们缺的零件）。
- 图标超过 20 个且需统一风格 → `Lucide.Avalonia`（Lucide 风格搭 Semi；`Material.Icons.Avalonia` 85 万下载但 Material 风格不搭，排除）。
- 加可复用交互行为 → `Xaml.Behaviors.Avalonia` 12.x。旧包 `Avalonia.Xaml.Behaviors` 已弃用（NuGet 标记 deprecation），勿用。
- 全局撤销/重做 → 自己写。`ReactiveHistory` 0.10.x 版本偏低、不活跃，不引。

宝藏作者：`wieslawsoltes` 维护 Dock / PanAndZoom / ThemeManager / Xaml.Behaviors / ReactiveHistory 一批「小而精、MIT、官方生态常客」库，以后需要「标准件」优先看他的。

## 不做

- 不调整仍在使用的视觉 token 值（只删无引用的孤儿 token）。
- 不做深色模式、不引扩展包、不引 Ursa。
