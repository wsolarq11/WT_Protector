using Avalonia;
using Avalonia.Controls;
using WTLauncher.Core;

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

/// <summary>
/// A fill-grid panel for cards: equal-width cells, container-aware row heights,
/// and an optional stretched last row so partially filled groups do not leave a
/// large right-hand gap. Columns come from the shared Core layout calculator; the
/// row count is derived from the child count so each group fills its own rows.
/// </summary>
public sealed class FillGridPanel : Panel
{
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<FillGridPanel, double>(nameof(ItemSpacing), 8);

    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<FillGridPanel, double>(nameof(LineSpacing), 8);

    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<FillGridPanel, double>(nameof(MinItemWidth), LauncherEntryLayoutCalculator.CardMinWidth);

    public static readonly StyledProperty<double> MinItemHeightProperty =
        AvaloniaProperty.Register<FillGridPanel, double>(nameof(MinItemHeight), LauncherEntryLayoutCalculator.CardMinHeight);

    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<FillGridPanel, int>(nameof(Columns), 0);

    public static readonly StyledProperty<double> ViewportHeightProperty =
        AvaloniaProperty.Register<FillGridPanel, double>(nameof(ViewportHeight), double.NaN);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<FillGridPanel, double>(nameof(ItemHeight), double.NaN);

    public static readonly StyledProperty<bool> FillViewportProperty =
        AvaloniaProperty.Register<FillGridPanel, bool>(nameof(FillViewport), true);

    public static readonly StyledProperty<bool> StretchLastRowProperty =
        AvaloniaProperty.Register<FillGridPanel, bool>(nameof(StretchLastRow), true);

    static FillGridPanel()
    {
        ItemSpacingProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        LineSpacingProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        MinItemWidthProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        MinItemHeightProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        ColumnsProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        ViewportHeightProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        ItemHeightProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        FillViewportProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
        StretchLastRowProperty.Changed.AddClassHandler<FillGridPanel>((p, _) => p.InvalidateMeasure());
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

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double MinItemHeight
    {
        get => GetValue(MinItemHeightProperty);
        set => SetValue(MinItemHeightProperty, value);
    }

    public int Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public double ViewportHeight
    {
        get => GetValue(ViewportHeightProperty);
        set => SetValue(ViewportHeightProperty, value);
    }

    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public bool FillViewport
    {
        get => GetValue(FillViewportProperty);
        set => SetValue(FillViewportProperty, value);
    }

    public bool StretchLastRow
    {
        get => GetValue(StretchLastRowProperty);
        set => SetValue(StretchLastRowProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var spacing = double.IsFinite(ItemSpacing) && ItemSpacing >= 0 ? ItemSpacing : 0;
        var lineSpacing = double.IsFinite(LineSpacing) && LineSpacing >= 0 ? LineSpacing : 0;
        var width = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 900;
        var height = ResolveMeasureHeight(availableSize.Height);

        var grid = CalculateGrid(width, height, Children.Count, spacing, lineSpacing);
        var childWidth = grid.Width;
        var childHeight = ResolveItemHeight(grid.Height);
        foreach (var child in Children)
            child.Measure(new Size(childWidth, childHeight));

        var contentWidth = (grid.Columns * childWidth) + ((grid.Columns - 1) * spacing);
        var contentHeight = (grid.Rows * childHeight) + ((grid.Rows - 1) * lineSpacing);
        var fillsViewport = grid.FillsAvailableHeight && contentHeight <= height + 0.001;
        var desiredHeight = FillViewport && fillsViewport ? height : contentHeight;
        return new Size(Math.Min(width, contentWidth), desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = double.IsFinite(ItemSpacing) && ItemSpacing >= 0 ? ItemSpacing : 0;
        var lineSpacing = double.IsFinite(LineSpacing) && LineSpacing >= 0 ? LineSpacing : 0;
        var width = double.IsFinite(finalSize.Width) && finalSize.Width > 0 ? finalSize.Width : DesiredSize.Width;
        var height = double.IsFinite(finalSize.Height) && finalSize.Height > 0 ? finalSize.Height : DesiredSize.Height;

        var grid = CalculateGrid(width, height, Children.Count, spacing, lineSpacing);
        var cellWidth = grid.Width;
        var cellHeight = ResolveItemHeight(grid.Height);
        var contentHeight = (grid.Rows * cellHeight) + ((grid.Rows - 1) * lineSpacing);

        // Ungrouped fill mode: if the parent gives us at least the minimum-content
        // height, redistribute the final height exactly so there is no residual strip.
        if (FillViewport && height >= contentHeight - 0.001)
            cellHeight = (height - ((grid.Rows - 1) * lineSpacing)) / grid.Rows;

        var columns = grid.Columns;
        var lastRowCount = Math.Min(Children.Count - ((grid.Rows - 1) * columns), columns);
        var lastRowItemWidth = cellWidth;
        if (StretchLastRow && lastRowCount > 0 && lastRowCount < columns)
        {
            var fullRowWidth = (columns * cellWidth) + ((columns - 1) * spacing);
            lastRowItemWidth = (fullRowWidth - ((lastRowCount - 1) * spacing)) / lastRowCount;
        }

        var index = 0;
        foreach (var child in Children)
        {
            var row = index / columns;
            var column = index % columns;
            var isStretchedLastRow = StretchLastRow && row == grid.Rows - 1 && lastRowCount < columns;
            var itemWidth = isStretchedLastRow ? lastRowItemWidth : cellWidth;
            var x = isStretchedLastRow ? column * (lastRowItemWidth + spacing) : column * (cellWidth + spacing);
            var y = row * (cellHeight + lineSpacing);
            child.Arrange(new Rect(x, y, itemWidth, cellHeight));
            index++;
        }

        return finalSize;
    }

    private CardGridLayout CalculateGrid(double width, double height, int itemCount, double spacing, double lineSpacing)
    {
        var count = Math.Max(0, itemCount);
        if (count == 0)
            return new CardGridLayout(0, 0, 1, 1, true);

        var minItemWidth = double.IsFinite(MinItemWidth) && MinItemWidth > 0 ? MinItemWidth : LauncherEntryLayoutCalculator.CardMinWidth;
        var minItemHeight = double.IsFinite(MinItemHeight) && MinItemHeight > 0 ? MinItemHeight : LauncherEntryLayoutCalculator.CardMinHeight;
        var columns = Math.Max(0, Columns);
        return columns > 0
            ? LauncherEntryLayoutCalculator.CalculateCardGridWithColumns(columns, width, height, count, spacing, lineSpacing, minItemWidth, minItemHeight)
            : LauncherEntryLayoutCalculator.CalculateCardGrid(width, height, count, spacing, lineSpacing, minItemWidth, minItemHeight);
    }

    private double ResolveItemHeight(double computedHeight)
    {
        var itemHeight = ItemHeight;
        if (!double.IsFinite(itemHeight) || itemHeight <= 0)
            return computedHeight;

        var minItemHeight = double.IsFinite(MinItemHeight) && MinItemHeight > 0 ? MinItemHeight : LauncherEntryLayoutCalculator.CardMinHeight;
        return Math.Max(minItemHeight, itemHeight);
    }

    private double ResolveMeasureHeight(double availableHeight)
    {
        if (double.IsFinite(availableHeight) && availableHeight > 0)
            return availableHeight;

        var viewportHeight = ViewportHeight;
        return double.IsFinite(viewportHeight) && viewportHeight > 0 ? viewportHeight : 600;
    }
}
