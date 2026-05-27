namespace WTLauncher.Core;

/// <summary>
/// Minimal interface for the owning context that controls layout and view-mode
/// flags.  Concrete implementations (e.g. the main window ViewModel) expose
/// these as read-only projections derived from a single writable source.
/// </summary>
public interface IEntryViewContext
{
    bool IsNamesViewMode { get; }
    bool IsDetailedListMode { get; }
}
