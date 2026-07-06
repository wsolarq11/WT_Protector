using Avalonia;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryLayoutPresentation
{
    public const double MinimumCardsWidth = 220;
    // MinimumCardsHeight (140) > CompactLayoutMaxHeight (124) because cards need
    // a minimum height for readable content (button labels, command preview, title +
    // directory lines) even in compact mode. CompactLayoutMaxHeight only governs the
    // density threshold at which an individual card switches to compact layout; it is
    // not the minimum card height the layout system will actually produce.
    public const double MinimumCardsHeight = 140;

    public static Size NormalizeAvailableSize(Size availableSize)
    {
        return new Size(
            double.IsFinite(availableSize.Width) ? Math.Max(MinimumCardsWidth, availableSize.Width) : MinimumCardsWidth,
            double.IsFinite(availableSize.Height) ? Math.Max(MinimumCardsHeight, availableSize.Height) : MinimumCardsHeight);
    }

    public static void ApplyCardSize(
        ICollection<LauncherEntryViewModel> entries,
        ICollection<LauncherEntryViewModel> visibleEntries,
        Size availableSize,
        HashSet<LauncherEntryViewModel>? reusableVisibleSet = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(visibleEntries);

        var count = visibleEntries.Count == 0 ? entries.Count : visibleEntries.Count;
        count = Math.Max(1, count);
        var layout = LauncherEntryLayoutCalculator.Calculate(
            availableSize.Width,
            availableSize.Height,
            count);

        // Single pass over all entries — sets both visible and hidden entries.
        // Hidden entries also get updated to prevent stale card sizes when they become visible later.
        foreach (var entry in entries)
        {
            if (entry.CardWidth != layout.CardWidth)
                entry.CardWidth = layout.CardWidth;
            if (entry.CardHeight != layout.CardHeight)
                entry.CardHeight = layout.CardHeight;
            if (entry.NameTileWidth != layout.NameTileWidth)
                entry.NameTileWidth = layout.NameTileWidth;
            if (entry.NameTileHeight != layout.NameTileHeight)
                entry.NameTileHeight = layout.NameTileHeight;
        }
    }

    public static void Refresh(IEnumerable<LauncherEntryViewModel> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            entry.RefreshLayoutPresentation();
        }
    }
}
