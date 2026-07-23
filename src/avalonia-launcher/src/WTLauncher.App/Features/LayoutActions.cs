using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.Features;

public record SetEntriesAction(ImmutableList<LaunchEntry> Entries);

public record SetFilterAction(string FilterText);

public record SetViewModeAction(string EntryViewMode);

public record SetSortModeAction(string SortMode, ImmutableList<string>? CustomEntryOrder = null);

public record SetGroupKeyAction(string CardsGroupKey);

public record SetContainerSizeAction(double Width, double Height);

public record SetScrollOffsetAction(string ViewMode, double Offset);

public record SetPresetsAction(ImmutableList<LauncherPreset> Presets, string? SelectedPresetId);
