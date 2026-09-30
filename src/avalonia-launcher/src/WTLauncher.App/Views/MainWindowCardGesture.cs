using Avalonia.Controls;

namespace WTLauncher.App.Views;

/// <summary>
/// 卡片级手势（双击启动、拖拽排序）的交互控件排除集单一源。
///
/// 核心等式（契约测试 <c>MainWindowCardGestureTests</c> 锁定）：
///   CursorRegion == DragRegion == DoubleTapLaunchRegion == card − ExcludedControlTypes
///
/// 光标由 <c>Border.card</c> 样式 <c>Cursor=SizeAll</c> 承担；排除控件在
/// MainWindow.axaml 显式 <c>Cursor=Arrow</c> 钉回点击语义。两端都必须从
/// <see cref="ExcludedControlTypes"/> 推导，禁止各自复制，否则光标域与手势域漂移。
/// </summary>
public static class MainWindowCardGesture
{
    /// <summary>卡片内保持点击/勾选语义、不参与卡片级手势的控件类型。</summary>
    public static readonly Type[] ExcludedControlTypes = [typeof(Button), typeof(CheckBox)];

    public static bool IsExcluded(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        foreach (var type in ExcludedControlTypes)
            if (type.IsInstanceOfType(control))
                return true;
        return false;
    }

    /// <summary>source 是否落在卡片空白区（非交互控件），可启动卡片级手势。</summary>
    public static bool IsGestureEligible(object? source)
    {
        foreach (var control in MainWindowVisualTree.ControlsFromSource(source))
            if (IsExcluded(control))
                return false;
        return true;
    }
}
