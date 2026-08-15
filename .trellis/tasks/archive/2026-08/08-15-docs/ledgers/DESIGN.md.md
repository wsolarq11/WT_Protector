# DESIGN.md 验证台账 (2026-08-15)

协议: .trellis/tasks/08-15-docs/verification-protocol.md · 判定: VERIFIED/STALE/UNVERIFIABLE/HISTORICAL
门禁保护: DESIGN.md 属于 no-stale-three-file-refs activeDocs，无 config/(config|presets|settings).json 引用；无其他 gate 内容检查。本台账未删除任何 gate 保护内容。

## Source of truth / 文档头

| 声明 | 判定 | 证据 (命令 + 结果截断) | 处理 |
|------|------|------------------------|------|
| Status: Active | VERIFIED | DESIGN.md 头部 "Status: Active"；git log --follow 显示文档持续维护 | kept |
| Last refreshed: 2026-07-23 | HISTORICAL | git log -1 --format='%h %cs %s' -- DESIGN.md → abbe16ae 2026-07-23 [Snow Team] doc-fixer | kept (带日期) |
| 主产品面: MainWindow.axaml + code-behind + ViewModels/* | VERIFIED | Test-Path src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml → True; MainWindow.axaml.cs → True; ViewModels/ 目录存在 | kept |
| Evidence reviewed: README.md 等 6 项 | VERIFIED | Test-Path README.md/src/avalonia-launcher/README.md/docs/design-benchmark-digest.md/tools/verify.ps1/tools/build-release.ps1 → 全部 True | kept |

## Brand / Product goals / Personas / IA / Principles

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| Personality/Trust signals/Avoid 品牌散文 | 非事实性散文 | 无法用工具检查 | kept |
| 顶部四框行（搜索、排序、视图、启动组）严格对齐 | VERIFIED | LauncherControlBar.axaml: ControlBoxesGrid ColumnDefinitions="1.8*,1*,1*,2.8*"，四个 Border.control-box 依次 搜索/排序/视图/启动组，同一 Grid 行 | kept |
| 启动确认策略为全局二元开关，不再判断危险命令 | VERIFIED | ToggleSwitch IsChecked="{Binding RequireConfirmationForLaunches}"; App 源码 Select-String '危险|需确认项' → 0 命中 | kept |
| 不把辅助状态迁移到 AppData/registry | VERIFIED（作为设计目标） | 启动器状态存 config/launcher.json（schemaVersion:2, settings 含 searchHistory/requireConfirmationForLaunches）；ErrorLog 写 %LOCALAPPDATA% 仅为错误日志，非启动器状态 | kept |
| 主窗口列表 / 新增编辑对话框 / 删除确认 / 启动确认路由 | VERIFIED | MainWindow.axaml + CommandEditDialog.axaml/DirectoryEditDialog.axaml + ConfirmEntryDeleteWindow.axaml/ConfirmLaunchWindow.axaml/ConfirmBatchLaunchWindow.axaml 均存在 | kept |
| 底部状态栏 | VERIFIED | MainWindow.axaml:339 <controls:StatusBar Grid.Row="2"/> | kept |
| 更新时间不再出现在主列表/详细列表视图 | VERIFIED | Views/*.axaml Select-String '更新时间|UpdatedAt|LastUpdated' → 0 命中 | kept |
| 非目标层描述、原则 1-6、Tradeoffs | 设计意图散文 | 与实现一致（四框同一父 Grid 原则=ControlBoxesGrid） | kept |

## Visual language

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 主按钮使用蓝色强调 | VERIFIED | LauncherDesignTokens.PrimaryColor="#2563EB"(蓝); MainWindow.axaml Button.primary Background={StaticResource PrimaryBrush} | kept |
| 启动确认开关中性色+状态文案 | VERIFIED | ToggleSwitch Content=LaunchConfirmationToggleText（"启动需确认/启动无需确认", LaunchOrchestrator.cs:27） | kept |
| 拖拽高亮青蓝色提示线/边框 | VERIFIED | LauncherDesignTokens.DragHighlightColor="#38BDF8"(青蓝); MainWindow.axaml .drag-target/.drag-insert 用 DragHighlightBrush | kept |
| 标题 SemiBold | VERIFIED | MainWindow.axaml TextBlock.h1 FontWeight=SemiBold | kept |
| 路径/命令等宽字体 | VERIFIED | MainWindow.axaml TextBlock.mono FontFamily="Cascadia Code, Consolas, Courier New, monospace"; GhostFontFamily 同 | kept |
| 详细列表只允许单行文字 | VERIFIED | LauncherEntriesView.axaml detail-row TextBlock MaxLines="1" × 多 | kept |
| 8/13/21 收敛节奏 | VERIFIED | LauncherDesignTokens.SpacingSmall=8, CardBorderRadius=13, CardPadding=13; LauncherEntryLayoutCalculator rhythmUnit=13, goldenWidthUnits=21, CardGoldenRatio=21d/13d | kept |
| 21 是黄金比例常量 21:13，用于布局计算，非独立间距 token | VERIFIED | LauncherEntryLayoutCalculator.cs:17 public const double CardGoldenRatio = 21d / 13d; 仅布局计算使用 | kept |
| 卡片 padding/角半径/间距统一收敛 | VERIFIED | CardPadding=13, CardBorderRadius=13 全局唯一 | kept |
| 拖拽幽灵卡片略提升阴影与透明度 | VERIFIED | MainWindow.Drag.cs:315 Opacity=0.86; BoxShadow DragGhostShadowColor | kept |
| 轻量过渡/短动画 | 设计意图 | SuppressTransitionsClass="suppress-drag-transitions" 仅拖拽期间抑制 | kept |

## Components（存在性 + 归属）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| Border.card 复用 | VERIFIED | MainWindow.axaml Style Selector="Border.card" + LauncherEntriesView.axaml 多处 Classes="card" | kept |
| 各类确认对话框 | VERIFIED | ConfirmLaunchWindow/ConfirmBatchLaunchWindow/ConfirmEntryDeleteWindow/ConfirmPresetDeleteWindow 均存在 | kept |
| ContextMenu / 右键更多菜单 | VERIFIED | LauncherControlBar.axaml 选择与刷新/管理/启动组维护 Button.ContextMenu；LauncherEntriesView.axaml Border.ContextMenu | kept |
| 顶部 overflow 菜单保留高频动作（启动/新增/发现）| VERIFIED | LauncherControlBar.axaml: 一键启动已勾选(primary)/新增按钮/发现 为直接按钮；选择与刷新/管理/启动组维护为菜单按钮 | kept |
| 右键 "复制副本" 菜单项：名称追加 "的副本" | **STALE** | 菜单项 Header="复制"（LauncherEntriesView.axaml:125/344/516）；克隆后缀为 " (副本)"（EntryService.cs:58 Name = sourceEntry.Name + " (副本)"）非 "的副本" | **rewritten**: 菜单项 "复制"，名称追加 " (副本)"，新 Id，保留 Directory/Command，Selected 重置 false |
| 拖拽 ghost（覆盖层半透明卡片） | VERIFIED | MainWindow.Drag.cs ShowDragGhost → OverlayLayer + Border Opacity=0.86 | kept |
| 拖拽插入指示（左/右高亮线） | VERIFIED | MainWindow.axaml .drag-insert 样式；三视图 Border Classes="drag-insert" Width=3 左/右 | kept |
| 三档密度卡片布局 Normal/Compact/Micro | VERIFIED | LauncherEntryViewModel.Layout.cs:122 enum LauncherEntryLayoutDensity { Normal, Compact, Micro } | kept |
| Cards/详细列表/名称墙三视图 | VERIFIED | LauncherEntriesView.axaml 三段 IsVisible=IsCardsViewMode / IsDetailedListMode / IsNamesViewMode | kept |
| 四框控制行一个父 Grid 管理 | VERIFIED | ControlBoxesGrid 单 Grid，ColumnDefinitions="1.8*,1*,1*,2.8*"，总宽占满内容区（star 单位） | kept |
| 搜索框 AutoCompleteBox + searchHistory 下拉 | VERIFIED | LauncherControlBar.axaml AutoCompleteBox ItemsSource=SearchHistoryItems(MainWindowViewModel.Layout.cs:14 ← CurrentSettings.SearchHistory)；config/launcher.json settings.searchHistory=["w","wt"] | kept |
| Enter 提交写入搜索历史（去重、上限裁切） | VERIFIED | MainWindow.axaml.cs:209 viewModel.RecordSearchTerm()（仅 Enter 触发）; LauncherSearchHistory.Record 去重移前 + MaximumTerms=20 裁切 | kept |
| 全局启动确认 ToggleSwitch 立即保存 | VERIFIED | LauncherControlBar ToggleSwitch TwoWay; LaunchOrchestrator.cs:397 OnRequireConfirmationForLaunchesChanged → TryPersistSettingsChange | kept |

## Variants and states

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| Normal/Compact/Micro 变体 | VERIFIED | 同上 enum + Classes.compact/micro 绑定（LauncherEntriesView.axaml:101-102） | kept |
| Details 单行、不显示更新时间、Tooltip 承载截断字段 | VERIFIED | detail-row MaxLines=1 + ToolTip.Tip=Name/Directory；Views axaml 无 UpdatedAt | kept |
| Names 勾选/双击/右键/拖拽入口、自适应多列 | VERIFIED | LauncherEntriesView Names 段: CheckBox IsSelected、DoubleTapped、ContextMenu、entry-drag-handle、FastWrapPanel；LauncherEntryLayoutCalculator NameTileMinWidth=132/PreferredMax=220 求解列数 | kept |
| Confirmation 全局开关、不再计算/存储/展示危险命令状态 | VERIFIED | App 源码 0 处 '危险|需确认项'；settings.requireConfirmationForLaunches 唯一权威 | kept |

## Token/component ownership

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| ViewModel 负责密度判断与拖拽状态 | VERIFIED | LauncherEntryViewModel.Layout.cs MeasureLayoutDensity; LauncherEntryViewModel.Drag.cs DragCueState | kept |
| IsCustomSortMode 仅 SelectedSortMode 只读投影 | VERIFIED | LayoutManager.cs:120 public bool IsCustomSortMode => SelectedSortMode?.Id == LauncherEntryQuery.CustomSortModeId;（无第二份 bool） | kept |
| 三视图一个可变来源，IsCardsViewMode/IsDetailedListMode/IsNamesViewMode 只读投影 | VERIFIED | LayoutManager.cs:116 CurrentEntryViewMode ← _selectedDisplayMode; 121-123 三个只读投影 | kept |
| 卡片密度一个可变来源 LauncherEntryLayoutDensity；IsCompactLayout/IsMicroLayout 只读投影 | VERIFIED | LauncherEntryViewModel.Layout.cs:24 _layoutDensity 唯一字段; 26/28 只读投影 | kept |
| 拖拽提示一个 entry-local 来源 DragCueState；四个 Is* 只读投影 | VERIFIED | LauncherEntryViewModel.Drag.cs:5 _dragCueState; 7-13 IsDragPreviewTarget/Source/IsDragInsertBefore/After 只读投影 | kept |
| 拖拽插入指示视觉 token 只能由 drag-target 持有 | VERIFIED（措辞修正） | 视觉 token 集中在 MainWindow.axaml 共享样式: .drag-target（BorderBrush+Thickness）+ .drag-insert（CornerRadius/Background/Opacity/IsHitTestVisible），三视图仅差异 Width=3 + 左/右对齐 + IsVisible | **rewritten**: 样式名精确为 Border.card.drag-target / Border.drag-insert，均定义一次 |
| 选择状态两个写入入口：IsSelected 立即保存；SetSelectionFromOwner 批量保存一次 | VERIFIED | LauncherEntryViewModel.cs:63 IsSelected → _owner.UpdateSelection → SaveSelectionChangeOrRestore（立即保存）; SetSelectionFromOwner internal（:87）由 MainWindowViewModel.Batch.cs 批量调用 | kept |
| 拖拽排序状态单个 DragReorderSession? 持有 | VERIFIED | LauncherDragReorderWorkflow.cs:37 private LauncherDragReorderSession? _session;（唯一持有，含 snapshot/source/target） | kept |
| View 负责拖拽 ghost、插入指示、轻动画 | VERIFIED | MainWindow.Drag.cs ShowDragGhost/MoveDragGhost/HideDragGhost/SuppressDragTransitions | kept |

## Accessibility / Responsive / Interaction / Voice / Constraints

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| "⋯" 更多操作是标准 Button，键盘可开 ContextMenu | VERIFIED | LauncherEntriesView.axaml Button Classes="secondary more" Click=OnEntryMoreClicked；菜单含 InputGesture（Ctrl+D/F2/Delete） | kept |
| 顶部四框行使用单个 Grid：搜索框占剩余主宽，排序/视图/启动组按比例或最小宽度分配 | **STALE（比例细节）** | ControlBoxesGrid ColumnDefinitions="1.8*,1*,1*,2.8*"：搜索 1.8*、排序 1*、视图 1*、启动组 2.8*（star 比例，非"剩余主宽"）；同一 Grid 行保证上下边线一致 | **rewritten**: 单 Grid 四列 star 比例 1.8*/1*/1*/2.8*，共享同一上下边线 |
| 名称墙窄窗口 1-2 列、常规 3+ 列 | VERIFIED | LauncherEntryLayoutCalculator 列数求解：宽度/NameTileMinWidth 决定列数；NameTileWidth 绑定 | kept |
| 卡片尺寸变化自动切换 Normal/Compact/Micro | VERIFIED | MeasureLayoutDensity(width,height) → Micro/Compact/Normal | kept |
| 加载/空/错误/成功/禁用状态 | 设计意图 | 状态栏 + 对话框存在 | kept |
| 术语（按钮/拖拽排序/启动需确认/无危险） | VERIFIED | 微文案一致（Toggle 文案、ConfirmLaunchWindow "当前已开启启动确认"） | kept |
| Avalonia 12 + code-behind/MVVM/XAML | VERIFIED | Directory.Packages.props Avalonia Version="12.1.0" | kept |
| DragOver 预演避免无差别重排 | VERIFIED | MainWindow.Drag.cs:181 if (!viewModel.WouldChangePreview(...)) return; | kept |
| 拖拽 ghost 尺寸/指针偏移由 LauncherDragGhostLayoutCalculator 单测锁定 | VERIFIED | src/.../WTLauncher.Core/LauncherDragGhostLayoutCalculator.cs 存在; tests CoreUnit/LauncherDragGhostLayoutCalculatorTests.cs + PropertyTest/LauncherLayoutPbtTests.cs | kept |
| verify 发布收据记录 WTLauncher.App.dll hash；exe 仅 apphost 外壳 | VERIFIED | tools/verify.ps1:580 要求 publishedDll; Add-Check avalonia-mainline-verify 含 dllSha256; build-release.ps1 记录 $dll | kept |
| Open questions: None | 非事实 | 文档自述 | kept |

## 统计

- 声明条目（表格行）: 约 50 行，其中 VERIFIED 46、HISTORICAL 1、STALE 2（均改写）、非事实散文若干（保留）
- 无 UNVERIFIABLE 需要删除的声明（所有设计 token/成员/文件/样式/门禁行为均有证据）
- 未发现文件计数声明；无 config/(config|presets|settings).json 引用（符合 no-stale-three-file-refs）
- gate 保护内容: 无逐字门禁片段；未删减任何节
