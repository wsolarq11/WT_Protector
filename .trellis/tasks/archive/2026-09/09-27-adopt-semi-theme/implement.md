# Implement — 换 Semi.Avalonia 主题

## 阶段 1：最小验证（gate，决定后续）

1. `Directory.Packages.props` 加 `<PackageVersion Include="Semi.Avalonia" Version="12.1.0.1" />`。
2. `WTLauncher.App.csproj` 加 `<PackageReference Include="Semi.Avalonia" />`。
3. `App.axaml`：`<FluentTheme />` → `<semi:SemiTheme Locale="zh-CN" />`（先不删手写样式，观察共存/冲突）。
4. `dotnet build` 编过（验证包解析 + 命名空间）。
5. `npm run release:build` AOT 发布成功（验证 AOT 兼容性）。

回滚点：阶段 1 任一失败，回退到 FluentTheme + 移除包，任务转 `blocked` 记录 AOT 失败证据。

## 阶段 2：全量替换（阶段 1 通过后）

1. 删 `App.axaml` 手写控件样式层（Button/TextBox/ComboBox/AutoCompleteBox/ListBox/ContextMenu/MenuItem/ToolTip），让 Semi 模板接管。
2. 类名映射：`accent`→`Primary`、`destructive`→`Danger`、`secondary`→`Secondary`（`grep -rn "Classes=" src/` 全量定位）。
3. `InjectDesignTokens()` 覆盖 Semi token（`ButtonCornerRadius` 等），保持圆角/间距/字号走 `LauncherDesignTokens`。
4. accent 桥接：把 `AccentNormalBrush` 五档映射到 Semi Primary 按钮色资源，验证热刷新仍工作。
5. GUI smoke 目检：按钮四态、对话框、卡片观感。

## 验证

- 每阶段末：`git diff --check`。
- 阶段 2 末：`npm run verify`（43 步门禁 + GUI smoke + `app-raw-hex-zero`）。
- 最终：`npm run release:contract`（locked restore + AOT + receipt）。

## 回滚

- 阶段 1：撤销包 + 还原 App.axaml 一行。
- 阶段 2：git 提交分段，每段可独立 revert；accent 桥接与类名映射分开提交。
