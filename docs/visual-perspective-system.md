Status: Active
Purpose: 视觉系统唯一设计源：三条规则、决策阶梯、变化闭包。
Update when: 颜色、间距、圆角、层级、响应式规则或控件皮肤变化时。

# 视觉系统

## 一句话

画布中性后撤，卡片承载信息，一个强调色聚焦主动作。层级靠表面与边框，不靠渐变与装饰。

## 三条规则

1. 画布单一中性色（`BackgroundColor`），卡片白色表面 + 1px 边框 + 轻阴影承担层级。
2. 一个强调色（accent，用户可调）只给「启动」主动作；激活/选中用 `ActiveBorderBrush` 描边，不引入第二套主色。
3. 焦点不散：搜索占满顶部是唯一入口；排序/视图/启动组降级到第二行；间距只走 8 / 13 / 21 / 34。

## 决策阶梯

所有视觉改动从上到下走同一条阶梯，不允许跳层。

1. 语义决策：这个元素是画布、表面还是控件，承担什么焦点。
2. 令牌决策：新增或修改 `LauncherDesignTokens.cs` 的常量，不改任何 XAML。
3. 资源决策：`App.axaml.cs InjectDesignTokens()` 把令牌转成 brush/corner/shadow。
4. 皮肤决策：`App.axaml` 全局样式消费资源；局部窗口只保留边界差异。
5. 布局决策：`MainWindow.axaml` / `LauncherEntriesView.axaml` 只负责排列，不重复定义视觉属性。

## 变化闭包

| 变化类型 | 唯一改点 | 不碰 |
| --- | --- | --- |
| 颜色 | `LauncherDesignTokens.cs` | XAML 硬编码颜色 |
| 圆角/间距/阴影 | `LauncherDesignTokens.cs` + `App.axaml.cs` | 窗口局部样式 |
| 控件皮肤 | `App.axaml` | 各窗口 `Window.Styles` 重复皮肤 |
| 卡片/拖拽样式 | `MainWindow.axaml` `Border.card` 系列 | 三视图模板重复定义 |
| 对话框公共样式 | `Views/Dialogs/DialogSharedStyles.axaml` | 各对话框内联样式 |
| 响应式密度 | `LauncherEntryViewModel.Layout.cs` + `CardLayoutProvider` | View 手写尺寸分支 |

## 响应式与位移

- 窗口尺寸变化只改卡片可用区域与密度档位，不缩放文字。
- 密度档位按 `CardWidth/CardHeight` 自动切 Normal / Compact / Micro。
- 悬停/按下位移只做 1px 级别的短过渡，尊重 reduced-motion。
- 画布不参与布局测量，避免影响卡片列数计算。

## 验收

- `dotnet build -warnaserror` 必须通过。
- `npm run verify` 必须 PASS，`no-imperative-view-ui` 只允许 `DiscoverAgentsWindow.axaml.cs` 一个命令式例外。
- 视觉令牌只来自 `LauncherDesignTokens.cs`，XAML 不得出现新的裸色值或裸字号。
