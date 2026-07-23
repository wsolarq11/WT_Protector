using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>Reusable selector functions for extracting projections from
/// LauncherStoreFile. Used via AppStore.Select(StoreSelectors.Xxx).</summary>
public static class StoreSelectors
{
    // ── Entry projections ─────────────────────────────────────────

    /// <summary>All entries in the store.</summary>
    public static LauncherStoreFile EntriesOnly(LauncherStoreFile state) => state;

    /// <summary>Find an entry by its id.</summary>
    public static LaunchEntry? EntryById(LauncherStoreFile state, EntryId id) =>
        state.Entries.FirstOrDefault(e => EntryId.Same(e.Id, id));

    // ── Settings projections ──────────────────────────────────────

    public static LauncherSettings CurrentSettings(LauncherStoreFile state) => state.Settings;

    public static string CurrentSortMode(LauncherStoreFile state) => state.Settings.SortMode;

    public static string CurrentViewMode(LauncherStoreFile state) => state.Settings.EntryViewMode;

    public static bool IsLaunchConfirmationRequired(LauncherStoreFile state) =>
        state.Settings.RequireConfirmationForLaunches;

    // ── Preset projections ────────────────────────────────────────

    public static LauncherPreset? PresetById(LauncherStoreFile state, string presetId) =>
        state.Presets.FirstOrDefault(p => p.Id == presetId);

    public static ImmutableList<LauncherPreset> AllPresets(LauncherStoreFile state) => state.Presets;

    // ── Derived ───────────────────────────────────────────────────

    /// <summary>Entry ids that belong to the default preset.</summary>
    public static ImmutableList<string> DefaultPresetEntryIds(LauncherStoreFile state)
    {
        if (string.IsNullOrEmpty(state.DefaultPresetId))
            return [];
        var preset = state.Presets.FirstOrDefault(p => p.Id == state.DefaultPresetId);
        return preset?.EntryIds ?? [];
    }
}
