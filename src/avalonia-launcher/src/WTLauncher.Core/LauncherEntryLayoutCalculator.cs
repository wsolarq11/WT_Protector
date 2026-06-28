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
        var count = Math.Max(1, itemCount);
        var cardLayout = CalculateCardLayout(safeWidth, safeHeight, count);
        var nameTileLayout = CalculateNameTileLayout(safeWidth, safeHeight, count);
        return (cardLayout.Width, cardLayout.Height, nameTileLayout.Width, nameTileLayout.Height);
    }

    private static (double Width, double Height) CalculateCardLayout(double availableWidth, double availableHeight, int count)
    {
        // The scoring function balances three competing goals:
        //   1. Card size close to the preferred golden-rectangle dimensions (273×169 px).
        //   2. Card aspect ratio close to the golden ratio (21:13 ≈ 1.615).
        //   3. Avoid cards shorter than the minimum height (overflow penalty).
        //
        // Weights were tuned by hand against the project's typical entry counts (10-30)
        // and window sizes (900-1400 px wide) to produce stable, readable columns.
        // The scale term dominates; shape penalty is 8% weight; overflow penalty is a
        // soft push (18%) that only activates when cards would be shorter than CardMinHeight.
        const double rhythmUnit = 13;          // base spacing unit from the design rhythm
        const double goldenWidthUnits = 21;    // golden-rectangle width in rhythm units
        const double goldenHeightUnits = 13;   // golden-rectangle height in rhythm units
        const double preferredWidth = rhythmUnit * goldenWidthUnits;   // 273 px
        const double preferredHeight = rhythmUnit * goldenHeightUnits; // 169 px

        var bestWidth = Math.Max(CardMinWidth, availableWidth);
        var bestHeight = Math.Max(CardMinHeight, availableHeight);
        var bestScore = double.NegativeInfinity;

        for (var columns = 1; columns <= count; columns++)
        {
            var rows = (int)Math.Ceiling(count / (double)columns);
            var width = (availableWidth - ((columns - 1) * CardGap)) / columns;
            var height = (availableHeight - ((rows - 1) * CardGap)) / rows;
            if (width <= 0 || height <= 0)
                continue;

            if (width < CardMinWidth)
                continue;

            var effectiveHeight = Math.Max(CardMinHeight, height);
            var shapedHeight = Math.Max(CardMinHeight, Math.Min(effectiveHeight, width / CardGoldenRatio));
            var scale = Math.Min(width / preferredWidth, shapedHeight / preferredHeight);
            var shapePenalty = Math.Abs((width / shapedHeight) - CardGoldenRatio);
            var overflowRatio = height < CardMinHeight ? (CardMinHeight / height) : 1;
            var overflowPenalty = (overflowRatio - 1) * 0.18;
            var score = scale - (shapePenalty * 0.08) - overflowPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                bestWidth = width;
                bestHeight = shapedHeight;
            }
        }

        return (Math.Max(CardMinWidth, Math.Floor(bestWidth)), Math.Max(CardMinHeight, Math.Floor(bestHeight)));
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
