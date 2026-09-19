Status: Active
Purpose: 油画透视视觉系统的唯一设计源，定义视觉决策阶梯与变化闭包。
Update when: 颜色、间距、圆角、层级、响应式规则或控件皮肤变化时。

# 油画透视视觉系统

## 一句话

把界面当成一幅有纵深的三层油画：远景画布后撤，中景卡片承载信息，近景控件抬高聚焦。

## 透视法则映射

| 油画透视法则 | 界面实现 | 落地位置 |
| --- | --- | --- |
| 大气透视（远处冷而灰） | 窗口画布从暖纸色渐变到冷石板色 | `LauncherDesignTokens.BackgroundTopColor/BottomColor` + `BackgroundBrush` |
| 焦点光（主光先照前景） | 左上径向柔光压在画布最上层，不遮内容 | `CanvasLightBrush` |
| 地平线（稳定空间锚点） | 内容区顶部一条低对比水平线 | `HorizonBrush` + `MainWindow.axaml` |
| 前景抬升（厚涂高光） | 控件圆角、边框、悬停上移、按下回落 | `App.axaml` 全局 Button/TextBox/ComboBox 样式 |
| 中景分层（画面主体） | 卡片表面白色、轻阴影、选中描边 | `SurfaceBrush`、`CardBoxShadow`、`Border.card` |
| 黄金分割节奏（构图稳定） | 间距只走 8 / 13 / 21 / 34 | `LauncherDesignTokens.Spacing*` |
| 视觉权重（焦点不散） | 主按钮高饱和聚焦，次按钮后撤 | `PrimaryBrush`、`SecondaryButtonBackgroundBrush`、`FocusBrush` |

## 决策阶梯

所有视觉改动从上到下走同一条阶梯，不允许跳层。

1. 语义决策：这个元素在画面里是远景、中景还是近景，承担什么焦点。
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
- 画布渐变和地平线不参与布局测量，避免影响卡片列数计算。

## 验收

- `dotnet build -warnaserror` 必须通过。
- `npm run verify` 必须 PASS，`no-imperative-view-ui` 只允许 `DiscoverAgentsWindow.axaml.cs` 一个命令式例外。
- 视觉令牌只来自 `LauncherDesignTokens.cs`，XAML 不得出现新的裸色值或裸字号。
