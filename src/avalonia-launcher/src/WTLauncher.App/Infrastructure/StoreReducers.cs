using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>Pure reducer functions for AppStore.Dispatch. Each reducer takes
/// the current state and returns a new state — no side effects, no I/O.
/// Ponytail: these are simple transformations on LauncherStoreFile records.
/// If mutation count grows beyond ~30, split into partial class by domain.</summary>
public static class StoreReducers
{
    // ── Entry mutations ───────────────────────────────────────────

    public static LauncherStoreFile AddEntry(LauncherStoreFile state, LaunchEntry entry) =>
        state with { Entries = state.Entries.Add(entry) };

    public static LauncherStoreFile UpdateEntry(LauncherStoreFile state, EntryId id, LaunchEntry updated) =>
        state with
        {
            Entries = state.Entries.Select(e => EntryId.Same(e.Id, id) ? updated : e)
                .ToImmutableList()
        };

    public static LauncherStoreFile RemoveEntry(LauncherStoreFile state, EntryId id) =>
        state with { Entries = state.Entries.RemoveAll(e => EntryId.Same(e.Id, id)) };

    public static LauncherStoreFile ReorderEntries(LauncherStoreFile state, ImmutableList<string> newOrder) =>
        state with { Settings = state.Settings with { CustomEntryOrder = newOrder } };

    // ── Preset mutations ──────────────────────────────────────────

    public static LauncherStoreFile AddPreset(LauncherStoreFile state, LauncherPreset preset) =>
        state with { Presets = state.Presets.Add(preset) };

    public static LauncherStoreFile UpdatePreset(LauncherStoreFile state, LauncherPreset preset) =>
        state with
        {
            Presets = state.Presets.Select(p => p.Id == preset.Id ? preset : p)
                .ToImmutableList()
        };

    public static LauncherStoreFile RemovePreset(LauncherStoreFile state, PresetId id) =>
        state with { Presets = state.Presets.RemoveAll(p => p.Id == id) };

    public static LauncherStoreFile SetDefaultPreset(LauncherStoreFile state, string presetId) =>
        state with { DefaultPresetId = presetId };

    // ── Settings mutations ────────────────────────────────────────

    public static LauncherStoreFile UpdateSettings(LauncherStoreFile state, LauncherSettings settings) =>
        state with { Settings = settings };

    public static LauncherStoreFile UpdateWindowGeometry(LauncherStoreFile state, int width, int height) =>
        state with
        {
            Settings = state.Settings with
            {
                Window = new LauncherWindowSettings
                {
                    Width = Math.Max(width, LauncherDesignTokens.WindowMinWidth),
                    Height = Math.Max(height, LauncherDesignTokens.WindowMinHeight),
                }
            }
        };

    public static LauncherStoreFile SetSortMode(LauncherStoreFile state, string sortMode) =>
        state with { Settings = state.Settings with { SortMode = sortMode } };

    public static LauncherStoreFile SetViewMode(LauncherStoreFile state, string viewMode) =>
        state with { Settings = state.Settings with { EntryViewMode = viewMode } };

    public static LauncherStoreFile SetLaunchConfirmation(LauncherStoreFile state, bool requireConfirmation) =>
        state with { Settings = state.Settings with { RequireConfirmationForLaunches = requireConfirmation } };

    public static LauncherStoreFile AddSearchTerm(LauncherStoreFile state, string term)
    {
        var history = state.Settings.SearchHistory ?? [];
        // Deduplicate: remove existing then add to front
        history = history.Remove(term);
        history = history.Insert(0, term);
        // Cap at 20
        if (history.Count > 20)
            history = history.RemoveRange(20, history.Count - 20);
        return state with
        {
            Settings = state.Settings with { SearchHistory = history }
        };
    }
}
