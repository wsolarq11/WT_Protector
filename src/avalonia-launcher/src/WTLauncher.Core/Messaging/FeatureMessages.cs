using System.Collections.Immutable;

namespace WTLauncher.Core.Messaging;

/// <summary>Published when a new entry is added.</summary>
public sealed record EntryAddedMessage(LaunchEntry Entry);

/// <summary>Published when an entry is deleted. Preset subscriptions
/// use this to remove dangling references.</summary>
public sealed record EntryDeletedMessage(string EntryId);

/// <summary>Published when an entry is created or its ID changes.
/// <c>OldId</c> equals <c>NewId</c> for non-rename edits.</summary>
public sealed record EntryUpdatedMessage(string OldId, string NewId, LaunchEntry Entry);

/// <summary>Published whenever the selected-entry set changes.</summary>
public sealed record SelectionChangedMessage(ImmutableList<string> SelectedIds);

/// <summary>Published when entries are reordered (drag or keyboard move).</summary>
public sealed record EntryOrderChangedMessage(ImmutableList<string> OrderedIds);

/// <summary>Published when settings are modified (view mode, sort, etc.).</summary>
public sealed record SettingsChangedMessage(LauncherSettings Settings);
