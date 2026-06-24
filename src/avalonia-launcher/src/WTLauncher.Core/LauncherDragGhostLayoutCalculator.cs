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
        var (left, top) = CalculatePosition(pointerX, pointerY);
        return (
            Math.Max(MinWidth, sourceWidth),
            Math.Max(MinHeight, sourceHeight),
            left,
            top);
    }

    /// <summary>
    /// L44: Calculates ghost layout with window-bounds clamping so the ghost never
    /// renders fully off-screen. At least 50% of the ghost stays visible inside
    /// the window.
    /// </summary>
    public static (double Width, double Height, double Left, double Top) Calculate(
        double sourceWidth,
        double sourceHeight,
        double pointerX,
        double pointerY,
        double windowWidth,
        double windowHeight)
    {
        var width = Math.Max(MinWidth, sourceWidth);
        var height = Math.Max(MinHeight, sourceHeight);
        var (left, top) = CalculatePosition(pointerX, pointerY, windowWidth, windowHeight, width, height);
        return (width, height, left, top);
    }

    public static (double Left, double Top) CalculatePosition(double pointerX, double pointerY)
    {
        return (pointerX + PointerOffsetX, pointerY + PointerOffsetY);
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
