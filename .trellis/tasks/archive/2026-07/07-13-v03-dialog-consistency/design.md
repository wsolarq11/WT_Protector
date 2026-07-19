# Design — V03 Dialog Consistency

## Ownership

`LauncherDesignTokens` 拥有纯数值 `DialogTitleFontSize=20`、`DialogOuterPadding=24`、`DialogFooterTopMargin=14`。`App.InjectDesignTokens()` 只为 XAML 注入所需的标题字号和 padding 资源；程序化窗口直接消费 Core token。

`LauncherDialogText.Title()` 与 `LauncherDialogButtons.Footer()` 继续是唯一 primitive。Footer 的调用者不再传任意视觉数字，而是传 `LauncherDesignTokens.DialogFooterTopMargin`；只有已由父 Grid `RowSpacing=14` 提供间距的确认启动框使用 0，避免叠加。

## Destructive semantics

删除确认框：Cancel = default + cancel + initial focus；Delete = destructive + non-default。这样 Escape、Enter 和首次键盘操作都偏向安全退出。

## Compatibility

仅改变视觉数值与初始焦点，不改变窗口结果类型、按钮 Click 回调、服务接口、ViewModel 或 store。

## Rollback

若统一 padding 导致某窗口内容裁切，只调整该窗口宽高或滚动容器，不恢复新的 spacing literal。
