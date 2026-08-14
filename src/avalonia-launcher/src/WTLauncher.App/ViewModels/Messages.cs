using CommunityToolkit.Mvvm.Messaging;
using WTLauncher.App.Features;

namespace WTLauncher.App.ViewModels;

// ── Messenger message types ─────────────────────────────────────────
// All messages use WeakReferenceMessenger (CommunityToolkit.Mvvm) so
// recipients are held weakly and need no manual unsubscription.
// Sent by sub-objects instead of bare events. MWVM registers for these
// in its constructor.

/// <summary>Sent by <see cref="StatusPresenter"/> when StatusContent changes.</summary>
public sealed record StatusChangedMessage(LauncherStatusContent Content);

/// <summary>Sent by <see cref="LayoutManager"/> on filter/sort/view-mode/group-key change.</summary>
public sealed record LayoutChangedMessage(LayoutChangeType ChangeType);

public enum LayoutChangeType { Filter, Sort, ViewMode, GroupKey }

/// <summary>Sent by <see cref="PresetManager"/> after the preset file is loaded/replaced.</summary>
public sealed record PresetFileChangedMessage;

/// <summary>Sent by <see cref="PresetManager"/> when SelectedPreset changes.</summary>
public sealed record PresetSelectionChangedMessage;

/// <summary>Sent by <see cref="Features.Entries.EntryListViewModel"/> when entries are added/removed/replaced.</summary>
public sealed record EntriesChangedMessage;

/// <summary>Sent by <see cref="Features.Entries.EntryListViewModel"/> when entry count changes.</summary>
public sealed record EntryCountChangedMessage;

/// <summary>Sent by <see cref="LaunchOrchestrator"/> on launch-state change (e.g. batch launching toggled).</summary>
public sealed record LaunchStateChangedMessage(bool IsBatchLaunching);
