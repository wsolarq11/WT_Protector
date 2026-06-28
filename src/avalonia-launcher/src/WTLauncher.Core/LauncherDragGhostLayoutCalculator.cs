namespace WTLauncher.Core;

public static class LauncherDragGhostLayoutCalculator
{
    public const double MinWidth = 200;
    public const double MinHeight = 96;
    public const double PointerOffsetX = 16;
    public const double PointerOffsetY = 16;

    public static (double Width, double Height, double Left, double Top) Calculate(
        double sourceWidth,
        double sourceHeight,
        double pointerX,
        double pointerY)
    {
        // Guard NaN — Math.Max(NaN, 0) behavior differs across .NET versions.
        var w = double.IsFinite(sourceWidth) ? Math.Max(MinWidth, sourceWidth) : MinWidth;
        var h = double.IsFinite(sourceHeight) ? Math.Max(MinHeight, sourceHeight) : MinHeight;
        var px = double.IsFinite(pointerX) ? pointerX : 0;
        var py = double.IsFinite(pointerY) ? pointerY : 0;
        return (w, h, px + PointerOffsetX, py + PointerOffsetY);
    }

    /// <summary>
    /// Calculates the drag ghost position with window bounds clamping so the ghost
    /// stays mostly visible. Allows up to 50% of the ghost to extend beyond container
    /// edges to prevent the ghost from jumping when the pointer is near the boundary.
    /// </summary>
    public static (double Left, double Top) CalculatePosition(
        double pointerX,
        double pointerY,
        double containerWidth,
        double containerHeight,
        double ghostWidth = MinWidth,
        double ghostHeight = MinHeight)
    {
        if (!double.IsFinite(pointerX) || !double.IsFinite(pointerY)
            || !double.IsFinite(containerWidth) || !double.IsFinite(containerHeight))
            return (0, 0);

        ghostWidth = Math.Max(0, ghostWidth);
        ghostHeight = Math.Max(0, ghostHeight);
        containerWidth = Math.Max(0, containerWidth);
        containerHeight = Math.Max(0, containerHeight);

        var left = pointerX + PointerOffsetX;
        var top = pointerY + PointerOffsetY;

        // Clamp so at least 50% of the ghost remains visible within the container.
        var halfW = ghostWidth * 0.5;
        var halfH = ghostHeight * 0.5;

        left = Math.Max(-halfW, Math.Min(left, containerWidth - halfW));
        top = Math.Max(-halfH, Math.Min(top, containerHeight - halfH));

        return (left, top);
    }
}
