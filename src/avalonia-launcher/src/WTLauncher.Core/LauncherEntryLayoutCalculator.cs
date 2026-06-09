namespace WTLauncher.Core;

public static class LauncherEntryLayoutCalculator
{
    public const double CardGap = LauncherDesignTokens.SpacingSmall;
    public const double CardMinWidth = 200;
    public const double CardMinHeight = 96;
    public const double CardGoldenRatio = 21d / 13d;
    public const double NameTileGap = LauncherDesignTokens.NameTileGap;
    public const double NameTileHeight = 24;
    public const double NameTileRowGap = LauncherDesignTokens.NameTileRowGap;
    public const double NameTileMinWidth = 132;
    public const double NameTilePreferredMaxWidth = 220;

    public static (
        double CardWidth,
        double CardHeight,
        double NameTileWidth,
        double NameTileHeight) Calculate(
            double availableWidth,
            double availableHeight,
            int itemCount)
    {
        var count = Math.Max(1, itemCount);
        var cardLayout = CalculateCardLayout(availableWidth, availableHeight, count);
        var nameTileLayout = CalculateNameTileLayout(availableWidth, availableHeight, count);
        return (cardLayout.Width, cardLayout.Height, nameTileLayout.Width, nameTileLayout.Height);
    }

    private static (double Width, double Height) CalculateCardLayout(double availableWidth, double availableHeight, int count)
    {
        const double rhythmUnit = 13;
        const double goldenWidthUnits = 21;
        const double goldenHeightUnits = 13;
        const double preferredWidth = rhythmUnit * goldenWidthUnits;
        const double preferredHeight = rhythmUnit * goldenHeightUnits;

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
        var maxColumns = Math.Max(1, (int)Math.Floor((availableWidth + NameTileGap) / (NameTileMinWidth + NameTileGap)));
        var rowsFit = Math.Max(1, (int)Math.Floor((availableHeight + NameTileRowGap) / (NameTileHeight + NameTileRowGap)));
        var columnsNeededToFitHeight = (int)Math.Ceiling(count / (double)rowsFit);
        var columnsNeededToAvoidSparseRows = (int)Math.Ceiling(
            (availableWidth + NameTileGap) / (NameTilePreferredMaxWidth + NameTileGap));
        var columns = Math.Clamp(
            Math.Max(columnsNeededToFitHeight, columnsNeededToAvoidSparseRows),
            1,
            maxColumns);

        if (availableWidth >= (NameTileMinWidth * 2) + NameTileGap)
            columns = Math.Max(2, columns);

        var width = Math.Max(NameTileMinWidth, Math.Floor((availableWidth - ((columns - 1) * NameTileGap)) / columns));
        return (width, NameTileHeight);
    }
}
