using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

/// <summary>Bridge from entry feature ViewModel to the shell. Each callback
/// wraps a part of MainWindowViewModel that entry CRUD needs. Replaced by
/// message-bus subscriptions as migration progresses (P3).</summary>
public sealed record EntryListBridge(
    // Shell state
    Func<LauncherSettings> Settings,
    Func<LauncherPresetFile> PresetFile,
    Func<IReadOnlyDictionary<string, string>?> CommandTemplates,
    Func<string, int> GetPresetReferenceCount,
    Func<string?> GetKeyboardFocusedEntryId,
    Action<string?> SetKeyboardFocusedEntryId,

    // Rollback
    Func<ShellSnapshot> CaptureSnapshot,
    Action<ShellSnapshot> RestoreSnapshot,

    // Persistence — receives mutated entries+settings+presetFile
    Func<IEnumerable<LaunchEntry>, LauncherSettings, LauncherPresetFile, string?> ApplyChanges,

    // Status + drag
    Action<LauncherStatusContent> ApplyStatusText,
    Action DiscardDragReorderPreview,

    // Selection
    Action<LauncherEntryViewModel, bool> UpdateSelection,

    // Layout projections (consumed by LauncherEntryViewModel.Layout.cs)
    Func<bool> IsDetailedListMode,
    Func<bool> IsNamesViewMode,

    // Entry card commands routed to shell (launch, move)
    Action<LauncherEntryViewModel> ShowDryRun,
    Func<LauncherEntryViewModel, Task> LaunchAsync,
    Action<LauncherEntryViewModel> MoveEntryUp,
    Func<LauncherEntryViewModel, bool> CanMoveEntryUp,
    Action<LauncherEntryViewModel> MoveEntryDown,
    Func<LauncherEntryViewModel, bool> CanMoveEntryDown);
