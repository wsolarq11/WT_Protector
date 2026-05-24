using System.Collections.ObjectModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherModeCatalog
{
    public static ObservableCollection<LauncherSortModeViewModel> CreateSortModes()
    {
        return
        [
            new("custom", "自定义拖拽"),
            new("updated", "更新时间"),
            new("alphabetical", "首字母顺序"),
        ];
    }

    public static ObservableCollection<LauncherDisplayModeViewModel> CreateDisplayModes()
    {
        return
        [
            new(LauncherEntryViewMode.Cards, "卡片"),
            new(LauncherEntryViewMode.Details, "详细列表"),
            new(LauncherEntryViewMode.Names, "名称墙"),
        ];
    }

    public static LauncherSortModeViewModel ResolveSortMode(
        IReadOnlyList<LauncherSortModeViewModel> sortModes,
        string? sortModeId)
    {
        ArgumentNullException.ThrowIfNull(sortModes);
        return sortModes.FirstOrDefault(mode => mode.Id == sortModeId)
            ?? sortModes[0];
    }

    public static LauncherDisplayModeViewModel ResolveDisplayMode(
        IReadOnlyList<LauncherDisplayModeViewModel> displayModes,
        string? entryViewMode)
    {
        ArgumentNullException.ThrowIfNull(displayModes);
        var savedViewMode = LauncherEntryViewModes.Parse(entryViewMode);
        return displayModes.FirstOrDefault(mode => mode.Mode == savedViewMode)
            ?? displayModes[0];
    }
}
