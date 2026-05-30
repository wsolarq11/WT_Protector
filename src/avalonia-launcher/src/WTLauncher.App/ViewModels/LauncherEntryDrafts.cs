using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryDrafts
{
    public static EntryDraft NormalizeDraft(EntryDraft draft)
    {
        return new EntryDraft(
            (draft.Id ?? string.Empty).Trim(),
            (draft.Name ?? string.Empty).Trim(),
            (draft.Directory ?? string.Empty).Trim(),
            (draft.Command ?? string.Empty).Trim(),
            draft.Selected);
    }

    public static LaunchEntry CreateEntry(EntryDraft draft, LaunchEntry? previousEntry)
    {
        var normalized = NormalizeDraft(draft);
        return new LaunchEntry
        {
            Id = normalized.Id,
            Name = normalized.Name,
            Directory = normalized.Directory,
            Command = normalized.Command,
            Confirm = false,
            Selected = normalized.Selected,
            UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty,
        };
    }

    public static string? ValidateEntry(
        LaunchEntry entry,
        string? originalId,
        IEnumerable<LaunchEntry> existingEntries)
    {
        var requiredFieldError = GetRequiredFieldError(
            entry.Id,
            entry.Name,
            entry.Directory,
            entry.Command);
        if (requiredFieldError is not null)
            return requiredFieldError;

        if (existingEntries.Any(existing =>
                !LauncherEntryIds.Same(existing.Id, originalId)
                && LauncherEntryIds.Same(existing.Id, entry.Id)))
            return LauncherEntryDraftStatus.DuplicateId(entry.Id);

        return null;
    }

    public static string? GetRequiredFieldError(EntryDraft draft)
    {
        return GetRequiredFieldError(
            draft.Id,
            draft.Name,
            draft.Directory,
            draft.Command);
    }

    private static string? GetRequiredFieldError(
        string? id,
        string? name,
        string? directory,
        string? command)
    {
        if (!LauncherEntryIds.IsUsable(id))
            return LauncherEntryDraftStatus.IdRequired;
        if (string.IsNullOrWhiteSpace(name))
            return LauncherEntryDraftStatus.NameRequired;
        if (string.IsNullOrWhiteSpace(directory))
            return LauncherEntryDraftStatus.DirectoryRequired;
        if (string.IsNullOrWhiteSpace(command))
            return LauncherEntryDraftStatus.CommandRequired;

        return null;
    }
}
