using Avalonia;
using Avalonia.Controls;

namespace WTLauncher.App.Views;

/// <summary>
/// A lightweight wrap panel that arranges children left-to-right with uniform item
/// sizes. Children are measured once during <see cref="MeasureOverride"/> and their
/// <see cref="Layoutable.DesiredSize"/> is reused during <see cref="ArrangeOverride"/>,
/// avoiding the re-measurement pass that <see cref="WrapPanel"/> performs.
/// </summary>
public sealed class FastWrapPanel : Panel
{
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<FastWrapPanel, double>(nameof(ItemSpacing), 8);

    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<FastWrapPanel, double>(nameof(LineSpacing), 8);

    static FastWrapPanel()
    {
        ItemSpacingProperty.Changed.AddClassHandler<FastWrapPanel>((p, _) => p.InvalidateMeasure());
        LineSpacingProperty.Changed.AddClassHandler<FastWrapPanel>((p, _) => p.InvalidateMeasure());
    }

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Guard against NaN from unresolved resource bindings.
        var spacing = double.IsNaN(ItemSpacing) ? 0 : ItemSpacing;
        var lineSpacing = double.IsNaN(LineSpacing) ? 0 : LineSpacing;
        // 900px is a conservative default for the primary monitor width minus typical
        // window chrome. Used only when availableSize.Width is infinite (no size
        // constraint from parent), which should not occur in the current single-window
        // layout.
        var width = double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width) ? 900 : availableSize.Width;
        var totalWidth = 0.0;
        var totalHeight = 0.0;
        var rowWidth = 0.0;
        var rowHeight = 0.0;

        foreach (var child in Children)
        {
            // Constrain child measure to the panel width so children don't
            // report DesiredSize wider than the available space, preventing overflow
            // at row start.
            var measureHeight = double.IsFinite(availableSize.Height) ? availableSize.Height : double.PositiveInfinity;
            child.Measure(new Size(width, measureHeight));
            var childWidth = double.IsFinite(child.DesiredSize.Width) ? child.DesiredSize.Width : 0;
            var childHeight = double.IsFinite(child.DesiredSize.Height) ? child.DesiredSize.Height : 0;

            // ponytail: rowWidth now tracks cursor position (including trailing spacing),
            // matching ArrangeOverride's x. This makes the wrap condition identical in both passes.
            if (rowWidth + childWidth > width && rowWidth > 0)
            {
                totalHeight += rowHeight + lineSpacing;
                totalWidth = Math.Max(totalWidth, rowWidth - spacing);
                rowWidth = 0;
                rowHeight = 0;
            }

            rowWidth += childWidth + spacing;
            rowHeight = Math.Max(rowHeight, childHeight);
        }

        totalHeight += rowHeight;
        totalWidth = Math.Max(totalWidth, rowWidth > 0 ? rowWidth - spacing : 0);

        return new Size(Math.Min(totalWidth, width), totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Guard against NaN/Infinity propagated from parent layout pass.
        var containerWidth = Math.Max(1, double.IsFinite(finalSize.Width) ? finalSize.Width : 900);
        // Guard against NaN from unresolved resource bindings.
        var spacing = double.IsNaN(ItemSpacing) ? 0 : ItemSpacing;
        var lineSpacing = double.IsNaN(LineSpacing) ? 0 : LineSpacing;
        var x = 0.0;
        var y = 0.0;
        var rowHeight = 0.0;

        foreach (var child in Children)
        {
            // Clamp child width to panel width to prevent overflow at row start,
            // even if the child's DesiredSize exceeds the panel bounds.
            // Guard against NaN/Infinity from unresolved bindings or layout bugs.
            var childW = child.DesiredSize.Width;
            var childH = child.DesiredSize.Height;
            var childWidth = double.IsFinite(childW) ? Math.Min(childW, containerWidth) : 0;
            var childHeight = double.IsFinite(childH) ? childH : 0;

            if (x + childWidth > containerWidth && x > 0)
            {
                x = 0;
                y += rowHeight + lineSpacing;
                rowHeight = 0;
            }

            child.Arrange(new Rect(x, y, childWidth, childHeight));
            x += childWidth + spacing;
            rowHeight = Math.Max(rowHeight, childHeight);
        }

        return finalSize;
    }
}
