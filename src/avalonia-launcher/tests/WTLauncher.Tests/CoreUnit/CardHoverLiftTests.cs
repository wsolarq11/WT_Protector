using WTLauncher.Core;

namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class CardHoverLiftTests
{
    // 预留空间闭合公式 P = L + O + AA：
    //   L  上浮幅度；O = T/2 中心描边外溢；AA 抗锯齿保险。
    // 三者和必须正好等于 margin 顶部：多了把静止位往下压，少了 hover 上浮时
    // 顶边（描边外半侧）被 ScrollViewer 视口裁掉。
    [Fact]
    public void MarginTop_IsLiftPlusOverflowPlusAntialias()
    {
        Assert.Equal(
            LauncherDesignTokens.CardHoverLift
                + LauncherDesignTokens.CardSelectedBorderOverflow
                + LauncherDesignTokens.CardHoverLiftAntialias,
            LauncherDesignTokens.CardHoverLiftMarginTop);
    }

    [Fact]
    public void SelectedBorderOverflow_IsHalfSelectedThickness()
    {
        // Avalonia Border 中心描边：最外沿比 bounds 高 Thickness/2 px。
        Assert.Equal(
            LauncherDesignTokens.CardSelectedBorderThickness / 2,
            LauncherDesignTokens.CardSelectedBorderOverflow);
    }

    [Fact]
    public void LiftOverflowAndAntialias_ArePositive()
    {
        Assert.True(LauncherDesignTokens.CardHoverLift > 0);
        Assert.True(LauncherDesignTokens.CardSelectedBorderThickness > 0);
        Assert.True(LauncherDesignTokens.CardSelectedBorderOverflow > 0);
        Assert.True(LauncherDesignTokens.CardHoverLiftAntialias > 0);
    }

    // 不变量：预留 ≥ 上浮幅度。任何让预留小于上浮的改动都会让第一行卡片
    // hover 上浮时顶部边框被裁。
    [Fact]
    public void MarginTop_ReservesFullLift()
    {
        Assert.True(LauncherDesignTokens.CardHoverLiftMarginTop >= LauncherDesignTokens.CardHoverLift);
    }

    // 借位闭合：ScrollViewer 负 margin 与 FillGridPanel 正 margin 绝对值相等，
    // 符号相反。两者抵消后第一行卡片静止位置不变（16px），否则会视觉下沉。
    [Fact]
    public void ScrollMargin_IsNegationOfMarginTop()
    {
        Assert.Equal(
            -LauncherDesignTokens.CardHoverLiftMarginTop,
            LauncherDesignTokens.CardHoverLiftScrollMarginTop);
    }
}
