using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

/// <summary>Normalization/validation port used by the entry editor view.</summary>
public interface IEntryDraftValidator
{
    public bool TryNormalizeRequiredDraft(EntryDraft rawDraft, out EntryDraft draft, out string? requiredFieldError);
    public string NormalizeId(string? id);
    public bool IsUsableId(string id);
    public string NormalizeText(string? text);
    public string ResolveAutoId(string? name, DateTime now);
}

/// <summary>Entry CRUD command logic. Each method is the full dialog→validate→mutate→persist flow.
/// Does NOT own state — the ViewModel passes collections and coordinates rollback.</summary>
public interface IEntryService
{
    /// <summary>Walk the user through adding a new entry.</summary>
    public Task<ServiceResult<LaunchEntry>> AddEntryAsync(
        IEnumerable<LaunchEntry> existingEntries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow);

    /// <summary>Clone an existing entry with user-modified fields.</summary>
    public Task<ServiceResult<LaunchEntry>> CloneEntryAsync(
        LauncherEntryViewModel source,
        IEnumerable<string> existingEntryIds,
        IEnumerable<LaunchEntry> existingEntries,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow);

    /// <summary>Edit an existing entry's fields.</summary>
    public Task<ServiceResult<LaunchEntry>> EditEntryAsync(
        LauncherEntryViewModel item,
        IEnumerable<LaunchEntry> existingEntries,
        LauncherPresetFile presetFile,
        LauncherSettings settings,
        IReadOnlyDictionary<string, string>? commandTemplates,
        int presetReferenceCount,
        Func<DateTimeOffset> utcNow);

    /// <summary>Delete an entry after confirmation.</summary>
    public Task<ServiceResult<EntryDeleteApproval>> DeleteEntryAsync(
        LauncherEntryViewModel item,
        string? keyboardFocusedEntryId);

    /// <summary>Validate a draft and create a LaunchEntry.</summary>
    public ServiceResult<LaunchEntry> CreateEntryFromDraft(EntryDraft draft, string? originalId, LaunchEntry? previousEntry, IEnumerable<LaunchEntry> existingEntries);

    /// <summary>Generate a unique clone ID avoiding collisions.</summary>
    public string GenerateUniqueCloneId(string sourceId, IEnumerable<string> existingIds);
}
