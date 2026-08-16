namespace WTLauncher.Core;

public static class LauncherEntryLayoutCalculator
{
    // --- Card layout constants ---
    // Gap between cards in the grid; sourced from the design-token spacing scale.
    public const double CardGap = LauncherDesignTokens.SpacingSmall;
    // Minimum card width in px — below this, cards become unreadable and the layout
    // falls back to fewer columns (or the scroll bar as last resort).
    public const double CardMinWidth = 200;
    // Minimum card height in px — keeps button labels and the command preview line
    // from being crushed.
    public const double CardMinHeight = 96;
    // Golden-ratio shape target (21:13 ≈ 1.615). Cards are scored against this
    // aspect ratio during the column-count search; the penalty term nudges the
    // solver toward the golden shape.
    public const double CardGoldenRatio = 21d / 13d;

    // --- Name-tile layout constants ---
    // Horizontal gap between name tiles; sourced from design tokens.
    public const double NameTileGap = LauncherDesignTokens.NameTileGap;
    // Fixed tile height in px — names wall rows are a single line of text.
    public const double NameTileHeight = 24;
    // Vertical gap between name-tile rows; sourced from design tokens.
    public const double NameTileRowGap = LauncherDesignTokens.NameTileRowGap;
    // Minimum tile width — narrower tiles would truncate Chinese entry names too aggressively.
    public const double NameTileMinWidth = 132;
    // Preferred maximum tile width — columns wider than this waste horizontal space
    // for typical entry names, so the solver adds columns once this ceiling is reached.
    public const double NameTilePreferredMaxWidth = 220;

    // Fallback dimensions used when upstream layout provides NaN/Infinity.
    private const double FallbackAvailableWidth = 900;
    private const double FallbackAvailableHeight = 600;

    // 2px hysteresis band around the column-count threshold in name-tile layout.
    private const double NameTileColumnHysteresisPx = 2;

    public static (
        double CardWidth,
        double CardHeight,
        double NameTileWidth,
        double NameTileHeight) Calculate(
            double availableWidth,
            double availableHeight,
            int itemCount)
    {
        // Guard against NaN/Infinity propagated from upstream layout calculations.
        // Use safe fallback dimensions that produce readable cards until
        // the next valid layout pass supplies real measurements.
        var safeWidth = double.IsFinite(availableWidth) && availableWidth > 0 ? availableWidth : FallbackAvailableWidth;
        var safeHeight = double.IsFinite(availableHeight) && availableHeight > 0 ? availableHeight : FallbackAvailableHeight;
        var count = Math.Min(Math.Max(1, itemCount), 10000); // ponytail: cap to prevent unbounded loop
        var cardLayout = CalculateCardLayout(safeWidth, safeHeight, count);
        var nameTileLayout = CalculateNameTileLayout(safeWidth, safeHeight, count);
        return (cardLayout.Width, cardLayout.Height, nameTileLayout.Width, nameTileLayout.Height);
    }

    public static CardGridLayout CalculateCardGrid(
        double availableWidth,
        double availableHeight,
        int itemCount,
        double itemSpacing = CardGap,
        double lineSpacing = CardGap,
        double minItemWidth = CardMinWidth,
        double minItemHeight = CardMinHeight)
    {
        var safeWidth = double.IsFinite(availableWidth) && availableWidth > 0 ? availableWidth : FallbackAvailableWidth;
        var safeHeight = double.IsFinite(availableHeight) && availableHeight > 0 ? availableHeight : FallbackAvailableHeight;
        var count = Math.Min(Math.Max(1, itemCount), 10000);
        return CalculateCardGridCore(safeWidth, safeHeight, count, null, itemSpacing, lineSpacing, minItemWidth, minItemHeight);
    }

    public static CardGridLayout CalculateCardGridWithColumns(
        int columns,
        double availableWidth,
        double availableHeight,
        int itemCount,
        double itemSpacing = CardGap,
        double lineSpacing = CardGap,
        double minItemWidth = CardMinWidth,
        double minItemHeight = CardMinHeight)
    {
        var safeWidth = double.IsFinite(availableWidth) && availableWidth > 0 ? availableWidth : FallbackAvailableWidth;
        var safeHeight = double.IsFinite(availableHeight) && availableHeight > 0 ? availableHeight : FallbackAvailableHeight;
        var count = Math.Min(Math.Max(1, itemCount), 10000);
        return CalculateCardGridCore(safeWidth, safeHeight, count, Math.Max(1, columns), itemSpacing, lineSpacing, minItemWidth, minItemHeight);
    }

    private static (double Width, double Height) CalculateCardLayout(double availableWidth, double availableHeight, int count)
    {
        var grid = CalculateCardGridCore(availableWidth, availableHeight, count, null, CardGap, CardGap, CardMinWidth, CardMinHeight);
        return (grid.Width, grid.Height);
    }

    private static CardGridLayout CalculateCardGridCore(
        double availableWidth,
        double availableHeight,
        int count,
        int? fixedColumns,
        double itemSpacing,
        double lineSpacing,
        double minItemWidth,
        double minItemHeight)
    {
        // The scoring function balances three competing goals:
        //   1. Card size close to the preferred golden-rectangle dimensions (273×169 px).
        //   2. Card aspect ratio close to the golden ratio (21:13 ≈ 1.615).
        //   3. Avoid cards shorter than the minimum height (overflow penalty).
        //
        // Fill-grid mode keeps the full row-cell height when the window fits; only the
        // overflow penalty allows the scroll fallback below the minimum height.
        const double rhythmUnit = 13;          // base spacing unit from the design rhythm
        const double goldenWidthUnits = 21;    // golden-rectangle width in rhythm units
        const double goldenHeightUnits = 13;   // golden-rectangle height in rhythm units
        const double preferredWidth = rhythmUnit * goldenWidthUnits;   // 273 px
        const double preferredHeight = rhythmUnit * goldenHeightUnits; // 169 px

        var maxColumns = Math.Max(1, (int)Math.Floor((availableWidth + itemSpacing) / (minItemWidth + itemSpacing)));
        var bestColumns = Math.Clamp(fixedColumns ?? 1, 1, Math.Max(1, maxColumns));
        var bestRows = (int)Math.Ceiling(count / (double)bestColumns);
        var bestWidth = Math.Max(minItemWidth, availableWidth);
        var bestHeight = Math.Max(minItemHeight, availableHeight);
        var bestFillsAvailableHeight = true;
        var bestScore = double.NegativeInfinity;

        var searchColumns = fixedColumns.HasValue
            ? Math.Max(1, Math.Min(fixedColumns.Value, maxColumns))
            : Math.Min(count, maxColumns);
        for (var columns = 1; columns <= searchColumns; columns++)
        {
            if (fixedColumns.HasValue && columns != fixedColumns.Value)
                continue;

            var rows = (int)Math.Ceiling(count / (double)columns);
            var width = (availableWidth - ((columns - 1) * itemSpacing)) / columns;
            var height = (availableHeight - ((rows - 1) * lineSpacing)) / rows;
            if (width <= 0 || height <= 0)
                continue;

            if (width < minItemWidth && !fixedColumns.HasValue)
                continue;

            var effectiveHeight = Math.Max(minItemHeight, height);
            var scale = Math.Min(width / preferredWidth, effectiveHeight / preferredHeight);
            var shapePenalty = Math.Abs((width / effectiveHeight) - CardGoldenRatio);
            var overflowRatio = height < minItemHeight ? (minItemHeight / height) : 1;
            var overflowPenalty = (overflowRatio - 1) * 0.18;
            var score = scale - (shapePenalty * 0.08) - overflowPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                bestWidth = width;
                bestHeight = effectiveHeight;
                bestColumns = columns;
                bestRows = rows;
                bestFillsAvailableHeight = height >= minItemHeight - 0.001;
            }
        }

        return new CardGridLayout(
            Math.Max(minItemWidth, bestWidth),
            Math.Max(minItemHeight, bestHeight),
            bestColumns,
            bestRows,
            bestFillsAvailableHeight);
    }

    private static (double Width, double Height) CalculateNameTileLayout(double availableWidth, double availableHeight, int count)
    {
        if (!double.IsFinite(availableWidth) || !double.IsFinite(availableHeight))
            return (NameTileMinWidth, NameTileHeight);

        // Hysteresis band around the column-count threshold to prevent
        // rapid toggling when the window is resized near the boundary.
        // Compute columns at current width AND at width minus hysteresis; bias toward the larger
        // count (i.e. keep more columns when within the hysteresis band of the reduction threshold).
        var columns = ComputeNameTileColumns(availableWidth, availableHeight, count);
        var hysteresisColumns = ComputeNameTileColumns(availableWidth - NameTileColumnHysteresisPx, availableHeight, count);
        columns = Math.Max(columns, hysteresisColumns);

        if (availableWidth >= (NameTileMinWidth * 2) + NameTileGap)
            columns = Math.Max(2, columns);

        var width = Math.Max(NameTileMinWidth, Math.Floor((availableWidth - ((columns - 1) * NameTileGap)) / columns));
        return (width, NameTileHeight);
    }

    private static int ComputeNameTileColumns(double availableWidth, double availableHeight, int count)
    {
        var maxColumns = Math.Max(1, (int)Math.Floor((availableWidth + NameTileGap) / (NameTileMinWidth + NameTileGap)));
        var rowsFit = Math.Max(1, (int)Math.Floor((availableHeight + NameTileRowGap) / (NameTileHeight + NameTileRowGap)));
        var columnsNeededToFitHeight = (int)Math.Ceiling(count / (double)rowsFit);
        // Computes how many columns are needed to keep individual tile width below
        // NameTilePreferredMaxWidth. A larger value produces more (narrower) columns.
        var columnsNeededToLimitTileWidth = (int)Math.Ceiling(
            (availableWidth + NameTileGap) / (NameTilePreferredMaxWidth + NameTileGap));
        return Math.Clamp(
            Math.Max(columnsNeededToFitHeight, columnsNeededToLimitTileWidth),
            1,
            maxColumns);
    }
}

public sealed record CardGridLayout(
    double Width,
    double Height,
    int Columns,
    int Rows,
    bool FillsAvailableHeight);
