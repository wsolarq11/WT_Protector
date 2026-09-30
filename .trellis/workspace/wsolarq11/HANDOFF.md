# Handoff — 2026-09-30（图标 well 化 + 卡片 hover 上浮修复收口）

## 最新现状

- `main` 已 push 到 `origin/main`（commit `52254486`），本地与远端同步。
- 工作树仅剩本 HANDOFF.md 待提交（CI 绿后收口），`git diff --check` 干净。
- 本地 `npm run verify:mainline` PASS（816 测试 / nativeAot / guiSmoke），含本轮 hover 修复。
- 远端 CI 因私有仓库 Actions 账单拦截（`recent account payments have failed`），已临时转公开跑通后转回私有（见「遗留提醒」）。

## 本轮（09-30）完成

- 图标几何注入：新增 `LucideIconGeometry`（24×24 视口中心精确落位的统一缩放 + `StrokeFor` 同步描边宽度），替换 `MainWindow.axaml` 里 8 个裸 `StreamGeometry`；`IconMoreVertical`/`IconGripVertical` 重命名为语义 `IconSettings`/`IconGripVertical`（六点）；`StrokeThickness="2"` 全部收敛为 `IconStroke16/14/12` 资源。配 `LucideIconGeometryTests`（3 断言）。
- 控件 well 化（第 5 轮 visible well 落地）：`Button.more` / `Border.entry-drag-handle` 改 32×32 常显 well（绘制框==命中框），名称墙磁贴 well 18×18、圆角 4。
- destructive 按钮成熟化：hover/pressed 从 Opacity 褪色改为 `AccentColorRamp` 同源 HSL 精确色值，补 `:disabled` 灰化。
- **卡片 hover 上浮修复**（本轮核心，见下节）。
- `npm run verify:mainline` PASS（816 测试 / AOT 发布 / GUI smoke）。

## 卡片 hover 上浮修复（根因与公式）

- 现象：第一行卡片 hover 上浮 2px 时顶部边框缺失，margin 调 2/3/4px 均「无效」。
- 根因（运行时诊断实证）：预留空间加错层级。嵌套的两个 `ItemsControl` 默认 `ClipToBounds=True`，其 clip 边界与卡片 y=0 对齐——margin 加在外层 Grid 时，卡片相对内层 clip 仍是 0，上浮 2px 被**内层 ItemsControl** 裁掉，而非 ScrollViewer。
- 另一元凶：卡片 `Border` 上的多余 `ClipToBounds="True"`（重构机械搬移残留），连同上浮部分、描边外半侧、BoxShadow 一起裁掉。
- 修复：预留 P 加到最内层 `FillGridPanel`；`ScrollViewer` 负 margin `-P` 借位抵消（静止位保持 16px）；删卡片 `ClipToBounds`。
- 单一源公式（`LauncherDesignTokens`）：`P = L + O + AA = 2 + 1 + 1 = 4`，L=上浮、O=T/2=selected 中心描边外溢、AA=抗锯齿。`CardSelectedBorderThickness` 同时收敛 `selected`/`keyboard-focused` 两处裸 `2`。
- 配 `CardHoverLiftTests`（5 断言，含借位闭合 `ScrollMargin == -MarginTop`）。
- 取证方法：运行时 `DispatcherTimer` + `TranslatePoint` + 祖先 clip 链打印（临时代码已删），绕开 `read_image` 不可用限制。

---

## 历史现状（09-27）

- `main` 已 push 到 `origin/main`（commit `4f5e4e7b`），本地与远端同步。
- 工作树有 4 个未提交改动（卡片按钮成熟化，见下），`git diff --check` 干净。
- 本地 `npm run verify` 43 步门禁全绿（exit 0），含本轮按钮改动。
- 远端 CI 全绿（run `36299402877`，6m41s，exit 0），临时转公开跑通后已转回私有（`isPrivate: true`）。

## 本轮完成

- 换 Semi.Avalonia 主题：`SemiTheme` 替换 `FluentTheme` 底座，删除手写 TextBox/ComboBox/AutoCompleteBox/ListBoxItem/ContextMenu/MenuItem/ToolTip 样式。
- 卡片成熟化：`Button.more` 与拖拽手柄幽灵化（`BorderlessButton` + 中性灰图标，悬停加深）。
- 全仓反推：新增 `BadgeCornerRadius` token 统一徽章圆角；裸圆角换 `ControlCornerRadius`/`DialogCornerRadius`；删除孤儿 token（`SecondaryButton*`/`DragHandleTextBrush`）。
- 分析器适配：WT1005 加 `ExternalThemeResourceKeys` 白名单（Semi 按钮 ControlTheme key）+ 补测试。
- 外部库深挖：整个 Avalonia 生态扫完，结论「当前不引任何新库」，登记四张未来牌（见下）。
- 整洁全仓：清理 `config/*.bak`、`.snow` 深挖残留（21MB）、`$env:TEMP` 克隆。
- 提交 `4f5e4e7b`（25 文件，319+/178-）+ push。

## 本轮（接手后）完成

- 深挖 Semi.Avalonia 按钮体系：4 主题层（Light/Solid/Outline/Borderless）× 6 颜色语义（Primary/Secondary/Tertiary/Success/Warning/Danger）× 2 尺寸档（Large 40 / Default 32 / Small 24）× 4 伪态（pointerover/pressed/disabled/focus 走 DefaultFocusAdorner）。
- destructive 按钮成熟化：hover/pressed 从 Opacity 0.86/0.72 整体褪色，改为 `AccentColorRamp` 同源的 HSL 精确色值（降 7%/14% 亮度，红变深、白字不动）；补上此前缺失的 `:disabled` 灰化态。
- 卡片内按钮圆角统一到 `ButtonCornerRadius` token：compact/micro 硬编码 9 与 detail-row 误用的 13 全部收敛到 10。
- 卡片动作区「启动」主按钮 `HorizontalAlignment=Stretch`，占满动作区、次要操作「⋯/⠿」靠右，主操作占主导。
- `npm run verify` 全绿（43 步 / 806 测试 / AOT 发布 / GUI smoke），无新增裸色值。

## 反推全仓（圆角 + 交互态一致性）

- 扫描全仓所有裸 `CornerRadius` 值（11 处）与所有 `:pointerover`/`:pressed` 交互态，逐一判断漂移。
- 唯一漂移：卡片 compact/micro 圆角裸值 `13` → `CardBorderRadius` token（上轮按钮圆角统一时漏网）。
- 确认无漂移：按钮圆角已全对齐 `ButtonCornerRadius`（InjectDesignTokens 覆盖 Semi token + 3 处显式引用）；交互态 Opacity hack 已清零（destructive 是唯一一处，已改精确色值）；对话框主按钮是「右对齐 MinWidth」成熟模式，不适用拉伸。
- 保留的合理局部值：色板/预览框/拖拽手柄的 8、状态圆点 4、列表行 6/0、拖拽插入线 2、控制盒 14 —— 分层之外的独特几何值，门禁不禁止裸圆角，不引入 token 膨胀。
- 踩坑记录：项目用 locked restore，手动 `dotnet build`（unlocked）会污染 `packages.lock.json`，需 build 后 `git restore` 三个 lock 文件。

## 图标按钮成熟化（点阵 → 语义图标）

- 深挖确认：本仓图标是 Lucide（描边风格，ISC license，`MainWindow.axaml` 头部注释），不是 Semi 图标；Semi 的 more/handle 图标本身也是点阵，非点阵的语义图标要从 Lucide 取。
- 卡片/详情/控制栏的「三点」（more 菜单）→ Lucide `settings` 齿轮图标（`IconSettings`）。
- 卡片/详情/名称墙的「六点」（拖拽手柄）→ Lucide `move-vertical` 上下移动箭头（`IconMoveVertical`）。
- 保持 Stroke 渲染 + 16/14 尺寸不变，与既有 Lucide 图标风格一致；资源名从 `IconMoreVertical`/`IconGripVertical` 重命名为语义名，6 处引用同步更新。
- `npm run verify:mainline` PASS（806 测试 / AOT / GUI smoke）。

## 图标按钮 hover 反馈统一（虚影 → 图标变色）

- 定位虚影根源：`Button.more` 用 Semi `BorderlessButton` 主题，该主题 BasedOn 默认 Button 但没 override `:pointerover`/`:pressed`，因此继承了默认 Button 的 `ButtonDefaultPointeroverBackground`(=SemiColorFill1 9% 灰) 和 `ButtonDefaultPressedBackground`(=SemiColorFill2) 背景虚影；`entry-drag-handle` 用 `HoverBackgroundBrush`(#F1F5F9) 背景虚影。两个虚影都比图标(16×16)大一圈(padding 8/6)，视觉上"虚影不贴图标"。
- 根本性决策：统一到 Semi GlyphButton 模型——hover/pressed 只让图标变深（`TextMutedBrush`→`TextPrimaryBrush`），背景恒透明，彻底消灭虚影。
- `Button.more`：加 `:pointerover`/`:pressed` 的 ContentPresenter Background=Transparent 消除虚影 + Path Stroke 变深；反推覆盖卡片/详情/控制栏 more + 分组折叠按钮。
- `entry-drag-handle`：hover 从 `HoverBackgroundBrush` 背景改为 Path Stroke 变深；反推覆盖卡片/详情/名称墙拖拽手柄。
- `npm run verify:mainline` PASS（806 测试 / AOT / GUI smoke）。

## 图标按钮 hover 反馈二轮修正（虚影贴合图标，非消除）

- 用户洞察：虚影 = 点击热区的可视化，有功能价值（标定可点击范围），不应消除，而应「不违和、贴合图标」。
- 深挖确认：`Button.more` 虚影继承 Semi 默认 hover 色 `SemiColorFill1`（=SemiGrey8Color #2E3238 @ 9% 透明）；`entry-drag-handle` 此前用 `HoverBackgroundBrush`(#F1F5F9 不透明) 违和。
- 根本性决策：虚影保留（标定热区），尺寸从 32/28 收紧到 24（more padding 8→4 + MinWidth/Height 32→24；drag padding 6→4），贴合 16 图标（4px 留白）；颜色统一为 9% 中性灰（新增 `IconHoverBackgroundColor=#172E3238`，对齐 SemiColorFill1）；hover 同时图标变深。
- 拖拽安全：`MainWindow.Drag.cs` 第 78 行 `pointer.Capture(this)`，拖拽开始后指针被窗口捕获，手柄热区 24 不影响拖拽全程。
- `npm run verify:mainline` PASS（806 测试 / AOT / GUI smoke）。

## 图标按钮 hover 反馈三轮定稿（图标即虚影）

- 用户洞察（根本性）：虚影（hover 反馈）不该是图标背后垫一层方块（哪怕紧贴），而应由图标自身承担——悬停图标从淡灰浮现为深色，无需第二个视觉元素。
- 统一前两轮矛盾：「虚影=点击范围」→ 图标自己变深就是在标定「这里可点」；「虚影不贴合」→ 不要方块，就不存在贴合问题。
- 最终模型 = Semi GlyphButton 本质（图标变色、无背景），macOS 工具栏同款。
- 改动：`Button.more` 恢复 `:pointerover`/`:pressed` 的 ContentPresenter Background=Transparent（消除继承的 Semi 虚影）+ Path 变深；`entry-drag-handle` 删除 hover Background + Path 变深；删除刚加的 `IconHoverBackgroundColor`/`IconHoverBackgroundBrush`（不留死资源）。保留紧凑热区 24（padding 4）。
- `npm run verify:mainline` PASS（806 测试 / AOT / GUI smoke）。

## 遗留提醒

- 私有仓库 Actions 账单持续拦截（本轮 commit `52254486` 的 CI 又被 `recent account payments have failed` 拦下）。既定解法：临时转公开跑 CI，**CI 绿后必须转回私有**：`gh repo edit wsolarq11/WT_Protector --visibility private --accept-visibility-change-consequences`。本轮转公开跑完已转回私有。
- Semi 换肤 + 本轮图标/控件/hover 均已在 hover 修复过程中经用户 GUI 目检（hover 上浮顶部边框「完整」确认）。
- 任务 `adopt-semi-theme`（`task.json`）status 仍 `in_progress`，若已全部目检通过可标记 `completed`。

## 搁置项（等触发条件，无欠账）

- 数据迁移登记器：等 schema v3 且迁移路径 ≥2 条。
- 外部库四张未来牌：跨预设拖拽→`Sortable.Avalonia`；图标超 20 个→`Lucide.Avalonia`；加交互行为→`Xaml.Behaviors.Avalonia` 12（旧包 `Avalonia.Xaml.Behaviors` 已弃用）；全局撤销→自写（`ReactiveHistory` 不活跃）。
- Semi 配套 `Dock`/`Tabalonia`/`AvaloniaEdit` 免费但闭源，用时需过 license 关。
- 宝藏作者 `wieslawsoltes`：Dock/PanAndZoom/ThemeManager/Xaml.Behaviors 等「小而精标准件」，需要小零件优先看他的主页。

## 关键命令

- 全量门禁：`npm run verify`
- 发布契约：`npm run release:contract`
- 产品构建：`npm run make`
- 转回私有：`gh repo edit wsolarq11/WT_Protector --visibility private`
- 收口：补 Session 日志（`add_session.py`）+ `/trellis:finish-work`

## 图标按钮最终决策（第 5 轮 · visible well）

用户逐字指令：感应范围仍是旧虚影的隐形命中圈，很违和，要求深挖 `router-for-me/EasyCLIProxyAPI` 的拖拽按钮实现。

**参考实现答案**（`styles.css:2765-2791` + `ApiAccessPage.tsx:186-196`）：`.icon-button{width:34px;height:34px;border:1px solid;border-radius:6px}`，quiet 变体常显 `border-color:#d5d2cb;background:#fffdf8`，`:hover` 只换填充 `#f4f1ea`。核心 = **绘制框 == 命中框，恒重合 → 无隐形环**。这与前三轮"幽灵/图标即虚影"方向相反。

**本仓落地**（`MainWindow.axaml`）：
- `Button.more` 与 `Border.entry-drag-handle` 改成 32×32 常显 well：`Background=SurfaceBrush` + `BorderBrush=CardBorderBrush` + `BorderThickness=1`，圆角 `ButtonCornerRadius`。
- hover/pressed 只换填充 `HoverBackgroundBrush`（pressed 显式钉回 hover 填充，压掉 Semi 继承的半透明鬼影）。
- 名称墙磁贴总高 24，well 缩到 18×18、圆角 4（`Border.card.name-tile Border.entry-drag-handle`），图标 12×12。
- 分组头折叠按钮删掉了行内 `Padding="8,2" MinWidth="32"`，统一走 `Button.more` 样式。

**关键教训（探针取证实证）**：图标颜色**绝不能**交给样式接管。`Button.more Path` / `Border.entry-drag-handle Path` 这类不带 `/template/` 的后代选择器**没有匹配到按钮 Content 里的 Path**（纯红/纯绿探针截图 0 命中，而窗口正常渲染）。删掉本地 `Stroke` 会让图标透明消失。图标颜色必须用本地 `Stroke="{StaticResource TextMutedBrush}"`（8 处已恢复）。且参考实现 hover 只换背景、不变图标色，图标 hover 变色本就是画蛇添足，已删。

**取证方法备忘**：`read_image` 不可用（模型无图像输入）时，用 PowerShell WinForms `CopyFromScreen` 截图 + `GetPixel` 像素计数做颜色断言（注入唯一色探针 → 判断选择器是否命中）。`.snow/` 下脚本用完即删。

**门禁**：`verify:mainline` PASS（806 测试 + nativeAot + guiSmoke）。`dotnet build` 会污染 3 个 `packages.lock.json`（ILLink.Tasks 10.0.12→10.0.10 + RID 漂移），跑门禁前必须 `git restore` 那 3 个锁文件。
