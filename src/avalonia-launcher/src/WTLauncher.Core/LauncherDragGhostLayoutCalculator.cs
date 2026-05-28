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

    public static (double Left, double Top) CalculatePosition(double pointerX, double pointerY)
    {
        return (pointerX + PointerOffsetX, pointerY + PointerOffsetY);
    }
}
