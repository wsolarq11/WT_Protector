using System.Collections.ObjectModel;
using Avalonia;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryVisibilityWorkflow
{
    public static LauncherEntryVisibilityResult ApplyFilter(
        ObservableCollection<LauncherEntryViewModel> visibleEntries,
        ICollection<LauncherEntryViewModel> entries,
        string? filterText,
        string? sortModeId,
        List<string> customEntryOrder,
        Size availableSize)
    {
        ArgumentNullException.ThrowIfNull(visibleEntries);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(customEntryOrder);

        var visibleCount = LauncherVisibleEntryCollection.Replace(
            visibleEntries,
            entries,
            filterText ?? string.Empty,
            sortModeId,
            customEntryOrder);
        LauncherEntryLayoutPresentation.ApplyCardSize(entries, visibleEntries, availableSize);
        return new LauncherEntryVisibilityResult(visibleCount);
    }

    public static Size UpdateAvailableSize(
        ICollection<LauncherEntryViewModel> entries,
        ICollection<LauncherEntryViewModel> visibleEntries,
        Size availableSize)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(visibleEntries);

        var normalized = LauncherEntryLayoutPresentation.NormalizeAvailableSize(availableSize);
        LauncherEntryLayoutPresentation.ApplyCardSize(entries, visibleEntries, normalized);
        return normalized;
    }
}

public sealed record LauncherEntryVisibilityResult(int VisibleCount);
