using Fluxor;
using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.Features;

[FeatureState]
public record LayoutState
{
    public ImmutableList<LaunchEntry> Entries { get; init; } = ImmutableList<LaunchEntry>.Empty;

    public string FilterText { get; init; } = string.Empty;

    public string EntryViewMode { get; init; } = "cards";

    public string SortMode { get; init; } = "custom";

    public ImmutableList<string> CustomEntryOrder { get; init; } = ImmutableList<string>.Empty;

    public string CardsGroupKey { get; init; } = "none";

    public ImmutableList<LauncherPreset> Presets { get; init; } = ImmutableList<LauncherPreset>.Empty;

    public string? SelectedPresetId { get; init; }

    public double ContainerWidth { get; init; }

    public double ContainerHeight { get; init; }

    public double CardsScrollOffset { get; init; }

    public double NamesScrollOffset { get; init; }
}
