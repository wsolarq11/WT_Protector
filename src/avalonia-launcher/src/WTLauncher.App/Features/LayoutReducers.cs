using Fluxor;

namespace WTLauncher.App.Features;

public static class LayoutReducers
{
    [ReducerMethod]
    public static LayoutState ReduceSetEntries(LayoutState state, SetEntriesAction action)
        => state with { Entries = action.Entries };

    [ReducerMethod]
    public static LayoutState ReduceSetFilter(LayoutState state, SetFilterAction action)
        => state with { FilterText = action.FilterText };

    [ReducerMethod]
    public static LayoutState ReduceSetViewMode(LayoutState state, SetViewModeAction action)
        => state with { EntryViewMode = action.EntryViewMode };

    [ReducerMethod]
    public static LayoutState ReduceSetSortMode(LayoutState state, SetSortModeAction action)
        => state with { SortMode = action.SortMode, CustomEntryOrder = action.CustomEntryOrder ?? state.CustomEntryOrder };

    [ReducerMethod]
    public static LayoutState ReduceSetGroupKey(LayoutState state, SetGroupKeyAction action)
        => state with { CardsGroupKey = action.CardsGroupKey };

    [ReducerMethod]
    public static LayoutState ReduceSetContainerSize(LayoutState state, SetContainerSizeAction action)
        => state with { ContainerWidth = action.Width, ContainerHeight = action.Height };

    [ReducerMethod]
    public static LayoutState ReduceSetScrollOffset(LayoutState state, SetScrollOffsetAction action)
        => action.ViewMode switch
        {
            "cards" => state with { CardsScrollOffset = action.Offset },
            "names" => state with { NamesScrollOffset = action.Offset },
            _ => state
        };

    [ReducerMethod]
    public static LayoutState ReduceSetPresets(LayoutState state, SetPresetsAction action)
        => state with { Presets = action.Presets, SelectedPresetId = action.SelectedPresetId };
}
