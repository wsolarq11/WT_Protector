using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

/// <summary>Entry CRUD command logic. Each method is the full dialog→validate→mutate→persist flow.
/// Does NOT own state — the ViewModel passes collections and coordinates rollback.</summary>
public interface IEntryService
{
    /// <summary>Walk the user through adding a new entry.</summary>
    public Task<(LaunchEntry? Entry, string? Status)> AddEntryAsync(
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow);

    /// <summary>Clone an existing entry with user-modified fields.</summary>
    public Task<(LaunchEntry? Entry, string? Status)> CloneEntryAsync(
        LauncherEntryViewModel source,
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow);

    /// <summary>Edit an existing entry's fields.</summary>
    public Task<(LaunchEntry? Updated, string? Status)> EditEntryAsync(
        LauncherEntryViewModel item,
        ObservableCollection<LauncherEntryViewModel> entries,
        LauncherPresetFile presetFile,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow);

    /// <summary>Delete an entry after confirmation. Returns (status, clearKeyboardFocusId).</summary>
    public Task<(string? Status, string? ClearKeyboardFocusId)> DeleteEntryAsync(
        LauncherEntryViewModel item,
        string? keyboardFocusedEntryId);

    /// <summary>Validate a draft and create a LaunchEntry, or return a validation error message.</summary>
    public LaunchEntry? CreateEntryFromDraft(EntryDraft draft, string? originalId, LaunchEntry? previousEntry, IEnumerable<LaunchEntry> existingEntries, out string? validationMessage);

    /// <summary>Generate a unique clone ID avoiding collisions.</summary>
    public string GenerateUniqueCloneId(string sourceId, IEnumerable<string> existingIds);
}
