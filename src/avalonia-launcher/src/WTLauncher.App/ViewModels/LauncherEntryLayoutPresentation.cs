using Avalonia;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryLayoutPresentation
{
    public static Size NormalizeAvailableSize(Size availableSize)
    {
        return new Size(
            Math.Max(MainWindowLayoutMetrics.MinimumCardsWidth, availableSize.Width),
            Math.Max(MainWindowLayoutMetrics.MinimumCardsHeight, availableSize.Height));
    }

    public static void ApplyCardSize(
        ICollection<LauncherEntryViewModel> entries,
        ICollection<LauncherEntryViewModel> visibleEntries,
        Size availableSize)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(visibleEntries);

        var count = Math.Max(1, visibleEntries.Count > 0 ? visibleEntries.Count : entries.Count);
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
