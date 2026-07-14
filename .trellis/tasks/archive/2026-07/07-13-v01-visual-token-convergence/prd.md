# V01 视觉 Token 收敛

## Goal

让重复的语义视觉值由 LauncherDesignTokens 单点拥有，并建立生产门禁阻止 App 源码重新引入 raw hex，同时避免把一次性布局尺寸机械抽象成无意义 token。

## Confirmed Facts

- LauncherDesignTokens 已拥有颜色、卡片、窗口、拖拽与部分间距 token，App.InjectDesignTokens 将它们注入资源。
- App 源码现有 4 个 raw hex：EntryDetailsWindow 重复 BadgeInfoForegroundColor；drag ghost 使用独立阴影色；MainWindow 两处透明 BoxShadow 清除值。
- XAML 数值盘点显示 12/14/16 字号分别出现 4/3/4 次；其余大量宽高和 margin 是组件结构或一次性布局，不应全部 token 化。
- verify.ps1 当前没有 raw-hex 漂移检查。

## Requirements

- R1：App 的颜色与阴影不得直接写 raw hex；复用已有颜色 token，确有独立语义的 drag ghost shadow 才新增 token。
- R2：透明“无阴影”通过注入资源表达，不在 XAML 保留特殊 raw-hex 例外。
- R3：将重复且语义稳定的 12/14/16 字号收敛为 small/body/emphasis token；不抽象一次性宽高、边距或具体网格比例。
- R4：每个新增 token 必须有生产消费者；禁止死 token 和仅测试消费。
- R5：tools/verify.ps1 新增稳定检查，扫描 WTLauncher.App 源码中的 #RRGGBB/#AARRGGBB 并要求零匹配。
- R6：保持现有视觉结果、布局、DPI、drag ghost 和三视图行为，不同时执行 V-02 降噪或 V-03 对话框重排。

## Acceptance Criteria

- [x] AC1：WTLauncher.App 的 cs/axaml 生产源码 raw hex 为 0。
- [x] AC2：EntryDetails 模板提示色复用 BadgeInfoForegroundColor；drag ghost shadow 由专用 token 拥有。
- [x] AC3：重复 12/14/16 字号使用语义 token，新增 token 均有生产消费。
- [x] AC4：verify 门禁能对临时 raw-hex fixture 失败，并在当前源码通过。
- [x] AC5：结构测试固定 token 注入、关键消费者和 raw-hex 零漂移。
- [x] AC6：没有新增依赖、设计系统、主题框架或视觉改版。
- [x] AC7：Release tests、git diff --check、tools/verify.ps1、publish 与 GUI smoke 全绿。

## Out of Scope

- 抽象所有 Margin/Padding/Width/Height。
- V-02 主界面降噪与 V-03 对话框一致性。
- V-05 图标集或新主题系统。
