# Design — V01 Visual Token Convergence

## Ownership

LauncherDesignTokens 继续是值源；App.InjectDesignTokens 负责将颜色、BoxShadow、字号和 double/Thickness 形状转换为 Avalonia resources。XAML 和 code-behind 只消费 token/resource。

## Minimal Token Set

- DragGhostShadowColor：drag ghost 独立高程语义。
- FontSizeSmall / FontSizeBody / FontSizeEmphasis：重复且跨组件稳定的字号层级。
- NoBoxShadow：App resource，由透明色在 composition root 构造，XAML 不拥有 raw hex。

现有 BadgeInfoForegroundColor 直接复用，不新增同值别名。

## Gate

verify.ps1 递归扫描 WTLauncher.App 下 cs/axaml，排除 bin/obj，匹配 # 后 6 或 8 个十六进制字符。任何匹配都失败；合法颜色只能在 WTLauncher.Core/LauncherDesignTokens.cs。

## Compatibility

所有替换保持原数值。门禁只检查生产 App 源码，不扫描测试数据中的品牌色或 Core 权威 token。回滚时可逐类恢复消费者，但不得移除门禁而保留 raw hex。
