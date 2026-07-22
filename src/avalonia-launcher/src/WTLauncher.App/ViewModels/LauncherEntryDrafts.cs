using WTLauncher.Core;
using WTLauncher.Core.ValueObjects;
namespace WTLauncher.App.ViewModels;

public sealed record EntryDraft(
    string Id = "",
    string Name = "",
    string Directory = "",
    string Command = "",
    bool Selected = false);
public static class LauncherEntryDrafts
{

    // Normalizes each field at the ViewModel boundary rather than relying on callers —
    // dialog inputs may contain unicode variants and whitespace before the caller validates.
    public static EntryDraft NormalizeDraft(EntryDraft draft)
    {
        return new EntryDraft(
            EntryId.Normalize(draft.Id),
            EntryName.Normalize(draft.Name),
            EntryName.Normalize(draft.Directory),
            EntryName.Normalize(draft.Command),
            draft.Selected);
    }

    public static bool TryNormalizeRequiredDraft(
        EntryDraft draft,
        out EntryDraft normalized,
        out string? requiredFieldError)
    {
        normalized = NormalizeDraft(draft);
        requiredFieldError = GetRequiredFieldError(normalized);
        return requiredFieldError is null;
    }

    public static LaunchEntry CreateEntry(EntryDraft draft, LaunchEntry? previousEntry)
    {
        var normalized = NormalizeDraft(draft);
        return new LaunchEntry
        {
            Id = normalized.Id,
            Name = normalized.Name,
            Directory = (DirectoryPath)normalized.Directory,
            Command = normalized.Command,
            Selected = normalized.Selected,
            UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty,
            LaunchedAtUtc = previousEntry?.LaunchedAtUtc ?? string.Empty,
        };
    }

    public static string? ValidateEntry(
        LaunchEntry entry,
        string? originalId,
        IEnumerable<LaunchEntry> existingEntries)
    {
        var entryId = EntryId.Normalize(entry.Id);
        var requiredFieldError = GetRequiredFieldError(
            entryId,
            entry.Name,
            entry.Directory.Value,
            entry.Command);
        if (requiredFieldError is not null)
            return requiredFieldError;

        // Per-item call (dialog submit, not in a loop), so O(n) scan over
        // existingEntries is acceptable. Typical entry count < 100.
        if (existingEntries.Any(existing =>
                !EntryId.SameNormalized(existing.Id, originalId)
                && EntryId.SameNormalized(existing.Id, entryId)))
            return LauncherEntryDraftStatus.DuplicateId(entryId);

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
        if (!EntryId.IsUsable(id))
            return LauncherEntryDraftStatus.IdRequired;
        // Reject Windows reserved names and path separators / parent-directory tokens
        // at dialog-validation time rather than at save time. Without this guard, the
        // save-time RequireEntryId rejects the ID and silently rolls back the entry
        // via the snapshot mechanism, leaving no user-facing explanation.
        // Use OrdinalIgnoreCase to match EntryId.RequireEntryId behavior
        // (which uses WindowsReservedNames with StringComparer.OrdinalIgnoreCase).
        // ponytail: reuse EntryId.WindowsReservedNames — single source of truth.
        var normalizedId = EntryId.Normalize(id);
        if (EntryId.WindowsReservedNames.Contains(normalizedId))
            return LauncherEntryDraftStatus.IdInvalid;
        if (normalizedId.Contains('\\') || normalizedId.Contains('/') || normalizedId.Contains(".."))
            return LauncherEntryDraftStatus.IdInvalid;
        if (string.IsNullOrWhiteSpace(name))
            return LauncherEntryDraftStatus.NameRequired;
        if (string.IsNullOrWhiteSpace(directory))
            return LauncherEntryDraftStatus.DirectoryRequired;
        if (string.IsNullOrWhiteSpace(command))
            return LauncherEntryDraftStatus.CommandRequired;

        return null;
    }
}

public static class LauncherEntryDraftStatus
{
    public static string IdRequired => "按钮 ID 不能为空";

    public static string IdInvalid => "按钮 ID 不能使用 Windows 保留名、路径分隔符或 ..";

    public static string NameRequired => "按钮名称不能为空";

    public static string DirectoryRequired => "按钮目录不能为空";

    public static string CommandRequired => "按钮命令不能为空";

    public static string DuplicateId(string entryId) =>
        $"按钮 ID 已存在: {entryId}";
}
