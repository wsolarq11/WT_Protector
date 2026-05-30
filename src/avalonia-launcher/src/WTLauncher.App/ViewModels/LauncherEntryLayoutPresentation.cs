using Avalonia;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryLayoutPresentation
{
    public const double MinimumCardsWidth = 220;
    public const double MinimumCardsHeight = 140;

    public static Size NormalizeAvailableSize(Size availableSize)
    {
        return new Size(
            Math.Max(MinimumCardsWidth, availableSize.Width),
            Math.Max(MinimumCardsHeight, availableSize.Height));
    }

    public static void ApplyCardSize(
        ICollection<LauncherEntryViewModel> entries,
        ICollection<LauncherEntryViewModel> visibleEntries,
        Size availableSize)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(visibleEntries);

        var count = visibleEntries.Count == 0 ? entries.Count : visibleEntries.Count;
        count = Math.Max(1, count);
        var layout = LauncherEntryLayoutCalculator.Calculate(
            availableSize.Width,
            availableSize.Height,
            count);

        foreach (var entry in entries)
        {
            entry.CardWidth = layout.CardWidth;
            entry.CardHeight = layout.CardHeight;
            entry.NameTileWidth = layout.NameTileWidth;
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
