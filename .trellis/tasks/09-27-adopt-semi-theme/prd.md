# PRD — 换 Semi.Avalonia 主题：按钮与控件样式成熟化

## 背景

现状按钮样式"不成熟"的根因：产品用的官方 `Avalonia.Themes.Fluent` 只给了默认底座，项目在 `App.axaml` 手写了一层很薄的控件样式（圆角 + 微动效 + 三个颜色档 accent/destructive/secondary），缺四态一致性、按钮层级、尺寸档这套设计系统。

调研结论（见 `docs/` 与本期会话）：复用 **Semi.Avalonia 12.1.0.1**，而不是 FluentAvalonia。理由：Semi 是纯主题库（正好解决"样式"而非"缺控件"），依赖极轻（2 个包），月级迭代 + 公司背书，MIT。

## 目标

用 Semi 成熟的控件模板替换官方 FluentTheme 底座，同时保留项目自己的设计语言（圆角 10、用户可调 accent 色、浅色中性画布）。

## 约束

- 项目是 Avalonia 12.1.2 / .NET 10 / win-x64 / NativeAOT 自包含发布，引入后必须 AOT 能编过。
- 设计 token 单一源仍是 `LauncherDesignTokens.cs`（颜色/圆角/间距/字号），Semi 模板不得引入第二套硬编码视觉值。
- 用户可调 accent 色系统（`AccentColorRamp` 五档 + 12 色古典色板 + 运行时热刷新）是核心卖点，必须保留。
- XAML 只用 `{StaticResource}`/`{DynamicResource}`，禁硬编码 `#RRGGBB`（门禁 `app-raw-hex-zero`）。
- 不引入 Semi 的扩展包（ColorPicker/DataGrid/Dock/Tabalonia/AvaloniaEdit），只用核心主题包。

## 非目标（本次不做）

- 不做深色模式（项目当前浅色 token 硬编码，深色是后续独立需求）。
- 不引入 Ursa 控件集。
- 不重做视觉系统或布局，只换控件样式底座。

## 验收标准

1. `release:build`（AOT 发布）成功，产出自包含 exe，receipt 记录 `WTLauncher.App.dll` hash。
2. 按钮具备成熟四态（pointerover / pressed / disabled / focus），颜色语义化（Primary / Danger / Secondary 至少三档）。
3. 用户切换 accent 色后，强调按钮仍实时换色（`ApplyAccentRamp` 机制不被破坏）。
4. 圆角、间距、字号仍由 `LauncherDesignTokens` 单一源控制，无 XAML 硬编码视觉值漂移。
5. `npm run verify` 全绿 + `git diff --check` 干净。
