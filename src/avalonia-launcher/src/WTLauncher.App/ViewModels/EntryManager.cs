using System.Collections.ObjectModel;
using WTLauncher.App.Features.Entries;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Owns _entries/_visibleEntries and EntryListViewModel. Eliminates EntryListBridge
/// by providing bridge callbacks directly as delegate fields set by the orchestrator.
/// Events: EntriesChanged, CountChanged.
/// 失败半径: Large — 持久化失败抛异常。
/// </summary>
public sealed class EntryManager
{
    private readonly ObservableCollection<LauncherEntryViewModel> _entries = [];
    private readonly ObservableCollection<LauncherEntryViewModel> _visibleEntries = [];

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }
    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    /// <summary>Mutable access for MWVM partial files (drag reorder, sync, etc.).</summary>
    internal ObservableCollection<LauncherEntryViewModel> MutableEntries => _entries;
    internal ObservableCollection<LauncherEntryViewModel> MutableVisibleEntries => _visibleEntries;

    public int VisibleCount => _visibleEntries.Count;
    internal string? KeyboardFocusedEntryId { get; set; }

    internal EntryListViewModel EntryListViewModel { get; }

    // ── events ──
    public event Action? EntriesChanged;
    public event Action? CountChanged;

    // ── bridge delegates (set by MWVM orchestrator) ──
    internal Func<LauncherSettings> SettingsGetter = null!;
    internal Func<IReadOnlyDictionary<string, string>?> CommandTemplatesGetter = null!;
    internal Func<LauncherPresetFile> PresetFileGetter = null!;
    internal Func<string, int> ReferenceCounter = null!;
    internal Func<ShellSnapshot> SnapshotCapturer = null!;
    internal Action<ShellSnapshot> SnapshotRestorer = null!;
    internal Func<IEnumerable<LaunchEntry>, LauncherSettings, LauncherPresetFile, string?> ChangesApplier = null!;
    internal Action<LauncherStatusContent> StatusApplier = null!;
    internal Action DragPreviewDiscarder = null!;
    internal Action<LauncherEntryViewModel, bool> SelectionUpdater = null!;
    internal Func<bool> DetailedListModeChecker = null!;
    internal Func<bool> NamesModeChecker = null!;
    internal Action<LauncherEntryViewModel> DryRunInitiator = null!;
    internal Func<LauncherEntryViewModel, Task> AsyncLauncher = null!;
    internal Action<LauncherEntryViewModel> EntryMoverUp = null!;
    internal Func<LauncherEntryViewModel, bool> EntryMoverUpChecker = null!;
    internal Action<LauncherEntryViewModel> EntryMoverDown = null!;
    internal Func<LauncherEntryViewModel, bool> EntryMoverDownChecker = null!;

    public EntryManager(IEntryService entryService, Func<DateTimeOffset> utcNow)
    {
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
        EntryListViewModel = new EntryListViewModel(_entries, _visibleEntries, entryService, this, utcNow);
    }

    public void ReplaceEntries(IEnumerable<LaunchEntry> entries)
    {
        EntryListViewModel.ReplaceEntries(entries);
        KeyboardFocusedEntryId = null;
        EntriesChanged?.Invoke();
        CountChanged?.Invoke();
    }

    public void NotifyCountChanged() => CountChanged?.Invoke();
}
