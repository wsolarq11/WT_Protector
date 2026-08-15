# 台账：docs/design-benchmark-digest.md（验证日期 2026-08-15）

> 协议：.trellis/tasks/08-15-docs/verification-protocol.md。判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL / EXTERNAL。

| 声明 | 判定 | 证据（命令 + 结果） | 处理 |
| --- | --- | --- | --- |
| Last refreshed: 2026-07-05 | HISTORICAL | `git log -1 --format='%h %cs %s' -- docs/design-benchmark-digest.md` → babab292 2026-07-05（最后修改即当日） | 保留带日期的刷新记录 |
| "12 external first-class design benchmarks"（12 条编号输入） | EXTERNAL | 文档内编号 1-12，每条含源 URL（learn.microsoft.com / fluent2 / developer.apple.com / w3.org / carbondesignsystem.com / atlassian.design / primer.style / shopify.dev / s2.spectrum.adobe.com / design-system.service.gov.uk / nngroup.com） | 保留；外部研究项，URL 齐备 |
| DESIGN.md 是本地设计契约 | VERIFIED | `Test-Path DESIGN.md` → True | 保留 |
| MainWindow.axaml 拥有视觉表面与 XAML 样式 token | VERIFIED | `Test-Path src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` → True；`Select-String 'Style Selector="Border.card.drag-target"' MainWindow.axaml` → 行187 | 保留 |
| LauncherDragGhostLayoutCalculator.cs 拥有拖影几何 | VERIFIED | `Test-Path src/avalonia-launcher/src/WTLauncher.Core/LauncherDragGhostLayoutCalculator.cs` → True；文件内含 `PointerOffsetX/Y = 16`（行7-8）与 `CalculatePosition(pointerX, pointerY, …)`（行21,46-47） | 保留 |
| MainWindowViewModel.cs 拥有视图状态/选择/presets/变更编排 | VERIFIED | `Test-Path src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` → True | 保留 |
| tests 锁定拖拽预览/右键菜单替代/三视图模式/尺寸回退/持久化契约 | VERIFIED | 测试文件清单（53 目录）：CoreUnit/LauncherDragGhostLayoutCalculatorTests.cs、UiContract/MainWindowDragInsertDecisionTests.cs、UiContract/MainWindowEntryCardContextMenuTests.cs、CoreUnit/LauncherEntryViewModeTests.cs、ViewModelBehavior/MainWindowViewModelTests.ModesTests.cs、ViewModelBehavior/MainWindowViewModelTests.LayoutTests.cs（`CardSizingPrefersScrollFallbackBeforeShrinkingBelowReadableMinimum` 行134）、Persistence/FileLauncherJsonIOTests.cs | 保留 |
| tools/verify.ps1 与 tools/build-release.ps1 是发布裁判 | VERIFIED | `Test-Path tools/verify.ps1` → True；`Test-Path tools/build-release.ps1` → True | 保留 |
| 8 / 13 / 21 节奏 | VERIFIED | `Select-String '8 / 13 / 21' DESIGN.md` → 行82 | 保留 |
| 插入提示属于 drag-target 类 | VERIFIED | MainWindow.axaml 行187 `Border.card.drag-target`；LauncherEntriesView.axaml 行99/319/490 `Classes.drag-target="{Binding IsDragPreviewTarget}"` | 保留 |
| 视图模式"详细列表 / 名称墙" | VERIFIED | `Select-String '详细列表|名称墙' src` → ViewModels/LayoutManager.cs 行36-37 | 保留 |
| "FLIP subscription safety"被测试覆盖 | UNVERIFIABLE | 全仓 `grep 'FLIP'` → 仅本文档自身命中；src/ 与 tests/ 无 FLIP 相关代码/测试 | 删除该子项 |
| GUI smoke 启动被覆盖 | VERIFIED | src/avalonia-launcher/verify.ps1 行86-124（guiSmoke = hidden-temp-config-window-title）；tools/verify.ps1 行588 收据 guiSmoke | 保留 |
| "One source per state / per visual token" 验收规则 | VERIFIED（设计规则，与实现一致） | LayoutManager.cs 只读投影 IsCustomSortMode/IsCardsViewMode/IsDetailedListMode/IsNamesViewMode；VisualTokenContractTests.cs 行42 断言 LauncherDesignTokens 注入 | 保留 |
| 状态栏反馈 + 模态确认边界 | VERIFIED | Controls/StatusBar.axaml.cs 存在；ConfirmLaunchWindow/ConfirmBatchLaunchWindow/ConfirmEntryDeleteWindow/ConfirmPresetDeleteWindow 存在 | 保留 |
| 双击行启动等非拖拽路径 | VERIFIED | UiContract/MainWindowEntryRowDoubleTapTests.cs 存在 | 保留 |

## 处理总结
- VERIFIED：12 项；HISTORICAL：1 项；EXTERNAL：1 项（12 基准）
- STALE：0 项；UNVERIFIABLE：1 项（FLIP 子项，已从文档删除）
- 重写点：仅"Optional visual receipts"段——删除 FLIP，改为已验证测试的精确文件名；其余原文保留。

## 重写
文档已写回 docs/design-benchmark-digest.md（仅改 receipts 段，其余未动）。
