using System.Collections.Immutable;

namespace WTLauncher.Core.Events;

/// <summary>
/// Pure function projector: Event → LauncherStoreFile → LauncherStoreFile.
/// Stateless and side-effect-free. Each call produces a new immutable state.
/// </summary>
public static class LauncherStoreProjector
{
    public static LauncherStoreFile Project(LauncherStoreFile state, LauncherEvent @event)
    {
        return @event switch
        {
            WindowGeometryChangedEvent e => state with
            {
                Settings = state.Settings with
                {
                    Window = state.Settings.Window with
                    {
                        Width = e.Width,
                        Height = e.Height,
                        X = e.X,
                        Y = e.Y,
                        WindowState = e.WindowState,
                    }
                }
            },
            SortModeChangedEvent e => state with
            {
                Settings = state.Settings with { SortMode = e.SortMode }
            },
            ViewModeChangedEvent e => state with
            {
                Settings = state.Settings with { EntryViewMode = e.ViewMode }
            },
            EntryAddedEvent e => state with
            {
                Entries = state.Entries.Add(e.Entry)
            },
            EntryRemovedEvent e => state with
            {
                Entries = state.Entries.RemoveAll(entry => EntryId.Same(entry.Id, e.EntryId))
            },
            EntryUpdatedEvent e => state with
            {
                Entries = state.Entries.Select(entry =>
                    EntryId.Same(entry.Id, e.EntryId) ? e.Entry : entry).ToImmutableList()
            },
            EntrySelectionChangedEvent e => state with
            {
                Entries = state.Entries.Select(entry =>
                    EntryId.Same(entry.Id, e.EntryId)
                        ? entry with { Selected = e.Selected }
                        : entry).ToImmutableList()
            },
            PresetAddedEvent e => state with
            {
                Presets = state.Presets.Add(e.Preset)
            },
            PresetRemovedEvent e => state with
            {
                Presets = state.Presets.RemoveAll(p => p.Id == e.PresetId)
            },
            SettingsUpdatedEvent e => state with
            {
                Settings = e.Settings
            },
            _ => state
        };
    }
}
