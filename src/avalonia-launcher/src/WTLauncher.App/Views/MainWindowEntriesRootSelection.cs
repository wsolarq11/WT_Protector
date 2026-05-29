using Avalonia.Controls;

namespace WTLauncher.App.Views;

public static class MainWindowEntriesRootSelection
{
    public static Control Select(
        bool isDetailedListMode,
        bool isNamesViewMode,
        Control detailsRoot,
        Control namesRoot,
        Control cardsRoot)
    {
        ArgumentNullException.ThrowIfNull(detailsRoot);
        ArgumentNullException.ThrowIfNull(namesRoot);
        ArgumentNullException.ThrowIfNull(cardsRoot);

        if (isDetailedListMode)
            return detailsRoot;

        return isNamesViewMode ? namesRoot : cardsRoot;
    }
}
