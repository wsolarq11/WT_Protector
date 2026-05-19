namespace WTLauncher.Core;

public sealed record LauncherEntryLayout(double CardWidth, double CardHeight, double NameTileWidth, double NameTileHeight);

public static class LauncherEntryLayoutCalculator
{
    public static LauncherEntryLayout Calculate(double availableWidth, double availableHeight, int itemCount)
    {
        var count = Math.Max(1, itemCount);
        var cardLayout = CalculateCardLayout(availableWidth, availableHeight, count);
        var nameTileLayout = CalculateNameTileLayout(availableWidth, availableHeight, count);
        return new LauncherEntryLayout(cardLayout.Width, cardLayout.Height, nameTileLayout.Width, nameTileLayout.Height);
    }

    private static (double Width, double Height) CalculateCardLayout(double availableWidth, double availableHeight, int count)
    {
        const double gap = 8;
        const double rhythmUnit = 13;
        const double goldenWidthUnits = 21;
        const double goldenHeightUnits = 13;
        const double goldenCardRatio = goldenWidthUnits / goldenHeightUnits;
        const double preferredWidth = rhythmUnit * goldenWidthUnits;
        const double preferredHeight = rhythmUnit * goldenHeightUnits;
        const double minWidth = 200;
        const double minHeight = 96;

        var bestWidth = Math.Max(minWidth, availableWidth);
        var bestHeight = Math.Max(minHeight, availableHeight);
        var bestScore = double.NegativeInfinity;

        for (var columns = 1; columns <= count; columns++)
        {
            var rows = (int)Math.Ceiling(count / (double)columns);
            var width = (availableWidth - ((columns - 1) * gap)) / columns;
            var height = (availableHeight - ((rows - 1) * gap)) / rows;
            if (width <= 0 || height <= 0)
                continue;

            if (width < minWidth)
                continue;

            var effectiveHeight = Math.Max(minHeight, height);
            var shapedHeight = Math.Max(minHeight, Math.Min(effectiveHeight, width / goldenCardRatio));
            var scale = Math.Min(width / preferredWidth, shapedHeight / preferredHeight);
            var shapePenalty = Math.Abs((width / shapedHeight) - goldenCardRatio);
            var overflowRatio = height < minHeight ? (minHeight / height) : 1;
            var overflowPenalty = (overflowRatio - 1) * 0.18;
            var score = scale - (shapePenalty * 0.08) - overflowPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                bestWidth = width;
                bestHeight = shapedHeight;
            }
        }

        return (Math.Max(minWidth, Math.Floor(bestWidth)), Math.Max(minHeight, Math.Floor(bestHeight)));
    }

    private static (double Width, double Height) CalculateNameTileLayout(double availableWidth, double availableHeight, int count)
    {
        const double gap = 6;
        const double rowHeight = 24;
        const double rowGap = 4;
        const double minWidth = 132;
        const double preferredMaxWidth = 220;

        var maxColumns = Math.Max(1, (int)Math.Floor((availableWidth + gap) / (minWidth + gap)));
        var rowsFit = Math.Max(1, (int)Math.Floor((availableHeight + rowGap) / (rowHeight + rowGap)));
        var columnsNeededToFitHeight = (int)Math.Ceiling(count / (double)rowsFit);
        var columnsNeededToAvoidSparseRows = (int)Math.Ceiling((availableWidth + gap) / (preferredMaxWidth + gap));
        var columns = Math.Clamp(
            Math.Max(columnsNeededToFitHeight, columnsNeededToAvoidSparseRows),
            1,
            maxColumns);

        if (availableWidth >= (minWidth * 2) + gap)
            columns = Math.Max(2, columns);

        var width = Math.Max(minWidth, Math.Floor((availableWidth - ((columns - 1) * gap)) / columns));
        return (width, rowHeight);
    }
}
