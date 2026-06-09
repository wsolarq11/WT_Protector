using Avalonia;
using Avalonia.Controls;

namespace WTLauncher.App.Views;

/// <summary>
/// A lightweight wrap panel that arranges children left-to-right with fixed item sizes.
/// Unlike <see cref="WrapPanel"/>, this panel skips the expensive re-measurement
/// of every child during arrange — it trusts the pre-calculated CardWidth/CardHeight
/// from the ViewModel. This eliminates the O(n²) layout cost on every collection change.
/// </summary>
public sealed class FastWrapPanel : Panel
{
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<FastWrapPanel, double>(nameof(ItemSpacing), 8);

    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<FastWrapPanel, double>(nameof(LineSpacing), 8);

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
        var spacing = ItemSpacing;
        var lineSpacing = LineSpacing;
        var width = double.IsInfinity(availableSize.Width) ? 900 : availableSize.Width;
        var totalWidth = 0.0;
        var totalHeight = 0.0;
        var rowWidth = 0.0;
        var rowHeight = 0.0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            var childWidth = child.DesiredSize.Width;
            var childHeight = child.DesiredSize.Height;

            if (rowWidth + childWidth > width && rowWidth > 0)
            {
                totalHeight += rowHeight + lineSpacing;
                totalWidth = Math.Max(totalWidth, rowWidth);
                rowWidth = 0;
                rowHeight = 0;
            }

            rowWidth += childWidth + (rowWidth > 0 ? spacing : 0);
            rowHeight = Math.Max(rowHeight, childHeight);
        }

        totalHeight += rowHeight;
        totalWidth = Math.Max(totalWidth, rowWidth);

        return new Size(Math.Min(totalWidth, width), totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = ItemSpacing;
        var lineSpacing = LineSpacing;
        var x = 0.0;
        var y = 0.0;
        var rowHeight = 0.0;

        foreach (var child in Children)
        {
            var childWidth = child.DesiredSize.Width;
            var childHeight = child.DesiredSize.Height;

            if (x + childWidth > finalSize.Width && x > 0)
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
