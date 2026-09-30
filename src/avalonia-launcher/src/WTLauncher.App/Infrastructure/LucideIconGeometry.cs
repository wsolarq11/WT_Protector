using Avalonia;
using Avalonia.Media;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Renders Lucide icon paths at a target pixel size.
///
/// Lucide icons are authored on a 24×24 viewbox whose visual centre is (12,12).
/// A uniform scale of <c>targetSize / 24</c> maps that centre onto the target
/// centre exactly, so one scale is both the correct shrink and the correct
/// centring. Avalonia's <see cref="Shapes.Path"/> with the default
/// <c>Stretch=None</c> renders geometry in its raw coordinates without any
/// translation or scaling, which is why the icons were offset and clipped —
/// a uniform scale fixes that deterministically instead of relying on
/// HorizontalAlignment/VerticalAlignment, which only centre the control, not
/// the geometry inside it.
/// </summary>
public static class LucideIconGeometry
{
    public const double ViewBoxSize = 24.0;

    /// <summary>Lucide's default stroke width, in viewbox units.</summary>
    public const double StrokeWidth = 2.0;

    // GripVertical: six dots (lucide grip-vertical), two columns × three rows.
    // Each dot is a r=1 circle rendered as two arcs so it stays pure path data.
    public const string GripVertical =
        "M 8 12 A 1 1 0 1 1 10 12 A 1 1 0 1 1 8 12 " +
        "M 8 5 A 1 1 0 1 1 10 5 A 1 1 0 1 1 8 5 " +
        "M 8 19 A 1 1 0 1 1 10 19 A 1 1 0 1 1 8 19 " +
        "M 14 12 A 1 1 0 1 1 16 12 A 1 1 0 1 1 14 12 " +
        "M 14 5 A 1 1 0 1 1 16 5 A 1 1 0 1 1 14 5 " +
        "M 14 19 A 1 1 0 1 1 16 19 A 1 1 0 1 1 14 19";
    public const string Settings = "M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915 M 12 9 A 3 3 0 1 1 12 15 A 3 3 0 1 1 12 9";
    public const string ChevronDown = "M 6 9 L 12 15 L 18 9";
    public const string ChevronRight = "M 9 6 L 15 12 L 9 18";
    public const string StatusCheck = "M 20 6 L 9 17 L 4 12";
    public const string StatusX = "M 18 6 L 6 18 M 6 6 L 18 18";
    public const string StatusCircle = "M 12 2 A 10 10 0 1 1 12 22 A 10 10 0 1 1 12 2";
    public const string StatusAlert = "M 12 2 L 22 21 L 2 21 Z M 12 9 L 12 14 M 12 17 L 12 17.5";

    /// <summary>Uniform scale that maps a 24×24 Lucide viewbox onto <paramref name="targetSize"/>.</summary>
    public static double ScaleFor(double targetSize) => targetSize / ViewBoxSize;

    /// <summary>
    /// Uniformly scales a 24×24 Lucide path down to <paramref name="targetSize"/>.
    /// The scale is computed once; no translation is needed because the viewbox
    /// centre (12,12) lands on the target centre by construction.
    /// </summary>
    public static Geometry Fit(string pathData, double targetSize)
    {
        var geometry = Geometry.Parse(pathData);
        var scale = ScaleFor(targetSize);
        geometry.Transform = new MatrixTransform(Matrix.CreateScale(scale, scale));
        return geometry;
    }

    /// <summary>Scales Lucide's 2-unit stroke to the same pixel size as the geometry.</summary>
    public static double StrokeFor(double targetSize) => StrokeWidth * ScaleFor(targetSize);
}
